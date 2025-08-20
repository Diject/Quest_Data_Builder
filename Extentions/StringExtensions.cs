using System.Text.RegularExpressions;

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
