using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Rules;
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
                new Regex("[a-hA-H ]{4,9}", RegexOptions.Compiled),
                IMatchType.Of(4, 9));


            var success = literal.TryRecognize(
                "abcd efgh...other stuff",
                ProductionPath.Of("dummy-path"),
                null!,
                out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var node = result.Resolve();
            Assert.AreEqual("dummy-path", node.Name);
            Assert.AreEqual(Tokens.Of("abcd efgh"), node.Tokens);


            success = literal.TryRecognize(
                "abcd exgh...other stuff",
                ProductionPath.Of("dummy-path"),
                null!,
                out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            node = result.Resolve();
            Assert.AreEqual("dummy-path", node.Name);
            Assert.AreEqual(Tokens.Of("abcd e"), node.Tokens);


            success = literal.TryRecognize(
                "not-stuff",
                ProductionPath.Of("dummy-path"),
                null!,
                out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out UnrecognizedTokens ute));
        }
    }
}
