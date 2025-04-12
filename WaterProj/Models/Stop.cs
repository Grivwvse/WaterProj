using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WaterProj.Models;

namespace Dprog.Models
{
    public class Stop
    {
        [Key]
        public int StopId { get; set; }
        public string Name { get; set; }

        // Дополнительные параметры, например, координаты
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        // Навигационное свойство для связи многие-к-многим
        public List<RouteStop> RouteStops { get; set; }
    }
}
