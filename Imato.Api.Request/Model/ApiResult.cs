using System.Collections.Generic;

namespace Imato.Api.Request
{
    public class ApiResult
    {
        public string? SuccessMessage { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Error { get; set; }
        public Dictionary<string, string[]>[]? Errors { get; set; }
    }
}