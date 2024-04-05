using Launcher.Helpers;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;
using System.Xml.XPath;

namespace Launcher_NewVersion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            SetUpDLL();
            SetUpLogger();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = (Exception)e.ExceptionObject;
                FileHelpers.WriteLog(ex.ToString());
            }
            catch (Exception ex)
            {
                string startUpFailurePath = Path.GetFullPath("Data\\Log");
                string currentTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                string logFileName = "StartUp" + currentTime + ".log";
                if (!Directory.Exists(startUpFailurePath))
                {
                    Directory.CreateDirectory(startUpFailurePath);
                }
                FileStream fs = new FileStream(Path.Combine(startUpFailurePath, logFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                fs.Close();

                File.WriteAllText(Path.Combine(startUpFailurePath, logFileName), ex.ToString());
            }
        }
        private void SetUpDLL()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string folderPath = @"Data/Libs";
                string assemblyName = new AssemblyName(args.Name).Name;
                string dllPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderPath), $"{assemblyName}.dll");
                return File.Exists(dllPath) ? Assembly.LoadFile(dllPath) : null;
            };
        }
        private void SetUpLogger()
        {
            string logPath = Path.GetFullPath("Data\\Log");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            log4net.Repository.ILoggerRepository Root;
            Root = log4net.LogManager.GetRepository(Assembly.GetCallingAssembly());
            XmlElement section = ConfigurationManager.GetSection("log4net") as XmlElement;

            XPathNavigator navigator = section.CreateNavigator();
            XPathNodeIterator nodes = navigator.Select("appender/file");

            foreach (XPathNavigator appender in nodes)
            {
                appender.MoveToAttribute("value", string.Empty);
                appender.SetValue(string.Format(appender.Value, DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss")));
            }

            log4net.Repository.IXmlRepositoryConfigurator xmlCon = Root as log4net.Repository.IXmlRepositoryConfigurator;
            xmlCon.Configure(section);
        }
    }
}
