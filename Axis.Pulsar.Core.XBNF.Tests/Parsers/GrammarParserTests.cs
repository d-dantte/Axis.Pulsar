using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Composite;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Lang;
using Axis.Pulsar.Core.XBNF.Parsers;
using Axis.Pulsar.Core.XBNF.Parsers.Models;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.XBNF.Tests.Parsers
{
    [TestClass]
    public class GrammarParserTests
    {
        #region Silent elements

        [TestMethod]
        public void TryParseWhtespsace_Tests()
        {            
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));
            var parentPath = SymbolPath.Of("parent");

            // new line
            var success = GrammarParser.TryParseWhitespace(
                "\n",
                parentPath,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Whitespace ws));
            Assert.AreEqual(Whitespace.WhitespaceChar.LineFeed, ws.Char);

            // carriage return
            success = GrammarParser.TryParseWhitespace(
                "\r",
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ws));
            Assert.AreEqual(Whitespace.WhitespaceChar.CarriageReturn, ws.Char);

            // tab
            success = GrammarParser.TryParseWhitespace(
                "\t",
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ws));
            Assert.AreEqual(Whitespace.WhitespaceChar.Tab, ws.Char);

            // space
            success = GrammarParser.TryParseWhitespace(
                " ",
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ws));
            Assert.AreEqual(Whitespace.WhitespaceChar.Space, ws.Char);

            // any other char
            success = GrammarParser.TryParseWhitespace(
                "\v",
                parentPath,
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out FailedRecognitionError ume));

        }

        [TestMethod]
        public void TryParseLineComment_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));
            var parentPath = SymbolPath.Of("parent");

            // comment
            var comment = "# and stuff that doesn't end in a new line";
            var success = GrammarParser.TryParseLineComment(
                comment,
                parentPath,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out LineComment lc));
            Assert.AreEqual(comment[1..], lc.Content.ToString());

            // comment with new line
            comment = "# and stuff that ends in a new line\nOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out lc));
            Assert.AreEqual(comment[1..35], lc.Content.ToString());

            // comment with carriage return
            comment = "# and stuff that ends in a new line\rOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out lc));
            Assert.AreEqual(comment[1..35], lc.Content.ToString());

            // comment with windows new-line
            comment = "# and stuff that ends in a new line\r\nOther stuff not part of the comment";
            success = GrammarParser.TryParseLineComment(
                comment,
                parentPath,
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out lc));
            Assert.AreEqual(comment[1..35], lc.Content.ToString());
        }

        [TestMethod]
        public void TryParseBlockComment_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            // same-line block comment
            TokenReader comment = "/* and stuff that doesn't end * in a new line */";
            var success = GrammarParser.TryParseBlockComment(
                comment,
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out BlockComment bc));
            Assert.IsTrue(comment.IsConsumed);
            Assert.AreEqual(comment.Source[2..^2], bc.Content.ToString());

            // same-line block comment
            comment = "/* and stuff\n that \rdoesn't \r\nend * \n\rin a new line \n*/";
            success = GrammarParser.TryParseBlockComment(
                comment,
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out bc));
            Assert.IsTrue(comment.IsConsumed);
            Assert.AreEqual(comment.Source[2..^2], bc.Content.ToString());
        }

        [TestMethod]
        public void TryParseSilentBlock_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));
            TokenReader block = " some text after a space";
            var success = GrammarParser.TryParseSilentBlock(
                block,
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out SilentBlock sb));
            Assert.AreEqual(1, sb.Elements.Length);
            Assert.AreEqual(" ", sb.Elements[0].Content.ToString());


            block = " \n# comment\r\nsome text after a space";
            success = GrammarParser.TryParseSilentBlock(
                block,
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out sb));
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
            Assert.IsTrue(result.Is(out sb));
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
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            // args
            var success = GrammarParser.TryParseArgument(
                "arg-name",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Parameter param));
            Assert.AreEqual("arg-name", param.Argument.ToString());
            Assert.IsNull(param.Value);

            // args / value
            success = GrammarParser.TryParseArgument(
                "arg-name :'value'",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out param));
            Assert.AreEqual("arg-name", param.Argument.ToString());
            Assert.AreEqual("value", param.Value);

            // args / value
            success = GrammarParser.TryParseArgument(
                "arg-name : 'value2'",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out param));
            Assert.AreEqual("arg-name", param.Argument.ToString());
            Assert.AreEqual("value2", param.Value);

            // args / bool
            success = GrammarParser.TryParseArgument(
                "arg-name : true",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out param));
            Assert.AreEqual("arg-name", param.Argument.ToString());
            Assert.AreEqual("True", param.Value);

            // args / number
            success = GrammarParser.TryParseArgument(
                "arg-name : 34",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out param));
            Assert.AreEqual("arg-name", param.Argument.ToString());
            Assert.AreEqual("34", param.Value);


            success = GrammarParser.TryParseArgument(
                "arg-name : 34.54",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out param));
            Assert.AreEqual("arg-name", param.Argument.ToString());
            Assert.AreEqual("34.54", param.Value);
        }

        [TestMethod]
        public void TryParseAtomicRuleArguments_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            // args
            var success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name}",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Parameter[] args));
            Assert.AreEqual(1, args.Length);

            // args / value
            success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name :'value', arg-2-flag, arg-3:'bleh'\n#abcd\n/*bleh */}",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out args));
            Assert.AreEqual(3, args.Length);

            // args / value
            success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name :'value', arg-2-flag, arg-3:'bleh', arg-4:'bleh' + \r\n ' bleh' \n#abcd\n/*bleh */}",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out args));
            Assert.AreEqual(4, args.Length);

            // args / value
            success = GrammarParser.TryParseAtomicRuleArguments(
                "{arg-name :'value\\''}",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out args));
            Assert.AreEqual(1, args.Length);
            Assert.AreEqual("value'", args[0].Value);
        }

        [TestMethod]
        public void TryParseDelimitedContent_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            // double quote
            var tryParse = GrammarParser.DelimitedContentParserDelegate('"', '"');
            var success = tryParse(
                "\"the content\\\"\"",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out string info));
            Assert.IsTrue(info.Equals("the content\\\""));


            // quote
            tryParse = GrammarParser.DelimitedContentParserDelegate('\'', '\'');
            success = tryParse(
                "'the content,' + ' and its concatenation'",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out info));
            Assert.IsTrue(info.Equals("the content, and its concatenation"));


            // quote
            tryParse = GrammarParser.DelimitedContentParserDelegate('\'', '\'');
            success = tryParse(
                "'the content,' + ' and its concatenation' and stuff behind that should't parse",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out info));
            Assert.IsTrue(info.Equals("the content, and its concatenation"));
        }

        [TestMethod]
        public void TryParseAtomicContent_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            // quote
            var success = GrammarParser.TryParseAtomicContent(
                "'the content\\''",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Parameter info));
            Assert.AreEqual(ContentArgumentDelimiter.Quote, info.Argument.As<ContentArgument>().Delimiter);
            Assert.IsTrue(info.Value!.Equals("the content\'"));

            // double quote
            success = GrammarParser.TryParseAtomicContent(
                "\"the content\\\"\"",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out info));
            Assert.AreEqual(ContentArgumentDelimiter.DoubleQuote, info.Argument.As<ContentArgument>().Delimiter);
            Assert.IsTrue(info.Value!.Equals("the content\\\""));

            // grave
            success = GrammarParser.TryParseAtomicContent(
                "`the content\\``",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out info));
            Assert.AreEqual(ContentArgumentDelimiter.Grave, info.Argument.As<ContentArgument>().Delimiter);
            Assert.IsTrue(info.Value!.Equals("the content\\`"));

            // sol
            success = GrammarParser.TryParseAtomicContent(
                "/the content\\//",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out info));
            Assert.AreEqual(ContentArgumentDelimiter.Sol, info.Argument.As<ContentArgument>().Delimiter);
            Assert.IsTrue(info.Value!.Equals("the content\\/"));

            // back-sol
            success = GrammarParser.TryParseAtomicContent(
                "\\the content\\\\\\",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out info));
            Assert.AreEqual(ContentArgumentDelimiter.BackSol, info.Argument.As<ContentArgument>().Delimiter);
            Assert.IsTrue(info.Value!.Equals("the content\\\\"));

            // vertical bar
            success = GrammarParser.TryParseAtomicContent(
                "|the content\\||",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out info));
            Assert.AreEqual(ContentArgumentDelimiter.VerticalBar, info.Argument.As<ContentArgument>().Delimiter);
            Assert.IsTrue(info.Value!.Equals("the content\\|"));
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
                .Build()
                .ApplyTo(x => new ParserContext(x));

            #region NL
            var success = GrammarParser.TryParseAtomicRule(
                "@nl",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out IAtomicRule rule));
            Assert.IsInstanceOfType<WindowsNewLine>(rule);

            success = GrammarParser.TryParseAtomicRule(
                "@nl{definitely, ignored: 'arguments'}",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out rule));
            Assert.IsInstanceOfType<WindowsNewLine>(rule);
            #endregion

            #region literal
            success = GrammarParser.TryParseAtomicRule(
                "\"literal\"",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out rule));
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
            Assert.IsTrue(result.Is(out rule));
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
            Assert.IsTrue(result.Is(out rule));
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
            Assert.IsTrue(result.Is(out rule));
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
            Assert.IsTrue(result.Is(out rule));
            pattern = rule.As<TerminalPattern>();
            Assert.AreEqual(IMatchType.Of(1, 1), pattern.MatchType);
            Assert.AreEqual("the pattern", pattern.Pattern.ToString());

            success = GrammarParser.TryParseAtomicRule(
                "/the pattern/{match-type: '1,+'}",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out rule));
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
            Assert.IsTrue(result.Is(out rule));
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
            Assert.IsTrue(result.Is(out rule));
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
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseRecognitionThreshold(
                ":2 ",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out uint threshold));
            Assert.AreEqual(2u, threshold);

            success = GrammarParser.TryParseRecognitionThreshold(
                ";2 ",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            success = GrammarParser.TryParseRecognitionThreshold(
                ":x2 ",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }


        [TestMethod]
        public void TryCardinality_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseCardinality(
                ".1",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Cardinality cardinality));
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".3,",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out cardinality));
            Assert.AreEqual(Cardinality.OccursAtLeast(3), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".1,2",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out cardinality));
            Assert.AreEqual(Cardinality.Occurs(1, 2), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".*",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out cardinality));
            Assert.AreEqual(Cardinality.OccursNeverOrMore(), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".?",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out cardinality));
            Assert.AreEqual(Cardinality.OccursOptionally(), cardinality);

            success = GrammarParser.TryParseCardinality(
                ".+",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out cardinality));
            Assert.AreEqual(Cardinality.OccursAtLeastOnce(), cardinality);
        }


        [TestMethod]
        public void TryProductionRef_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseProductionRef(
                "$symbol.?",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ProductionRef @ref));
            Assert.AreEqual("symbol", @ref.Ref);
            Assert.AreEqual(Cardinality.OccursOptionally(), @ref.Cardinality);

            success = GrammarParser.TryParseProductionRef(
                "$symbol",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out @ref));
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
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseAtomicRuleRef(
                "@nl.?",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out AtomicRuleRef @ref));
            Assert.AreEqual(Cardinality.OccursOptionally(), @ref.Cardinality);
            Assert.IsInstanceOfType<WindowsNewLine>(@ref.Ref);

            success = GrammarParser.TryParseAtomicRuleRef(
                "/\\\\d/{flags:'i'}.2",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out @ref));
            Assert.AreEqual(Cardinality.OccursOnly(2), @ref.Cardinality);
            Assert.IsInstanceOfType<TerminalPattern>(@ref.Ref);

            success = GrammarParser.TryParseAtomicRuleRef(
                "'+, \\x2d'",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out @ref));
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), @ref.Cardinality);
            Assert.IsInstanceOfType<CharacterRanges>(@ref.Ref);

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
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseGroupElement(
                "@nl",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out IGroupRule element));
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), element.Cardinality);
            Assert.IsInstanceOfType<AtomicRuleRef>(element);

            success = GrammarParser.TryParseGroupElement(
                "$stuff",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out element));
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
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseElementList(
                "[$stuff '^a-z'.3 ]",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out IGroupRule[] options));
            Assert.AreEqual(2, options.Length);


            success = GrammarParser.TryParseElementList(
                "[ @EOF]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out options));
            Assert.AreEqual(1, options.Length);


            success = GrammarParser.TryParseElementList(
                "[ $a $b $c   \t\t\n\r\n]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out options));
            Assert.AreEqual(3, options.Length);


            success = GrammarParser.TryParseElementList(
                "[  ]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out IGroupRule[] elements));
            Assert.AreEqual(0, elements.Length);
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
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseChoice(
                "?[ $stuff $other-stuff ].1,5",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Choice choice));
            Assert.AreEqual(2, choice.Elements.Length);
            Assert.AreEqual(Cardinality.Occurs(1, 5), choice.Cardinality);

            success = GrammarParser.TryParseChoice(
                @"?[ ""Z"" + ""T""
        +[ 'x, y' ""bleh""] ].1,5",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out choice));
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
                .Build()
                .ApplyTo(x => new ParserContext(x));


            var success = GrammarParser.TryParseSequence(
                "+[ $stuff $other-stuff ].1,5",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Sequence sequence));
            Assert.AreEqual(2, sequence.Elements.Length);
            Assert.AreEqual(Cardinality.Occurs(1, 5), sequence.Cardinality);


            success = GrammarParser.TryParseSequence(
                "+[ $stuff $other-stuff ]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out sequence));
            Assert.AreEqual(2, sequence.Elements.Length);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), sequence.Cardinality);


            success = GrammarParser.TryParseSequence(
                @"+[
    '+, \x2d' ]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out sequence));
            Assert.AreEqual(1, sequence.Elements.Length);
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
                .Build()
                .ApplyTo(x => new ParserContext(x));


            var success = GrammarParser.TryParseSet(
                "#[ $stuff $other-stuff ]",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Set set));
            Assert.AreEqual(2, set.Elements.Length);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), set.Cardinality);


            success = GrammarParser.TryParseSet(
                "#5[ $stuff $other-stuff ]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out set));
            Assert.AreEqual(2, set.Elements.Length);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), set.Cardinality);
            Assert.AreEqual(5, set.MinRecognitionCount);
        }

        [TestMethod]
        public void TryParseGroup_Test()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseGroup(
                "#[$tuff]",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out IGroup group));
            Assert.IsInstanceOfType<Set>(group);

            success = GrammarParser.TryParseGroup(
                "+[$tuff]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out group));
            Assert.IsInstanceOfType<Sequence>(group);

            success = GrammarParser.TryParseGroup(
                "?[$tuff]",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out group));
            Assert.IsInstanceOfType<Choice>(group);

            metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build()
                .ApplyTo(x => new ParserContext(x));
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
            Assert.IsTrue(result.Is(out group));
            Assert.IsInstanceOfType<Sequence>(group);
        }

        [TestMethod]
        public void TryParseCompositeRule_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseCompositeRule(
                "#[$tuff ?[$other-stuff $more-stuff].?]",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ICompositeRule rule));
            Assert.IsInstanceOfType<NonTerminal>(rule);
        }

        #endregion

        #region Production

        [TestMethod]
        public void TryParseMapOperator_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseMapOperator(
                "->",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Tokens opTokens));
            Assert.IsTrue(opTokens.Equals("->"));
        }

        [TestMethod]
        public void TryParseAtomicSymbolName_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseAtomicSymbolName(
                "@name",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out string name));
            Assert.AreEqual("name", name);

            success = GrammarParser.TryParseAtomicSymbolName(
                "@name-with-a-dash",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out name));
            Assert.AreEqual("name-with-a-dash", name);

            success = GrammarParser.TryParseAtomicSymbolName(
                "@name-with-1234-numbers-and-dash-",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out name));
            Assert.AreEqual("name-with-1234-numbers-and-dash-", name);

            success = GrammarParser.TryParseAtomicSymbolName(
                "@-name",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);

            success = GrammarParser.TryParseAtomicSymbolName(
                "@7name",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
        }

        [TestMethod]
        public void TryParseCompositeSymbolName_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseCompositeSymbolName(
                "$name",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out string name));
            Assert.AreEqual("name", name);

            success = GrammarParser.TryParseCompositeSymbolName(
                "$name-with-a-dash",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out name));
            Assert.AreEqual("name-with-a-dash", name);

            success = GrammarParser.TryParseCompositeSymbolName(
                "$name-with-1234-numbers-and-dash-",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out name));
            Assert.AreEqual("name-with-1234-numbers-and-dash-", name);

            success = GrammarParser.TryParseCompositeSymbolName(
                "$-name",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);

            success = GrammarParser.TryParseCompositeSymbolName(
                "$7name",
                "parent",
                metaContext,
                out result);

            Assert.IsFalse(success);
        }

        [TestMethod]
        public void TryParseProduction_Tests()
        {
            var metaContext = MetaContextBuilder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build()
                .ApplyTo(x => new ParserContext(x));

            var success = GrammarParser.TryParseProduction(
                "$name -> +[$stuff $other-stuff ?[$much-more $options].2 'a-z']",
                "parent",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out Production production));
            Assert.IsInstanceOfType<ICompositeRule>(production.Rule);

            success = GrammarParser.TryParseProduction(
                "$name -> 'a-z'",
                "parent",
                metaContext,
                out result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out production));
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
                .Build()
                .ApplyTo(x => new ParserContext(x));

            using var langDefStream = ResourceLoader.Load("SampleGrammar.Int1.xbnf");
            var langText = new StreamReader(langDefStream!).ReadToEnd();
            

            var success = GrammarParser.TryParseGrammar(
                langText,
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out IGrammar grammar));
            Assert.AreEqual(5, grammar.ProductionCount);
            Assert.AreEqual("int", grammar.Root);
        }
        #endregion

        #region Nested types
        internal class WindowsNewLine : IAtomicRule
        {
            public string Id { get; set; } = string.Empty;

            public bool TryRecognize(TokenReader reader, SymbolPath productionPath, ILanguageContext context, out NodeRecognitionResult result)
            {
                var position = reader.Position;

                if (!reader.TryGetToken(out var r)
                    || '\r' != r[0])
                {
                    reader.Reset(position);
                    result = NodeRecognitionResult.Of(FailedRecognitionError.Of(productionPath, position));
                    return false;
                }

                if (!reader.TryGetToken(out var n)
                    || '\n' != n[0])
                {
                    reader.Reset(position);
                    result = NodeRecognitionResult.Of(PartialRecognitionError.Of(
                        productionPath,
                        position,
                        r.Segment.Count));
                    return false;
                }

                result = NodeRecognitionResult.Of(ICSTNode.Of(productionPath.Symbol, r + n));
                return true;
            }
        }

        internal class WindowsNewLineFactory : IAtomicRuleFactory
        {
            public IAtomicRule NewRule(
                string id,
                LanguageMetadata context,
                ImmutableDictionary<IAtomicRuleFactory.IArgument, string> arguments)
            {
                return new WindowsNewLine { Id = id };
            }
        }
        #endregion
    }
}
