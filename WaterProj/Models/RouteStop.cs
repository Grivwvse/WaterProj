using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class RouteStop
    {
        [Key]
        public int RouteStopId { get; set; }

        // Внешний ключ для маршрута
        public int RouteId { get; set; }
        [ForeignKey("RouteId")]
        public Route Route { get; set; }

        // Внешний ключ для остановки
        public int StopId { get; set; }
        [ForeignKey("StopId")]
        public Stop Stop { get; set; }

        // Порядковый номер остановки в маршруте
        public int StopOrder { get; set; }
    }
}
