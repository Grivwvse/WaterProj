using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterProj.Models
{
    public class Сonvenience
    {
        [Key]
        public int ShipСonvenienceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // Связь многие-ко-многим с Ship через ShipСonvenience
        public List<ShipСonvenience> ShipСonveniences { get; set; }
    }

}
