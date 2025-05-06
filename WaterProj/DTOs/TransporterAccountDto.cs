using WaterProj.Models;
using Route = WaterProj.Models.Route;

namespace WaterProj.DTOs
{
    public class TransporterAccountDto
    {
        public required Transporter Transporter { get; set; }
        public required List<Route> Routes { get; set; }
        public required List<Ship> Ships { get; set; }
        public Dictionary<int, RouteOrderStatsDto> RouteStats { get; set; } = new Dictionary<int, RouteOrderStatsDto>();

    }
}
