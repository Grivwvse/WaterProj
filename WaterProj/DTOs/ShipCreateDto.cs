using System.ComponentModel.DataAnnotations;
using WaterProj.Models;

namespace WaterProj.DTOs
{
    public class ShipCreateDto
    {

        [Required(ErrorMessage = "Введите название судна")]
        [Display(Name = "Название судна")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "IMO номер")]
        public string IMO { get; set; }

        [Required(ErrorMessage = "Выберите статус судна")]
        [Display(Name = "Статус судна")]
        public ShipStatus Status { get; set; }

        [Required(ErrorMessage = "Выберите тип судна")]
        [Display(Name = "Тип судна")]
        public int SelectedShipTypeId { get; set; }

        public int TransporterId { get; set; }

        // Для хранения файлов изображений
        [Display(Name = "Основное изображение")]
        public IFormFile MainImage { get; set; }

        [Display(Name = "Дополнительные изображения")]
        public List<IFormFile> AdditionalImages { get; set; }

        // Для хранения выбранных удобств
        public List<int> SelectedConvenienceIds { get; set; }

        // Списки для отображения в представлении
        public List<ShipType> ShipTypes { get; set; }
        public List<Сonvenience> ShipСonveniences { get; set; }

    }
}
