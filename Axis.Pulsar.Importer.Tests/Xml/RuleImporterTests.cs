using Axis.Pulsar.Importer.Common.Xml;
using Axis.Pulsar.Parser.Grammar;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Axis.Pulsar.Importer.Tests.Xml
{
    [TestClass]
    public class RuleImporterTests
    {
        private static readonly Stream SampleXmlStream = Assembly
            .GetAssembly(typeof(GrammarImporter))
            .GetManifestResourceStream($"{typeof(GrammarImporter).Namespace}.SampleRule.xml");

        [TestMethod]
        public void ValidateDocument_WithValidXml_ShouldPass()
        {
            var xdoc = XDocument.Load(SampleXmlStream);
            XmlBuilder.ValidateDocument(xdoc);
        }

        #region Extract Cardinality
        [TestMethod]
        public void ExtractCardinality_WithValidElement_ShouldReturnValidCardinality()
        {
            //occurs once
            var element = new XElement("symbol");
            var cardinality = XmlBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //occurs once
            element = new XElement(
                "symbol",

                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 1),
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, 1));
            cardinality = XmlBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 0));
            cardinality = XmlBuilder.ExtractCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 0),
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, "unbounded"));
            cardinality = XmlBuilder.ExtractCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 1),
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, "unbounded"));
            cardinality = XmlBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 1));
            cardinality = XmlBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //between 1 and 5 times
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, 5));
            cardinality = XmlBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(5, cardinality.MaxOccurence);
        }

        [TestMethod]
        public void ExtractCardinality_WithInvalidElement_ShouldThrowException()
        {
            //negative max-occurs
            var element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, -1));
            Assert.ThrowsException<ArgumentException>(() => XmlBuilder.ExtractCardinality(element));

            //negative min-occurs
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, -1));
            Assert.ThrowsException<ArgumentException>(() => XmlBuilder.ExtractCardinality(element));

            //both zero
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 0),
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, 0));
            Assert.ThrowsException<ArgumentException>(() => XmlBuilder.ExtractCardinality(element));
        }
        #endregion

        #region Extract Match Cardinality
        [TestMethod]
        public void ExtractMatchCardinality_WithValidElement_ShouldReturnValidCardinality()
        {
            //occurs once
            var element = new XElement("symbol");
            var cardinality = XmlBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //occurs once
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 1),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, 1));
            cardinality = XmlBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 0));
            cardinality = XmlBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 0),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, "unbounded"));
            cardinality = XmlBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 1),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, "unbounded"));
            cardinality = XmlBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 1));
            cardinality = XmlBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //between 1 and 5 times
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, 5));
            cardinality = XmlBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(5, cardinality.MaxOccurence);
        }

        [TestMethod]
        public void ExtractMatchCardinality_WithInvalidElement_ShouldThrowException()
        {
            //negative max-match
            var element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, -1));
            Assert.ThrowsException<ArgumentException>(() => XmlBuilder.ExtractMatchCardinality(element));

            //negative min-match
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, -1));
            Assert.ThrowsException<ArgumentException>(() => XmlBuilder.ExtractMatchCardinality(element));

            //both zero
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 0),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, 0));
            Assert.ThrowsException<ArgumentException>(() => XmlBuilder.ExtractMatchCardinality(element));
        }
        #endregion

        #region Extract Case Sensitivity
        [TestMethod]
        public void ExtractCaseSensitivity_WithValidElement_ShouldReturnValidResult()
        {
            var element = new XElement("any-element");
            var cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", false));
            cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "False"));
            cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "FALSE"));
            cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "false"));
            cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", true));
            cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "True"));
            cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "TRUE"));
            cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "true"));
            cardinality = XmlBuilder.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);
        }

        [TestMethod]
        public void ExtractCaseSensitivity_WithInvalidElement_ShouldThrowException()
        {
            var element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", 5));
            Assert.ThrowsException<FormatException>(() => XmlBuilder.ExtractCaseSensitivity(element));

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", new object()));
            Assert.ThrowsException<FormatException>(() => XmlBuilder.ExtractCaseSensitivity(element));

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "null"));
            Assert.ThrowsException<FormatException>(() => XmlBuilder.ExtractCaseSensitivity(element));
        }
        #endregion

        #region Extract Pattern Regex
        [TestMethod]
        public void ExtractPatternRegex_WithValidElement_ShouldReturnValidResult()
        {
            var pattern = "abc123{2,5}";
            var element = new XElement(
                "any-name",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_Regex, pattern),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_CaseSensitive, false));
            var regex = XmlBuilder.ExtractPatternRegex(element);
            Assert.IsTrue(regex.Options.HasFlag(RegexOptions.IgnoreCase));
            Assert.AreEqual(pattern, regex.ToString());


            pattern = "abc123{2,5}";
            element = new XElement(
                "any-name",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_Regex, pattern),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_CaseSensitive, true));
            regex = XmlBuilder.ExtractPatternRegex(element);
            Assert.IsTrue(regex.Options.HasFlag(RegexOptions.None));
            Assert.AreEqual(pattern, regex.ToString());


            pattern = "abc123{2,5}";
            element = new XElement(
                "any-name",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_Regex, pattern));
            regex = XmlBuilder.ExtractPatternRegex(element);
            Assert.IsTrue(regex.Options.HasFlag(RegexOptions.IgnoreCase));
            Assert.AreEqual(pattern, regex.ToString());
        }

        #endregion

        #region ImportRule
        [TestMethod]
        public void ImportRule_WithValidSequenceElement_ShouldReturnValidResult()
        {
            var ruleImporter = new GrammarImporter();
            var sampleStream = typeof(GrammarImporter)
                .Assembly
                .GetManifestResourceStream($"{typeof(GrammarImporter).Namespace}.SampleRule.xml");

            var grammar = ruleImporter.ImportGrammar(sampleStream);

            //run all sorts of tests on the rule map.youtube
            var nonterminalCount = grammar
                .Productions
                .Where(production => production.Rule is SymbolExpressionRule)
                .Count();

            var terminalCount = grammar
                .Productions
                .Where(rule => rule.Rule is ITerminal)
                .Count();

            Assert.AreEqual(10, nonterminalCount);
            Assert.AreEqual(13, terminalCount);
            Assert.AreEqual(10, grammar.Productions.Count() - terminalCount);
            Assert.AreEqual(13, grammar.Productions.Count() - nonterminalCount);

        }
        #endregion
    }
}
