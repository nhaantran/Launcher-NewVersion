using System.Net;
using System.Security.Authentication;

namespace Launcher_NewVersion
{
    public static class SslProtocals
    {
        private const SslProtocols _Tls12 = (SslProtocols)0x00000C00;
        private const SslProtocols _Tls10 = (SslProtocols)0x00000300;
        

        private const SecurityProtocolType Tls10 = (SecurityProtocolType)_Tls10;
        private const SecurityProtocolType Tls12 = (SecurityProtocolType)_Tls12;

        public static void SetUpSslProtocals()
        {
            ServicePointManager.SecurityProtocol = Tls12 | Tls10;
        }
    }
}
