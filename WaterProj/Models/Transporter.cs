using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class Transporter
    {
        [Key]
        public int TransporterId { get; set; }
        public int RouteId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public float Rating { get; set; }

        // Навигационное свойство для Route
        public List<Route>Routes { get; set; }

    }
}
