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

namespace Imato.Api.Request
{
    public class ApiService
    {
        private ApiOptions options;
        private static CancellationToken noToken = CancellationToken.None;

        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                | JsonIgnoreCondition.WhenWritingDefault
        };

        public EventHandler<Exception>? OnError;
        public Func<HttpClient, Task>? ConfigureRequest;

        public ApiService(ApiOptions? options = null)
        {
            this.options = options ?? new ApiOptions();
        }

        private async Task<HttpClient> GetClient()
        {
            HttpClientHandler? handler = null;
            if (options.IgnoreSslErrors)
            {
                handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (x, y, z, v) => true;
            }
            var http = handler != null ? new HttpClient(handler) : new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            http.Timeout = TimeSpan.FromMilliseconds(options.Timeout > 0 ? options.Timeout : 30000);
            if (!string.IsNullOrEmpty(options?.ApiUrl)) http.BaseAddress = new Uri(options.ApiUrl);
            if (ConfigureRequest != null) await ConfigureRequest(http);
            return http;
        }

        public void Configure(ApiOptions options)
        {
            this.options = options;
        }

        private TryOptions TryOptions => new TryOptions
        {
            Delay = options.Delay,
            ErrorOnFail = options.ErrorOnFail,
            RetryCount = options.RetryCount,
            Timeout = options.Timeout
        };

        public string GetApiUrl(string path, object? queryParams = null)
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

            var str = new StringBuilder();
            bool isFirst = true;
            str.Append("?");
            foreach (var p in obj.ToDictionaty())
            {
                if (p.Value != null && !string.IsNullOrEmpty(p.Value.ToString()))
                {
                    var array = p.Value as IEnumerable;
                    if (array != null && !(p.Value is string))
                    {
                        var fa = true;
                        foreach (var item in array)
                        {
                            if (!fa) str.Append(',');
                            else
                            {
                                AddKey(str, p.Key);
                            }

                            str.Append(item.ToString());
                            fa = false;
                        }
                    }
                    else
                    {
                        if (DateTime.TryParse(p.Value.ToString(), out var date))
                        {
                            if (date.Year > 1)
                            {
                                AddKey(str, p.Key);
                                str.Append(date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                            }
                        }
                        else
                        {
                            AddKey(str, p.Key);
                            str.Append(p.Value);
                        }
                    }
                }
            }

            if (str.Length == 1) return "";
            return str.ToString();
        }

        private StringBuilder AddKey(StringBuilder str, string key)
        {
            if (str.Length > 1) str.Append("&");
            str.Append(key);
            str.Append("=");
            return str;
        }

        private async Task<T?> GetResultAsync<T>(Func<Task<T>> func) where T : class
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

        public async Task<Stream?> GetStreamAsync(string path,
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

        public async Task<T?> GetAsync<T>(string path,
            object? queryParams = null,
            string jsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await GetResultAsync(async () =>
            {
                using var http = await GetClient();
                using var response = await http.GetAsync(GetApiUrl(path, queryParams), token ?? noToken);
                return await ParseAsync<T>(response, jsonPath);
            });
        }

        public async Task GetAsync(string path,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await ExecuteAsync(async () =>
            {
                using var http = await GetClient();
                await http.GetAsync(GetApiUrl(path, queryParams), token ?? noToken);
            });
        }

        public async Task<T?> DeleteAsync<T>(string path,
            object? queryParams = null,
            string jsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await GetResultAsync(async () =>
            {
                using var http = await GetClient();
                using var response = await http.DeleteAsync(GetApiUrl(path, queryParams), token ?? noToken);
                return await ParseAsync<T>(response, jsonPath);
            });
        }

        public async Task DeleteAsync(string path,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await ExecuteAsync(async () =>
            {
                using var http = await GetClient();
                await http.DeleteAsync(GetApiUrl(path, queryParams), token ?? noToken);
            });
        }

        private async Task<T?> SendAsync<T>(
                HttpContent content,
                Func<HttpContent, Task<HttpResponseMessage>> func,
                string jsonPath = "") where T : class
        {
            using (content)
                return await GetResultAsync(async () =>
                {
                    var response = await func(content);
                    return await ParseAsync<T>(response, jsonPath);
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
            string jsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await SendAsync<T>(Serialize(data),
                async (content) =>
                {
                    using var http = await GetClient();
                    return await http.PostAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                },
                jsonPath);
        }

        public async Task<T?> PostAsync<T>(
            string path,
            string filePath,
            string fileFieldName = "file",
            object? queryParams = null,
            Dictionary<string, string>? parameters = null,
            string jsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await SendAsync<T>(
                Serialize(filePath, fileFieldName, parameters),
                async (content) =>
                {
                    using var http = await GetClient();
                    return await http.PostAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                },
                jsonPath);
        }

        public async Task PostAsync(string path,
            object? data = null,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await SendAsync(Serialize(data),
                async (content) =>
                {
                    using var http = await GetClient();
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
            await SendAsync(
                Serialize(filePath, fileFieldName, parameters),
                async (content) =>
                {
                    using var http = await GetClient();
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
            string jsonPath = "",
            CancellationToken? token = null) where T : class
        {
            return await SendAsync<T>(Serialize(data),
                async (content) =>
                {
                    var http = await GetClient();
                    return await http.PutAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                },
                jsonPath);
        }

        public async Task PutAsync(string path,
            object data,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await SendAsync(Serialize(data),
                async (content) =>
                {
                    using var http = await GetClient();
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

        private async Task<T?> ParseAsync<T>(
            HttpResponseMessage response,
            string jsonPath = "") where T : class
        {
            var str = await ValidateAsync(response);
            return TryDeserialize<T>(str, jsonPath);
        }

        public static HttpContent Serialize(object? data)
        {
            var str = data != null ? JsonSerializer.Serialize(data, jsonSerializerOptions) : "";
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

        public static T Deserialize<T>(string str, string jsonPath = "")
        {
            try
            {
                if (jsonPath == "")
                {
                    return JsonSerializer.Deserialize<T>(str, jsonSerializerOptions)
                        ?? throw new EmptyException();
                }
                else
                {
                    var element = JsonSerializer.Deserialize<JsonElement>(str);
                    if (element.TryGetProperty(jsonPath, out var property))
                    {
                        if (property.ValueKind == JsonValueKind.Undefined ||
                            property.ValueKind == JsonValueKind.Null)
                        {
                            throw new EmptyException();
                        }

                        return property.Deserialize<T>(jsonSerializerOptions) ?? throw new EmptyException();
                    }
                    throw new EmptyException();
                }
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Cannot parse response {str}", ex);
            }
        }

        public static T? TryDeserialize<T>(string str, string jsonPath = "") where T : class
        {
            try
            {
                return Deserialize<T>(str, jsonPath);
            }
            catch
            {
                return default;
            }
        }
    }
}