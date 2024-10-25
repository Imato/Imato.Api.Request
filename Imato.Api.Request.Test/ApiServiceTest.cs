using System;
using System.Net.Http;
using System.Threading.Tasks;
using Imato.Try;
using NUnit.Framework;
using System.Collections.Generic;

namespace Imato.Api.Request.Test
{
    public class ApiServiceTest
    {
        private ApiService service = new ApiService(new ApiOptions
        {
            ApiUrl = "https://www.boredapi.com/api",
            IgnoreSslErrors = true,
            TryOptions = new TryOptions { Timeout = 3000 }
        });

        [Test]
        public void GetApiUrl()
        {
            var result = service.GetApiUrl("activity");
            Assert.That("https://www.boredapi.com/api/activity", Is.EqualTo(result));
            result = service.GetApiUrl("activity", new { type = "education", price = 0 });
            Assert.That("https://www.boredapi.com/api/activity?type=education&price=0", Is.EqualTo(result));
        }

        [Test]
        public void Deserialize()
        {
            var str = @"{""result"": {""activity"":""Research a topic you're interested in"",""type"":""education"",""participants"":1,""price"":0,""link"":"""",""key"":""3561421"",""accessibility"":0.9}}";
            var result = ApiService.Deserialize<NewActivity>(str, "result");
            Assert.That("3561421", Is.EqualTo(result.Key));
        }

        [Test]
        public async Task Get()
        {
            var tasks = new List<Task>();

            tasks.Add(Task.Run(async () =>
            {
                var result = await service.GetAsync<NewActivity>("/activity");
                Assert.That(string.IsNullOrEmpty(result?.Activity), Is.False);
                Assert.That(string.IsNullOrEmpty(result?.Type), Is.False);
                Assert.That(string.IsNullOrEmpty(result?.Key), Is.False);
                Assert.That(result?.Accessibility > 0, Is.True);
            }));

            tasks.Add(Task.Run(async () =>
            {
                var result = await service.GetAsync<NewActivity>(path: "/activity", queryParams: new { type = "education" });
                Assert.That(string.IsNullOrEmpty(result?.Activity), Is.False);
                Assert.That(string.IsNullOrEmpty(result?.Type), Is.False);
                Assert.That(result?.Key, Is.Not.Empty);
            }));

            await Task.WhenAll(tasks);
        }

        [Test]
        public void GetError()
        {
            Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetAsync<NewActivity>("/activi"));
        }

        [Test]
        public void QueryStringTest()
        {
            var qs = "";
            var result = service.QueryString("");
            Assert.That(qs, Is.EqualTo(result));
            result = service.QueryString(null);
            Assert.That(qs, Is.EqualTo(result));
            var q = "parameter=T";
            result = service.QueryString(q);
            Assert.That("?" + q, Is.EqualTo(result));
            result = service.QueryString(2);
            Assert.That(qs, Is.EqualTo(result));
            result = service.QueryString(new int[] { 1, 2, 3 });
            Assert.That(qs, Is.EqualTo(result));

            result = service.QueryString(new TestClass
            {
                Test1 = 1,
                Test2 = "t",
                Test3 = DateTime.Parse("2022-01-01 01:12:12.432"),
                Test4 = new int[] { 1, 2, 3 }
            });
            Assert.That("?Test1=1&Test2=t&Test3=2022-01-01T01:12:12.000Z&Test4=1&Test4=2&Test4=3",
                Is.EqualTo(result));

            result = service.QueryString(new { test1 = "", test4 = new int[0] });
            Assert.That("", Is.EqualTo(result));

            result = service.QueryString(new TestClass { Test5 = "test" });
            Assert.That("?Test1=0", Is.EqualTo(result));
        }

        [Test]
        public void SerializeTextTest()
        {
            var text = "{\"origID\": 1,\"status\":\"firing\",\"externalURL\":\"https://semaphores.odkl.ru/semaphore/3666\",\"groupKey\":\"3666\",\"alerts\":[{\"status\": \"firing\",\"startsAt\":\"2024-10-21T09:25:00.000Z\",\"endsAt\":\"0001-01-01T00:00:00Z\",\"labels\":{\"alertname\":\"Ошибки 500 S3\",\"severity\":\"critical\",\"team\":\"smart-monitoring\",\"component\":\"unknown\",\"product\":\"unknown\"},\"annotations\":{\"description\":\"Рост ошибок может говорит о недоступности какого то хранилища , что вызывает даунтайм на сервис который использует эти хранилища. Заводим инцидент привлекаем дежурных по S3 https://wiki.odkl.ru/display/admin/calendar/b0107b72-a8fb-4c7a-993c-1ca2414334ac\",\"summary\":\"Semaphore Ошибки 500 S3 https://semaphores.odkl.ru/semaphore/3666 Type: MAX Limit red: 4000 Limit yellow: 4000 Value in 12:25 21.10.2024: 1357233.00 Description: Рост ошибок может говорит о недоступности какого то хранилища , что вызывает даунтайм на сервис который использует эти хранилища. Заводим инцидент привлекаем дежурных по S3 https://wiki.odkl.ru/display/admin/calendar/b0107b72-a8fb-4c7a-993c-1ca2414334ac https://charts.odkl.ru/reports?id_repository=1353326&report=1100772&StartDate=2024-10-22&ColumnName=Failures&ReportName=Ошибки%20500%20S3&Activity=0&ObjectStorageStatStatId=one%2Dobject%2Dstorage%2Eapi&ObjectStorageStatOperation=http%2Eresponse&ObjectStorageStatParam1=500&Aggregation=Sum&AccumulativeSum=0&MinValue=0&MaxValue=0&Days=0%2C-1%2C-2%2C-3%2C-4%2C-5%2C-6%2C-7&timeScale=PT5M\",\"jira_host\":\"jira.mvk.com\"}}]}";

            var result = ApiService.Serialize(text);
            Assert.That(result, Is.EqualTo(text));
        }
    }
}