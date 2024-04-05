using System.Configuration;
using System.Collections.Specialized;
namespace Launcher
{
    public static class Settings
    {
        private readonly static NameValueCollection _setting = ConfigurationManager.AppSettings;

        public readonly static string GameExePath = _setting["GameExePath"];
        public readonly static string SettingPath = _setting["SettingPath"];
        public readonly static string IPSavedFile = _setting["IPSavedFile"];
        public readonly static string ModeSavedFile = _setting["ModeSavedFile"];
        public readonly static string LibFile = _setting["LibFile"];
        public readonly static string GameFilePath = _setting["GameFilePath"];
        public readonly static string LoginServerFile = _setting["LoginServerFile"];
        public readonly static string VersionFile = _setting["VersionFile"];
        public readonly static string ConfigFilePath = _setting["ConfigFilePath"];
        public readonly static string HashSumFile = _setting["HashSumFile"];
        public readonly static string MessageBoxContentFile = _setting["MessageBoxContentFile"];
        public readonly static string FairyResourcesFile = _setting["FairyResourcesFile"];
    }
}
