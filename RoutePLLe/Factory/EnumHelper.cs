using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Factory
{
    internal static class EnumHelper
    {
        public static bool TryParseIgnoreCase<TEnum>(string value, out TEnum result)
            where TEnum : struct, System.Enum
        {
            return System.Enum.TryParse(value ?? string.Empty, ignoreCase: true, out result);
        }
    }
}
