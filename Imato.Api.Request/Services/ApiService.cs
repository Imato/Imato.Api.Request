using System.Text;
using System.Text.Json;

namespace Imato.Api.Request.Services
{
    public class ApiService
    {
        private readonly HttpClient http = new HttpClient();
        private readonly ApiOptions options = new ApiOptions();

        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService(ApiOptions? options = null)
        {
            if (options != null)
            {
                this.options = options;
                http.Timeout = TimeSpan.FromMilliseconds(options.TryOptions.Timeout);
                http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
                if (!string.IsNullOrEmpty(options.ApiUrl)) http.BaseAddress = new Uri(options.ApiUrl);
                if (options.Authentication != null) http.DefaultRequestHeaders.Authorization = options.Authentication;
            }
        }

        public string GetApiUrl(string path, object? queryParams = null)
        {
            var url = path;
            if (!path.StartsWith("http"))
            {
                if (http.BaseAddress == null || !http.BaseAddress.ToString().StartsWith("http"))
                    throw new ArgumentException("ApiUrl must start with http");

                var u = path.StartsWith("/") ? path : "/" + path;
                url = $"{http.BaseAddress}{u}";
            }

            if (queryParams != null)
            {
                var query = string.Join("&", queryParams.ToDictionaty().Select(x => $"{x.Key}={x.Value}"));
                url += $"?{query}";
            }

            return url;
        }

        private async Task<T> GetResult<T>(Func<Task<T>> func)
        {
            return await Try
                .Function(func)
                .Setup(options.TryOptions)
                .GetResultNotEmpty();
        }

        private async Task Execute(Func<Task> func)
        {
            await Try
                .Function(func)
                .Setup(options.TryOptions)
                .Execute();
        }

        public async Task<T> Get<T>(string path, object? queryParams = null)
        {
            return await GetResult(async () =>
            {
                using (var response = await http.GetAsync(GetApiUrl(path, queryParams)))
                {
                    return await Parse<T>(response);
                }
            });
        }

        public async Task Get(string path, object? queryParams = null)
        {
            await Execute(() => http.GetAsync(GetApiUrl(path, queryParams)));
        }

        public async Task<T> Delete<T>(string path, object? queryParams = null)
        {
            return await GetResult(async () =>
            {
                using (var response = await http.DeleteAsync(GetApiUrl(path, queryParams)))
                {
                    return await Parse<T>(response);
                }
            });
        }

        public async Task Delete(string path, object? queryParams = null)
        {
            await Execute(() => http.DeleteAsync(GetApiUrl(path, queryParams)));
        }

        private async Task<T> Send<T>(object data,
                Func<HttpContent, Task<HttpResponseMessage>> func)
        {
            return await GetResult(async () =>
            {
                using (var response = await func(Serialize(data)))
                {
                    return await Parse<T>(response);
                }
            });
        }

        private async Task Send(object data,
            Func<HttpContent, Task<HttpResponseMessage>> func)
        {
            await Execute(() => func(Serialize(data)));
        }

        public async Task<T> Post<T>(string path, object data, object? queryParams = null)
        {
            return await Send<T>(data,
                (content) => http.PostAsync(GetApiUrl(path, queryParams), content));
        }

        public async Task<T> Put<T>(string path, object data, object? queryParams = null)
        {
            return await Send<T>(data,
                (content) => http.PutAsync(GetApiUrl(path, queryParams), content));
        }

        public async Task Post(string path, object data, object? queryParams = null)
        {
            await Send(data,
                (content) => http.PostAsync(GetApiUrl(path, queryParams), content));
        }

        public async Task Put(string path, object data, object? queryParams = null)
        {
            await Send(data,
                (content) => http.PutAsync(GetApiUrl(path, queryParams), content));
        }

        private async Task<T> Parse<T>(HttpResponseMessage response)
        {
            if (response != null)
            {
                var str = await response.Content.ReadAsStringAsync();
                var result = TryDeserialize<ApiResult>(str);
                var error = result?.ErrorMessage ?? result?.Error;

                if (!response.IsSuccessStatusCode || !string.IsNullOrEmpty(error))
                {
                    error = $"{(int)response.StatusCode}: {error ?? str}";
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedException(error);
                    }
                    throw new HttpRequestException(error);
                }

                return Deserialize<T>(str);
            }

            throw new HttpRequestException("Cannot get result from API");
        }

        public static HttpContent Serialize(object data)
        {
            var str = JsonSerializer.Serialize(data);
            return new StringContent(str, Encoding.UTF8, "application/json");
        }

        public static T Deserialize<T>(string str)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(str, jsonSerializerOptions)
                    ?? throw new HttpRequestException("API result is empty");
            }
            catch
            {
                throw new HttpRequestException($"Cannot parse response {str}");
            }
        }

        public static T? TryDeserialize<T>(string str)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(str, jsonSerializerOptions);
            }
            catch
            {
                return default;
            }
        }
    }
}