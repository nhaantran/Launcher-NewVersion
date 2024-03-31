using System;
using System.Net;


namespace Launcher.Helpers
{
    public class TimeOutWebClientHelper : WebClient
    {
        public int Timeout { get; set; }

        public TimeOutWebClientHelper(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.Timeout;
            }
            return request;
        }
    }
}
