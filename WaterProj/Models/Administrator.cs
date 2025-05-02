using System.ComponentModel.DataAnnotations;

namespace WaterProj.Models
{
    public class Administrator
    {
        [Key]
        public int AdminId { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните имя.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните логин.")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните пароль.")]
        public string PasswordHash { get; set; }
    }
}