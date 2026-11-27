using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FromTheKitchen_API.Models.Dto
{
    public class OrderDetailsCreateDTO
    {
        [Required]
        public int MenuItemId { get; set; }
        [Required]
        public int Quantity { get; set; }
        // Name and price are looked up on the server, so the client can't set them.
    }
}
