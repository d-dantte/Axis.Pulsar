using Axis.Pulsar.Importer.Common.Json.Models;
using Axis.Pulsar.Importer.Common.Json.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Axis.Pulsar.Importer.Tests.Json
{
    [TestClass]
    public class JsonRuleConverterTest
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            Converters = new List<JsonConverter>
            {
                new RuleJsonConverter()
            }
        };

        #region literal

        [TestMethod]
        public void Convert_WithValidLiteralJson_ReturnsValidLiteral()
        {
            var obj = JsonConvert
                .DeserializeObject<IRule>(LiteralRule1, Settings)
                as Literal;
            Assert.AreEqual(RuleType.Literal, obj.Type);
            Assert.AreEqual("Something", obj.Value);
            Assert.IsFalse(obj.IsCaseSensitive);

            obj = JsonConvert
                .DeserializeObject<IRule>(LiteralRule2, Settings)
                as Literal;
            Assert.AreEqual(RuleType.Literal, obj.Type);
            Assert.AreEqual("Something", obj.Value);
            Assert.IsTrue(obj.IsCaseSensitive);
        }


        public static readonly string LiteralRule1 = @"
{
    ""Type"": ""Literal"",
    ""Value"": ""Something""
}
";


        public static readonly string LiteralRule2 = @"
{
    ""Type"": ""Literal"",
    ""Value"": ""Something"",
    ""IsCaseSensitive"": true
}
";
        #endregion

        #region regex

        [TestMethod]
        public void Convert_WithValidRegexJson_ReturnsValidRegex()
        {
            var obj = JsonConvert
                .DeserializeObject<IRule>(RegexRule1, Settings)
                as Pattern;
            Assert.AreEqual(RuleType.Pattern, obj.Type);
            Assert.AreEqual("\\w+", obj.Regex);
            Assert.IsFalse(obj.IsCaseSensitive);

            obj = JsonConvert
                .DeserializeObject<IRule>(RegexRule2, Settings)
                as Pattern;
            Assert.AreEqual(RuleType.Pattern, obj.Type);
            Assert.AreEqual("\\w{1, 5}_\\d+", obj.Regex);
            Assert.IsTrue(obj.IsCaseSensitive);
        }


        public static readonly string RegexRule1 = @"
{
    ""Type"": ""Pattern"",
    ""Regex"": ""\\w+""
}
";


        public static readonly string RegexRule2 = @"
{
    ""Type"": ""Pattern"",
    ""Regex"": ""\\w{1, 5}_\\d+"",
    ""IsCaseSensitive"": true,
    ""MinMatch"": 3,
    ""MaxMatch"": null
}
";
        #endregion

        #region REf
        [TestMethod]
        public void Convert_WithValidRefJson_ReturnsValidRef()
        {
            var obj = JsonConvert
                .DeserializeObject<IRule>(RefRule1, Settings)
                as Ref;
            Assert.AreEqual(RuleType.Ref, obj.Type);
            Assert.AreEqual("Stuff", obj.Symbol);
            Assert.AreEqual(1, obj.MinOccurs);
            Assert.AreEqual(1, obj.MaxOCcurs);

            obj = JsonConvert
                .DeserializeObject<IRule>(RefRule2, Settings)
                as Ref;
            Assert.AreEqual(RuleType.Ref, obj.Type);
            Assert.AreEqual("Stuff", obj.Symbol);
            Assert.AreEqual(3, obj.MinOccurs);
            Assert.IsNull(obj.MaxOCcurs);
        }


        public static readonly string RefRule1 = @"
{
    ""Type"": ""Ref"",
    ""Symbol"": ""Stuff""
}
";


        public static readonly string RefRule2 = @"
{
    ""Type"": ""Ref"",
    ""Symbol"": ""Stuff"",
    ""MinOccurs"": 3,
    ""MaxOccurs"": null
}
";
        #endregion

        #region Grouping
        [TestMethod]
        public void Convert_WithValidGrouping_ReturnsValidGrouping()
        {
            var obj = JsonConvert
                .DeserializeObject<IRule>(GroupRule1, Settings)
                as Grouping;
            Assert.AreEqual(RuleType.Grouping, obj.Type);
            Assert.AreEqual(GroupMode.Sequence, obj.Mode);
        }


        public static readonly string GroupRule1 = @"
{
    ""Type"": ""Grouping"",
    ""Mode"": ""Sequence"",
    ""Rules"":[
        {
            ""Type"": ""Literal"",
            ""Value"": ""Inner""
        },
        {
            ""Type"": ""Ref"",
            ""Symbol"": ""Meh""
        }
    ]
}
";
        #endregion
    }
}
