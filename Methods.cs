using System;
using System.IO;

namespace VeeamTestTask
{
    class Methods
    {
        public static bool FilesAreEqual(string file1, string file2)
        {
            var fileInfo1 = new FileInfo(file1);
            var fileInfo2 = new FileInfo(file2);

            // check if files are the same size and last modified time
            return fileInfo1.Length == fileInfo2.Length &&
                   fileInfo1.LastWriteTime == fileInfo2.LastWriteTime;
        }

        public static void DeleteEmptyDirectories(string directory)
        {
            foreach (var subdirectory in Directory.GetDirectories(directory))
            {
                DeleteEmptyDirectories(subdirectory);

                // if directory is empty, delete it
                if (Directory.GetFiles(subdirectory).Length == 0 &&
                    Directory.GetDirectories(subdirectory).Length == 0)
                {
                    Directory.Delete(subdirectory);
                    Console.WriteLine("deleted empty directory: " + subdirectory);
                }
            }
        }
    }
}
