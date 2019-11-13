using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AzUnzipEverything
{
    public class Helper
    {
        public static string SetLocalPath(string fileName)
        {
            var workDir = Environment.GetEnvironmentVariable("TMP");
            var guid = Guid.NewGuid();
            var zipUnarchivedPath = $"{workDir}\\unarchived";
            var localZipFile = ($@"{zipUnarchivedPath}\{guid}_{fileName}");

            // Create temp folder
            if (!Directory.Exists(zipUnarchivedPath))
            {
                Directory.CreateDirectory(zipUnarchivedPath);
            }

            // Clean the temp folder
            var dir = new DirectoryInfo(zipUnarchivedPath);
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo directory in dir.GetDirectories())
            {
                directory.Delete(true);
            }
            return localZipFile;
        }
    }
}
