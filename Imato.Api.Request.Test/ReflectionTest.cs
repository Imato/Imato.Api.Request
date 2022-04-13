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

            Assert.AreEqual(test.Int, resutl["Int"]);
            Assert.AreEqual(test.Str, resutl["Str"]);
            Assert.AreEqual(test.Date, resutl["Date"]);
            Assert.AreEqual(test.Null, resutl["Null"]);
            Assert.AreEqual(test.test, resutl["test"]);
        }
    }
}