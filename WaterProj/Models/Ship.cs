using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WaterProj.Controllers;

namespace WaterProj.Models
{
    public class Ship
    {
        [Key]
        public int ShipId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string IMO  { get; set; }
        public ShipStatus Status { get; set; }

        [Required]
        public int ShipTypeId { get; set; }

        [ForeignKey("ShipTypeId")]
        public ShipType ShipType { get; set; }


        // Внешний ключ для Transporter
        public int TransporterId { get; set; }

        [ForeignKey("TransporterId")]
        public Transporter Transporter { get; set; }

        // Навигационное свойство для Route
        public List<Route> Routes { get; set; }

        // Навигационное свойство для изображений
        public List<ShipImage> ShipImages { get; set; }

        // Навигационное свойство для изображений
        // Связь многие-ко-многим с Сonvenience через ShipСonvenience
        public List<ShipСonvenience> ShipСonveniences { get; set; }
    }
}
