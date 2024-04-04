using log4net;
using log4net.Repository;
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
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                #region Set up logger
                string logPath = Path.GetFullPath("Data\\Log");
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                ILoggerRepository Root;
                Root = LogManager.GetRepository(Assembly.GetCallingAssembly());
                XmlElement section = ConfigurationManager.GetSection("log4net") as XmlElement;

                XPathNavigator navigator = section.CreateNavigator();
                XPathNodeIterator nodes = navigator.Select("appender/file");

                foreach (XPathNavigator appender in nodes)
                {
                    appender.MoveToAttribute("value", string.Empty);
                    appender.SetValue(string.Format(appender.Value, DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss")));
                }

                IXmlRepositoryConfigurator xmlCon = Root as IXmlRepositoryConfigurator;
                xmlCon.Configure(section);
                #endregion

                #region Set up DLL
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    string folderPath = @"Data/Libs";
                    string assemblyName = new AssemblyName(args.Name).Name;
                    string dllPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderPath), $"{assemblyName}.dll");
                    return File.Exists(dllPath) ? Assembly.LoadFile(dllPath) : null;
                };
                #endregion
                base.OnStartup(e);
            } catch (Exception ex)
            {
                string startUpFailurePath = Path.GetFullPath("Data\\Log");
                string logFileName = "StartUp.log";
                if (!Directory.Exists(startUpFailurePath)) {
                    Directory.CreateDirectory(startUpFailurePath);
                }

                FileStream fs = new FileStream(Path.Combine(startUpFailurePath, logFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                fs.Close();

                File.WriteAllText(Path.Combine(startUpFailurePath, logFileName), ex.ToString());
            }
 
        }
    }
}
