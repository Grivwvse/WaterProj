using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class ShipСonvenience
    {
        [Key]
        public int Id { get; set; }  // Добавлен первичный ключ для этой промежуточной таблицы

        [Required]
        public int ShipId { get; set; }

        [ForeignKey("ShipId")]
        public Ship Ship { get; set; }

        [Required]
        public int СonvenienceId { get; set; }

        [ForeignKey("СonvenienceId")]
        public Сonvenience Сonvenience { get; set; }
    }
}
