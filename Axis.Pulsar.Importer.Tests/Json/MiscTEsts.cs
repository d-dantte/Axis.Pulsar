using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;

namespace Axis.Pulsar.Importer.Tests.Json
{
    [TestClass]
    public class MiscTEsts
    {
        [TestMethod]
        public void SampleTest()
        {
            JValue v = new(DateTimeOffset.Now);
            var values = v.Value<DateTimeOffset>();

            var jobj = JObject.Parse("{\"stuff\":[1,2,3,4]}");
            var jt = jobj["stuff"];
        }
    }
}
