using Axis.Luna.Extensions;
using Axis.Luna.Result;
using Axis.Luna.Unions;
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

            reader = new TokenReader("<bleh \\\\> bleh>");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out t));
            Assert.AreEqual(Tokens.Of("bleh \\\\"), t);

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
            var reader = new TokenReader("@c");
            var path = SymbolPath.Of("dummy");
            var parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out NodeType nt));
            Assert.AreEqual(NodeType.Composite, nt);

            reader = new TokenReader("@A");
            path = SymbolPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out nt));
            Assert.AreEqual(NodeType.Atomic, nt);

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

            reader = new TokenReader("@");
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

            parsed = PathParser.TryRecognizeFilter("", "path", new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

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

            parsed = PathParser.TryRecognizeSegment("abc|@a", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out ps));
            Assert.AreEqual("@U:abc|@A", ps.ToString());

            parsed = PathParser.TryRecognizeSegment("", "path", new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            parsed = PathParser.TryRecognizeSegment("abc|", "path", new object(), out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out PartialRecognitionError pre));

            parsed = PathParser.TryRecognizeSegment("abc|@a|:stuff", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out ps));
            Assert.AreEqual("@U:abc|@A|@U:stuff", ps.ToString());

            parsed = PathParser.TryRecognizeSegment("abc|@a|:stuff|bleh<tokenized>", "path", new object(), out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out ps));
            Assert.AreEqual("@U:abc|@A|@U:stuff|@U:bleh<tokenized>", ps.ToString());
        }

        [TestMethod]
        public void TryRecognizePath_Tests()
        {
            var parsed = PathParser.TryRecognizePath("abc", out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out NodePath np));
            Assert.AreEqual("@U:abc", np.ToString());

            parsed = PathParser.TryRecognizePath("", out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            parsed = PathParser.TryRecognizePath("abcd/xyz/", out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));

            parsed = PathParser.TryRecognizePath("abc|", out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.Is(out PartialRecognitionError pre));


            parsed = PathParser.TryRecognizePath("abc/@c:me/you|<them>", out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.Is(out np));
            Assert.AreEqual("@U:abc/@C:me/@U:you|@U<them>", np.ToString());
        }

        [TestMethod]
        public void ParsePath_Tests()
        {
            var path = PathParser.Parse("abcd");
            Assert.IsNotNull(path);

            Assert.ThrowsException<FormatException>(() => PathParser.Parse("abcd|"));
            Assert.ThrowsException<FormatException>(() => PathParser.Parse(""));
        }

        [TestMethod]
        public void TryParse_Tests()
        {
            var isParsed = PathParser.TryParse("abcd", out var result);
            Assert.IsTrue(result.IsDataResult(out NodePath _));

            isParsed = PathParser.TryParse("abcd|", out result);
            Assert.IsTrue(result.IsErrorResult(out FormatException _));

            isParsed = PathParser.TryParse("", out result);
            Assert.IsTrue(result.IsErrorResult(out FormatException _));
        }

        [TestMethod]
        public void ToFormatException_Tests()
        {
            var nodeResult = PathParserResult<NodePath>.Of(new NodePath());
            var failureResult = PathParserResult<NodePath>.Of(FailedRecognitionError.Of(SymbolPath.Of("bleh"), 0));
            var partialResult = PathParserResult<NodePath>.Of(PartialRecognitionError.Of(SymbolPath.Of("bleh"), 0, 1));

            Assert.ThrowsException<InvalidOperationException>(() => PathParser.ToFormatException(nodeResult));
            Assert.IsNotNull(PathParser.ToFormatException(failureResult));
            Assert.IsNotNull(PathParser.ToFormatException(partialResult));
        }

        [TestMethod]
        public void Misc_Tests()
        {
            var result = PathParserResult<NodePath>.Of(new NodePath());
            Assert.IsFalse(result.IsNull());
        }
    }


    [TestClass]
    public class PathParserResultTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var result = PathParserResult<string>.Of("stuff");
            Assert.IsNotNull(result);
            Assert.AreEqual("stuff", result.Value);

            result = new PathParserResult<string>(null!);
            Assert.IsNull(result.Value);

            result = PathParserResult<string>.Of(FailedRecognitionError.Of("bleh", 0));
            Assert.IsInstanceOfType<FailedRecognitionError>(result.Value);

            result = PathParserResult<string>.Of(PartialRecognitionError.Of("bleh", 0, 1));
            Assert.IsInstanceOfType<PartialRecognitionError>(result.Value);

            Assert.ThrowsException<ArgumentException>(
                () => new PathParserResult<string>(new List<int>()));
        }

        [TestMethod]
        public void Is_Tests()
        {
            var @string = "bleh";
            var failed = FailedRecognitionError.Of("bleh", 0);
            var partial = PartialRecognitionError.Of("bleh", 0, 1);

            var stringResult = PathParserResult<string>.Of(@string);
            var failedResult = PathParserResult<string>.Of(failed);
            var partialResult = PathParserResult<string>.Of(partial);
            var nullResult = default(PathParserResult<string>);

            Assert.IsTrue(stringResult.Is(out string s));
            Assert.AreEqual(@string, s);
            Assert.IsFalse(stringResult.Is(out FailedRecognitionError fre));
            Assert.AreEqual<FailedRecognitionError>(default, fre);

            Assert.IsTrue(failedResult.Is(out fre));
            Assert.AreEqual(failed, fre);
            Assert.IsFalse(failedResult.Is(out PartialRecognitionError pre));
            Assert.AreEqual<PartialRecognitionError>(default, pre);

            Assert.IsTrue(partialResult.Is(out pre));
            Assert.AreEqual(partial, pre);
            Assert.IsFalse(partialResult.Is(out s));
            Assert.IsNull(s);

            Assert.IsTrue(nullResult.IsNull());
        }

        [TestMethod]
        public void MapMatch_Tests()
        {
            var @string = "bleh";
            var failed = FailedRecognitionError.Of("bleh", 0);
            var partial = PartialRecognitionError.Of("bleh", 0, 1);

            var stringResult = PathParserResult<string>.Of(@string);
            var failedResult = PathParserResult<string>.Of(failed);
            var partialResult = PathParserResult<string>.Of(partial);
            var nullResult = default(PathParserResult<string>);

            var result = stringResult.MapMatch(
                s => "1",
                f => "2",
                p => "3",
                () => "4");
            Assert.AreEqual("1", result);

            result = failedResult.MapMatch(
                s => "1",
                f => "2",
                p => "3",
                () => "4");
            Assert.AreEqual("2", result);

            result = partialResult.MapMatch(
                s => "1",
                f => "2",
                p => "3",
                () => "4");
            Assert.AreEqual("3", result);

            result = nullResult.MapMatch(
                s => "1",
                f => "2",
                p => "3",
                () => "4");
            Assert.AreEqual("4", result);

            result = nullResult.MapMatch(
                s => "1",
                f => "2",
                p => "3");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ConsumeMatch_Tests()
        {
            var @string = "bleh";
            var failed = FailedRecognitionError.Of("bleh", 0);
            var partial = PartialRecognitionError.Of("bleh", 0, 1);

            var stringResult = PathParserResult<string>.Of(@string);
            var failedResult = PathParserResult<string>.Of(failed);
            var partialResult = PathParserResult<string>.Of(partial);
            var nullResult = default(PathParserResult<string>);
            string? result = null;

            stringResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3",
                () =>result =  "4");
            Assert.AreEqual("1", result);

            failedResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3",
                () => result = "4");
            Assert.AreEqual("2", result);

            partialResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3",
                () => result = "4");
            Assert.AreEqual("3", result);

            nullResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3",
                () => result = "4");
            Assert.AreEqual("4", result);

            nullResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3");
            // nothing to assert
        }

        [TestMethod]
        public void WithMatch_Tests()
        {
            var @string = "bleh";
            var failed = FailedRecognitionError.Of("bleh", 0);
            var partial = PartialRecognitionError.Of("bleh", 0, 1);

            var stringResult = PathParserResult<string>.Of(@string);
            var failedResult = PathParserResult<string>.Of(failed);
            var partialResult = PathParserResult<string>.Of(partial);
            var nullResult = default(PathParserResult<string>);
            string? result = null;

            var r = stringResult.WithMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3",
                () => result = "4");
            Assert.AreEqual("1", result);
            Assert.AreEqual(r, stringResult);

            r = failedResult.WithMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3",
                () => result = "4");
            Assert.AreEqual("2", result);
            Assert.AreEqual(r, failedResult);

            r = partialResult.WithMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3",
                () => result = "4");
            Assert.AreEqual("3", result);
            Assert.AreEqual(r, partialResult);

            r = nullResult.WithMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3",
                () => result = "4");
            Assert.AreEqual("4", result);
            Assert.AreEqual(r, nullResult);

            r = nullResult.WithMatch(
                s => result = "1",
                f => result = "2",
                p => result = "3");
            Assert.AreEqual(r, nullResult);
        }
    }
}
