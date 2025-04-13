using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterProj.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }
        public int ConsumerId { get; set; }
        [ForeignKey("ConsumerId")]
        public Consumer Consumer { get; set; }
        
        public string Comment { get; set; }
        public float Rating { get; set; }

        // Внешний ключ для Route
        public int RouteId { get; set; }
        [ForeignKey("RouteId")]
        public Route Route { get; set; }

    }
}
