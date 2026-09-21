using System.ComponentModel;
using System.Reflection;

namespace RoadGuardSystem.aBusinessObjects.Commons
{
    public static class EnumExtensions
    {
        // P1-00 F4 fix: added null-conditional guards for GetField() and GetCustomAttribute()
        // which can return null when the enum value does not correspond to a named field.
        // CS8600: Converting null literal or possible null value to non-nullable type.
        public static string GetDescription(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        public static List<T> GetAllValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }
    }
}
