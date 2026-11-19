using System.Net;
using System.Net.Http.Json;
using FromTheKitchen_API.Models;
using FromTheKitchen_API.Tests.Infrastructure;
using FromTheKitchen_API.Utility;

namespace FromTheKitchen_API.Tests;

public class OrderTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ---------- Placing and reading orders ----------

    [Fact]
    public async Task Order_endpoints_require_a_login()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/OrderHeader")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/OrderHeader", NewOrder())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/OrderDetails/1", new { orderDetailId = 1, rating = 5 })).StatusCode);
    }

    [Fact]
    public async Task A_new_order_is_confirmed_and_belongs_to_the_caller_not_the_user_named_in_the_body()
    {
        var alice = await factory.CreateCustomerAsync();
        var bob = await factory.CreateCustomerAsync();

        var response = await factory.ClientFor(alice).PostAsJsonAsync("/api/OrderHeader", NewOrder(applicationUserId: bob.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.ReadEnvelopeAsync<OrderHeader>()).Result!;
        Assert.Equal(alice.Id, order.ApplicationUserId);
        Assert.Equal(SD.status_confirmed, order.Status);
        Assert.Equal($"/api/OrderHeader/{order.OrderHeaderId}", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task An_order_is_returned_with_its_lines_and_menu_items()
    {
        var alice = await factory.CreateCustomerAsync();
        var placed = await PlaceOrderAsync(alice);

        var order = await GetOrderAsync(alice, placed.OrderHeaderId);

        Assert.Equal(2, order.OrderDetails.Count);
        Assert.All(order.OrderDetails, line => Assert.NotNull(line.MenuItem));
        Assert.Contains(order.OrderDetails, line => line.MenuItem!.Name == "Bruschetta");
    }

    [Fact]
    public async Task Customers_can_only_read_their_own_orders_and_admins_can_read_any()
    {
        var alice = await factory.CreateCustomerAsync();
        var bob = await factory.CreateCustomerAsync();
        var admin = await factory.GetAdminAsync();
        var order = await PlaceOrderAsync(alice);
        string url = $"/api/OrderHeader/{order.OrderHeaderId}";

        Assert.Equal(HttpStatusCode.OK, (await factory.ClientFor(alice).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.ClientFor(bob).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.ClientFor(admin).GetAsync(url)).StatusCode);
    }

    [Theory]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(999999, HttpStatusCode.NotFound)]
    public async Task Invalid_or_unknown_order_ids_are_rejected(int id, HttpStatusCode expected)
    {
        var alice = await factory.CreateCustomerAsync();

        var response = await factory.ClientFor(alice).GetAsync($"/api/OrderHeader/{id}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_only_lists_their_own_orders_even_when_asking_for_someone_else()
    {
        var alice = await factory.CreateCustomerAsync();
        var bob = await factory.CreateCustomerAsync();
        await PlaceOrderAsync(alice);
        await PlaceOrderAsync(bob);

        var orders = await ListOrdersAsync(alice, $"?userId={bob.Id}");

        Assert.NotEmpty(orders);
        Assert.All(orders, o => Assert.Equal(alice.Id, o.ApplicationUserId));
    }

    [Fact]
    public async Task An_admin_lists_every_order_or_filters_by_user()
    {
        var alice = await factory.CreateCustomerAsync();
        var bob = await factory.CreateCustomerAsync();
        var admin = await factory.GetAdminAsync();
        var aliceOrder = await PlaceOrderAsync(alice);
        var bobOrder = await PlaceOrderAsync(bob);

        var all = await ListOrdersAsync(admin);
        var onlyBob = await ListOrdersAsync(admin, $"?userId={bob.Id}");

        Assert.Contains(all, o => o.OrderHeaderId == aliceOrder.OrderHeaderId);
        Assert.Contains(all, o => o.OrderHeaderId == bobOrder.OrderHeaderId);
        Assert.Equal(all.Select(o => o.OrderHeaderId).OrderDescending(), all.Select(o => o.OrderHeaderId)); // newest first
        Assert.All(onlyBob, o => Assert.Equal(bob.Id, o.ApplicationUserId));
        Assert.Contains(onlyBob, o => o.OrderHeaderId == bobOrder.OrderHeaderId);
    }

    // ---------- Updating orders ----------

    [Fact]
    public async Task A_customer_can_edit_pickup_details_of_their_own_order()
    {
        var alice = await factory.CreateCustomerAsync();
        var order = await PlaceOrderAsync(alice);

        var response = await UpdateOrderAsync(alice, order.OrderHeaderId, new { phoneNumber = "555-0199" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await GetOrderAsync(alice, order.OrderHeaderId);
        Assert.Equal("555-0199", saved.PhoneNumber);
        Assert.Equal(order.PickUpName, saved.PickUpName); // fields that weren't sent are left alone
    }

    [Fact]
    public async Task A_customer_cannot_update_someone_elses_order()
    {
        var alice = await factory.CreateCustomerAsync();
        var bob = await factory.CreateCustomerAsync();
        var order = await PlaceOrderAsync(alice);

        var response = await UpdateOrderAsync(bob, order.OrderHeaderId, new { phoneNumber = "555-6666" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(order.PhoneNumber, (await GetOrderAsync(alice, order.OrderHeaderId)).PhoneNumber);
    }

    [Fact]
    public async Task The_id_in_the_url_must_match_the_body()
    {
        var alice = await factory.CreateCustomerAsync();
        var order = await PlaceOrderAsync(alice);

        var response = await factory.ClientFor(alice).PutAsJsonAsync($"/api/OrderHeader/{order.OrderHeaderId}",
            new { orderHeaderId = order.OrderHeaderId + 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_can_cancel_their_own_order_but_not_move_it_forward()
    {
        var alice = await factory.CreateCustomerAsync();
        var order = await PlaceOrderAsync(alice);

        var forward = await UpdateOrderAsync(alice, order.OrderHeaderId, new { status = SD.status_readyForPickUp });
        var cancel = await UpdateOrderAsync(alice, order.OrderHeaderId, new { status = SD.status_cancelled });

        Assert.Equal(HttpStatusCode.Forbidden, forward.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(SD.status_cancelled, (await GetOrderAsync(alice, order.OrderHeaderId)).Status);
    }

    // Every status change an admin can ask for, and whether the workflow allows it
    [Theory]
    [InlineData("Confirmed", "Ready For Pick Up", true)]
    [InlineData("Ready For Pick Up", "Completed", true)]
    [InlineData("Confirmed", "Cancelled", true)]
    [InlineData("Ready For Pick Up", "Cancelled", true)]
    [InlineData("Confirmed", "confirmed", true)] // resending the current status is a no-op
    [InlineData("Confirmed", "Completed", false)] // skips a step
    [InlineData("Completed", "Cancelled", false)]
    [InlineData("Completed", "Ready For Pick Up", false)]
    [InlineData("Cancelled", "Ready For Pick Up", false)]
    [InlineData("Confirmed", "Eaten", false)] // not a status at all
    public async Task Status_changes_follow_the_order_workflow(string from, string to, bool allowed)
    {
        var alice = await factory.CreateCustomerAsync();
        var admin = await factory.GetAdminAsync();
        var order = await PlaceOrderAsync(alice);
        await MoveToStatusAsync(admin, order.OrderHeaderId, from);

        // The phone number rides along to prove a rejected request saves nothing at all
        var response = await UpdateOrderAsync(admin, order.OrderHeaderId, new { status = to, phoneNumber = "555-0123" });

        var saved = await GetOrderAsync(admin, order.OrderHeaderId);
        if (allowed)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(to, saved.Status, ignoreCase: true);
            Assert.Equal("555-0123", saved.PhoneNumber);
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Single((await response.ReadEnvelopeAsync<object>()).ErrorMessages);
            Assert.Equal(from, saved.Status);
            Assert.Equal(order.PhoneNumber, saved.PhoneNumber);
        }
    }

    // ---------- Rating items ----------

    [Fact]
    public async Task The_customer_who_placed_the_order_can_rate_its_items()
    {
        var alice = await factory.CreateCustomerAsync();
        var line = (await GetOrderAsync(alice, (await PlaceOrderAsync(alice)).OrderHeaderId)).OrderDetails[0];

        var response = await RateAsync(alice, line.OrderDetailId, 4);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await GetOrderAsync(alice, line.OrderHeaderId);
        Assert.Equal(4, saved.OrderDetails.Single(l => l.OrderDetailId == line.OrderDetailId).Rating);
    }

    [Fact]
    public async Task Nobody_else_can_rate_an_order_not_even_an_admin()
    {
        var alice = await factory.CreateCustomerAsync();
        var bob = await factory.CreateCustomerAsync();
        var admin = await factory.GetAdminAsync();
        var line = (await GetOrderAsync(alice, (await PlaceOrderAsync(alice)).OrderHeaderId)).OrderDetails[0];

        Assert.Equal(HttpStatusCode.Forbidden, (await RateAsync(bob, line.OrderDetailId, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RateAsync(admin, line.OrderDetailId, 1)).StatusCode);
        Assert.Null((await GetOrderAsync(alice, line.OrderHeaderId)).OrderDetails.Single(l => l.OrderDetailId == line.OrderDetailId).Rating);
    }

    [Fact]
    public async Task Rating_an_unknown_item_or_with_mismatched_ids_is_rejected()
    {
        var alice = await factory.CreateCustomerAsync();
        var line = (await GetOrderAsync(alice, (await PlaceOrderAsync(alice)).OrderHeaderId)).OrderDetails[0];
        var client = factory.ClientFor(alice);

        var mismatch = await client.PutAsJsonAsync($"/api/OrderDetails/{line.OrderDetailId}", new { orderDetailId = line.OrderDetailId + 1, rating = 5 });
        var unknown = await RateAsync(alice, 999999, 5);

        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    // ---------- Helpers ----------

    private static object NewOrder(string applicationUserId = "") => new
    {
        pickUpName = "Test Customer",
        phoneNumber = "555-0100",
        pickUpEmail = "customer@example.test",
        applicationUserId,
        orderTotal = 16.48,
        totalItem = 2,
        orderDetailsDTO = new[]
        {
            new { menuItemId = 1, quantity = 1, itemName = "Bruschetta", price = 6.99 },
            new { menuItemId = 2, quantity = 1, itemName = "Crispy Calamari", price = 9.49 },
        },
    };

    private async Task<OrderHeader> PlaceOrderAsync(TestUser user)
    {
        var response = await factory.ClientFor(user).PostAsJsonAsync("/api/OrderHeader", NewOrder());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadEnvelopeAsync<OrderHeader>()).Result!;
    }

    private async Task<OrderHeader> GetOrderAsync(TestUser user, int orderId)
    {
        var response = await factory.ClientFor(user).GetAsync($"/api/OrderHeader/{orderId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadEnvelopeAsync<OrderHeader>()).Result!;
    }

    private async Task<List<OrderHeader>> ListOrdersAsync(TestUser user, string query = "")
    {
        var response = await factory.ClientFor(user).GetAsync($"/api/OrderHeader{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadEnvelopeAsync<List<OrderHeader>>()).Result!;
    }

    private Task<HttpResponseMessage> UpdateOrderAsync(TestUser user, int orderId, object changes)
    {
        // Merge the changes with the required id so each test only spells out what it changes
        var body = System.Text.Json.JsonSerializer.SerializeToNode(changes)!.AsObject();
        body["orderHeaderId"] = orderId;
        return factory.ClientFor(user).PutAsJsonAsync($"/api/OrderHeader/{orderId}", body);
    }

    private Task<HttpResponseMessage> RateAsync(TestUser user, int orderDetailId, int rating) =>
        factory.ClientFor(user).PutAsJsonAsync($"/api/OrderDetails/{orderDetailId}", new { orderDetailId, rating });

    private async Task MoveToStatusAsync(TestUser admin, int orderId, string status)
    {
        string[] path = status switch
        {
            SD.status_confirmed => [],
            SD.status_readyForPickUp => [SD.status_readyForPickUp],
            SD.status_completed => [SD.status_readyForPickUp, SD.status_completed],
            SD.status_cancelled => [SD.status_cancelled],
            _ => throw new ArgumentException($"Unknown status {status}"),
        };
        foreach (string step in path)
        {
            Assert.Equal(HttpStatusCode.OK, (await UpdateOrderAsync(admin, orderId, new { status = step })).StatusCode);
        }
    }
}
