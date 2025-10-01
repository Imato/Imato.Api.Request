using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Api.Request
{
    public interface IApiService
    {
        Task<HttpClient> GetClientAsync(CancellationToken token);

        Task DeleteAsync(string path, object? queryParams = null, CancellationToken token = default);

        Task<T> DeleteAsync<T>(string path, object? queryParams = null, string resultJsonPath = "", CancellationToken token = default) where T : class;

        string GetApiUrl(string path, object? queryParams = null);

        Task GetAsync(string path, object? queryParams = null, CancellationToken token = default);

        Task<T> GetAsync<T>(string path, object? queryParams = null, string resultJsonPath = "", CancellationToken token = default) where T : class;

        Task<Stream?> GetStreamAsync(string path, object? queryParams = null, CancellationToken token = default);

        Task PostAsync(string path, object? data = null, object? queryParams = null, CancellationToken token = default);

        Task PostAsync(string path, string filePath, string fileFieldName = "file", object? queryParams = null, Dictionary<string, string>? parameters = null, CancellationToken token = default);

        Task<T?> PostAsync<T>(string path, object? data = null, object? queryParams = null, string resultJsonPath = "", CancellationToken token = default) where T : class;

        Task<T?> PostAsync<T>(string path, string filePath, string fileFieldName = "file", object? queryParams = null, Dictionary<string, string>? parameters = null, string resultJsonPath = "", CancellationToken token = default) where T : class;

        Task<Stream?> PostStreamAsync(string path, object data, object? queryParams = null, CancellationToken token = default);

        Task PutAsync(string path, object data, object? queryParams = null, CancellationToken token = default);

        Task<T?> PutAsync<T>(string path, object data, object? queryParams = null, string resultJsonPath = "", CancellationToken token = default) where T : class;
    }
}