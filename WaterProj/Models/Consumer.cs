using System.ComponentModel.DataAnnotations;
using WaterProj.Models;

namespace Dprog.Models
{
    public class Consumer
    {
        [Key]
        public int ConsumerId { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните имя.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните фамилию.")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните номер телефона.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните логин.")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните пароль.")]
        public string PasswordHash { get; set; }

        public List<Feedback> Feedbacks { get; set; }
        public List<Order> Orders { get; set; }

        public Consumer()
        {
            Feedbacks = new List<Feedback>();
            Orders = new List<Order>();
        }
    }
}
