using System;
using System.Net;
using System.Security.Authentication;

namespace Launcher_NewVersion
{
    public static class SslProtocals
    {
        private const SslProtocols _Tls12 = (SslProtocols) 0x00000C00;
        private const SslProtocols _Tls10 = (SslProtocols) 0x00000300;

        private const SecurityProtocolType Tls10 = (SecurityProtocolType)_Tls10;
        private const SecurityProtocolType Tls12 = (SecurityProtocolType)_Tls12;

        public static void SetUpDefaultSslProtocals()
        {
            // Get the major and minor version numbers of the operating system
            int majorVersion = Environment.OSVersion.Version.Major;
            int minorVersion = Environment.OSVersion.Version.Minor;

            // Set the default TLS version based on the operating system
            if (majorVersion == 6 && minorVersion == 1)
            {
                // Windows 7
                ServicePointManager.SecurityProtocol = Tls10;
            }
            else if(majorVersion < 6)
            {
                // Windows XP or earlier
                ServicePointManager.SecurityProtocol = Tls10;
            }
            else
            {
                // Windows 10 or later
                ServicePointManager.SecurityProtocol = Tls10 | Tls12;
            }
        }
    }
}
