using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class ShipType
    {
        [Key]
        public int ShipTypesId { get; set; }
        public string Name { get; set; }

        // Навигационное свойство для изображений
        public List<Ship> Ships { get; set; }

    }
}
