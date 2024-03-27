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
            // WIN XP will not run this code
            if(Environment.OSVersion.Version.Major >= 6)
                ServicePointManager.SecurityProtocol = Tls10 | Tls12 ;
        }
    }
}
