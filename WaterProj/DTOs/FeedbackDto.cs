using WaterProj.Models;

namespace WaterProj.DTOs
{
    public class FeedbackDto
    {
        public int OrderId { get; set; }
        public int RouteId { get; set; }

        public int TransporterId { get; set; }
        public string TransporterName { get; set; }
        public string RouteName { get; set; }
        public int Stars { get; set; } // Рейтинг от 0 до 5
        public List<int> SelectedAdvantages { get; set; } // ID выбранных преимуществ
        public List<Advantage> AvailableAdvantages { get; set; } // ID выбранных преимуществ

        public string Comment { get; set; } // Основной комментарий
        public string PositiveComments { get; set; } // Положительные моменты
        public string NegativeComments { get; set; } // Отрицательные моменты

    }
}
