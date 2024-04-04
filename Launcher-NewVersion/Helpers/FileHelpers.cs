using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Launcher.Helpers
{
    public static class FileHelpers
    {
        public static bool IsFileInUse(this string path)
        {
            try
            {
                if (File.Exists(path))
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write)) { }
                else
                {
                    return false;
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"IsFileInUse: File is in use {ex}");
                WriteLog(ex.ToString());
                return true;
            }
            return false;
        }

        public static void WriteLog(string msg)
        {
            ILog logger = LogManager.GetLogger(typeof(string));
            logger.Error(msg);
        }
    }
}
