using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FromTheKitchen_API.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingToOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MyProperty",
                table: "OrderHeaders",
                newName: "OrderTotal");

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "OrderDetails",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "OrderTotal",
                table: "OrderHeaders",
                newName: "MyProperty");
        }
    }
}
