using Axis.Luna.Common.Results;
using Axis.Misc.Pulsar.Utils;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class PathParserTests
    {
        //[TestMethod]
        //public void PathTest()
        //{
        //    var result = PathParser.Parse("abcd");
        //    Assert.IsNotNull(result);
        //    var path = result.Resolve();
        //    Assert.AreEqual(1, path.Segments.Length);
        //    Assert.AreEqual(1, path.Segments[0].NodeFilters.Length);

        //    result = PathParser.Parse(":abcd");
        //    Assert.IsNotNull(result);
        //    path = result.Resolve();
        //    Assert.AreEqual(1, path.Segments.Length);
        //    Assert.AreEqual(1, path.Segments[0].NodeFilters.Length);
        //    Assert.AreEqual(NodeType.None, path.Segments[0].NodeFilters[0].NodeType);

        //    result = PathParser.Parse("@r:ab-cd");
        //    Assert.IsNotNull(result);
        //    path = result.Resolve();
        //    Assert.AreEqual(1, path.Segments.Length);
        //    Assert.AreEqual(1, path.Segments[0].NodeFilters.Length);
        //    Assert.AreEqual(NodeType.Ref, path.Segments[0].NodeFilters[0].NodeType);

        //    result = PathParser.Parse("@R:ab-cd/bleh");
        //    Assert.IsNotNull(result);
        //    path = result.Resolve();
        //    Assert.AreEqual(2, path.Segments.Length);
        //    Assert.AreEqual(NodeType.None, path.Segments[1].NodeFilters[0].NodeType);

        //    result = PathParser.Parse("@R:ab-cd/bleh/@l<total>/@c:mello<crew>");
        //    Assert.IsNotNull(result);
        //    path = result.Resolve();

        //    result = PathParser.Parse("@R:ab-cd/bleh/@l<total>/@c:mello<cre\\>w>");
        //    Assert.IsNotNull(result);
        //    path = result.Resolve();
        //}

        //[TestMethod]
        //public void TryParseTokens_Tests()
        //{
        //    var reader = new TokenReader("<bleh bleh>");
        //    var path = ProductionPath.Of("dummy");
        //    var parsed = PathParser.TryParseTokens(reader, path, out var result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("<bleh bleh>"), result.Resolve());

        //    reader = new TokenReader("<bleh \\> bleh>");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseTokens(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("<bleh \\> bleh>"), result.Resolve());

        //    reader = new TokenReader("no delimiter");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseTokens(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.UnrecognizedTokens));

        //    reader = new TokenReader("<bleh \\h bleh>");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseTokens(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.PartiallyRecognizedTokens));

        //    reader = new TokenReader("<abcd");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseTokens(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.PartiallyRecognizedTokens));
        //}

        //[TestMethod]
        //public void TryParseSymbolName_Tests()
        //{
        //    var reader = new TokenReader("no-delimiter-symbol-name");
        //    var path = ProductionPath.Of("dummy");
        //    var parsed = PathParser.TryParseSymbolName(reader, path, out var result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("no-delimiter-symbol-name"), result.Resolve().Tokens);

        //    reader = new TokenReader(":delimited-symbol-name");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseSymbolName(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of(":delimited-symbol-name"), result.Resolve().Tokens);

        //    reader = new TokenReader("");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseSymbolName(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.UnrecognizedTokens));

        //    reader = new TokenReader("?name");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseSymbolName(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.UnrecognizedTokens));

        //    reader = new TokenReader("::name");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseSymbolName(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.PartiallyRecognizedTokens));
        //}

        //[TestMethod]
        //public void TryParseFilterType_Tests()
        //{
        //    var reader = new TokenReader("@r");
        //    var path = ProductionPath.Of("dummy");
        //    var parsed = PathParser.TryParseFilterType(reader, path, out var result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("@r"), result.Resolve().Tokens);

        //    reader = new TokenReader("@C");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilterType(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("@C"), result.Resolve().Tokens);

        //    reader = new TokenReader("");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilterType(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of(""), result.Resolve().Tokens);
        //    Assert.AreEqual(NodeType.None, result.Resolve().NodeType);

        //    reader = new TokenReader("@u");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilterType(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.PartiallyRecognizedTokens));

        //}

        //[TestMethod]
        //public void TryParseFilter_Tests()
        //{
        //    var reader = new TokenReader("name");
        //    var path = ProductionPath.Of("dummy");
        //    var parsed = PathParser.TryParseFilter(reader, path, out var result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("name"), result.Resolve().Tokens);

        //    reader = new TokenReader("@C:name");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilter(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("@C:name"), result.Resolve().Tokens);

        //    reader = new TokenReader("@c:name<tokens>");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilter(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("@c:name<tokens>"), result.Resolve().Tokens);

        //    reader = new TokenReader("@u");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilter(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.PartiallyRecognizedTokens));

        //    reader = new TokenReader("*");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilter(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.PartiallyRecognizedTokens));

        //    reader = new TokenReader("name<stuff");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilter(reader, path, out result);
        //    Assert.IsFalse(parsed);
        //    Assert.IsTrue(result.IsErrorResult());
        //    Assert.IsInstanceOfType(result.AsError().ActualCause(), typeof(Errors.PartiallyRecognizedTokens));

        //    reader = new TokenReader("name++");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseFilter(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("name"), result.Resolve().Tokens);
        //}


        //[TestMethod]
        //public void TryParseSegment_Tests()
        //{
        //    var reader = new TokenReader("name");
        //    var path = ProductionPath.Of("dummy");
        //    var parsed = PathParser.TryParseSegment(reader, path, out var result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("name"), result.Resolve().Tokens);

        //    reader = new TokenReader("@C:name");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseSegment(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("@C:name"), result.Resolve().Tokens);

        //    reader = new TokenReader("@C:name|name-two");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseSegment(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("@C:name|name-two"), result.Resolve().Tokens);

        //    reader = new TokenReader("name|@C:name-two");
        //    path = ProductionPath.Of("dummy");
        //    parsed = PathParser.TryParseSegment(reader, path, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("name|@C:name-two"), result.Resolve().Tokens);
        //}


        //[TestMethod]
        //public void TryParsePath_Tests()
        //{
        //    var reader = new TokenReader("name/dantte");
        //    var parsed = PathParser.TryParsePath(reader, out var result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("name/dantte"), result.Resolve().Tokens);

        //    reader = new TokenReader("@C:name.dantte");
        //    parsed = PathParser.TryParsePath(reader, out result);
        //    Assert.IsTrue(parsed);
        //    Assert.IsTrue(result.IsDataResult());
        //    Assert.AreEqual(Tokens.Of("@C:name.dantte"), result.Resolve().Tokens);
        //}



        [TestMethod]
        public void TryRecognizeTokens_Tests()
        {
            var reader = new TokenReader("<bleh bleh>");
            var path = ProductionPath.Of("dummy");
            var parsed = PathParser.TryRecognizeTokens(reader, path, out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("<bleh bleh>"), result.MapAs<ICSTNode.Literal>().Resolve().Tokens);

            reader = new TokenReader("<bleh \\> bleh>");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeTokens(reader, path, out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("<bleh \\> bleh>"), result.MapAs<ICSTNode.Literal>().Resolve().Tokens);

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
        public void TryParseFilter_Tests()
        {
            var reader = new TokenReader("@r");
            var path = ProductionPath.Of("dummy");
            var parsed = PathParser.TryRecognizeFilterType(reader, path, out var result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("@r"), result.Resolve().Tokens);

            reader = new TokenReader("@C");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, out result);
            Assert.IsTrue(parsed);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(Tokens.Of("@C"), result.Resolve().Tokens);

            reader = new TokenReader("");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out UnrecognizedTokens _));

            reader = new TokenReader("@u");
            path = ProductionPath.Of("dummy");
            parsed = PathParser.TryRecognizeFilterType(reader, path, out result);
            Assert.IsFalse(parsed);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsTrue(result.IsErrorResult(out PartiallyRecognizedTokens _));

        }
    }
}
