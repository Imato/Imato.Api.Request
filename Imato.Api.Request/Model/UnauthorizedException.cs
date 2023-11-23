namespace Imato.Api.Request
{
    public class UnauthorizedException : HttpException
    {
        public UnauthorizedException(string str) : base($"Unauthorized: {str}")
        {
        }
    }
}