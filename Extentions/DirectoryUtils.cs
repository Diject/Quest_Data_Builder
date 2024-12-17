using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.Extentions
{
    internal static class DirectoryUtils
    {
        public static string? GetParentDirectoryPathWithName(string path, string targetName)
        {
            DirectoryInfo? currentDir = new(path);

            while (currentDir is not null)
            {
                if (string.Equals(currentDir.Name, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return currentDir?.FullName;
                }

                currentDir = currentDir.Parent;
            }

            return null;
        }
    }
}
