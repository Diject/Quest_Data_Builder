using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.Logger
{
    public enum LogLevel
    {
        Misc = 3,
        Info = 2,
        Warn = 1,
        Error = 0,
        Text = -1,
    }

    internal static class CustomLogger
    {
        public static LogLevel Level { get; set; } = LogLevel.Warn;

        public static void WriteLine(LogLevel level, string str)
        {
            if (Level >= level)
                Console.WriteLine(str);
        }
    }
}
