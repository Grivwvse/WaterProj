using System.ComponentModel.DataAnnotations;

// Пока хардкорим в коде, по возможности перенести в БД (при необходимости)
namespace WaterProj.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Активный")]
        Active = 1,

        [Display(Name = "Завершен ")]
        Completed = 2,
    }
}