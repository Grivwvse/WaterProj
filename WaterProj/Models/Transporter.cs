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
        public float Rating { get; set; }

        // Навигационное свойство для Route
        public List<Route>Routes { get; set; }

        public Transporter()
        {
            Routes = new List<Route>();
            Rating = 0;
        }

    }
}
