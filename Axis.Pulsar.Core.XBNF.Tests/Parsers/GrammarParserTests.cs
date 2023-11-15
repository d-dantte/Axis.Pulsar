using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Parsers.Models;

namespace Axis.Pulsar.Core.XBNF.Tests.Parsers
{
    [TestClass]
    public class GrammarParserTests
    {
        #region Silent elements

        [TestMethod]
        public void TryParseWhtespsace_Tests()
        {            
            var metaContext = MetaContext.Builder.NewBuilder().Build();

            // new line
            var success = GrammarParser.TryParseWhitespace(
                "\n",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.LineFeed, ws.Char);

            // carriage return
            success = GrammarParser.TryParseWhitespace(
                "\r",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.CarriageReturn, ws.Char);

            // tab
            success = GrammarParser.TryParseWhitespace(
                "\t",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.Tab, ws.Char);

            // space
            success = GrammarParser.TryParseWhitespace(
                " ",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.Space, ws.Char);

            // any other char
            success = GrammarParser.TryParseWhitespace(
                "\v",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out UnmatchedError ume));

        }

        [TestMethod]
        public void TryParseLineComment_Tests()
        {
            var metaContext = MetaContext.Builder.NewBuilder().Build();

            // comment
            var comment = "# and stuff that doesn't end in a new line";
            var success = GrammarParser.TryParseLineComment(
                comment,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var lc = result.Resolve();
            Assert.AreEqual(comment[1..], lc.Content);

            // comment with new line
            comment = "# and stuff that ends in a new line\nOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            lc = result.Resolve();
            Assert.AreEqual(comment[1..35], lc.Content);

            // comment with carriage return
            comment = "# and stuff that ends in a new line\rOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            lc = result.Resolve();
            Assert.AreEqual(comment[1..35], lc.Content);

            // comment with windows new-line
            comment = "# and stuff that ends in a new line\r\nOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            lc = result.Resolve();
            Assert.AreEqual(comment[1..35], lc.Content);
        }

        [TestMethod]
        public void TryParseBlockComment_Tests()
        {
            var metaContext = MetaContext.Builder.NewBuilder().Build();

            // same-line block comment
            TokenReader comment = "/* and stuff that doesn't end * in a new line */";
            var success = GrammarParser.TryParseBlockComment(
                comment,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsTrue(comment.IsConsumed);
            var bc = result.Resolve();
            Assert.AreEqual(comment.Source[2..^2], bc.Content);

            // same-line block comment
            comment = "/* and stuff\n that \rdoesn't \r\nend * \n\rin a new line \n*/";
            success = GrammarParser.TryParseBlockComment(
                comment,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsTrue(comment.IsConsumed);
            bc = result.Resolve();
            Assert.AreEqual(comment.Source[2..^2], bc.Content);
        }

        [TestMethod]
        public void TryParseSilentBlock_Tests()
        {
            var metaContext = MetaContext.Builder.NewBuilder().Build();
            TokenReader block = " some text after a space";
            var success = GrammarParser.TryParseSilentBlock(
                block,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var sb = result.Resolve();
            Assert.AreEqual(1, sb.Elements.Length);
            Assert.AreEqual(" ", sb.Elements[0].Content);


            block = " \n# comment\r\nsome text after a space";
            success = GrammarParser.TryParseSilentBlock(
                block,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            sb = result.Resolve();
            Assert.AreEqual(5, sb.Elements.Length);
            Assert.IsInstanceOfType<Whitespace>(sb.Elements[0]);
            Assert.IsInstanceOfType<Whitespace>(sb.Elements[1]);
            Assert.IsInstanceOfType<LineComment>(sb.Elements[2]);
            Assert.IsInstanceOfType<Whitespace>(sb.Elements[3]);
            Assert.IsInstanceOfType<Whitespace>(sb.Elements[4]);


            block = " \n# comment\n/*\r\n*/some text after a space";
            success = GrammarParser.TryParseSilentBlock(
                block,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            sb = result.Resolve();
            Assert.AreEqual(5, sb.Elements.Length);
            Assert.IsInstanceOfType<Whitespace>(sb.Elements[0]);
            Assert.IsInstanceOfType<Whitespace>(sb.Elements[1]);
            Assert.IsInstanceOfType<LineComment>(sb.Elements[2]);
            Assert.IsInstanceOfType<Whitespace>(sb.Elements[3]);
            Assert.IsInstanceOfType<BlockComment>(sb.Elements[4]);
        }

        #endregion

        #region Atomic Rule

        [TestMethod]
        public void TryParseArgument_Tests()
        {
            var metaContext = MetaContext.Builder.NewBuilder().Build();

            // args
            var success = GrammarParser.TryParseArgument(
                "arg-name",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var argPair = result.Resolve();
            Assert.AreEqual("arg-name", argPair.Argument.ToString());
            Assert.IsNull(argPair.Value);

            // args / value
            success = GrammarParser.TryParseArgument(
                "arg-name :'value'",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            argPair = result.Resolve();
            Assert.AreEqual("arg-name", argPair.Argument.ToString());
            Assert.AreEqual("value", argPair.Value);

            // args / value
            success = GrammarParser.TryParseArgument(
                "arg-name : 'value2'",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            argPair = result.Resolve();
            Assert.AreEqual("arg-name", argPair.Argument.ToString());
            Assert.AreEqual("value2", argPair.Value);
        }

        [TestMethod]
        public void TryParseAtomicRuleArguments_Tests()
        {
            var metaContext = MetaContext.Builder.NewBuilder().Build();

            // args
            var success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name}",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var args = result.Resolve();
            Assert.AreEqual(1, args.Length);

            // args / value
            success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name :'value', arg-2-flag, arg-3:'bleh'}",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            args = result.Resolve();
            Assert.AreEqual(3, args.Length);
        }

        [TestMethod]
        public void TryParseDelimitedContent_Tests()
        {

        }
        #endregion
    }
}
