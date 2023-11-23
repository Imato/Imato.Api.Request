using Imato.Try;

namespace Imato.Api.Request
{
    public class ApiOptions
    {
        public string ApiUrl { get; set; } = string.Empty;
        public bool IgnoreSslErrors { get; set; }
        public TryOptions TryOptions { get; set; } = new TryOptions();
    }
}