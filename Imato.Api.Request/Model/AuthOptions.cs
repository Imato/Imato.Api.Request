namespace Imato.Api.Request
{
    public class AuthOptions
    {
        public ApiUser? ApiUser { get; set; }
        public ApiKey? ApiKey { get; set; }
    }

    public class ApiUser
    {
        public string Name { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class ApiKey
    {
        public string Name { get; set; } = "";
        public string Key { get; set; } = "";
    }
}