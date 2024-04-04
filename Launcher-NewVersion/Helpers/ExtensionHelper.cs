using Ionic.Zip;
using Launcher.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace Launcher.Helpers
{
    public static class ExtensionHelper
    {
        public static void Extract(this FileExtentions extension, string sourceFile, string destinationDirectory)
        {
            sourceFile = Path.GetFullPath(sourceFile);
            destinationDirectory = Path.GetFullPath(destinationDirectory);
            if (!File.Exists(sourceFile))
            {
                return;
            }

            destinationDirectory = Path.GetDirectoryName(destinationDirectory);

            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            switch (extension)
            {
                case FileExtentions.HlZip:
                    ExtractZipFile(sourceFile, destinationDirectory);
                    break;
                default:
                    ExtractZipFile(sourceFile, destinationDirectory);
                    break;
            }
        }

        public static void ExtractZipFile(string sourceFile, string destinationDirectory)
        {
            try
            {
                using (ZipFile zip = ZipFile.Read(sourceFile))
                {
                    foreach (ZipEntry e in zip)
                    {
                        e.Extract(destinationDirectory, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExtractZipFile {ex}");
                FileHelpers.WriteLog(ex.ToString());
            }

        }
    }
}
