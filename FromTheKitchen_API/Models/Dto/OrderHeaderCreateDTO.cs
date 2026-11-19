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
        public string ApplicationUserId { get; set; } = string.Empty;
        public double OrderTotal { get; set; }
        public int TotalItem { get; set; }

        public List<OrderDetail> OrderDetailsDTO { get; set; } = new List<OrderDetail>();
    }
}
