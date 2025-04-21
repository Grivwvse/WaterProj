using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class RouteRating
    {
        [Key]
        public int RouteRatingId { get; set; } // Первичный ключ

        [Required]
        public int RouteId { get; set; } // Внешний ключ для Route (исправлено название поля)
        [ForeignKey("RouteId")] // Исправлена привязка внешнего ключа
        public Route Route { get; set; }

        [Required]
        public int ConsumerId { get; set; } // Внешний ключ для Consumer
        [ForeignKey("ConsumerId")]
        public Consumer Consumer { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Рейтинг должен быть от 1 до 5.")]
        public int Stars { get; set; } // Общая оценка маршрута от 1 до 5 звезд

        // Комментарии, разделенные на категории
        [StringLength(500)]
        public string Comment { get; set; } // Основной комментарий

        [StringLength(500)]
        public string PositiveComments { get; set; } // Положительные моменты

        [StringLength(500)]
        public string NegativeComments { get; set; } // Отрицательные моменты

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Связь для преимуществ маршрута
        public ICollection<RouteRatingAdvantage> RouteRatingAdvantages { get; set; }

        // Связь для изображений
        public ICollection<Image> Images { get; set; }
    }
}
