using WaterProj.Models;

namespace WaterProj.DTOs
{
    /// <summary>
    /// DTO для отображения всей необходимой информации в личной кабинете 
    /// </summary>
    public class ConsumerAccontDto
    {
        public required Consumer Consumer { get; set; }
        public required List<OrdersRoutesDto> OrdersRoutesDto { get; set; }
        public string ProfileImagePath { get; set; } = string.Empty; // Путь к изображению профиля

    }

    /// <summary>
    /// Для соответствия заказа и маршрута и вывода информации как о заказе, так и о маршруте
    /// </summary>
    public class OrdersRoutesDto
    {
        public required Order Order { get; set; }
        public required Models.Route Route { get; set; }
    }

}