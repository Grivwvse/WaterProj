using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class ShipImage
    {
        [Key]
        public int ShipImageId { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public string? Title { get; set; }

        [Required]
        public int ShipId { get; set; }

        [ForeignKey("ShipId")]
        public Ship Ship { get; set; }
    }
}
