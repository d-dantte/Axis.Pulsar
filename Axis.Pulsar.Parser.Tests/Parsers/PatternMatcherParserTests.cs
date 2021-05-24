using Axis.Pulsar.Parser.Builder;
using Axis.Pulsar.Parser.Language;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    [TestClass]
    public class PatternMatcherParserTests
    {
        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = new PatternMatcherParser(
                new PatternTerminal(
                    "identifier_name",
                    new Regex("[a-z_]\\w*", RegexOptions.IgnoreCase),
                    new PatternTerminal.PatternInfo(1)));

            Assert.IsNotNull(parser);
        }
    }
}
