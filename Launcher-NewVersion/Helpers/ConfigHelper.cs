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
                string configFilePath = Path.GetFullPath(Settings.ConfigFilePath);
                JObject configFile = JObject.Parse(File.ReadAllText(configFilePath));

                return configFile ?? throw new Exception();
            }
            catch (Exception ex)
            { 
                MessageBox.Show(MessageBoxTitle.ConfigFileNotFound.GetDescription(), ex.GetBaseException().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                FileHelpers.WriteLog(ex.ToString());
                Environment.Exit(0);
                return null;
            }
        }

        public static MessageBoxContent ReadMessageBoxContent()
        {
            try
            {
                string configFilePath = Path.GetFullPath(Settings.MessageBoxContentFile);
                var messageContent = JObject.Parse(File.ReadAllText(configFilePath)).ToObject<MessageBoxContent>();
                return messageContent;
            }
            catch (Exception ex)
            {
                FileHelpers.WriteLog(ex.ToString());
                throw;
            }
        }
    }
}
