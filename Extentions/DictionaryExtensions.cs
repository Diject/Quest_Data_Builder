using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder
{
    public static class DictionaryExtensions
    {
        public static TV? GetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key)
        {
            TV? value;
            return dict.TryGetValue(key, out value) ? value : default(TV);
        }
    }
}
