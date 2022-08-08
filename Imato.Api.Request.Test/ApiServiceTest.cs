using System;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Imato.Api.Request.Test
{
    public class ApiServiceTest
    {
        private ApiService service = new ApiService(new ApiOptions
        {
            ApiUrl = "https://www.boredapi.com/api",
            TryOptions = new TryOptions
            {
                Timeout = 3000
            }
        });

        [Test]
        public void GetApiUrl()
        {
            var result = service.GetApiUrl("activity");
            Assert.AreEqual("https://www.boredapi.com/api/activity", result);
            result = service.GetApiUrl("activity", new { type = "education", price = 0 });
            Assert.AreEqual("https://www.boredapi.com/api/activity?type=education&price=0", result);
        }

        [Test]
        public void Deserialize()
        {
            var str = @"{""result"": {""activity"":""Research a topic you're interested in"",""type"":""education"",""participants"":1,""price"":0,""link"":"""",""key"":""3561421"",""accessibility"":0.9}}";
            var result = ApiService.Deserialize<NewActivity>(str, "result");
            Assert.AreEqual("3561421", result.Key);
        }

        [Test]
        public async Task Get()
        {
            var result = await service.Get<NewActivity>("/activity");
            Assert.False(string.IsNullOrEmpty(result?.Activity));
            Assert.False(string.IsNullOrEmpty(result?.Type));
            Assert.False(string.IsNullOrEmpty(result?.Key));
            Assert.IsTrue(result?.Accessibility > 0);

            result = await service.Get<NewActivity>(path: "/activity", queryParams: new { type = "education" });
            Assert.False(string.IsNullOrEmpty(result?.Activity));
            Assert.False(string.IsNullOrEmpty(result?.Type));
            Assert.IsNotEmpty(result?.Key);
        }

        [Test]
        public void GetError()
        {
            Assert.ThrowsAsync<HttpRequestException>(async () => await service.Get<NewActivity>("/activi"));
        }

        [Test]
        public void QueryStringTest()
        {
            var qs = "";
            var result = service.QueryString("");
            Assert.AreEqual(qs, result);
            result = service.QueryString(null);
            Assert.AreEqual(qs, result);
            result = service.QueryString("test");
            Assert.AreEqual(qs, result);
            result = service.QueryString(2);
            Assert.AreEqual(qs, result);
            result = service.QueryString(new int[] { 1, 2, 3 });
            Assert.AreEqual(qs, result);

            result = service.QueryString(new TestClass
            {
                Test1 = 1,
                Test2 = "t",
                Test3 = DateTime.Parse("2022-01-01 01:12:12.432"),
                Test4 = new int[] { 1, 2, 3 }
            });
            Assert.AreEqual("?Test1=1&Test2=t&Test3=2022-01-01T01:12:12.000Z&Test4=1,2,3",
                result);

            result = service.QueryString(new { test1 = "", test4 = new int[0] });
            Assert.AreEqual("", result);

            result = service.QueryString(new TestClass { Test5 = "test" });
            Assert.AreEqual("?Test1=0", result);
        }
    }
}