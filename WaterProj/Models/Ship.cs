using System.ComponentModel.DataAnnotations;

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
    }
}
