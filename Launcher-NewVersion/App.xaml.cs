using System.Windows;

namespace Launcher_NewVersion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() 
        {
            SslProtocals.SetUpDefaultSslProtocals();
        }
    }
}
