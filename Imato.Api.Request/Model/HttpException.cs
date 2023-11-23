using System;

namespace Imato.Api.Request
{
    public class HttpException : ApplicationException
    {
        public int StatusCode { get; set; } = 200;

        public HttpException(string error, int statusCode = 200)
            : base($"{statusCode}: {error}")
        {
        }
    }
}