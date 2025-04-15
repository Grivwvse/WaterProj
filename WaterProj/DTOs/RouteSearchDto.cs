namespace WaterProj.DTOs
{
    public class RouteSearchDto
    {
        public string? RouteName { get; set; }
        public int? StartStopId { get; set; }
        public int? EndStopId { get; set; }
        public string? TransporterName { get; set; }
        public DateTime? DepartureDate { get; set; }
        // Можно добавить дополнительные критерии поиска
    }

    public class RouteSearchResultDto
    {
        public int RouteId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Schedule { get; set; }
        public int TransporterId { get; set; }
        public string TransporterName { get; set; }
        public double Rating { get; set; }
        public string? ImageUrl { get; set; }

    }
}
