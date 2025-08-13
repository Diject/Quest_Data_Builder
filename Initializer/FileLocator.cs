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


        public static string ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            if (path.StartsWith("~"))
            {
                var home = Environment.GetEnvironmentVariable("HOME") ??
                           Environment.GetEnvironmentVariable("USERPROFILE");
                if (home != null)
                    path = home + path.Substring(1);
            }

            path = System.Text.RegularExpressions.Regex.Replace(
                path,
                @"\$(\w+)",
                m => Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? m.Value
            );

            path = Environment.ExpandEnvironmentVariables(path);

            return path;
        }
    }
}
