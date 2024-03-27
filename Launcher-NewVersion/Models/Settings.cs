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
        public readonly static string LoginServer = _setting["LoginServer"];
        public readonly static string VersionFile = _setting["VersionFile"];      
    }
}
