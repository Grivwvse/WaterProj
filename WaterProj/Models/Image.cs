using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class Image
    {
        [Key]
        public int ImageID { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string ImagePath { get; set; }
        public string Title { get; set; }
    }
}
