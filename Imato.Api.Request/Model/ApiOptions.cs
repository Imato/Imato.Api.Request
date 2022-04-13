﻿using System.Net.Http.Headers;

namespace Imato.Api.Request
{
    public class ApiOptions
    {
        public string ApiUrl { get; set; } = string.Empty;
        public AuthenticationHeaderValue? Authentication { get; set; }
        public TryOptions TryOptions { get; set; } = new TryOptions();
    }
}