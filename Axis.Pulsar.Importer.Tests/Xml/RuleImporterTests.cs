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
            .GetAssembly(typeof(RuleImporter))
            .GetManifestResourceStream($"{typeof(RuleImporter).Namespace}.SampleRule.xml");

        [TestMethod]
        public void ValidateDocument_WithValidXml_ShouldPass()
        {
            var xdoc = XDocument.Load(SampleXmlStream);
            RuleBuilder.ValidateDocument(xdoc);
        }

        #region Extract Cardinality
        [TestMethod]
        public void ExtractCardinality_WithValidElement_ShouldReturnValidCardinality()
        {
            //occurs once
            var element = new XElement("symbol");
            var cardinality = RuleBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //occurs once
            element = new XElement(
                "symbol",

                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 1),
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, 1));
            cardinality = RuleBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 0));
            cardinality = RuleBuilder.ExtractCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 0),
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, "unbounded"));
            cardinality = RuleBuilder.ExtractCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 1),
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, "unbounded"));
            cardinality = RuleBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 1));
            cardinality = RuleBuilder.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //between 1 and 5 times
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, 5));
            cardinality = RuleBuilder.ExtractCardinality(element);
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
            Assert.ThrowsException<ArgumentException>(() => RuleBuilder.ExtractCardinality(element));

            //negative min-occurs
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, -1));
            Assert.ThrowsException<ArgumentException>(() => RuleBuilder.ExtractCardinality(element));

            //both zero
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MinOccurs, 0),
                new XAttribute(Common.Xml.Legend.Enumerations.ProductionElement_MaxOccurs, 0));
            Assert.ThrowsException<ArgumentException>(() => RuleBuilder.ExtractCardinality(element));
        }
        #endregion

        #region Extract Match Cardinality
        [TestMethod]
        public void ExtractMatchCardinality_WithValidElement_ShouldReturnValidCardinality()
        {
            //occurs once
            var element = new XElement("symbol");
            var cardinality = RuleBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //occurs once
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 1),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, 1));
            cardinality = RuleBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 0));
            cardinality = RuleBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 0),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, "unbounded"));
            cardinality = RuleBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 1),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, "unbounded"));
            cardinality = RuleBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 1));
            cardinality = RuleBuilder.ExtractMatchCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //between 1 and 5 times
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, 5));
            cardinality = RuleBuilder.ExtractMatchCardinality(element);
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
            Assert.ThrowsException<ArgumentException>(() => RuleBuilder.ExtractMatchCardinality(element));

            //negative min-match
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, -1));
            Assert.ThrowsException<ArgumentException>(() => RuleBuilder.ExtractMatchCardinality(element));

            //both zero
            element = new XElement(
                "symbol",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MinMatch, 0),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_MaxMatch, 0));
            Assert.ThrowsException<ArgumentException>(() => RuleBuilder.ExtractMatchCardinality(element));
        }
        #endregion

        #region Extract Case Sensitivity
        [TestMethod]
        public void ExtractCaseSensitivity_WithValidElement_ShouldReturnValidResult()
        {
            var element = new XElement("any-element");
            var cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", false));
            cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "False"));
            cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "FALSE"));
            cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "false"));
            cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", true));
            cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "True"));
            cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "TRUE"));
            cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "true"));
            cardinality = RuleBuilder.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);
        }

        [TestMethod]
        public void ExtractCaseSensitivity_WithInvalidElement_ShouldThrowException()
        {
            var element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", 5));
            Assert.ThrowsException<FormatException>(() => RuleBuilder.ExtractCaseSensitivity(element));

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", new object()));
            Assert.ThrowsException<FormatException>(() => RuleBuilder.ExtractCaseSensitivity(element));

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "null"));
            Assert.ThrowsException<FormatException>(() => RuleBuilder.ExtractCaseSensitivity(element));
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
            var regex = RuleBuilder.ExtractPatternRegex(element);
            Assert.IsTrue(regex.Options.HasFlag(RegexOptions.IgnoreCase));
            Assert.AreEqual(pattern, regex.ToString());


            pattern = "abc123{2,5}";
            element = new XElement(
                "any-name",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_Regex, pattern),
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_CaseSensitive, true));
            regex = RuleBuilder.ExtractPatternRegex(element);
            Assert.IsTrue(regex.Options.HasFlag(RegexOptions.None));
            Assert.AreEqual(pattern, regex.ToString());


            pattern = "abc123{2,5}";
            element = new XElement(
                "any-name",
                new XAttribute(Common.Xml.Legend.Enumerations.PatternElement_Regex, pattern));
            regex = RuleBuilder.ExtractPatternRegex(element);
            Assert.IsTrue(regex.Options.HasFlag(RegexOptions.IgnoreCase));
            Assert.AreEqual(pattern, regex.ToString());
        }

        #endregion

        #region ImportRule
        [TestMethod]
        public void ImportRule_WithValidSequenceElement_ShouldReturnValidResult()
        {
            var ruleImporter = new RuleImporter();
            var sampleStream = typeof(RuleImporter)
                .Assembly
                .GetManifestResourceStream($"{typeof(RuleImporter).Namespace}.SampleRule.xml");

            var ruleMap = ruleImporter.ImportRule(sampleStream);

            //run all sorts of tests on the rule map.youtube
            var nonterminalCount = ruleMap
                .Productions()
                .Where(rule => rule.Value is SymbolExpressionRule || rule.Value is RuleRef)
                .Count();

            var terminalCount = ruleMap
                .Productions()
                .Where(rule => rule.Value is LiteralRule || rule.Value is PatternRule)
                .Count();

            Assert.AreEqual(10, nonterminalCount);
            Assert.AreEqual(13, terminalCount);
            Assert.AreEqual(10, ruleMap.Productions().Count() - terminalCount);
            Assert.AreEqual(13, ruleMap.Productions().Count() - nonterminalCount);

        }
        #endregion
    }
}
