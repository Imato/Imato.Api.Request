namespace Imato.Api.Request.Services
{
    public class ApiService<T> : ApiService, IApiService<T>
    {
    }

    public interface IApiService<T> : IApiService
    {
    }
}