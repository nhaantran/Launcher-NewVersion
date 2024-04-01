using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Launcher_NewVersion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string folderPath = @"Data/Libs";
                string assemblyName = new AssemblyName(args.Name).Name;
                string dllPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderPath), $"{assemblyName}.dll");
                return File.Exists(dllPath) ? Assembly.LoadFile(dllPath) : null;
            };
            base.OnStartup(e);
        }
        public App() 
        {
            SslProtocals.SetUpDefaultSslProtocals();
        }
    }
}
