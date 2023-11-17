using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Parsers.Models;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

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

            // args / bool
            success = GrammarParser.TryParseArgument(
                "arg-name : true",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            argPair = result.Resolve();
            Assert.AreEqual("arg-name", argPair.Argument.ToString());
            Assert.AreEqual("True", argPair.Value);

            // args / number
            success = GrammarParser.TryParseArgument(
                "arg-name : 34",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            argPair = result.Resolve();
            Assert.AreEqual("arg-name", argPair.Argument.ToString());
            Assert.AreEqual("34", argPair.Value);


            success = GrammarParser.TryParseArgument(
                "arg-name : 34.54",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            argPair = result.Resolve();
            Assert.AreEqual("arg-name", argPair.Argument.ToString());
            Assert.AreEqual("34.54", argPair.Value);
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
                "{arg-name :'value', arg-2-flag, arg-3:'bleh'\n#abcd\n/*bleh */}",
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
            var metaContext = MetaContext.Builder.NewBuilder().Build();

            // quote
            var success = GrammarParser.TryParseDelimitedContent(
                "'the content\\''",
                metaContext,
                '\'',
                '\'',
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var info = result.Resolve();
            Assert.IsTrue(info.Equals("the content\\'"));
        }

        [TestMethod]
        public void TryParseAtomicContent_Tests()
        {
            var metaContext = MetaContext.Builder.NewBuilder().Build();

            // quote
            var success = GrammarParser.TryParseAtomicContent(
                "'the content\\''",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var info = result.Resolve();
            Assert.AreEqual(AtomicContentDelimiterType.Quote, info.ContentType);
            Assert.IsTrue(info.Content.Equals("the content\\'"));

            // double quote
            success = GrammarParser.TryParseAtomicContent(
                "\"the content\\\"\"",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            info = result.Resolve();
            Assert.AreEqual(AtomicContentDelimiterType.DoubleQuote, info.ContentType);
            Assert.IsTrue(info.Content.Equals("the content\\\""));

            // grave
            success = GrammarParser.TryParseAtomicContent(
                "`the content\\``",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            info = result.Resolve();
            Assert.AreEqual(AtomicContentDelimiterType.Grave, info.ContentType);
            Assert.IsTrue(info.Content.Equals("the content\\`"));

            // sol
            success = GrammarParser.TryParseAtomicContent(
                "/the content\\//",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            info = result.Resolve();
            Assert.AreEqual(AtomicContentDelimiterType.Sol, info.ContentType);
            Assert.IsTrue(info.Content.Equals("the content\\/"));

            // back-sol
            success = GrammarParser.TryParseAtomicContent(
                "\\the content\\\\\\",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            info = result.Resolve();
            Assert.AreEqual(AtomicContentDelimiterType.BackSol, info.ContentType);
            Assert.IsTrue(info.Content.Equals("the content\\\\"));

            // vertical bar
            success = GrammarParser.TryParseAtomicContent(
                "|the content\\||",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            info = result.Resolve();
            Assert.AreEqual(AtomicContentDelimiterType.VerticalBar, info.ContentType);
            Assert.IsTrue(info.Content.Equals("the content\\|"));
        }

        [TestMethod]
        public void TryParseAtomicRule_Tests()
        {
            var metaContext = MetaContext.Builder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .WithAtomicRuleDefinition(AtomicRuleDefinition.Of(
                    "nl",
                    new WindowsNewLineFactory()))
                .WithAtomicRuleDefinition(AtomicRuleDefinition.Of(
                    "bleh",
                    new DelimitedStringRuleFactory()))
                .Build();

            #region NL
            var success = GrammarParser.TryParseAtomicRule(
                "@nl",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var rule = result.Resolve();
            Assert.IsInstanceOfType<WindowsNewLine>(rule);

            success = GrammarParser.TryParseAtomicRule(
                "@nl{definitely, ignored: 'arguments'}",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            Assert.IsInstanceOfType<WindowsNewLine>(rule);
            #endregion

            #region literal
            success = GrammarParser.TryParseAtomicRule(
                "\"literal\"",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            Assert.IsInstanceOfType<TerminalLiteral>(rule);
            var literal = rule.As<TerminalLiteral>();
            Assert.IsFalse(literal.IsCaseInsensitive);
            Assert.AreEqual("literal", literal.Tokens);

            success = GrammarParser.TryParseAtomicRule(
                "\"literal with falg\"{case-insensitive}",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            Assert.IsInstanceOfType<TerminalLiteral>(rule);
            literal = rule.As<TerminalLiteral>();
            Assert.IsTrue(literal.IsCaseInsensitive);
            Assert.AreEqual("literal with falg", literal.Tokens);
            #endregion

            #region Pattern
            success = GrammarParser.TryParseAtomicRule(
                "/the pattern/",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            Assert.IsInstanceOfType<TerminalPattern>(rule);
            var pattern = rule.As<TerminalPattern>();
            Assert.AreEqual(IMatchType.Of(1), pattern.MatchType);
            Assert.AreEqual("the pattern", pattern.Pattern.ToString());
            Assert.AreEqual(RegexOptions.Compiled, pattern.Pattern.Options);

            success = GrammarParser.TryParseAtomicRule(
                "/the pattern/{flags: 'ixcn'}",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            Assert.IsInstanceOfType<TerminalPattern>(rule);
            pattern = rule.As<TerminalPattern>();
            Assert.AreEqual(IMatchType.Of(1), pattern.MatchType);
            Assert.AreEqual("the pattern", pattern.Pattern.ToString());
            Assert.AreEqual(
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture |
                RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
                pattern.Pattern.Options);

            success = GrammarParser.TryParseAtomicRule(
                "/the pattern/{match-type: '1'}",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            pattern = rule.As<TerminalPattern>();
            Assert.AreEqual(IMatchType.Of(1, 1), pattern.MatchType);
            Assert.AreEqual("the pattern", pattern.Pattern.ToString());

            success = GrammarParser.TryParseAtomicRule(
                "/the pattern/{match-type: '1,+'}",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            pattern = rule.As<TerminalPattern>();
            Assert.AreEqual(IMatchType.Of(1, false), pattern.MatchType);
            Assert.AreEqual("the pattern", pattern.Pattern.ToString());
            #endregion

            #region char ranges
            success = GrammarParser.TryParseAtomicRule(
                "'a-d, p-s, ^., ^&, ^^'",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            Assert.IsInstanceOfType<CharacterRanges>(rule);
            var charRange = rule.As<CharacterRanges>();
            Assert.AreEqual(3, charRange.ExcludeList.Length);
            Assert.AreEqual(2, charRange.IncludeList.Length);
            #endregion

            #region delimited strings

            success = GrammarParser.TryParseAtomicRule(
                "@bleh{start: '(', end: ')'}",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            rule = result.Resolve();
            var dstring = rule.As<DelimitedString>();
            Assert.AreEqual("(", dstring.StartDelimiter);
            Assert.AreEqual(")", dstring.EndDelimiter);
            #endregion
        }
        #endregion

        #region Composite Rule

        #endregion

        #region Nested types
        internal class WindowsNewLine : IAtomicRule
        {
            public bool TryRecognize(TokenReader reader, ProductionPath productionPath, ILanguageContext context, out IResult<ICSTNode> result)
            {
                var position = reader.Position;

                if (!reader.TryGetToken(out var r)
                    || '\r' != r[0])
                {
                    reader.Reset(position);
                    result = Result.Of<ICSTNode>(UnrecognizedTokens.Of(productionPath, position));
                    return false;
                }

                if (!reader.TryGetToken(out var n)
                    || '\n' != n[0])
                {
                    reader.Reset(position);
                    result = Result.Of<ICSTNode>(PartiallyRecognizedTokens.Of(productionPath, position, r));
                    return false;
                }

                result = Result.Of(ICSTNode.Of(productionPath.Name, r + n));
                return true;
            }
        }

        internal class WindowsNewLineFactory : IAtomicRuleFactory
        {
            public IAtomicRule NewRule(MetaContext context, ImmutableDictionary<IAtomicRuleFactory.Argument, string> arguments)
            {
                return new WindowsNewLine();
            }
        }
        #endregion
    }
}
