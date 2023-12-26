namespace Imato.Api.Request
{
    public class DeserializationException : HttpException
    {
        public DeserializationException(string message) : base(message)
        {
        }
    }

    public class DeserializationException<T> : DeserializationException
    {
        public DeserializationException()
            : base($"Cannot deserialize to type {typeof(T).Name}")
        {
        }
    }
}