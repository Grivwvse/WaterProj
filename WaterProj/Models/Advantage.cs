using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class Advantage
    {
        [Key]
        public int AdvantageId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        // Связь с RouteRatingAdvantage
        public ICollection<RouteRatingAdvantage> RouteRatingAdvantages { get; set; }
    }
}
