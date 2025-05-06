namespace WaterProj.DTOs
{
    public class RouteOrderStatsDto
    {
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CanceledOrders { get; set; }
    }
}
