using WaterProj.Models;

namespace WaterProj.DTOs
{
    public class EditRouteDto
    {
        public int RouteId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Schedule { get; set; }
        public int Price { get; set; }
        public int ShipId { get; set; }
        public List<DayOfWeek> RouteDays { get; set; } = new List<DayOfWeek>();

        public List<Ship> AvailableShips { get; set; } = new List<Ship>();
    }
}
