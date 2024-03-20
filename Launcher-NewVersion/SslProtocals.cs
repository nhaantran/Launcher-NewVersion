using System.Net;
using System.Security.Authentication;

namespace Launcher_NewVersion
{
    public static class SslProtocals
    {
        public const SslProtocols _Tls12 = (SslProtocols)0x00000C00;
        public const SecurityProtocolType Tls12 = (SecurityProtocolType)_Tls12;

        public static void SetUpSslProtocals()
        {
            ServicePointManager.SecurityProtocol = Tls12;
        }
    }
}
