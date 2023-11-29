using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Parsers;
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
            var metaContext = MetaContextBuilder.NewBuilder().Build();
            var parentPath = ProductionPath.Of("parent");

            // new line
            var success = GrammarParser.TryParseWhitespace(
                "\n",
                parentPath,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.LineFeed, ws.Char);

            // carriage return
            success = GrammarParser.TryParseWhitespace(
                "\r",
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.CarriageReturn, ws.Char);

            // tab
            success = GrammarParser.TryParseWhitespace(
                "\t",
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.Tab, ws.Char);

            // space
            success = GrammarParser.TryParseWhitespace(
                " ",
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.Space, ws.Char);

            // any other char
            success = GrammarParser.TryParseWhitespace(
                "\v",
                parentPath,
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError ume));

        }

        [TestMethod]
        public void TryParseLineComment_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();
            var parentPath = ProductionPath.Of("parent");

            // comment
            var comment = "# and stuff that doesn't end in a new line";
            var success = GrammarParser.TryParseLineComment(
                comment,
                parentPath,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var lc = result.Resolve();
            Assert.AreEqual(comment[1..], lc.Content.ToString());

            // comment with new line
            comment = "# and stuff that ends in a new line\nOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            lc = result.Resolve();
            Assert.AreEqual(comment[1..35], lc.Content.ToString());

            // comment with carriage return
            comment = "# and stuff that ends in a new line\rOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            lc = result.Resolve();
            Assert.AreEqual(comment[1..35], lc.Content.ToString());

            // comment with windows new-line
            comment = "# and stuff that ends in a new line\r\nOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            lc = result.Resolve();
            Assert.AreEqual(comment[1..35], lc.Content.ToString());
        }

        [TestMethod]
        public void TryParseBlockComment_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            // same-line block comment
            TokenReader comment = "/* and stuff that doesn't end * in a new line */";
            var success = GrammarParser.TryParseBlockComment(
                comment,
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsTrue(comment.IsConsumed);
            var bc = result.Resolve();
            Assert.AreEqual(comment.Source[2..^2], bc.Content.ToString());

            // same-line block comment
            comment = "/* and stuff\n that \rdoesn't \r\nend * \n\rin a new line \n*/";
            success = GrammarParser.TryParseBlockComment(
                comment,
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsTrue(comment.IsConsumed);
            bc = result.Resolve();
            Assert.AreEqual(comment.Source[2..^2], bc.Content.ToString());
        }

        [TestMethod]
        public void TryParseSilentBlock_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();
            TokenReader block = " some text after a space";
            var success = GrammarParser.TryParseSilentBlock(
                block,
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var sb = result.Resolve();
            Assert.AreEqual(1, sb.Elements.Length);
            Assert.AreEqual(" ", sb.Elements[0].Content.ToString());


            block = " \n# comment\r\nsome text after a space";
            success = GrammarParser.TryParseSilentBlock(
                block,
                "parent",
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
                "parent",
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
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            // args
            var success = GrammarParser.TryParseArgument(
                "arg-name",
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            argPair = result.Resolve();
            Assert.AreEqual("arg-name", argPair.Argument.ToString());
            Assert.AreEqual("34", argPair.Value);


            success = GrammarParser.TryParseArgument(
                "arg-name : 34.54",
                "parent",
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
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            // args
            var success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name}",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var args = result.Resolve();
            Assert.AreEqual(1, args.Length);

            // args / value
            success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name :'value', arg-2-flag, arg-3:'bleh'\n#abcd\n/*bleh */}",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            args = result.Resolve();
            Assert.AreEqual(3, args.Length);

            // args / value
            success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name :'value', arg-2-flag, arg-3:'bleh', arg-4:'bleh' + \r\n ' bleh' \n#abcd\n/*bleh */}",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            args = result.Resolve();
            Assert.AreEqual(4, args.Length);
        }

        [TestMethod]
        public void TryParseDelimitedContent_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            // double quote
            var tryParse = GrammarParser.DelimitedContentParserDelegate('"', '"');
            var success = tryParse(
                "\"the content\\\"\"",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var info = result.Resolve();
            Assert.IsTrue(info.Equals("the content\\\""));


            // quote
            tryParse = GrammarParser.DelimitedContentParserDelegate('\'', '\'');
            success = tryParse(
                "'the content,' + ' and its concatenation'",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            info = result.Resolve();
            Assert.IsTrue(info.Equals("the content, and its concatenation"));


            // quote
            tryParse = GrammarParser.DelimitedContentParserDelegate('\'', '\'');
            success = tryParse(
                "'the content,' + ' and its concatenation' and stuff behind that should't parse",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            info = result.Resolve();
            Assert.IsTrue(info.Equals("the content, and its concatenation"));
        }

        [TestMethod]
        public void TryParseAtomicContent_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            // quote
            var success = GrammarParser.TryParseAtomicContent(
                "'the content\\''",
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
            var metaContext = MetaContextBuilder
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
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var rule = result.Resolve();
            Assert.IsInstanceOfType<WindowsNewLine>(rule);

            success = GrammarParser.TryParseAtomicRule(
                "@nl{definitely, ignored: 'arguments'}",
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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
                "parent",
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

        [TestMethod]
        public void TryParseRecognitionThreshold_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            var success = GrammarParser.TryParseRecognitionThreshold(
                ":2 ",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var threshold = result.Resolve();
            Assert.AreEqual(2u, threshold);

            success = GrammarParser.TryParseRecognitionThreshold(
                ";2 ",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError _));

            success = GrammarParser.TryParseRecognitionThreshold(
                ":x2 ",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out PartialRecognitionError _));

        }


        [TestMethod]
        public void TryCardinality_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            var success = GrammarParser.TryParseCardinality(
                ".1",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var cardinality = result.Resolve();
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".3,",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            cardinality = result.Resolve();
            Assert.AreEqual(Cardinality.OccursAtLeast(3), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".1,2",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            cardinality = result.Resolve();
            Assert.AreEqual(Cardinality.Occurs(1, 2), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".*",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            cardinality = result.Resolve();
            Assert.AreEqual(Cardinality.OccursNeverOrMore(), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".?",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            cardinality = result.Resolve();
            Assert.AreEqual(Cardinality.OccursOptionally(), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".+",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            cardinality = result.Resolve();
            Assert.AreEqual(Cardinality.OccursAtLeastOnce(), cardinality);

        }


        [TestMethod]
        public void TryProductionRef_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            var success = GrammarParser.TryParseProductionRef(
                "$symbol.?",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var @ref = result.Resolve();
            Assert.AreEqual("symbol", @ref.Ref);
            Assert.AreEqual(Cardinality.OccursOptionally(), @ref.Cardinality);

            success = GrammarParser.TryParseProductionRef(
                "$symbol",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            @ref = result.Resolve();
            Assert.AreEqual("symbol", @ref.Ref);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), @ref.Cardinality);
        }


        [TestMethod]
        public void TryAtomicRef_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .WithAtomicRuleDefinition(AtomicRuleDefinition.Of(
                    "nl",
                    new WindowsNewLineFactory()))
                .Build();

            var success = GrammarParser.TryParseAtomicRuleRef(
                "@nl.?",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var @ref = result.Resolve();
            Assert.AreEqual(Cardinality.OccursOptionally(), @ref.Cardinality);
            Assert.IsInstanceOfType<WindowsNewLine>(@ref.Ref);

            success = GrammarParser.TryParseAtomicRuleRef(
                "/\\\\d/{flags:'i'}.2",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            @ref = result.Resolve();
            Assert.AreEqual(Cardinality.OccursOnly(2), @ref.Cardinality);
            Assert.IsInstanceOfType<TerminalPattern>(@ref.Ref);

        }

        [TestMethod]
        public void TryGroupElement_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .WithAtomicRuleDefinition(AtomicRuleDefinition.Of(
                    "nl",
                    new WindowsNewLineFactory()))
                .Build();

            var success = GrammarParser.TryParseGroupElement(
                "@nl",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var element = result.Resolve();
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), element.Cardinality);
            Assert.IsInstanceOfType<AtomicRuleRef>(element);

            success = GrammarParser.TryParseGroupElement(
                "$stuff",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            element = result.Resolve();
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), element.Cardinality);
            Assert.IsInstanceOfType<ProductionRef>(element);
        }

        [TestMethod]
        public void TryParseElementList_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .WithAtomicRuleDefinition(AtomicRuleDefinition.Of(
                    "nl",
                    new WindowsNewLineFactory()))
                .Build();

            var success = GrammarParser.TryParseElementList(
                "[$stuff '^a-z'.3 ]",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var options = result.Resolve();
            Assert.AreEqual(2, options.Length);


            success = GrammarParser.TryParseElementList(
                "[ @EOF]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            options = result.Resolve();
            Assert.AreEqual(1, options.Length);


            success = GrammarParser.TryParseElementList(
                "[ $a $b $c   \t\t\n\r\n]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            options = result.Resolve();
            Assert.AreEqual(3, options.Length);


            success = GrammarParser.TryParseElementList(
                "[  ]",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out PartialRecognitionError fme));
        }

        [TestMethod]
        public void TryParseChoice_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .WithAtomicRuleDefinition(AtomicRuleDefinition.Of(
                    "nl",
                    new WindowsNewLineFactory()))
                .Build();


            var success = GrammarParser.TryParseChoice(
                "?[ $stuff $other-stuff ].1,5",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var choice = result.Resolve();
            Assert.AreEqual(2, choice.Elements.Length);
            Assert.AreEqual(Cardinality.Occurs(1, 5), choice.Cardinality);
        }

        [TestMethod]
        public void TryParseSequence_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .WithAtomicRuleDefinition(AtomicRuleDefinition.Of(
                    "nl",
                    new WindowsNewLineFactory()))
                .Build();


            var success = GrammarParser.TryParseSequence(
                "+[ $stuff $other-stuff ].1,5",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var sequence = result.Resolve();
            Assert.AreEqual(2, sequence.Elements.Length);
            Assert.AreEqual(Cardinality.Occurs(1, 5), sequence.Cardinality);


            success = GrammarParser.TryParseSequence(
                "+[ $stuff $other-stuff ]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            sequence = result.Resolve();
            Assert.AreEqual(2, sequence.Elements.Length);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), sequence.Cardinality);
        }

        [TestMethod]
        public void TryParseSet_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .WithAtomicRuleDefinition(AtomicRuleDefinition.Of(
                    "nl",
                    new WindowsNewLineFactory()))
                .Build();


            var success = GrammarParser.TryParseSet(
                "#[ $stuff $other-stuff ]",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var set = result.Resolve();
            Assert.AreEqual(2, set.Elements.Length);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), set.Cardinality);


            success = GrammarParser.TryParseSet(
                "#5[ $stuff $other-stuff ]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            set = result.Resolve();
            Assert.AreEqual(2, set.Elements.Length);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), set.Cardinality);
            Assert.AreEqual(5, set.MinRecognitionCount);
        }

        [TestMethod]
        public void TryParseGroup_Test()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build();

            var success = GrammarParser.TryParseGroup(
                "#[$tuff]",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsInstanceOfType<Set>(result.Resolve());

            success = GrammarParser.TryParseGroup(
                "+[$tuff]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsInstanceOfType<Sequence>(result.Resolve());

            success = GrammarParser.TryParseGroup(
                "?[$tuff]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsInstanceOfType<Choice>(result.Resolve());

            metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build();
            success = GrammarParser.TryParseGroup(
                context: metaContext,
                result: out result,
                path: "parent",
                reader: @"+[
	'{'
	$block-space.?
	$property.?
	+[
		$block-space.?
		'\,'
		$block-space.?
		$property
	].?
	$block-space.?
	'}'
]");

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsInstanceOfType<Sequence>(result.Resolve());
        }

        [TestMethod]
        public void TryParseCompositeRule_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            var success = GrammarParser.TryParseCompositeRule(
                "#[$tuff ?[$other-stuff $more-stuff].?]",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.IsInstanceOfType<NonTerminal>(result.Resolve());
        }

        #endregion

        #region Production

        [TestMethod]
        public void TryParseMapOperator_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            var success = GrammarParser.TryParseMapOperator(
                "->",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var opTokens = result.Resolve();
            Assert.IsTrue(opTokens.Equals("->"));
        }

        [TestMethod]
        public void TryParseAtomicSymbolName_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            var success = GrammarParser.TryParseAtomicSymbolName(
                "@name",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var name = result.Resolve();
            Assert.AreEqual("name", name);

            success = GrammarParser.TryParseAtomicSymbolName(
                "@name-with-a-dash",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            name = result.Resolve();
            Assert.AreEqual("name-with-a-dash", name);

            success = GrammarParser.TryParseAtomicSymbolName(
                "@name-with-1234-numbers-and-dash-",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            name = result.Resolve();
            Assert.AreEqual("name-with-1234-numbers-and-dash-", name);

            success = GrammarParser.TryParseAtomicSymbolName(
                "@-name",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());

            success = GrammarParser.TryParseAtomicSymbolName(
                "@7name",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());
        }

        [TestMethod]
        public void TryParseCompositeSymbolName_Tests()
        {
            var metaContext = MetaContextBuilder.NewBuilder().Build();

            var success = GrammarParser.TryParseCompositeSymbolName(
                "$name",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var name = result.Resolve();
            Assert.AreEqual("name", name);

            success = GrammarParser.TryParseCompositeSymbolName(
                "$name-with-a-dash",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            name = result.Resolve();
            Assert.AreEqual("name-with-a-dash", name);

            success = GrammarParser.TryParseCompositeSymbolName(
                "$name-with-1234-numbers-and-dash-",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            name = result.Resolve();
            Assert.AreEqual("name-with-1234-numbers-and-dash-", name);

            success = GrammarParser.TryParseCompositeSymbolName(
                "$-name",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());

            success = GrammarParser.TryParseCompositeSymbolName(
                "$7name",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());
        }

        [TestMethod]
        public void TryParseProduction_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build();

            var success = GrammarParser.TryParseProduction(
                "$name -> +[$stuff $other-stuff ?[$much-more $options].2 'a-z']",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var production = result.Resolve();
            Assert.IsInstanceOfType<ICompositeRule>(production.Rule);

            success = GrammarParser.TryParseProduction(
                "$name -> 'a-z'",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            production = result.Resolve();
            Assert.IsInstanceOfType<ICompositeRule>(production.Rule);
            var nt = production.Rule as NonTerminal;
            Assert.IsInstanceOfType<AtomicRuleRef>(nt!.Element);
        }

        [TestMethod]
        public void TryParseGrammar_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build();

            using var langDefStream = ResourceLoader.Load("SampleGrammar.Int1.xbnf");
            var langText = new StreamReader(langDefStream!).ReadToEnd();
            

            var success = GrammarParser.TryParseGrammar(
                langText,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var grammar = result.Resolve();
            Assert.AreEqual(5, grammar.ProductionCount);
            Assert.AreEqual("int", grammar.Root);
        }
        #endregion


        #region Nested types
        internal class WindowsNewLine : IAtomicRule
        {
            public string Id { get; set; } = string.Empty;

            public bool TryRecognize(TokenReader reader, ProductionPath productionPath, ILanguageContext context, out IResult<ICSTNode> result)
            {
                var position = reader.Position;

                if (!reader.TryGetToken(out var r)
                    || '\r' != r[0])
                {
                    reader.Reset(position);
                    result = Result.Of<ICSTNode>(FailedRecognitionError.Of(productionPath, position));
                    return false;
                }

                if (!reader.TryGetToken(out var n)
                    || '\n' != n[0])
                {
                    reader.Reset(position);
                    result = Result.Of<ICSTNode>(PartialRecognitionError.Of(productionPath, position, r.SourceSegment.Length));
                    return false;
                }

                result = Result.Of(ICSTNode.Of(productionPath.Name, r + n));
                return true;
            }
        }

        internal class WindowsNewLineFactory : IAtomicRuleFactory
        {
            public IAtomicRule NewRule(
                string id,
                MetaContext context,
                ImmutableDictionary<IAtomicRuleFactory.Argument, string> arguments)
            {
                return new WindowsNewLine { Id = id };
            }
        }
        #endregion
    }
}
