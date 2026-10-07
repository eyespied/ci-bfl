using System;
using System.Reflection;

namespace CI_BFL.Helpers
{
    internal static class ReflectionExtensions
    {
        public static T? GetFieldValue<T>(this object obj, string name)
        {
            FieldInfo? field = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field == null ? default : (T?)field.GetValue(obj);
        }

        public static void SetFieldValue<T>(this object obj, string name, T value)
        {
            FieldInfo? field = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        public static void SetPropertyValue(this object obj, string propertyName, object value)
        {
            PropertyInfo? prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            prop?.SetValue(obj, value);
        }

        public static MethodInfo? GetMethodInfo(this object obj, string methodName)
        {
            return obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
