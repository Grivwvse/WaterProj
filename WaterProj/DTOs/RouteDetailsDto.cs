using WaterProj.Models;

namespace WaterProj.DTOs
{
    public class RouteDetailsDto
    {
        public required Models.Route Route { get; set; }
        public required Ship Ship { get; set; }
        public required List<Image> Image { get; set; }

        public required Transporter Transporter { get; set; }
    }
}
