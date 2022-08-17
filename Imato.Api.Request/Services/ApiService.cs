﻿using Imato.Api.Request.Model;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Imato.Api.Request
{
    public class ApiService
    {
        private ApiOptions options;
        private static CancellationToken noToken = CancellationToken.None;

        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public EventHandler<Exception>? OnError;
        public Func<HttpClient, Task>? ConfigureRequest;

        public ApiService(ApiOptions? options = null)
        {
            this.options = options ?? new ApiOptions();
        }

        private async Task<HttpClient> GetClient()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            var http = new HttpClient(handler);
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            http.Timeout = TimeSpan.FromMilliseconds(this.options.TryOptions.Timeout > 0 ? this.options.TryOptions.Timeout : 30000);
            if (!string.IsNullOrEmpty(options?.ApiUrl)) http.BaseAddress = new Uri(this.options.ApiUrl);
            if (ConfigureRequest != null) await ConfigureRequest(http);
            return http;
        }

        public void Configure(ApiOptions options)
        {
            this.options = options;
        }

        private TryOptions TryOptions => new TryOptions
        {
            Delay = options.TryOptions.Delay,
            ErrorOnFail = options.TryOptions.ErrorOnFail,
            RetryCount = options.TryOptions.RetryCount,
            Timeout = options.TryOptions.Timeout
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

        private async Task<T?> GetResult<T>(Func<Task<T>> func)
        {
            var exec = Try
                 .Function(func)
                 .OnError(OnError != null ? e => OnError.Invoke(this, e) : e => throw e)
                 .Setup(TryOptions);

            return await exec.GetResult();
        }

        private async Task Execute(Func<Task> func)
        {
            var exec = Try
                .Function(func)
                .OnError(OnError != null ? e => OnError.Invoke(this, e) : e => throw e)
                .Setup(TryOptions);

            await exec.Execute();
        }

        public async Task<T?> Get<T>(string path,
            object? queryParams = null,
            string jsonPath = "",
            CancellationToken? token = null)
        {
            return await GetResult(async () =>
            {
                using var http = await GetClient();
                using var response = await http.GetAsync(GetApiUrl(path, queryParams), token ?? noToken);
                return await Parse<T>(response, jsonPath);
            });
        }

        public async Task Get(string path,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await Execute(async () =>
            {
                using var http = await GetClient();
                await http.GetAsync(GetApiUrl(path, queryParams), token ?? noToken);
            });
        }

        public async Task<T?> Delete<T>(string path,
            object? queryParams = null,
            string jsonPath = "",
            CancellationToken? token = null)
        {
            return await GetResult(async () =>
            {
                using var http = await GetClient();
                using var response = await http.DeleteAsync(GetApiUrl(path, queryParams), token ?? noToken);
                return await Parse<T>(response, jsonPath);
            });
        }

        public async Task Delete(string path,
            object? queryParams = null,
            CancellationToken? token = null)
        {
            await Execute(async () =>
            {
                using var http = await GetClient();
                await http.DeleteAsync(GetApiUrl(path, queryParams), token ?? noToken);
            });
        }

        private async Task<T?> Send<T>(object data,
                Func<HttpContent, Task<HttpResponseMessage>> func,
                string jsonPath = "")
        {
            return await GetResult(async () =>
            {
                using var response = await func(Serialize(data));
                return await Parse<T>(response, jsonPath);
            });
        }

        private async Task Send(object data,
            Func<HttpContent, Task<HttpResponseMessage>> func)
        {
            await Execute(async () =>
            {
                using var response = await func(Serialize(data));
                await Validate(response);
            });
        }

        public async Task<T?> Post<T>(string path,
            object data,
            object? queryParams = null,
            string jsonPath = "",
            CancellationToken? token = null)
        {
            return await Send<T>(data,
                async (content) =>
                {
                    using var http = await GetClient();
                    return await http.PostAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                },
                jsonPath);
        }

        public async Task<T?> Put<T>(string path,
            object data,
            object? queryParams = null,
            string jsonPath = "",
            CancellationToken? token = null)
        {
            return await Send<T>(data,
                async (content) =>
                {
                    var http = await GetClient();
                    return await http.PutAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                },
                jsonPath);
        }

        public async Task Post(string path,
            object data, object?
            queryParams = null,
            CancellationToken? token = null)
        {
            await Send(data,
                async (content) =>
                {
                    using var http = await GetClient();
                    return await http.PostAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                });
        }

        public async Task Put(string path,
            object data, object?
            queryParams = null,
            CancellationToken? token = null)
        {
            await Send(data,
                async (content) =>
                {
                    using var http = await GetClient();
                    return await http.PutAsync(GetApiUrl(path, queryParams), content, token ?? noToken);
                });
        }

        private async Task<string> Validate(HttpResponseMessage response)
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

        private async Task<T> Parse<T>(HttpResponseMessage response, string jsonPath = "")
        {
            var str = await Validate(response);
            return Deserialize<T>(str, jsonPath);
        }

        public static HttpContent Serialize(object data)
        {
            var str = JsonSerializer.Serialize(data);
            return new StringContent(str, Encoding.UTF8, "application/json");
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