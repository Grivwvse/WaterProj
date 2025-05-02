using WaterProj.Models;

namespace WaterProj.DTOs
{
    public class CreateRouteDto
    {
        public required List<Ship> Ships { get; set; }
    }
}
