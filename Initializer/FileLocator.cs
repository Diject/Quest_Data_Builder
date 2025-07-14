using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.Initializer
{
    static class FileLocator
    {
        public static List<string> ResolveFullFilePaths(List<string> fileNames, List<string> directories)
        {
            List<string> res = new();
            foreach (var fileName in fileNames)
            {
                foreach (var directory in directories)
                {
                    string filePath = Path.Combine(directory, fileName);
                    if (File.Exists(filePath))
                    {
                        res.Add(filePath);
                        break;
                    }
                }
            }
            return res;
        }
    }
}
