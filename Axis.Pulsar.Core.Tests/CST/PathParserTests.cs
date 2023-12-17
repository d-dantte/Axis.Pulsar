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
            var path = SymbolPath.Of("dummy");
            var parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out Tokens t));
            Assert.AreEqual(Tokens.Of("bleh bleh"), t);

            reader = new TokenReader("<bleh \\> bleh>");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out t));
            Assert.AreEqual(Tokens.Of("bleh \\> bleh"), t);

            reader = new TokenReader("no delimiter");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            reader = new TokenReader("<bleh \\h bleh>");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));

            reader = new TokenReader("<abcd");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }

        [TestMethod]
        public void TryRecongzeSymbolName_Tests()
        {
            var reader = new TokenReader("no-delimiter-symbol-name");
            var path = SymbolPath.Of("dummy");
            var parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out Tokens t));
            Assert.AreEqual(Tokens.Of("no-delimiter-symbol-name"), t);

            reader = new TokenReader(":delimited-symbol-name");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out t));
            Assert.AreEqual(Tokens.Of("delimited-symbol-name"), t);

            reader = new TokenReader("");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            reader = new TokenReader("?name");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            reader = new TokenReader("::name");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeSymbolName(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }


        [TestMethod]
        public void TryRecognizeFilterType_Tests()
        {
            var reader = new TokenReader("@n");
            var path = SymbolPath.Of("dummy");
            var parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out NodeType nt));
            Assert.AreEqual(NodeType.NonTerminal, nt);

            reader = new TokenReader("@T");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nt));
            Assert.AreEqual(NodeType.Terminal, nt);

            reader = new TokenReader("@U");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nt));
            Assert.AreEqual(NodeType.Unspecified, nt);

            reader = new TokenReader("bleh");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nt));
            Assert.AreEqual(NodeType.Unspecified, nt);

            reader = new TokenReader("");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            reader = new TokenReader("@p");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }

        [TestMethod]
        public void TryRecognizeFilter()
        {
            var parsed = PathParser.TryRecognizeFilter("abc", "path", new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out NodeFilter nf));
            Assert.AreEqual("@U:abc", nf.ToString());

            parsed = PathParser.TryRecognizeFilter(":abc", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nf));
            Assert.AreEqual("@U:abc", nf.ToString());

            parsed = PathParser.TryRecognizeFilter("@u:abc", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nf));
            Assert.AreEqual("@U:abc", nf.ToString());

            parsed = PathParser.TryRecognizeFilter("@u:abc<tokens>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nf));
            Assert.AreEqual("@U:abc<tokens>", nf.ToString());

            parsed = PathParser.TryRecognizeFilter("@u<tokens>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nf));
            Assert.AreEqual("@U<tokens>", nf.ToString());

            parsed = PathParser.TryRecognizeFilter("<tokens>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nf));
            Assert.AreEqual("@U<tokens>", nf.ToString());

            parsed = PathParser.TryRecognizeFilter("symbol-name<tokens>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nf));
            Assert.AreEqual("@U:symbol-name<tokens>", nf.ToString());
        }

        [TestMethod]
        public void TryRecognizeSegment_Tests()
        {
            var parsed = PathParser.TryRecognizeSegment("abc", "path", new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out PathSegment ps));
            Assert.AreEqual("@U:abc", ps.ToString());

            parsed = PathParser.TryRecognizeSegment("abc|@t", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out ps));
            Assert.AreEqual("@U:abc|@T", ps.ToString());

            parsed = PathParser.TryRecognizeSegment("abc|@t|:stuff", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out ps));
            Assert.AreEqual("@U:abc|@T|@U:stuff", ps.ToString());

            parsed = PathParser.TryRecognizeSegment("abc|@t|:stuff|bleh<tokenized>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out ps));
            Assert.AreEqual("@U:abc|@T|@U:stuff|@U:bleh<tokenized>", ps.ToString());
        }

        [TestMethod]
        public void TryRecognizePath_Tests()
        {
            var parsed = PathParser.TryRecognizePath("abc", out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out NodePath np));
            Assert.AreEqual("@U:abc", np.ToString());


            parsed = PathParser.TryRecognizePath("abc/@n:me/you|<them>", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out np));
            Assert.AreEqual("@U:abc/@N:me/@U:you|@U<them>", np.ToString());
        }

        [TestMethod]
        public void ParsePath_Tests()
        {
            var path = PathParser.Parse("abc/@n:me/you|<them>");
            Assert.IsNotNull(path);

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
