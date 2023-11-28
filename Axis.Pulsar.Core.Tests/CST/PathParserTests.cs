using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
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
            var parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("bleh bleh"), result.Resolve());

            reader = new TokenReader("<bleh \\> bleh>");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("bleh \\> bleh"), result.Resolve());

            reader = new TokenReader("no delimiter");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError _));

            reader = new TokenReader("<bleh \\h bleh>");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out PartialRecognitionError xx));

            reader = new TokenReader("<abcd");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out PartialRecognitionError _));
        }

        [TestMethod]
        public void TryRecongzeSymbolName_Tests()
        {
            var reader = new TokenReader("no-delimiter-symbol-name");
            var path = ProductionPath.Of("dummy");
            var parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("no-delimiter-symbol-name"), result.Resolve());

            reader = new TokenReader(":delimited-symbol-name");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("delimited-symbol-name"), result.Resolve());

            reader = new TokenReader("");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError _));

            reader = new TokenReader("?name");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError _));

            reader = new TokenReader("::name");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult(out PartialRecognitionError _));
        }


        [TestMethod]
        public void TryRecognizeFilterType_Tests()
        {
            var reader = new TokenReader("@n");
            var path = ProductionPath.Of("dummy");
            var parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(NodeType.NonTerminal, result.Resolve());

            reader = new TokenReader("@T");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(NodeType.Terminal, result.Resolve());

            reader = new TokenReader("@U");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(NodeType.Unspecified, result.Resolve());

            reader = new TokenReader("bleh");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(NodeType.Unspecified, result.Resolve());

            reader = new TokenReader("");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError _));

            reader = new TokenReader("@p");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult(out PartialRecognitionError _));
        }

        [TestMethod]
        public void TryRecognizeFilter()
        {
            var parsed = PathParser.TryRecognizeFilter("abc", "path", new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeFilter(":abc", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeFilter("@u:abc", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeFilter("@u:abc<tokens>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc<tokens>", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeFilter("@u<tokens>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U<tokens>", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeFilter("<tokens>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U<tokens>", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeFilter("symbol-name<tokens>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:symbol-name<tokens>", result.Resolve().ToString());
        }

        [TestMethod]
        public void TryRecognizeSegment_Tests()
        {
            var parsed = PathParser.TryRecognizeSegment("abc", "path", new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeSegment("abc|@t", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc|@T", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeSegment("abc|@t|:stuff", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc|@T|@U:stuff", result.Resolve().ToString());

            parsed = PathParser.TryRecognizeSegment("abc|@t|:stuff|bleh<tokenized>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc|@T|@U:stuff|@U:bleh<tokenized>", result.Resolve().ToString());
        }

        [TestMethod]
        public void TryRecognizePath_Tests()
        {
            var parsed = PathParser.TryRecognizePath("abc", out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc", result.Resolve().ToString());


            parsed = PathParser.TryRecognizePath("abc/@n:me/you|<them>", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual("@U:abc/@N:me/@U:you|@U<them>", result.Resolve().ToString());
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
