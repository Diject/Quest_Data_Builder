using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Quest_Data_Builder.Extentions
{
    public static class StringExtension
    {
        public static string RemoveMWScriptComments(this string str)
        {
            return Regex.Replace(str, "([;]+.*)$", "", RegexOptions.Multiline);
        }

        public static string RemoveEmptyLines(this string str)
        {
            return Regex.Replace(str, @"^\s*$\n|\r", "", RegexOptions.Multiline);
        }
    }
}
