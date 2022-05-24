using System;
using System.Text.Json.Serialization;

namespace Imato.Api.Request.Test
{
    public class TestClass
    {
        public int Test1 { get; set; }
        public string Test2 { get; set; } = "";
        public DateTime Test3 { get; set; }
        public int[] Test4 { get; set; } = new int[0];

        [JsonIgnore]
        public string? Test5 { get; set; }
    }
}