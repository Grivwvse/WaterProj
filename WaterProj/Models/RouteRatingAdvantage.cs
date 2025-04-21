using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class RouteRatingAdvantage
    {
        [Key]
        public int RouteRatingAdvantageId { get; set; }

        [Required]
        public int RouteRatingId { get; set; }

        [ForeignKey("RouteRatingId")]
        public RouteRating RouteRating { get; set; }

        [Required]
        public int AdvantageId { get; set; }

        [ForeignKey("AdvantageId")]
        public Advantage Advantage { get; set; }
    }
}
