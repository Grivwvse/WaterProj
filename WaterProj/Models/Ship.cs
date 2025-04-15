using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterProj.Models
{
    public class Ship
    {
        [Key]
        public int ShipId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        //JSON
        public string ImagePath { get; set; }

        // Навигационное свойство для Route
        public List<Route> Routes { get; set; }

        [NotMapped]
        public List<Image> Images { get; set; }
    }
}
