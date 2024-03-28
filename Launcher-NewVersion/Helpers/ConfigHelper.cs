using Launcher.Models;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows;

namespace Launcher.Helpers
{
    public static class ConfigHelper
    {
        /// <summary>
        /// Get apis from config file
        /// </summary>
        /// <returns></returns>
        public static JObject ReadConfig()
        {
            try
            {
                string configFilePath = Path.GetFullPath(Settings.ConfigFile);
                JObject configFile = JObject.Parse(File.ReadAllText(configFilePath));

                return configFile ?? throw new Exception();
            }
            catch (Exception)
            { 
                MessageBox.Show(MessageBoxContent.ConfigFileNotFound.GetDescription(), "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
                return null;
            }
        }
    }
}
