using FromTheKitchen_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FromTheKitchen_API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MenuItem>().HasData(
            new MenuItem
            {
                Id = 1,
                Name = "Bruschetta",
                Description = "Grilled bread topped with diced tomatoes, garlic, basil, and olive oil.",
                Category = "Appetizer",
                SpecialTag = "",
                Price = 6.99,
                Image = "/images/bruschetta.jpg"
            },
            new MenuItem
            {
                Id = 2,
                Name = "Crispy Calamari",
                Description = "Lightly battered squid rings served with marinara and lemon aioli.",
                Category = "Appetizer",
                SpecialTag = "Best Seller",
                Price = 9.49,
                Image = "/images/calamari.jpg"
            },
            new MenuItem
            {
                Id = 3,
                Name = "Caesar Salad",
                Description = "Romaine lettuce, parmesan, croutons, and house Caesar dressing.",
                Category = "Salad",
                SpecialTag = "",
                Price = 8.25,
                Image = "/images/caesar-salad.jpg"
            },
            new MenuItem
            {
                Id = 4,
                Name = "Margherita Pizza",
                Description = "Wood-fired pizza with fresh mozzarella, tomato, and basil.",
                Category = "Entree",
                SpecialTag = "Chef's Special",
                Price = 13.50,
                Image = "/images/margherita.jpg"
            },
            new MenuItem
            {
                Id = 5,
                Name = "Grilled Salmon",
                Description = "Atlantic salmon with lemon butter sauce and seasonal vegetables.",
                Category = "Entree",
                SpecialTag = "",
                Price = 18.99,
                Image = "/images/salmon.jpg"
            },
            new MenuItem
            {
                Id = 6,
                Name = "Ribeye Steak",
                Description = "12oz ribeye cooked to order with garlic mashed potatoes.",
                Category = "Entree",
                SpecialTag = "Best Seller",
                Price = 26.00,
                Image = "/images/ribeye.jpg"
            },
            new MenuItem
            {
                Id = 7,
                Name = "Spaghetti Carbonara",
                Description = "Pasta with pancetta, egg, parmesan, and cracked black pepper.",
                Category = "Entree",
                SpecialTag = "",
                Price = 14.75,
                Image = "/images/carbonara.jpg"
            },
            new MenuItem
            {
                Id = 8,
                Name = "Tiramisu",
                Description = "Classic Italian dessert with espresso-soaked ladyfingers and mascarpone.",
                Category = "Dessert",
                SpecialTag = "New",
                Price = 7.50,
                Image = "/images/tiramisu.jpg"
            },
            new MenuItem
            {
                Id = 9,
                Name = "Chocolate Lava Cake",
                Description = "Warm chocolate cake with a molten center, served with vanilla ice cream.",
                Category = "Dessert",
                SpecialTag = "",
                Price = 7.95,
                Image = "/images/lava-cake.jpg"
            },
            new MenuItem
            {
                Id = 10,
                Name = "Iced Lemon Tea",
                Description = "Freshly brewed black tea over ice with a hint of lemon.",
                Category = "Beverage",
                SpecialTag = "",
                Price = 3.25,
                Image = "/images/lemon-tea.jpg"
            }
);

        }
    }
}
