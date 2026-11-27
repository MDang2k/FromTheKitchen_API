using System.Net.Http.Json;
using System.Net;
using FromTheKitchen_API.Models;
using FromTheKitchen_API.Tests.Infrastructure;

namespace FromTheKitchen_API.Tests;

public class MenuItemTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task The_menu_is_public_and_includes_the_seeded_dishes()
    {
        var response = await factory.CreateClient().GetAsync("/api/MenuItem");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var menu = (await response.ReadEnvelopeAsync<List<MenuItem>>()).Result!;
        Assert.Contains(menu, m => m.Name == "Margherita Pizza" && m.Price == 13.50);
        Assert.True(menu.Count >= 10);
    }

    [Fact]
    public async Task A_single_menu_item_can_be_fetched_by_id()
    {
        var response = await factory.CreateClient().GetAsync("/api/MenuItem/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bruschetta", (await response.ReadEnvelopeAsync<MenuItem>()).Result!.Name);
    }

    [Theory]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(999999, HttpStatusCode.NotFound)]
    public async Task Invalid_or_unknown_menu_item_ids_are_rejected(int id, HttpStatusCode expected)
    {
        var response = await factory.CreateClient().GetAsync($"/api/MenuItem/{id}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_menu_item_requires_a_login()
    {
        var response = await factory.CreateClient().PostAsync("/api/MenuItem", NewMenuItemForm("Soup"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Customers_cannot_change_the_menu()
    {
        var customer = await factory.CreateCustomerAsync();
        var client = factory.ClientFor(customer);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/MenuItem", NewMenuItemForm("Soup"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsync("/api/MenuItem?id=1", NewMenuItemForm("Soup", id: 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/MenuItem?id=1")).StatusCode);
    }

    [Fact]
    public async Task An_admin_can_create_update_and_delete_a_menu_item_with_its_image()
    {
        var admin = factory.ClientFor(await factory.GetAdminAsync());

        // Create: the image is uploaded with the form, saved under a fresh name and served under /images
        var created = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Tomato Soup", imageName: "soup.jpg"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        int id = int.Parse(created.Headers.Location!.Segments[^1]);
        var item = await GetItemAsync(admin, id);
        Assert.Equal("Tomato Soup", item.Name);
        Assert.Matches(@"^images/[0-9a-f]{32}\.jpg$", item.Image);
        string firstImage = ImagePath(item);
        Assert.True(File.Exists(firstImage));
        var served = await factory.CreateClient().GetAsync($"/{item.Image}");
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(ImageBytes, await served.Content.ReadAsByteArrayAsync());

        // Update without a file keeps the current image
        var renamed = await admin.PutAsync($"/api/MenuItem?id={id}", NewMenuItemForm("Pumpkin Soup", id: id, withImage: false));
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        item = await GetItemAsync(admin, id);
        Assert.Equal("Pumpkin Soup", item.Name);
        Assert.Equal(firstImage, ImagePath(item));

        // Update with a file swaps the image and removes the old file
        var reimaged = await admin.PutAsync($"/api/MenuItem?id={id}", NewMenuItemForm("Pumpkin Soup", id: id, imageName: "pumpkin.png"));
        Assert.Equal(HttpStatusCode.OK, reimaged.StatusCode);
        item = await GetItemAsync(admin, id);
        Assert.EndsWith(".png", item.Image);
        Assert.True(File.Exists(ImagePath(item)));
        Assert.False(File.Exists(firstImage));

        // Delete removes both the row and the image file
        var deleted = await admin.DeleteAsync($"/api/MenuItem?id={id}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/MenuItem/{id}")).StatusCode);
        Assert.False(File.Exists(ImagePath(item)));
    }

    [Fact]
    public async Task Two_uploads_with_the_same_file_name_do_not_overwrite_each_other()
    {
        var admin = factory.ClientFor(await factory.GetAdminAsync());

        var first = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Soup A", imageName: "same.jpg"));
        var second = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Soup B", imageName: "same.jpg"));

        var a = await GetItemAsync(admin, int.Parse(first.Headers.Location!.Segments[^1]));
        var b = await GetItemAsync(admin, int.Parse(second.Headers.Location!.Segments[^1]));
        Assert.NotEqual(a.Image, b.Image);
        Assert.True(File.Exists(ImagePath(a)));
        Assert.True(File.Exists(ImagePath(b)));
    }

    [Fact]
    public async Task An_uploaded_file_name_cannot_escape_the_images_folder()
    {
        var admin = factory.ClientFor(await factory.GetAdminAsync());
        string fileName = $"escape-{Guid.NewGuid():N}.jpg";

        var response = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Sneaky Soup", imageName: $"../../{fileName}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await GetItemAsync(admin, int.Parse(response.Headers.Location!.Segments[^1]));
        Assert.True(File.Exists(ImagePath(item)));
        Assert.False(File.Exists(Path.GetFullPath(Path.Combine(factory.ImagesPath, "..", "..", fileName))));
    }

    [Theory]
    [InlineData("page.html")]
    [InlineData("logo.svg")]
    [InlineData("no-extension")]
    public async Task Only_image_files_can_be_uploaded(string fileName)
    {
        var admin = factory.ClientFor(await factory.GetAdminAsync());

        var response = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Soup", imageName: fileName));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_item_that_was_ordered_cannot_be_deleted()
    {
        var admin = factory.ClientFor(await factory.GetAdminAsync());
        var created = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Ordered Soup"));
        int id = int.Parse(created.Headers.Location!.Segments[^1]);
        var customer = factory.ClientFor(await factory.CreateCustomerAsync());
        var order = await customer.PostAsJsonAsync("/api/OrderHeader", new
        {
            pickUpName = "Test Customer",
            phoneNumber = "555-0100",
            pickUpEmail = "customer@example.test",
            orderDetailsDTO = new[] { new { menuItemId = id, quantity = 1 } },
        });
        Assert.Equal(HttpStatusCode.Created, order.StatusCode);

        var deleted = await admin.DeleteAsync($"/api/MenuItem?id={id}");

        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/MenuItem/{id}")).StatusCode);
    }

    [Fact]
    public async Task Creating_a_menu_item_without_an_image_is_rejected()
    {
        var admin = factory.ClientFor(await factory.GetAdminAsync());
        var response = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Soup", withImage: false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<MenuItem> GetItemAsync(HttpClient client, int id) =>
        (await (await client.GetAsync($"/api/MenuItem/{id}")).ReadEnvelopeAsync<MenuItem>()).Result!;

    private string ImagePath(MenuItem item) => Path.Combine(factory.ImagesPath, Path.GetFileName(item.Image));

    private static readonly byte[] ImageBytes = [0xFF, 0xD8, 0xFF, 0xD9];

    private static MultipartFormDataContent NewMenuItemForm(string name, int? id = null, string? imageName = null, bool withImage = true)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(name), "Name" },
            { new StringContent("Made in a test"), "Description" },
            { new StringContent("Soup"), "Category" },
            { new StringContent(""), "SpecialTag" },
            // Whole number on purpose: form values are parsed with the server's culture
            { new StringContent("9"), "Price" },
        };
        if (withImage)
        {
            form.Add(new ByteArrayContent(ImageBytes), "File", imageName ?? $"test-{Guid.NewGuid():N}.jpg");
        }
        if (id != null)
        {
            form.Add(new StringContent(id.Value.ToString()), "Id");
        }
        return form;
    }
}
