using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterProj.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int RouteId { get; set; }
        [ForeignKey("RouteId")]
        public Route Route { get; set; }
        public int ConsumerId { get; set; }

        [ForeignKey("ConsumerId")]
        public Consumer Consumer { get; set; }
        public OrderStatus Status { get; set; }
        public bool IsFeedback { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
