using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules
{
    [TestClass]
    public class TerminalPatternTests
    {
        [TestMethod]
        public void TryRecognize_Tests()
        {
            // closed match
            var literal = new TerminalPattern(
                "t",
                new Regex("[a-hA-H ]{4,9}", RegexOptions.Compiled),
                IMatchType.Of(4, 9));


            var success = literal.TryRecognize(
                "abcd efgh...other stuff",
                SymbolPath.Of("dummy-path"),
                null!,
                out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ICSTNode node));
            Assert.AreEqual("t", node.Symbol);
            Assert.AreEqual(Tokens.Of("abcd efgh"), node.Tokens);


            success = literal.TryRecognize(
                "abcd exgh...other stuff",
                SymbolPath.Of("dummy-path"),
                null!,
                out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out node));
            Assert.AreEqual("t", node.Symbol);
            Assert.AreEqual(Tokens.Of("abcd e"), node.Tokens);


            success = literal.TryRecognize(
                "not-stuff",
                SymbolPath.Of("dummy-path"),
                null!,
                out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));
        }
    }
}
