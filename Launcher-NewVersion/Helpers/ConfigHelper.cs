using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows;

namespace Launcher.Helpers
{
    public static class ConfigHelper
    {
        public static JObject ReadConfig()
        {
            try
            {
                string configFilePath = Path.GetFullPath("launcher.json");
                JObject configFile = JObject.Parse(File.ReadAllText(configFilePath));

                if (configFile == null)
                    throw new Exception("Can not find config file");

                return configFile;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
                return null;
            }
        }
    }
}
