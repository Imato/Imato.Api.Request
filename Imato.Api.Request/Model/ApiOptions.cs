namespace Imato.Api.Request
{
    public class ApiOptions
    {
        public string ApiUrl { get; set; } = string.Empty;
        public bool IgnoreSslErrors { get; set; }
        public int RetryCount { get; set; } = 1;
        public int Delay { get; set; }
        public bool ErrorOnFail { get; set; } = true;
        public int Timeout { get; set; } = 30000;
    }
}