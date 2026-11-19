using System.ComponentModel.DataAnnotations;

namespace FromTheKitchen_API.Models.Dto
{
    public class OrderDetailsUpdateDTO
    {
        [Required]
        public int OrderDetailId { get; set; }
        [Required]
        public int Rating { get; set; }
    }
}
