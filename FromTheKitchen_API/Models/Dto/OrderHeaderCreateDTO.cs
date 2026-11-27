using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FromTheKitchen_API.Models.Dto
{
    public class OrderHeaderCreateDTO
    {
        [Required]
        public string PickUpName { get; set; } = string.Empty;
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string PickUpEmail { get; set; } = string.Empty;
        // User, totals and prices are set on the server from the token and the menu.
        public List<OrderDetailsCreateDTO> OrderDetailsDTO { get; set; } = new List<OrderDetailsCreateDTO>();
    }
}
