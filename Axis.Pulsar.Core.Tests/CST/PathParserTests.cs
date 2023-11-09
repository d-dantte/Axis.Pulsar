using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class PathParserTests
    {
        [TestMethod]
        public void TryRecognizeTokens_Tests()
        {
            var reader = new TokenReader("<bleh bleh>");
            var path = ProductionPath.Of("dummy");
            var parsed = PathParser.TryRecognizeTokens(reader, path, out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("<bleh bleh>"), result.MapAs<ICSTNode.Terminal>().Resolve().Tokens);

            reader = new TokenReader("<bleh \\> bleh>");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("<bleh \\> bleh>"), result.MapAs<ICSTNode.Terminal>().Resolve().Tokens);

            reader = new TokenReader("no delimiter");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out UnrecognizedTokens _));

            reader = new TokenReader("<bleh \\h bleh>");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out PartiallyRecognizedTokens xx));

            reader = new TokenReader("<abcd");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out PartiallyRecognizedTokens _));
        }

        [TestMethod]
        public void TryRecongzeSymbolName_Tests()
        {
            var reader = new TokenReader("no-delimiter-symbol-name");
            var path = ProductionPath.Of("dummy");
            var parsed = PathParser.TryRecognizeSymbolName(reader, path, out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("no-delimiter-symbol-name"), result.Resolve().Tokens);

            reader = new TokenReader(":delimited-symbol-name");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of(":delimited-symbol-name"), result.Resolve().Tokens);

            reader = new TokenReader("");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out UnrecognizedTokens _));

            reader = new TokenReader("?name");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out UnrecognizedTokens _));

            reader = new TokenReader("::name");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out PartiallyRecognizedTokens _));
        }


        [TestMethod]
        public void TryRecognizeFilterType_Tests()
        {
            var reader = new TokenReader("@n");
            var path = ProductionPath.Of("dummy");
            var parsed = PathParser.TryRecognizeFilterType(reader, path, out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("@n"), result.Resolve().Tokens);

            reader = new TokenReader("@T");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("@T"), result.Resolve().Tokens);

            reader = new TokenReader("@U");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("@U"), result.Resolve().Tokens);

            reader = new TokenReader("bleh");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Empty, result.Resolve().Tokens);

            reader = new TokenReader("");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out UnrecognizedTokens _));

            reader = new TokenReader("@p");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out PartiallyRecognizedTokens _));

        }

        [TestMethod]
        public void TryRecognizeFilter()
        {
            var parsed = PathParser.TryRecognizeFilter("abc", "path", out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("abc"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeFilter(":abc", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of(":abc"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeFilter("@u:abc", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("@u:abc"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeFilter("@u:abc<tokens>", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("@u:abc<tokens>"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeFilter("@u<tokens>", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("@u<tokens>"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeFilter("<tokens>", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("<tokens>"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeFilter("symbol-name<tokens>", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("symbol-name<tokens>"), result.Resolve().Tokens);
        }

        [TestMethod]
        public void TryRecognizeSegment_Tests()
        {
            var parsed = PathParser.TryRecognizeSegment("abc", "path", out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("abc"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeSegment("abc|@t", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("abc|@t"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeSegment("abc|@t|:stuff", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("abc|@t|:stuff"), result.Resolve().Tokens);

            parsed = PathParser.TryRecognizeSegment("abc|@t|:stuff|bleh<tokenized>", "path", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("abc|@t|:stuff|bleh<tokenized>"), result.Resolve().Tokens);
        }

        [TestMethod]
        public void TryRecognizePath_Tests()
        {
            var parsed = PathParser.TryRecognizePath("abc", out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("abc"), result.Resolve().Tokens);


            parsed = PathParser.TryRecognizePath("abc/@n:me/you|<them>", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("abc/@n:me/you|<them>"), result.Resolve().Tokens);
        }

        [TestMethod]
        public void ParsePath_Tests()
        {
            var result = PathParser.Parse("abc/@n:me/you|<them>");
            Assert.IsTrue(result.IsDataResult());

            var path = result.Resolve();
            Assert.AreEqual(3, path.Segments.Length);

            var segment = path.Segments[0];
            Assert.AreEqual(1, segment.NodeFilters.Length);
            Assert.AreEqual(NodeType.Unspecified, segment.NodeFilters[0].NodeType);
            Assert.AreEqual("abc", segment.NodeFilters[0].SymbolName);

            segment = path.Segments[1];
            Assert.AreEqual(1, segment.NodeFilters.Length);
            Assert.AreEqual(NodeType.NonTerminal, segment.NodeFilters[0].NodeType);
            Assert.AreEqual("me", segment.NodeFilters[0].SymbolName);

            segment = path.Segments[2];
            Assert.AreEqual(2, segment.NodeFilters.Length);
            Assert.AreEqual(NodeType.Unspecified, segment.NodeFilters[0].NodeType);
            Assert.AreEqual("you", segment.NodeFilters[0].SymbolName);
            Assert.AreEqual(NodeType.Unspecified, segment.NodeFilters[1].NodeType);
            Assert.AreEqual(null, segment.NodeFilters[1].SymbolName);
            Assert.AreEqual("them", segment.NodeFilters[1].Tokens);
        }
    }
}
