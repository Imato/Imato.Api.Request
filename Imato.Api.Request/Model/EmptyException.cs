namespace Imato.Api.Request
{
    public class EmptyException : ApplicationException
    {
        public EmptyException() : base("Result is empty")
        {
        }
    }
}