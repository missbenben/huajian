using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TS.Web.Helper
{
    public class EnumHelper
    {
        public static List<SelectListItem> EnumToSelectListItem<T>()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            Type enumType = typeof(T);
            var fieldstrs = Enum.GetNames(enumType);
            foreach (var fieldstr in fieldstrs)
            {
                SelectListItem item = new SelectListItem();
                var field = enumType.GetField(fieldstr);
                string description = string.Empty;
                object[] arr = field.GetCustomAttributes(typeof(DescriptionAttribute), true);

                if (arr != null && arr.Length > 0)
                {
                    description = ((DescriptionAttribute)arr[0]).Description;   //属性描述
                }
                else
                {
                    description = fieldstr;  //描述不存在取字段名称
                }
                item.Text = description;
                item.Value = ((int)Enum.Parse(enumType, fieldstr)).ToString();
                list.Add(item);
            }
            return list;
        }
    }
}