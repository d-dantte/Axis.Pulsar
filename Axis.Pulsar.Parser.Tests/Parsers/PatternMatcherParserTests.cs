using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using Moq;
using Axis.Pulsar.Parser.CST;

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
                    new IPatternMatchType.Open(1)));

            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);
            var symbolName = "variableName";
            var terminal = new PatternRule(
                    regex,
                    new IPatternMatchType.Closed(1, 1));
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
                new IPatternMatchType.Closed(2, 2));
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
            regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);

            terminal = new PatternRule(
                regex,
                new IPatternMatchType.Closed(2, 15));
            parser = new PatternMatcherParser(symbolName, terminal);

            reader = new BufferedTokenReader("variaBle");
            succeeded = parser.TryParse(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult.Symbol);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("variaBle", trueResult.Symbol.TokenValue());


            //test 4
            regex = new Regex("^\\d{4}([-/]\\d{2})?$", RegexOptions.IgnoreCase);

            terminal = new PatternRule(
                regex,
                new IPatternMatchType.Closed(4, 7));
            parser = new PatternMatcherParser(symbolName, terminal);

            reader = new BufferedTokenReader("2021- and other stuff");
            succeeded = parser.TryParse(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult.Symbol);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("2021", trueResult.Symbol.TokenValue());


            //test 5
            terminal = new PatternRule(
                regex,
                new IPatternMatchType.Closed(4, 7));
            parser = new PatternMatcherParser(symbolName, terminal);

            reader = new BufferedTokenReader("2021/22 and other stuff");
            succeeded = parser.TryParse(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult.Symbol);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("2021/22", trueResult.Symbol.TokenValue());


            //test 6
            regex = new Regex("^(//|[^/])+$", RegexOptions.IgnoreCase);
            var mockValidator = new Mock<IRuleValidator<PatternRule>>();
            mockValidator
                .Setup(v => v.IsValidCSTNode(It.IsAny<PatternRule>(), It.IsAny<Parser.CST.ICSTNode>()))
                .Returns(true)
                .Verifiable();

            terminal = new PatternRule(regex, new IPatternMatchType.Open(2), mockValidator.Object);
            parser = new PatternMatcherParser(symbolName, terminal);

            reader = new BufferedTokenReader("2021- and other stuff// and other stuffs///");
            succeeded = parser.TryParse(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult.Symbol);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("2021- and other stuff// and other stuffs//", trueResult.Symbol.TokenValue());
            mockValidator.Verify();
        }


        [TestMethod]
        public void TryParse_WithInvalidInput_Should_ReturnErroredParseResult()
        {
            //test 1
            var symbolName = "variable";
            var regex = new Regex("^[a-z_]\\w*$", RegexOptions.IgnoreCase);
            var terminal = new PatternRule(
                    regex,
                    new IPatternMatchType.Open(1));
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
                new IPatternMatchType.Open(1));
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
