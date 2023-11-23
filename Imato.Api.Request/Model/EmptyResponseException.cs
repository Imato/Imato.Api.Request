using System;

namespace Imato.Api.Request
{
    public class EmptyResponseException : HttpException
    {
        public EmptyResponseException() : base("Result is empty")
        {
        }
    }
}