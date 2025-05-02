using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class Transporter
    {
        [Key]
        
        public int TransporterId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        public string Login { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        public bool IsBlocked { get; set; } = false;
        public string? BlockReason { get; set; }
        public DateTime? BlockedAt { get; set; }
        public float Rating { get; set; }

        // Навигационное свойство для Route

        // Навигационное свойство для Ship
        public List<Ship> Ships { get; set; }
        public List<Route>Routes { get; set; }

        public Transporter()
        {
            Routes = new List<Route>();
            Rating = 0;
            Ships = new List<Ship>();
        }

    }
}
