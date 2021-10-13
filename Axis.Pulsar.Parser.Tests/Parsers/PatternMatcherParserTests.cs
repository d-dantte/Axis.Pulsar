using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
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
                new PatternRule(
                    new Regex("[a-z_]\\w*", RegexOptions.IgnoreCase),
                    Cardinality.OccursOnlyOnce()));

            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);
            var terminal = new PatternRule(
                    regex,
                    Cardinality.OccursOnlyOnce());
            var parser = new PatternMatcherParser(terminal);

            var reader = new BufferedTokenReader("variable = 5;");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.IsTrue(terminal.Regex.IsMatch(result.Symbol.Value));


            //test 2
            regex = new Regex("^\\$[a-z_]\\w*$", RegexOptions.IgnoreCase);

            terminal = new PatternRule(
                regex,
                Cardinality.OccursOnly(2));
            parser = new PatternMatcherParser(terminal);

            reader = new BufferedTokenReader("$variable = 5;");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.IsTrue(terminal.Regex.IsMatch(result.Symbol.Value));


            //test 3
            regex = new Regex("^\\$[a-z_]\\w*$", RegexOptions.IgnoreCase);

            terminal = new PatternRule(
                regex,
                Cardinality.OccursOnly(2));
            parser = new PatternMatcherParser(terminal);

            reader = new BufferedTokenReader("$variable = 5;");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.IsTrue(terminal.Regex.IsMatch(result.Symbol.Value));


            //test 4
            regex = new Regex("^\\d{4}([-/]\\d{2})?$", RegexOptions.IgnoreCase);

            terminal = new PatternRule(
                regex,
                new Cardinality(4, 7));
            parser = new PatternMatcherParser(terminal);

            reader = new BufferedTokenReader("2021- and other stuff");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.IsTrue(terminal.Regex.IsMatch(result.Symbol.Value));


            //test 5
            reader = new BufferedTokenReader("2021-05 bleh");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.IsTrue(terminal.Regex.IsMatch(result.Symbol.Value));
        }


        [TestMethod]
        public void TryParse_WithInvalidInput_Should_ReturnErroredParseResult()
        {
            //test 1
            var regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);
            var terminal = new PatternRule(
                    regex,
                    Cardinality.OccursOnlyOnce());
            var parser = new PatternMatcherParser(terminal);

            var reader = new BufferedTokenReader(" variable = 5;");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Symbol);
            Assert.IsNotNull(result.Error);

            //test 2
            regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);
            terminal = new PatternRule(
                regex,
                Cardinality.OccursOnlyOnce());
            parser = new PatternMatcherParser(terminal);

            reader = new BufferedTokenReader("1_something");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Symbol);
            Assert.IsNotNull(result.Error);
        }
    }
}
