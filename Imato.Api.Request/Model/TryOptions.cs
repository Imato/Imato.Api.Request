namespace Imato.Api.Request
{
    public class TryOptions
    {
        public int RetryCount { get; set; } = 1;
        public int Delay { get; set; } = 0;
        public bool ErrorOnFail { get; set; } = true;
        public int Timeout { get; set; } = 30000;
    }
}