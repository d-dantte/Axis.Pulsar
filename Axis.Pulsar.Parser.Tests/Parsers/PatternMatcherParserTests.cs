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
                "stuff",
                new PatternRule(
                    new Regex("[a-z_]\\w*", RegexOptions.IgnoreCase),
                    Cardinality.OccursOnlyOnce()));

            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);
            var symbolName = "variableName";
            var terminal = new PatternRule(
                    regex,
                    Cardinality.OccursOnlyOnce());
            var parser = new PatternMatcherParser(symbolName, terminal);

            var reader = new BufferedTokenReader("variable = 5;");
            var succeeded = parser.TryParse(reader, out var result);
            var trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult.Symbol);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("v", trueResult.Symbol.TokenValue());


            //test 2
            regex = new Regex("^\\$[a-z_]\\w*$", RegexOptions.IgnoreCase);

            terminal = new PatternRule(
                regex,
                Cardinality.OccursOnly(2));
            parser = new PatternMatcherParser(symbolName, terminal);

            reader = new BufferedTokenReader("$VariaBle = 5;");
            succeeded = parser.TryParse(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult.Symbol);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("$V", trueResult.Symbol.TokenValue());


            //test 3
            regex = new Regex("^\\d{4}([-/]\\d{2})?$", RegexOptions.IgnoreCase);

            terminal = new PatternRule(
                regex,
                Cardinality.Occurs(4, 7));
            parser = new PatternMatcherParser(symbolName, terminal);

            reader = new BufferedTokenReader("2021- and other stuff");
            succeeded = parser.TryParse(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult.Symbol);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("2021", trueResult.Symbol.TokenValue());


            //test 4
            terminal = new PatternRule(
                regex,
                Cardinality.Occurs(4, 7));
            parser = new PatternMatcherParser(symbolName, terminal);

            reader = new BufferedTokenReader("2021/22 and other stuff");
            succeeded = parser.TryParse(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult.Symbol);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("2021/22", trueResult.Symbol.TokenValue());
        }


        [TestMethod]
        public void TryParse_WithInvalidInput_Should_ReturnErroredParseResult()
        {
            //test 1
            var symbolName = "variable";
            var regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);
            var terminal = new PatternRule(
                    regex,
                    Cardinality.OccursAtLeastOnce());
            var parser = new PatternMatcherParser(symbolName, terminal);

            var reader = new BufferedTokenReader(" variable = 5;");
            var succeeded = parser.TryParse(reader, out var result);
            var trueResult = result as IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, trueResult.InputPosition);
            Assert.AreEqual(symbolName, trueResult.ExpectedSymbolName);

            //test 2
            regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);
            terminal = new PatternRule(
                regex,
                Cardinality.OccursAtLeastOnce());
            parser = new PatternMatcherParser(symbolName, terminal);

            reader = new BufferedTokenReader("1_something");
            succeeded = parser.TryParse(reader, out result); 
            trueResult = result as IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, trueResult.InputPosition);
            Assert.AreEqual(symbolName, trueResult.ExpectedSymbolName);
        }
    }
}
