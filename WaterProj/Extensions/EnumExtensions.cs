using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace WaterProj.Extensions
{
    public static class EnumExtensions
    {
        /// Метод расширения для получения отображаемого имени перечисления
        public static string GetDisplayName(this Enum enumValue)
        {
            var displayAttribute = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.Name ?? enumValue.ToString();
        }
    }
}