using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Languages.Xml;
using Moq;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Axis.Pulsar.Languages.IO.Tests.Xml
{
    [TestClass]
    public class ImporterTests
    {
        [TestMethod]
        public void ValidateDocument_WithValidXml_ShouldPass()
        {
            using Stream SampleXmlStream = Assembly
                .GetAssembly(typeof(Importer))?
                .GetManifestResourceStream($"{typeof(Languages.xBNF.Importer).Namespace}.xBNFRule.xml")
                ?? throw new InvalidOperationException("sample xml could not be found");

            var xdoc = XDocument.Load(SampleXmlStream);
            Importer.ValidateDocument(xdoc);
        }

        #region Valiidator API
        [TestMethod]
        public void RegisterValidator_WithValidArgs_ShouldRegister()
        {
            var mockValidator = new Mock<IProductionValidator>();
            var importer = new Importer();
            importer.RegisterValidator("symbol-name", mockValidator.Object);

            Assert.AreEqual(mockValidator.Object, importer.RegisteredValidator("symbol-name"));
        }

        [TestMethod]
        public void RegisterValidator_WithInvalidArgs_ShouldThrowException()
        {
            var mockValidator = new Mock<IProductionValidator>();
            var importer = new Importer();
            
            Assert.ThrowsException<ArgumentNullException>(() => importer.RegisterValidator("symbol-name", null));
            Assert.ThrowsException<ArgumentNullException>(() => importer.RegisterValidator(null, mockValidator.Object));
            Assert.ThrowsException<ArgumentException>(() => importer.RegisterValidator("invalid symbol name", mockValidator.Object));
        }
        #endregion

        #region Custom Terminal API
        [TestMethod]
        public void RegisterTerminal_WithValidArgs_ShouldRegister()
        {
            var importer = new Importer();
            var mockTerminal = new Mock<ICustomTerminal>();
            mockTerminal
                .Setup(t => t.SymbolName)
                .Returns("symbol-name");

            importer.RegisterTerminal(mockTerminal.Object);

            Assert.AreEqual(mockTerminal.Object, importer.RegisteredTerminal("symbol-name"));
        }

        [TestMethod]
        public void RegisterTerminal_WithInvalidArgs_ShouldThrowException()
        {
            var importer = new Importer();
            var mockTerminal = new Mock<ICustomTerminal>();

            Assert.ThrowsException<ArgumentNullException>(() => importer.RegisterTerminal(null));
            Assert.ThrowsException<ArgumentNullException>(() => importer.RegisterTerminal(mockTerminal.Object));

            mockTerminal
                .Setup(t => t.SymbolName)
                .Returns("invalid symbol name");
            Assert.ThrowsException<ArgumentException>(() => importer.RegisterTerminal(mockTerminal.Object));


            mockTerminal
                .Setup(t => t.SymbolName)
                .Returns("symbol-name");
            importer.RegisterTerminal(mockTerminal.Object);
            Assert.ThrowsException<InvalidOperationException>(() => importer.RegisterTerminal(mockTerminal.Object));
        }
        #endregion


        #region Extract Cardinality
        [TestMethod]
        public void ExtractCardinality_WithValidElement_ShouldReturnValidCardinality()
        {
            //occurs once
            var element = new XElement("symbol");
            var cardinality = Importer.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //occurs once
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MinOccurs, 1),
                new XAttribute(Legend.ProductionElement_MaxOccurs, 1));
            cardinality = Importer.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MinOccurs, 0));
            cardinality = Importer.ExtractCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //zero or more
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MinOccurs, 0),
                new XAttribute(Legend.ProductionElement_MaxOccurs, "unbounded"));
            cardinality = Importer.ExtractCardinality(element);
            Assert.AreEqual(0, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MinOccurs, 1),
                new XAttribute(Legend.ProductionElement_MaxOccurs, "unbounded"));
            cardinality = Importer.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);

            //At least
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MinOccurs, 1));
            cardinality = Importer.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);

            //between 1 and 5 times
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MaxOccurs, 5));
            cardinality = Importer.ExtractCardinality(element);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(5, cardinality.MaxOccurence);
        }

        [TestMethod]
        public void ExtractCardinality_WithInvalidElement_ShouldThrowException()
        {
            //negative max-occurs
            var element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MaxOccurs, -1));
            Assert.ThrowsException<ArgumentException>(() => Importer.ExtractCardinality(element));

            //negative min-occurs
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MinOccurs, -1));
            Assert.ThrowsException<ArgumentException>(() => Importer.ExtractCardinality(element));

            //both zero
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MinOccurs, 0),
                new XAttribute(Legend.ProductionElement_MaxOccurs, 0));
            Assert.ThrowsException<InvalidOperationException>(() => Importer.ExtractCardinality(element));

            //min > max
            element = new XElement(
                "symbol",
                new XAttribute(Legend.ProductionElement_MinOccurs, 3),
                new XAttribute(Legend.ProductionElement_MaxOccurs, 1));
            Assert.ThrowsException<InvalidOperationException>(() => Importer.ExtractCardinality(element));
        }
        #endregion

        #region Extract Match Type
        [TestMethod]
        public void ExtractMatchType_WithValidElement_ShouldReturnValidCardinality()
        {
            //occurs once
            var element = new XElement("symbol");
            var closedMatch = Importer.ExtractClosedMatchType(element);
            Assert.AreEqual(1, closedMatch.MinMatch);
            Assert.AreEqual(1, closedMatch.MaxMatch);

            //occurs once
            element = new XElement(
                "symbol",
                new XAttribute(Legend.PatternElement_MinMatch, 1),
                new XAttribute(Legend.PatternElement_MaxMatch, 1));
            closedMatch = Importer.ExtractClosedMatchType(element);
            Assert.AreEqual(1, closedMatch.MinMatch);
            Assert.AreEqual(1, closedMatch.MaxMatch);

            //1 max-mismatch
            element = new XElement(
                "symbol");
            var openMatch = Importer.ExtractOpenMatchType(element);
            Assert.AreEqual(1, openMatch.MaxMismatch);
            Assert.IsFalse(openMatch.AllowsEmptyTokens);

            //1 max-mismatch
            element = new XElement(
                "symbol",
                new XAttribute(Legend.PatternElement_MaxMismatch, 1),
                new XAttribute(Legend.PatternElement_AllowsEmpty, "true"));
            openMatch = Importer.ExtractOpenMatchType(element);
            Assert.AreEqual(1, openMatch.MaxMismatch);
            Assert.IsTrue(openMatch.AllowsEmptyTokens);
        }

        [TestMethod]
        public void ExtractMatchType_WithInvalidElement_ShouldThrowException()
        {
            //negative max-mismatch
            var element = new XElement(
                "symbol",
                new XAttribute(Legend.PatternElement_MaxMismatch, -1));
            Assert.ThrowsException<ArgumentException>(() => Importer.ExtractOpenMatchType(element));

            //invalid allows-empty
            element = new XElement(
                "symbol",
                new XAttribute(Legend.PatternElement_AllowsEmpty, "non-bool"));
            Assert.ThrowsException<FormatException>(() => Importer.ExtractOpenMatchType(element));

            //negative max-match
            element = new XElement(
                "symbol",
                new XAttribute(Legend.PatternElement_MaxMatch, -1));
            Assert.ThrowsException<ArgumentException>(() => Importer.ExtractClosedMatchType(element));

            //negative mix-match
            element = new XElement(
                "symbol",
                new XAttribute(Legend.PatternElement_MinMatch, -1));
            Assert.ThrowsException<ArgumentException>(() => Importer.ExtractClosedMatchType(element));
        }
        #endregion

        #region Extract Case Sensitivity
        [TestMethod]
        public void ExtractCaseSensitivity_WithValidElement_ShouldReturnValidResult()
        {
            var element = new XElement("any-element");
            var cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", false));
            cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "False"));
            cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "FALSE"));
            cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "false"));
            cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsFalse(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", true));
            cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "True"));
            cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "TRUE"));
            cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "true"));
            cardinality = Importer.ExtractCaseSensitivity(element);
            Assert.IsTrue(cardinality);
        }

        [TestMethod]
        public void ExtractCaseSensitivity_WithInvalidElement_ShouldThrowException()
        {
            var element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", 5));
            Assert.ThrowsException<FormatException>(() => Importer.ExtractCaseSensitivity(element));

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", new object()));
            Assert.ThrowsException<FormatException>(() => Importer.ExtractCaseSensitivity(element));

            element = new XElement(
                "any-element",
                new XAttribute("case-sensitive", "null"));
            Assert.ThrowsException<FormatException>(() => Importer.ExtractCaseSensitivity(element));
        }
        #endregion

        #region Extract Pattern Regex
        [TestMethod]
        public void ExtractPatternRegex_WithValidElement_ShouldReturnValidResult()
        {
            var pattern = "abc123{2,5}";
            var element = new XElement(
                "any-name",
                new XAttribute(Legend.PatternElement_Regex, pattern));
            var regex = Importer.ExtractPatternRegex(element);
            var expectedOptions = RegexOptions.Compiled;
            Assert.AreEqual(expectedOptions, regex.Options);
            Assert.AreEqual(pattern, regex.ToString());

            element.Add(new XAttribute(Legend.PatternElement_CaseSensitive, true));
            regex = Importer.ExtractPatternRegex(element);
            Assert.AreEqual(expectedOptions, regex.Options);
            Assert.AreEqual(pattern, regex.ToString());

            pattern = "abc123{2,5}";
            expectedOptions |= RegexOptions.IgnoreCase;
            element.Attribute(Legend.PatternElement_CaseSensitive).Value = false.ToString();
            regex = Importer.ExtractPatternRegex(element);
            Assert.AreEqual(expectedOptions, regex.Options);
            Assert.AreEqual(pattern, regex.ToString());

            pattern = "abc123{2,5}";
            expectedOptions |= RegexOptions.ExplicitCapture;
            element.Add(new XAttribute(Legend.PatternElement_ExplicitCapture, true));
            regex = Importer.ExtractPatternRegex(element);
            Assert.AreEqual(expectedOptions, regex.Options);
            Assert.AreEqual(pattern, regex.ToString());

            pattern = "abc123{2,5}";
            expectedOptions |= RegexOptions.IgnorePatternWhitespace;
            element.Add(new XAttribute(Legend.PatternElement_IgnoreWhitespace, true));
            regex = Importer.ExtractPatternRegex(element);
            Assert.AreEqual(expectedOptions, regex.Options);
            Assert.AreEqual(pattern, regex.ToString());

            pattern = "abc123{2,5}";
            expectedOptions |= RegexOptions.Multiline;
            element.Add(new XAttribute(Legend.PatternElement_MultiLine, true));
            regex = Importer.ExtractPatternRegex(element);
            Assert.AreEqual(expectedOptions, regex.Options);
            Assert.AreEqual(pattern, regex.ToString());

            pattern = "abc123{2,5}";
            expectedOptions |= RegexOptions.Singleline;
            element.Add(new XAttribute(Legend.PatternElement_SingleLine, true));
            regex = Importer.ExtractPatternRegex(element);
            Assert.AreEqual(expectedOptions, regex.Options);
            Assert.AreEqual(pattern, regex.ToString());
        }

        #endregion


        #region ImportRule
        [TestMethod]
        public void ImportRule_WithValidSequenceElement_ShouldReturnValidResult()
        {
            using Stream SampleXmlStream = Assembly
                .GetAssembly(typeof(Importer))?
                .GetManifestResourceStream($"{typeof(Languages.xBNF.Importer).Namespace}.xBNFRule.xml")
                ?? throw new InvalidOperationException("sample xml could not be found");

            var importer = new Importer();
            var grammar = importer.ImportGrammar(SampleXmlStream);

            Assert.AreEqual(45, grammar.ProductionCount);
        }
        #endregion
    }
}
