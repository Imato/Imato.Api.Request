using System;

namespace Imato.Api.Request
{
    public class UnauthorizedException : ApplicationException
    {
        public UnauthorizedException(string str) : base($"Unauthorized: {str}")
        {
        }
    }
}