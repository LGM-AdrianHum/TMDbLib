using System;
using System.Collections.Generic;
using System.Reflection;

namespace TMDbLib.Utilities
{
    public static class EnumExtensions
    {
        public static string GetDescription<T>(this T value) where T : struct
        {
            Type type = value.GetType();
            if (!(value is Enum))
                throw new ArgumentException("EnumerationValue must be of Enum type", nameof(value));
            
            IEnumerable<FieldInfo> aa = type.GetRuntimeFields();

            NameAttribute displayAttrib = null;
            foreach (FieldInfo field in aa)
            {
                if (!field.GetValue(value).Equals(value))
                    continue;

                // Found it
                displayAttrib = field.GetCustomAttribute<NameAttribute>();

                break;
            }

            // If we have no description attribute, just return the ToString of the enum
            return displayAttrib?.Description ?? value.ToString();
        }
    }
}
