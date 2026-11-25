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
        string imageName = $"soup-{Guid.NewGuid():N}.jpg";

        // Create: the image is uploaded with the form, saved to the uploads folder and served under /images
        var created = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Tomato Soup", imageName: imageName));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        int id = int.Parse(created.Headers.Location!.Segments[^1]);
        Assert.True(File.Exists(Path.Combine(factory.ImagesPath, imageName)));
        var served = await factory.CreateClient().GetAsync($"/images/{imageName}");
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(ImageBytes, await served.Content.ReadAsByteArrayAsync());

        var item = (await (await admin.GetAsync($"/api/MenuItem/{id}")).ReadEnvelopeAsync<MenuItem>()).Result!;
        Assert.Equal("Tomato Soup", item.Name);
        Assert.Equal($"images/{imageName}", item.Image);

        // Update
        var updated = await admin.PutAsync($"/api/MenuItem?id={id}", NewMenuItemForm("Pumpkin Soup", id: id, imageName: imageName));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        item = (await (await admin.GetAsync($"/api/MenuItem/{id}")).ReadEnvelopeAsync<MenuItem>()).Result!;
        Assert.Equal("Pumpkin Soup", item.Name);

        // Delete removes both the row and the image file
        var deleted = await admin.DeleteAsync($"/api/MenuItem?id={id}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/MenuItem/{id}")).StatusCode);
        Assert.False(File.Exists(Path.Combine(factory.ImagesPath, imageName)));
    }

    [Fact]
    public async Task An_uploaded_file_name_cannot_escape_the_images_folder()
    {
        var admin = factory.ClientFor(await factory.GetAdminAsync());
        string fileName = $"escape-{Guid.NewGuid():N}.jpg";

        var response = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Sneaky Soup", imageName: $"../../{fileName}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(File.Exists(Path.Combine(factory.ImagesPath, fileName)));
        Assert.False(File.Exists(Path.GetFullPath(Path.Combine(factory.ImagesPath, "..", "..", fileName))));
    }

    [Fact]
    public async Task Creating_a_menu_item_without_an_image_is_rejected()
    {
        var admin = factory.ClientFor(await factory.GetAdminAsync());
        var response = await admin.PostAsync("/api/MenuItem", NewMenuItemForm("Soup", withImage: false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

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
