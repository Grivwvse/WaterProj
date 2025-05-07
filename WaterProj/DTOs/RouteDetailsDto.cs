using WaterProj.Models;

namespace WaterProj.DTOs
{
    public class RouteDetailsDto
    {
        public required Models.Route Route { get; set; }
        public required Ship Ship { get; set; }
        public required List<Image> Image { get; set; }

        public required Transporter Transporter { get; set; }

        public required List<Сonvenience> ShipConveniences { get; set; }
        public required int RouteOrderStats { get; set; }

        // Для комментариев к маршруту
        public required List<Advantage> RouteAdvantages { get; set; }
        public required List<RouteRating> RouteRatings { get; set; }
        public required List<DayOfWeek> RouteDays { get; set; }

    }
}
