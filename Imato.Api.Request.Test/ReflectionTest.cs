using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Imato.Api.Request.Test
{
    public class ReflectionTest
    {
        [Test]
        public void ToDictionaty()
        {
            var test = new
            {
                Int = 23,
                Str = "Df",
                Date = DateTime.Now,
                Null = (string?)null,
                test = "Test"
            };
            var resutl = test.ToDictionaty();

            Assert.That(test.Int, Is.EqualTo(resutl["Int"]));
            Assert.That(test.Str, Is.EqualTo(resutl["Str"]));
            Assert.That(test.Date, Is.EqualTo(resutl["Date"]));
            Assert.That(test.Null, Is.EqualTo(resutl["Null"]));
            Assert.That(test.test, Is.EqualTo(resutl["test"]));
        }
    }
}