using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Languages.Xml;
using Moq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Axis.Pulsar.Languages.IO.Tests.Xml
{
    [TestClass]
    public class ExporterTests
    {
        [TestMethod]
        public void ToCardinalityTests()
        {
            var exporter = new Exporter();
            var mockRepeatable = new Mock<IRepeatable>();

            // test {1, 1}
            mockRepeatable
                .Setup(r => r.Cardinality)
                .Returns(Cardinality.OccursOnly(1));
            var attributes = exporter
                .ToCardinalityAttributes(mockRepeatable.Object)
                .ToList();
            Assert.AreEqual(0, attributes.Count);

            // test {1, unbounded}
            mockRepeatable
                .Setup(r => r.Cardinality)
                .Returns(Cardinality.OccursAtLeast(1));
            attributes = exporter
                .ToCardinalityAttributes(mockRepeatable.Object)
                .ToList();
            Assert.AreEqual(1, attributes.Count);
            Assert.AreEqual("unbounded", attributes[0].Value);

            // test {2, 2}
            mockRepeatable
                .Setup(r => r.Cardinality)
                .Returns(Cardinality.OccursOnly(2));
            attributes = exporter
                .ToCardinalityAttributes(mockRepeatable.Object)
                .ToList();
            Assert.AreEqual(2, attributes.Count);
            Assert.AreEqual("2", attributes[0].Value);
            Assert.AreEqual("2", attributes[1].Value);
        }


        [TestMethod]
        public void ToRuleElementTests()
        {
            var exporter = new Exporter();

            #region literal
            var literal = new Grammar.Language.Rules.Literal("stuff");
            var xelt = exporter.ToRuleElement(literal);
            var lelt = xelt;
            var expected = new XElement(
                Legend.InlineLiteralElement,
                new XAttribute(
                    Legend.LiteralElement_Value,
                    "stuff"),
                new XAttribute(
                    Legend.LiteralElement_CaseSensitive,
                    true));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region open pattern
            var regex = new Regex("abcd",
                RegexOptions.IgnoreCase|RegexOptions.ExplicitCapture|
                RegexOptions.Multiline|RegexOptions.Singleline|
                RegexOptions.IgnorePatternWhitespace);
            var openMatch = (Grammar.Language.MatchType.Open)Grammar.Language.MatchType.Of(4, false);
            var pattern = new Grammar.Language.Rules.Pattern(
                regex,
                openMatch);
            xelt = exporter.ToRuleElement(pattern);
            expected = new XElement(
                Legend.InlineOpenPatternElement,
                new XAttribute(
                    Legend.PatternElement_Regex,
                    regex.ToString()),
                new XAttribute(
                    Legend.PatternElement_CaseSensitive,
                    false),
                new XAttribute(
                    Legend.PatternElement_MultiLine,
                    true),
                new XAttribute(
                    Legend.PatternElement_ExplicitCapture,
                    true),
                new XAttribute(
                    Legend.PatternElement_SingleLine,
                    true),
                new XAttribute(
                    Legend.PatternElement_IgnoreWhitespace,
                    true),
                new XAttribute(
                    Legend.PatternElement_MaxMismatch,
                    openMatch.MaxMismatch),
                new XAttribute(
                    Legend.PatternElement_AllowsEmpty,
                    openMatch.AllowsEmptyTokens));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region closed pattern
            var closedMatch = (Grammar.Language.MatchType.Closed)Grammar.Language.MatchType.Of(4, 10);
            pattern = new Grammar.Language.Rules.Pattern(
                regex,
                closedMatch);
            xelt = exporter.ToRuleElement(pattern);
            expected = new XElement(
                Legend.InlineClosedPatternElement,
                new XAttribute(
                    Legend.PatternElement_Regex,
                    regex.ToString()),
                new XAttribute(
                    Legend.PatternElement_CaseSensitive,
                    false),
                new XAttribute(
                    Legend.PatternElement_MultiLine,
                    true),
                new XAttribute(
                    Legend.PatternElement_ExplicitCapture,
                    true),
                new XAttribute(
                    Legend.PatternElement_SingleLine,
                    true),
                new XAttribute(
                    Legend.PatternElement_IgnoreWhitespace,
                    true),
                new XAttribute(
                    Legend.PatternElement_MinMatch,
                    closedMatch.MinMatch),
                new XAttribute(
                    Legend.PatternElement_MaxMatch,
                    closedMatch.MaxMatch));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region Custom
            var mockCustom = new Mock<ICustomTerminal>();
            mockCustom
                .Setup(ct => ct.SymbolName)
                .Returns("the-custom-symbol");
            xelt = exporter.ToRuleElement(mockCustom.Object);
            expected = new XElement(
                Legend.CustomTerminalElement,
                new XAttribute(
                    Legend.CustomTerminallement_Symbol,
                    "the-custom-symbol"));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region Production ref
            var @ref = new Grammar.Language.Rules.ProductionRef("stuff");
            xelt = exporter.ToRuleElement(@ref);
            var refelt = xelt;
            expected = new XElement(
                Legend.SymbolElement,
                new XAttribute(
                    Legend.SymbolElement_Name,
                    "stuff"));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region EOF
            var eof = new Grammar.Language.Rules.EOF();
            xelt = exporter.ToRuleElement(eof);
            var eofelt = xelt;
            expected = new XElement(Legend.EOFlement);
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region Choice
            var choice = new Grammar.Language.Rules.Choice(
                literal,
                eof);
            xelt = exporter.ToRuleElement(choice);
            expected = new XElement(
                Legend.ChoiceElement,
                lelt,
                eofelt);
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region Sequence
            var sequence = new Grammar.Language.Rules.Sequence(
                literal,
                eof);
            xelt = exporter.ToRuleElement(sequence);
            expected = new XElement(
                Legend.SequenceElement,
                lelt,
                eofelt);
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region Set
            var set = new Grammar.Language.Rules.Set(
                Cardinality.OccursOnlyOnce(),
                2,
                literal,
                eof);
            xelt = exporter.ToRuleElement(set);
            expected = new XElement(
                Legend.SetElement,
                lelt,
                eofelt,
                new XAttribute(
                    Legend.SetElement_MinRecognitionCount,
                    set.MinRecognitionCount.Value));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion
        }

        [TestMethod]
        public void ToNonTerminalTest()
        {
            var exporter = new Exporter();

            var literal = new Literal("stuff");
            var prule = new ProductionRule("xyz", 1, literal);
            var xelt = exporter.ToNonTerminalElement(prule);
            var expected = new XElement(
                Legend.NonTerminalElement,
                new XAttribute(
                    Legend.NonTerminalElement_Name,
                    "xyz"),
                new XAttribute(
                    Legend.NonTerminalElement_Threshold,
                    1),
                new XElement(
                    Legend.InlineLiteralElement,
                    new XAttribute(
                        Legend.LiteralElement_Value,
                        "stuff"),
                    new XAttribute(
                        Legend.LiteralElement_CaseSensitive,
                        true)));
            Assert.IsTrue(AreEqual(expected, xelt));
        }

        [TestMethod]
        public void ToProductionElementTest()
        {
            var exporter = new Exporter();

            #region literal
            var literal = new Literal("stuff");
            var xelt = exporter.ToProductionElement(new Production(new ProductionRule("abc", literal)));
            var lelt = xelt;
            var expected = new XElement(
                Legend.LiteralElement,
                new XAttribute(
                    Legend.LiteralElement_Name,
                    "abc"),
                new XAttribute(
                    Legend.LiteralElement_Value,
                    "stuff"),
                new XAttribute(
                    Legend.LiteralElement_CaseSensitive,
                    true));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region open pattern
            var regex = new Regex("abcd",
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture |
                RegexOptions.Multiline | RegexOptions.Singleline |
                RegexOptions.IgnorePatternWhitespace);
            var openMatch = (Grammar.Language.MatchType.Open)Grammar.Language.MatchType.Of(4, false);
            var pattern = new Pattern(
                regex,
                openMatch);
            xelt = exporter.ToProductionElement(new(new("abc", pattern)));
            expected = new XElement(
                Legend.OpenPatternElement,
                new XAttribute(
                    Legend.PatternElement_Name,
                    "abc"),
                new XAttribute(
                    Legend.PatternElement_Regex,
                    regex.ToString()),
                new XAttribute(
                    Legend.PatternElement_CaseSensitive,
                    false),
                new XAttribute(
                    Legend.PatternElement_MultiLine,
                    true),
                new XAttribute(
                    Legend.PatternElement_ExplicitCapture,
                    true),
                new XAttribute(
                    Legend.PatternElement_SingleLine,
                    true),
                new XAttribute(
                    Legend.PatternElement_IgnoreWhitespace,
                    true),
                new XAttribute(
                    Legend.PatternElement_MaxMismatch,
                    openMatch.MaxMismatch),
                new XAttribute(
                    Legend.PatternElement_AllowsEmpty,
                    openMatch.AllowsEmptyTokens));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region closed pattern
            var closedMatch = (Grammar.Language.MatchType.Closed)Grammar.Language.MatchType.Of(4, 10);
            pattern = new Grammar.Language.Rules.Pattern(
                regex,
                closedMatch);
            xelt = exporter.ToProductionElement(new(new("abcd", pattern)));
            expected = new XElement(
                Legend.ClosedPatternElement,
                new XAttribute(
                    Legend.PatternElement_Name,
                    "abcd"),
                new XAttribute(
                    Legend.PatternElement_Regex,
                    regex.ToString()),
                new XAttribute(
                    Legend.PatternElement_CaseSensitive,
                    false),
                new XAttribute(
                    Legend.PatternElement_MultiLine,
                    true),
                new XAttribute(
                    Legend.PatternElement_ExplicitCapture,
                    true),
                new XAttribute(
                    Legend.PatternElement_SingleLine,
                    true),
                new XAttribute(
                    Legend.PatternElement_IgnoreWhitespace,
                    true),
                new XAttribute(
                    Legend.PatternElement_MinMatch,
                    closedMatch.MinMatch),
                new XAttribute(
                    Legend.PatternElement_MaxMatch,
                    closedMatch.MaxMatch));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion

            #region Custom
            var mockCustom = new Mock<ICustomTerminal>();
            mockCustom
                .Setup(ct => ct.SymbolName)
                .Returns("the-custom-symbol");
            xelt = exporter.ToRuleElement(mockCustom.Object);
            expected = new XElement(
                Legend.CustomTerminalElement,
                new XAttribute(
                    Legend.CustomTerminallement_Symbol,
                    "the-custom-symbol"));
            Assert.IsTrue(AreEqual(expected, xelt));
            #endregion
        }

        [TestMethod]
        public void GrammarTest()
        {
            var grammar = new xBNF.Importer().ImporterGrammar;
            var stream = new MemoryStream();
            var exporter = new Exporter();
            exporter.ExportGrammar(grammar, stream);
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = stream.ToArray();
            var xml = Encoding.UTF8.GetString(buffer);

            Console.WriteLine(xml);
        }

        private bool AreEqual(XElement element1, XElement element2)
        {
            if (element1 == null && element2 == null)
                return true;

            else if (element1 == null ^ element2 == null)
                return false;

            if (!element1.Name.LocalName.Equals(element2.Name.LocalName))
                return false;

            var attributes1 = element1.Attributes().ToArray();
            var attributes2 = element2.Attributes().ToArray();

            if (attributes1.Length != attributes2.Length)
                return false;

            
            if (!attributes1.PairWith(attributes2).All(pair => AreEqual(pair.Item1, pair.Item2)))
                return false;

            var children1 = element1.Elements().ToArray();
            var children2 = element2.Elements().ToArray();

            if (children1.Length != children2.Length)
                return false;

            if (!children1.PairWith(children2).All(pair => AreEqual(pair.Item1, pair.Item2)))
                return false;

            return true;
        }

        private bool AreEqual(XAttribute attribute1, XAttribute attribute2)
        {
            if (attribute1 == null && attribute2 == null)
                return true;

            else if (attribute1 == null ^ attribute2 == null)
                return false;

            // neither is null
            return attribute1.Name.LocalName.Equals(attribute2.Name.LocalName)
                && attribute1.Value.Equals(attribute2.Value);
        }
    }
}
    