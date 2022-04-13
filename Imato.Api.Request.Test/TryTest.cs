using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Imato.Api.Request.Test
{
    public class TryTest
    {
        private Func<int, Task<int>> testFunc1 = (int id) =>
                                    {
                                        if (id % 2 == 0)
                                        {
                                            return Task.FromResult(id);
                                        }
                                        throw new ArgumentException(nameof(id));
                                    };

        private Func<int, Task> testFunc2 = (int id) =>
                             {
                                 if (id % 2 == 0)
                                 {
                                     return Task.FromResult(id);
                                 }
                                 throw new ArgumentException(nameof(id));
                             };

        [Test]
        public async Task Test1()
        {
            Exception? exception = null;

            var result = await Try
                .Function(() => testFunc1(2))
                .OnError((ex) => exception = ex)
                .GetResult();

            Assert.AreEqual(2, result);
            Assert.IsNull(exception);

            result = await Try
                .Function(() => testFunc1(1))
                .OnError((ex) => exception = ex)
                .GetResult();

            Assert.AreEqual(0, result);
            Assert.True(exception is ArgumentException);
        }

        [Test]
        public async Task Test2()
        {
            var errorCount = 0;
            var startTime = DateTime.Now;
            var options = new TryOptions
            {
                RetryCount = 3,
                Delay = 20
            };

            var result = await Try
                .Function(() => testFunc1(1))
                .OnError((ex) => errorCount++)
                .Setup(options)
                .GetResult();

            Assert.AreEqual(0, result);
            Assert.AreEqual(1, errorCount);
            Assert.AreEqual(0, options.RetryCount);
            var t = (DateTime.Now - startTime).TotalMilliseconds;
            Assert.True(t > 50);
        }

        [Test]
        public async Task Test3()
        {
            var errorCount = 0;
            var startTime = DateTime.Now;
            var options = new TryOptions
            {
                RetryCount = 3,
                Delay = 20
            };

            await Try
                .Function(() => testFunc2(1))
                .OnError((ex) => errorCount++)
                .Setup(options)
                .Execute();

            Assert.AreEqual(1, errorCount);
            Assert.AreEqual(0, options.RetryCount);
            Assert.True((DateTime.Now - startTime).TotalMilliseconds > 50);
        }

        [Test]
        public async Task Test4()
        {
            var errorCount = 0;
            var options = new TryOptions
            {
                RetryCount = 3
            };

            await Try
                .Function(() => testFunc2(2))
                .OnError((ex) => errorCount++)
                .Setup(options)
                .Execute();

            Assert.AreEqual(0, errorCount);
            Assert.AreEqual(3, options.RetryCount);
        }

        [Test]
        public void Test5()
        {
            var errorCount = 0;
            var options = new TryOptions
            {
                RetryCount = 3
            };

            Assert.ThrowsAsync<Exception>(async () => await Try
                .OnError((ex) => errorCount++)
                .Setup(options)
                .Execute());
        }

        [Test]
        public void Test6()
        {
            var options = new TryOptions
            {
                RetryCount = 3
            };

            Assert.ThrowsAsync<Exception>(async () => await Try
                .Setup<int>(options)
                .GetResult());
        }

        [Test]
        public void Test7()
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await Try
                .Function(() => testFunc2(1))
                .Execute());
        }
    }
}