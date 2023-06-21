using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imato.Api.Request
{
    internal static class ConsoleOutput
    {
        private static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static LogLevel LogLevel { get; set; } = LogLevel.Error;

        public static void WriteJson(object data)
        {
            Console.WriteLine(JsonSerializer.Serialize(data, JsonOptions));
        }

        public static void WriteJson(object data, LogLevel level)
        {
            if (level < LogLevel)
                return;
            WriteJson(data);
        }

        public static void Log(string message, LogLevel level)
        {
            if (level < LogLevel)
                return;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {level}: {message}");
        }

        public static void Log(Exception ex, LogLevel level)
        {
            if (level < LogLevel)
                return;
            Console.WriteLine(ex.ToString());
        }

        public static void Log(object obj, LogLevel level)
        {
            if (level < LogLevel)
                return;
            WriteJson(obj);
        }

        public static void LogDebug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public static void LogDebug(object obj)
        {
            Log(obj, LogLevel.Debug);
        }

        public static void LogWarning(string message)
        {
            Log(message, LogLevel.Warning);
        }

        public static void LogWarning(object obj)
        {
            Log(obj, LogLevel.Warning);
        }

        public static void LogInformation(string message)
        {
            Log(message, LogLevel.Information);
        }

        public static void LogInformation(object obj)
        {
            Log(obj, LogLevel.Information);
        }

        public static void LogError(string message)
        {
            Log(message, LogLevel.Error);
        }

        public static void LogError(object obj)
        {
            Log(obj, LogLevel.Error);
        }

        public static void LogError(Exception obj)
        {
            Log(obj, LogLevel.Error);
        }
    }
}