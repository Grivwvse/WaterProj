namespace WaterProj.DTOs
{
    public class RouteDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Schedule { get; set; }
        public int ShipId { get; set; }
        public string Map { get; set; }
        public int Price { get; set; }
        public List<StopDto> Stops { get; set; }
        public List<RouteLineDto> RouteLine { get; set; } // Добавляем маршрут (линии)
    }

    public class StopDto
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Hint { get; set; }
        public string Balloon { get; set; }
        public string ArrivalTime { get; set; }
        public string DepartureTime { get; set; }
        public int DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, ...
        public string TimeOfDay { get; set; } // "Morning", etc.
        public int? ExistingStopId { get; set; } // Новое поле для ID существующей остановки
    }

    public class RouteLineDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}
