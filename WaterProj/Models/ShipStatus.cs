using System.ComponentModel.DataAnnotations;

// Пока хардкорим в коде, по возможности перенести в БД (при необходимости)
namespace WaterProj.Models
{
    public enum ShipStatus
    {
        [Display(Name = "Активный")]
        Active = 1,

        [Display(Name = "На обслуживании")]
        Maintenance = 2,

        [Display(Name = "Не активен")]
        Inactive = 3,
    }
}