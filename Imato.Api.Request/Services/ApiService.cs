using Imato.Try;
using System.Collections;
using System.Text.Json.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Imato.Api.Request
{
    public class ApiService : IApiService
    {
        private ApiOptions options;
        private readonly AuthOptions? authOptions;
        private readonly HttpClientHandler? customHandler;
        private readonly ILogger? logger;
        private HttpClient? client;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private static CancellationToken noToken = CancellationToken.None;

        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                | JsonIgnoreCondition.WhenWritingDefault
        };

        /// <summary>
        /// Add error handler
        /// </summary>
        public EventHandler<Exception>? OnError;

        /// <summary>
        /// Update Http client before request
        /// </summary>
        public Func<HttpClient, Task>? ConfigureRequest;

        public ApiService(ApiOptions? options = null,
            AuthOptions? authOptions = null,
            HttpClientHandler? customHandler = null,
            ILogger? logger = null)
        {
            this.options = options ?? new ApiOptions();
            this.authOptions = authOptions;
            this.logger = logger;
            this.customHandler = customHandler;
        }

        public void Configure(ApiOptions options)
        {
            this.options = options;
        }

        private async Task<HttpClient> GetClient()
        {
            await semaphore.WaitAsync();

            if (client == null)
            {
                logger?.LogDebug("Create HTTP client");

                var handler = customHandler ?? new HttpClientHandler();

                if (options.IgnoreSslErrors)
                {
                    handler.ServerCertificateCustomValidationCallback = (x, y, z, v) => true;
                }

                if (authOptions?.Cookies != null)
                {
                    logger?.LogDebug("Using cookies");
                    handler.UseCookies = true;
                    foreach (var coockie in authOptions.Cookies)
                    {
                        handler.CookieContainer.Add(coockie);
                    }
                }

                client = new HttpClient(handler);

                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
                client.Timeout = TimeSpan.FromMilliseconds(options.TryOptions.Timeout > 0 ? options.TryOptions.Timeout : 30000);
                if (!string.IsNullOrEmpty(options?.ApiUrl))
                    client.BaseAddress = new Uri(options.ApiUrl);

                if (authOptions?.ApiUser != null)
                {
                    logger?.LogDebug($"Using user: {authOptions.ApiUser.Name}");

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            Encoding.ASCII.GetBytes($"{authOptions.ApiUser.Name}:{authOptions.ApiUser.Password}")));
                }

                if (authOptions?.ApiKey != null)
                {
                    logger?.LogDebug($"Using API key: {authOptions.ApiKey.Name}");

                    client.DefaultRequestHeaders.TryAddWithoutValidation(
                        authOptions.ApiKey.Name,
                        authOptions.ApiKey.Key);
                }

                if (authOptions?.Token != null)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(
                        "Authorization",
                        $"Bearer {authOptions.Token}");
                }
            }

            semaphore.Release();

            if (ConfigureRequest != null)
                await ConfigureRequest(client);
            return client!;
        }

        private TryOptions TryOptions => new TryOptions
        {
            Delay = options.TryOptions.Delay,
            ErrorOnFail = options.TryOptions.ErrorOnFail,
            RetryCount = options.TryOptions.RetryCount,
            Timeout = options.TryOptions.Timeout
        };

        public string GetApiUrl(string path = "", object? queryParams = null)
        {
            var url = path;
            if (!path.StartsWith("http"))
            {
                if (options.ApiUrl == "" || !options.ApiUrl.StartsWith("http"))
                    throw new ArgumentException("ApiUrl must start with http");

                var u = path.StartsWith("/") ? path : "/" + path;
                url = $"{options.ApiUrl}{u}";
            }
            url += QueryString(queryParams);

            logger?.LogDebug($"API URL: {url}");
            return url;
        }

        /// <summary>
        /// Construct query string from object properties
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string QueryString(object? obj)
        {
            if (obj == null) return "";

            var builder = new StringBuilder();
            builder.Append("?");

            if (obj is string)
            {
                var str = obj.ToString();
                if (str?.Length > 1)
                {
                    builder.Append(str);
                    return builder.ToString();
                }
            }

            foreach (var p in obj.ToDictionaty())
            {
                if (p.Value != null && !string.IsNullOrEmpty(p.Value.ToString()))
                {
                    var array = p.Value as IEnumerable;
                    if (array != null && !(p.Value is string))
                    {
                        foreach (var item in array)
                        {
                            AddParameter(builder, p.Key, item.ToString());
                        }
                    }
                    else
                    {
                        if (DateTime.TryParse(p.Value.ToString(), out var date))
                        {
                            if (date.Year > 1)
                            {
                                date = date.Kind == DateTimeKind.Local
                                    ? date.ToUniversalTime()
                                    : date;
                                AddParameter(builder, p.Key, date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                            }
                        }
                        else
                        {
                            AddParameter(builder, p.Key, p.Value.ToString());
                        }
                    }
                }
            }

            if (builder.Length == 1) return "";
            return builder.ToString();
        }

        private StringBuilder AddParameter(StringBuilder str, string key, string value)
        {
            if (str.Length > 1) str.Append("&");
            str.Append(key);
            str.Append("=");
            str.Append(value);
            return str;
        }

        private async Task<T> GetResultAsync<T>(Func<Task<T>> func) where T : class
        {
            return await Try.Try
                 .Function(func)
                 .OnError((e) =>
                 {
                     if (OnError != null)
                     {
                         OnError.Invoke(this, e);
                     }
                     else { throw e; }
                 })
                 .Setup(TryOptions)
                 .GetResultAsync();
        }

        private async Task ExecuteAsync(Func<Task> func)
        {
            await Try.Try
                .Function(func)
                .OnError((e) =>
                {
                    if (OnError != null)
                    {
                        OnError.Invoke(this, e);
                    }
                    else { throw e; }
                })
                .Setup(TryOptions)
                .ExecuteAsync();
        }

        public async Task<Stream?> GetStreamAsync(string path = "",
            object? queryParams = null,
            CancellationToken? token = null)
        {
            return await GetResultAsync(async () =>
            {
                var http = await GetClient();
                var response = await http.GetAsync(GetApiUrl(path, queryParams), token ?? noToken);
                return await response.Content.ReadAsStreamAsync();
            });
        }

        public async Task<T> GetAsync<T>(string path,
            object? queryParams = null,
            string resultJsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await GetResultAsync(async () =>
            {
                var http = await GetClient();
                using var response = await http.GetAsync(GetApiUrl(path, queryParams), token ?? noToken);
                logger?.LogDebug($"Result: {response.StatusCode}");
                logger?.LogDebug($"Response: {response.Headers}");
                return await ParseAsync<T>(response, resultJsonPath);
            });
        }

        public async Task GetAsync(string path,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await ExecuteAsync(async () =>
            {
                var http = await GetClient();
                await http.GetAsync(GetApiUrl(path, queryParams), token ?? noToken);
            });
        }

        public async Task<T> DeleteAsync<T>(string path,
            object? queryParams = null,
            string resultJsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await GetResultAsync(async () =>
            {
                var http = await GetClient();
                using var response = await http.DeleteAsync(GetApiUrl(path, queryParams), token ?? noToken);
                return await ParseAsync<T>(response, resultJsonPath);
            });
        }

        public async Task DeleteAsync(string path,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await ExecuteAsync(async () =>
            {
                var http = await GetClient();
                await http.DeleteAsync(GetApiUrl(path, queryParams), token ?? noToken);
            });
        }

        private async Task<T> SendAsync<T>(
                HttpContent content,
                Func<HttpContent, Task<HttpResponseMessage>> func,
                string resultJsonPath = "") where T : class
        {
            using (content)
                return await GetResultAsync(async () =>
                {
                    var response = await func(content);
                    return await ParseAsync<T>(response, resultJsonPath);
                });
        }

        private async Task SendAsync(
            HttpContent content,
            Func<HttpContent, Task<HttpResponseMessage>> func)
        {
            using (content)
                await ExecuteAsync(async () =>
                {
                    using var response = await func(content);
                    await ValidateAsync(response);
                });
        }

        public async Task<T?> PostAsync<T>(string path,
            object? data = null,
            object? queryParams = null,
            string resultJsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await SendAsync<T>(Serialize(data),
                async (content) =>
                {
                    var http = await GetClient();
                    return await http.PostAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                },
                resultJsonPath);
        }

        public async Task<T?> PostAsync<T>(
            string path,
            string filePath,
            string fileFieldName = "file",
            object? queryParams = null,
            Dictionary<string, string>? parameters = null,
            string resultJsonPath = "",
            CancellationToken? token = null) where T : class
        {
            using var fileContent = Serialize(filePath, fileFieldName, parameters);
            return await SendAsync<T>(
                fileContent,
                async (content) =>
                {
                    var http = await GetClient();
                    return await http.PostAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                },
                resultJsonPath);
        }

        public async Task PostAsync(string path,
            object? data = null,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await SendAsync(Serialize(data),
                async (content) =>
                {
                    var http = await GetClient();
                    return await http.PostAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                });
        }

        public async Task PostAsync(string path,
            string filePath,
            string fileFieldName = "file",
            object? queryParams = null,
            Dictionary<string, string>? parameters = null,
            CancellationToken? token = null)
        {
            using var fileContent = Serialize(filePath, fileFieldName, parameters);
            await SendAsync(
                fileContent,
                async (content) =>
                {
                    var http = await GetClient();
                    return await http.PostAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                });
        }

        public async Task<Stream?> PostStreamAsync(string path,
            object data,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            var http = await GetClient();
            var response = await http.PostAsync(GetApiUrl(path, queryParams),
                Serialize(data),
                token ?? noToken);
            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<T?> PutAsync<T>(string path,
            object data,
            object? queryParams = null,
            string resultJsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await SendAsync<T>(Serialize(data),
                async (content) =>
                {
                    var http = await GetClient();
                    return await http.PutAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                },
                resultJsonPath);
        }

        public async Task PutAsync(string path,
            object data,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await SendAsync(Serialize(data),
                async (content) =>
                {
                    var http = await GetClient();
                    return await http.PutAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                });
        }

        private async Task<string> ValidateAsync(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new HttpRequestException("Empty responce");
            }

            var str = await response.Content.ReadAsStringAsync();
            logger?.LogDebug($"Response result: {str}");

            var result = TryDeserialize<ApiResult>(str);
            var error = result?.ErrorMessage ?? result?.Error ?? "";
            if (result?.Errors != null && result.Errors.Length > 0)
            {
                foreach (var err in result.Errors)
                {
                    foreach (var key in err.Keys)
                    {
                        error += $"{key}: {err[key]}. ";
                    }
                }
            }

            if (!response.IsSuccessStatusCode || !string.IsNullOrEmpty(error))
            {
                error ??= str ?? response.StatusCode.ToString();
                error = $"{(int)response.StatusCode} {error}";
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedException(error);
                }
                throw new HttpRequestException(error);
            }

            return str;
        }

        private async Task<T> ParseAsync<T>(
            HttpResponseMessage response,
            string resultJsonPath = "") where T : class
        {
            var str = await ValidateAsync(response);
            return Deserialize<T>(str, resultJsonPath);
        }

        public static HttpContent Serialize(object? data)
        {
            var str = string.Empty;
            if (data?.GetType() == typeof(string))
            {
                str = data?.ToString() ?? string.Empty;
            }
            else
            {
                if (data == null)
                {
                    str = JsonSerializer.Serialize(data, jsonSerializerOptions);
                }
            }

            return new StringContent(str, Encoding.UTF8, "application/json");
        }

        public static HttpContent Serialize(
            string filePath,
            string fileFieldName,
            Dictionary<string, string>? parameters = null)
        {
            var content = new StreamContent(File.OpenRead(filePath));
            var form = new MultipartFormDataContent
            {
                { content, fileFieldName, Path.GetFileName(filePath) }
            };
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    form.Add(new StringContent(p.Value), $"\"{p.Key}\"");
                }
            }
            return form;
        }

        public static T Deserialize<T>(string str, string resultJsonPath = "")
        {
            try
            {
                if (resultJsonPath == "")
                {
                    return JsonSerializer.Deserialize<T>(str, jsonSerializerOptions)
                        ?? throw new DeserializationException<T>();
                }
                else
                {
                    var element = JsonSerializer.Deserialize<JsonElement>(str);
                    foreach (var p in resultJsonPath.Split("."))
                    {
                        element = GetProperty(element, p);
                    }

                    return element.Deserialize<T>(jsonSerializerOptions)
                        ?? throw new DeserializationException<T>();
                }
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Cannot parse response {str}", ex);
            }
        }

        public static JsonElement GetProperty(JsonElement element, string name)
        {
            if (element.TryGetProperty(name, out var property)
                && property.ValueKind != JsonValueKind.Undefined
                && property.ValueKind != JsonValueKind.Null)
            {
                return property;
            }
            throw new DeserializationException($"Cannot find property {name} in JSON string");
        }

        public static T? TryDeserialize<T>(string str, string resultJsonPath = "") where T : class
        {
            try
            {
                return Deserialize<T>(str, resultJsonPath);
            }
            catch
            {
                return default;
            }
        }
    }
}