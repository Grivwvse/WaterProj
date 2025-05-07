namespace WaterProj.Models
{
    public class RouteDay
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public Route Route { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
    }
}
