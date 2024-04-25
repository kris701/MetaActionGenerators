namespace MetaActionGenerators.Helpers
{
    public static class IOHelper
    {
        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            PathHelper.RecratePath(targetPath);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                var target = newPath.Replace(sourcePath, targetPath);
                File.Copy(newPath, target, true);
            }
        }
    }
}
