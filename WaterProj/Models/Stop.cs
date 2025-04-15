using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WaterProj.Models;

namespace WaterProj.Models
{
    public class Stop
    {
        [Key]
        public int StopId { get; set; }
        public string Name { get; set; }

        // Дополнительные параметры, например, координаты
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Навигационное свойство для связи многие-к-многим
        public List<RouteStop> RouteStops { get; set; }
    }
}
