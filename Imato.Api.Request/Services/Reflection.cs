using System.Reflection;

namespace Imato.Api.Request
{
    public static class Reflection
    {
        public static Dictionary<string, object?> ToDictionaty(this object obj)
        {
            return obj.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(x => x.Name, x => x.GetValue(obj, null));
        }
    }
}