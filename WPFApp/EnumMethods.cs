using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Codewise.FooSync.WPFApp
{
    public static class EnumMethods
    {
        public static string GetDescription(this Enum value)
        {
            MemberInfo[] memInfo = value.GetType().GetMember(value.ToString());
            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return null;
        }

        public static Enum GetEnumFromDescription(Type enumType, string description)
        {
            foreach (Enum value in Enum.GetValues(enumType).OfType<Enum>())
            {
                if (value.GetDescription() == description)
                {
                    return value;
                }
            }

            return null;
        }

        public static IEnumerable<string> GetEnumDescriptions(Type enumType)
        {
            List<string> descriptions = new List<string>();

            foreach (Enum value in Enum.GetValues(enumType).OfType<Enum>())
            {
                string description = value.GetDescription();
                if (description != null)
                {
                    descriptions.Add(description);
                }
            }

            return descriptions;
        }
    }
}
