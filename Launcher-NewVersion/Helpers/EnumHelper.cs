using Launcher.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


namespace Launcher.Helpers
{
    public static class EnumHelper
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.ToString();
            }
        }
        public static string GetMessageBoxDescription(
            this List<KeyValuePair<MessageBoxTitle, string>> messageBoxDescription, 
            MessageBoxTitle title)
        {
            var message = messageBoxDescription
                .Where(x => x.Key.Equals(title))
                .Select(x => x.Value).FirstOrDefault();
            return message ?? title.GetDescription();
        }
    }
}
