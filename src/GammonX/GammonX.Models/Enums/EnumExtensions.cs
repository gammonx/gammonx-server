using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GammonX.Models.Enums
{
    public static class EnumExtensions
    {
        public static string GetName(this Enum enumValue)
        {
            var enumMember = enumValue?.GetType()
                            .GetMember(enumValue.ToString())
                            .FirstOrDefault();
            if (enumMember != null)
            {
                var customAttribute = enumMember.GetCustomAttribute<DisplayAttribute>();
                return customAttribute?.GetName() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
