using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Imato.Api.Request.Services;
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
        public async Task Get()
        {
            var result = await service.Get<NewActivity>("/activity");
            Assert.False(string.IsNullOrEmpty(result.Activity));
            Assert.False(string.IsNullOrEmpty(result.Type));
            Assert.False(string.IsNullOrEmpty(result.Key));
            Assert.IsTrue(result.Accessibility > 0);

            result = await service.Get<NewActivity>(path: "/activity", queryParams: new { type = "education" });

            var postResult = await service.Post<ApiResult>(path: "/activity",
                    queryParams: new { key = "100" },
                    data: new NewActivity
                    {
                        Activity = "Test"
                    });

            Assert.False(string.IsNullOrEmpty(result.Activity));
            Assert.False(string.IsNullOrEmpty(result.Type));
            Assert.IsNotEmpty(result.Key);
        }

        [Test]
        public async Task GetError()
        {
            Assert.ThrowsAsync<HttpRequestException>(async () => await service.Get<NewActivity>("/activi"));
        }
    }
}