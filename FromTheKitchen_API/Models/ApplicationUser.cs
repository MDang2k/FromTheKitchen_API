using Microsoft.AspNetCore.Identity;

namespace FromTheKitchen_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;


    }
}
