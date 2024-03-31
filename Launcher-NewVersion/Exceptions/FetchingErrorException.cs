using System;


namespace Launcher.Exceptions
{
    public class FetchingErrorException : Exception
    {
        public FetchingErrorException(string message) : base(message)
        {
        }
    }
}
