namespace MetaActionGenerators.Helpers
{
    public static class PathHelper
    {
        public static string RootPath(string path)
        {
            if (!Path.IsPathRooted(path))
                path = Path.Join(Directory.GetCurrentDirectory(), path);
            path = path.Replace("\\", "/");
            return path;
        }

        public static List<FileInfo> ResolveFileWildcards(List<string> items)
        {
            List<FileInfo> returnFiles = new List<FileInfo>();

            foreach (var item in items)
            {
                if (item.Contains('*'))
                {
                    List<string> subItems = new List<string>();
                    var currentWildcard = item.IndexOf('*');
                    var preChar = item.LastIndexOf('/', currentWildcard - 1);
                    var postChar = item.IndexOf('/', currentWildcard);
                    if (preChar == currentWildcard - 1 && postChar == currentWildcard + 1)
                    {
                        var route = item.Substring(0, currentWildcard);
                        var remaining = item.Substring(currentWildcard + 2);
                        if (Directory.Exists(route))
                            foreach (var option in new DirectoryInfo(route).GetDirectories())
                                subItems.Add(Path.Combine(route, option.Name, remaining));

                        returnFiles.AddRange(ResolveFileWildcards(subItems));
                    }
                    else if (postChar == -1)
                    {
                        var route = item.Substring(0, preChar);
                        if (Directory.Exists(route))
                        {
                            var remaining = item.Substring(preChar + 1);
                            foreach (var option in Directory.GetFiles(route, remaining))
                                subItems.Add(option);

                            returnFiles.AddRange(ResolveFileWildcards(subItems));
                        }
                    }
                }
                else
                {
                    var target = RootPath(item);
                    if (File.Exists(target))
                        returnFiles.Add(new FileInfo(target));
                }
            }

            return returnFiles;
        }

        public static List<DirectoryInfo> ResolveDirectoryWildcards(List<string> items)
        {
            List<DirectoryInfo> returnFiles = new List<DirectoryInfo>();

            foreach (var item in items)
            {
                if (item.Contains('*'))
                {
                    List<string> subItems = new List<string>();
                    var currentWildcard = item.IndexOf('*');
                    var preChar = item.LastIndexOf('/', currentWildcard - 1);
                    var postChar = item.IndexOf('/', currentWildcard);
                    if (preChar == currentWildcard - 1 && postChar == currentWildcard + 1)
                    {
                        var route = item.Substring(0, currentWildcard);
                        var remaining = item.Substring(currentWildcard + 2);
                        if (Directory.Exists(route))
                            foreach (var option in new DirectoryInfo(route).GetDirectories())
                                subItems.Add(Path.Combine(route, option.Name, remaining));

                        returnFiles.AddRange(ResolveDirectoryWildcards(subItems));
                    }
                    else if (postChar == -1)
                    {
                        var route = item.Substring(0, preChar);
                        if (Directory.Exists(route))
                        {
                            var remaining = item.Substring(preChar + 1);
                            foreach (var option in Directory.GetFiles(route, remaining))
                                subItems.Add(option);

                            returnFiles.AddRange(ResolveDirectoryWildcards(subItems));
                        }
                    }
                }
                else
                {
                    var target = RootPath(item);
                    if (Directory.Exists(target))
                        returnFiles.Add(new DirectoryInfo(target));
                }
            }

            return returnFiles;
        }

        public static void RecratePath(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }
    }
}
