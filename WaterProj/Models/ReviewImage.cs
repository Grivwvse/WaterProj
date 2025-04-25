using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    namespace WaterProj.Models
    {
        public class ReviewImage
        {

            [Key]
            public int ReviewImageID { get; set; } // Первичный ключ

            [Required]
            public string ImagePath { get; set; } = string.Empty;

            [Required]
            public int RouteRatingId { get; set; }

            [ForeignKey("RouteRatingId")]
            public RouteRating RouteRating { get; set; }
        }
    }
}
