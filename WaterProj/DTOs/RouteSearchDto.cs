namespace WaterProj.DTOs
{
    public class RouteSearchDto
    {
        public string? RouteName { get; set; }
        public int? StartStopId { get; set; }
        public int? EndStopId { get; set; }
        public string? TransporterName { get; set; }
        public DateTime? DepartureDate { get; set; }
        
    }

    public class RouteSearchResultDto
    {
        public string? StartStopName { get; set; }
        public string? EndStopName { get; set; }
        public int RouteId { get; set; }
        public int Price { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Schedule { get; set; }
        public int TransporterId { get; set; }
        public double TransporterRating { get; set; }
        public string TransporterName { get; set; }
        public double Rating { get; set; }
        public string? Image { get; set; }
        public int RouteOrderStats { get; set; }
        public List<DayOfWeek> RouteDays { get; set; } = new List<DayOfWeek>();
    }
}
