using System;
using System.Net;

namespace Sylvanas.Exceptions
{
    public class WebServiceException : Exception
    {
        public WebServiceException()
        {
        }

        public WebServiceException(string message) : base(message)
        {
        }

        public WebServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public int StatusCode { get; set; }

        public string ResponseBody { get; set; }

        public WebHeaderCollection ResponseHeaders { get; set; }

        public bool IsAny400()
        {
            return StatusCode >= 400 && StatusCode < 500;
        }

        public bool IsAny500()
        {
            return StatusCode >= 500 && StatusCode < 600;
        }
    }
}