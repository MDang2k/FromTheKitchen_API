using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FromTheKitchen_API.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Categeory",
                table: "MenuItems",
                newName: "Category");

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Category", "Description", "Image", "Name", "Price", "SpecialTag" },
                values: new object[,]
                {
                    { "1", "Appetizer", "Grilled bread topped with diced tomatoes, garlic, basil, and olive oil.", "/images/bruschetta.jpg", "Bruschetta", 6.9900000000000002, "" },
                    { "10", "Beverage", "Freshly brewed black tea over ice with a hint of lemon.", "/images/lemon-tea.jpg", "Iced Lemon Tea", 3.25, "" },
                    { "2", "Appetizer", "Lightly battered squid rings served with marinara and lemon aioli.", "/images/calamari.jpg", "Crispy Calamari", 9.4900000000000002, "Best Seller" },
                    { "3", "Salad", "Romaine lettuce, parmesan, croutons, and house Caesar dressing.", "/images/caesar-salad.jpg", "Caesar Salad", 8.25, "" },
                    { "4", "Entree", "Wood-fired pizza with fresh mozzarella, tomato, and basil.", "/images/margherita.jpg", "Margherita Pizza", 13.5, "Chef's Special" },
                    { "5", "Entree", "Atlantic salmon with lemon butter sauce and seasonal vegetables.", "/images/salmon.jpg", "Grilled Salmon", 18.989999999999998, "" },
                    { "6", "Entree", "12oz ribeye cooked to order with garlic mashed potatoes.", "/images/ribeye.jpg", "Ribeye Steak", 26.0, "Best Seller" },
                    { "7", "Entree", "Pasta with pancetta, egg, parmesan, and cracked black pepper.", "/images/carbonara.jpg", "Spaghetti Carbonara", 14.75, "" },
                    { "8", "Dessert", "Classic Italian dessert with espresso-soaked ladyfingers and mascarpone.", "/images/tiramisu.jpg", "Tiramisu", 7.5, "New" },
                    { "9", "Dessert", "Warm chocolate cake with a molten center, served with vanilla ice cream.", "/images/lava-cake.jpg", "Chocolate Lava Cake", 7.9500000000000002, "" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "1");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "10");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "2");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "3");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "4");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "5");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "6");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "7");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "8");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: "9");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "MenuItems",
                newName: "Categeory");
        }
    }
}
