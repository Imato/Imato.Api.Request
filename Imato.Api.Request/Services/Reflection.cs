using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Imato.Api.Request
{
    public static class Reflection
    {
        public static Dictionary<string, object?> ToDictionaty(this object obj)
        {
            if (obj == null
                || obj is string
                || obj is IEnumerable)
                return new Dictionary<string, object?>();

            return obj.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && !x.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                .ToDictionary(x => x.Name, x => x.GetValue(obj, null));
        }
    }
}