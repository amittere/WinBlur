using Newtonsoft.Json.Linq;
using System;

namespace WinBlur.Shared
{
    public static class ParseHelper
    {
        public static T ParseValueRef<T>(JToken t, T defaultValue) where T : class
        {
            if (t != null)
            {
                try
                {
                    T value = t.ToObject<T>();
                    if (value != null) return value;
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public static T ParseValueStruct<T>(JToken t, T defaultValue) where T : struct
        {
            if (t != null)
            {
                try
                {
                    T? v = t.ToObject<T?>();
                    if (v.HasValue) return v.Value;
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
