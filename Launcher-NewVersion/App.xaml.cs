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
                string assemblyName = new AssemblyName(args.Name).Name;
                string dllPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs"), $"{assemblyName}.dll");
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
