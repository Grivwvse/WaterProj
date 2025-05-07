using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterProj.Models
{
    public class Route
    {
        [Key]
        public int RouteId { get; set; }
        public int ShipId { get; set; }

        [ForeignKey("ShipId")]
        public Ship Ship { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Map { get; set; }
        public float Rating { get; set; }
        public string Schedule { get; set; }
         public int Price { get; set; }  // Цена от 
        public bool IsActive { get; set; } = true; // Статус маршрута
        public bool IsBlocked { get; set; } = false; // Блокировка администратором 
        public string? BlockReason { get; set; }

        public List<Order> Orders { get; set; }

        public int TransporterId { get; set; }
        [ForeignKey("TransporterId")]
        public Transporter Transporter { get; set; }
        // Связь с днями недели
        public ICollection<RouteDay> RouteDays { get; set; }

        // Навигационное свойство для связи с остановками
        public List<RouteStop> RouteStops { get; set; }

        [NotMapped]
        public List<Image> Images { get; set; }

    }
}
