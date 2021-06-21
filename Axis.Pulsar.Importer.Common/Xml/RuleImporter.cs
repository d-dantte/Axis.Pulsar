using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Language;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Axis.Pulsar.Importer.Common.Xml
{
    public class RuleImporter: IRuleImporter
    {
        public IRule ImportRule(Stream inputStream)
        {
            var xml = XDocument.Load(inputStream);

            ValidateDocument(xml);

            return ImportLanguage(xml);
        }

        public async Task<IRule> ImportRuleAsync(Stream inputStream)
        {
            var xml = await XDocument.LoadAsync(
                inputStream,
                LoadOptions.None,
                CancellationToken.None);

            ValidateDocument(xml);

            return ImportLanguage(xml);
        }

        public static void ValidateDocument(XDocument xml)
        {
            var schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(GetXsdResourceStream()));

            var errors = new List<XmlImporterException.Info>();
            xml.Validate(schemas, (source, args) => errors.Add(new(
                args.Message,
                args.Exception,
                args.Severity)));

            if (errors.Count > 0)
                throw new XmlImporterException(errors.ToArray());
        }

        public static Stream GetXsdResourceStream()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream($"{nameof(Axis.Pulsar.Importer.Common.Xml)}.RuleDefinition.xsd");
        }

        public static PatternTerminal ImportPattern(XElement patternElement) => new PatternTerminal(
            patternElement.Attribute(Legend.Enumerations.PatterElement_Name).Value,
            ExtractPatternRegex(patternElement),
            ExtractPatternMatchCardinality(patternElement));

        public static StringTerminal ImportString(XElement stringElement) => new StringTerminal(
            stringElement.Attribute(Legend.Enumerations.StringElement_Name).Value,
            stringElement.Attribute(Legend.Enumerations.StringElement_Value).Value,
            ExtractStringCaseSensitivity(stringElement));

        public static Production ImportProduction(XElement productionElement)
        {
            var innerProductions = productionElement
                .Nodes()
                .Cast<XElement>()
                .Select(ImportProduction)
                .ToArray();

            return productionElement.Name.LocalName switch
            {
                "set" => Production.Set(
                    ExtractProductionCardinality(productionElement),
                    innerProductions[0],
                    innerProductions[1],
                    innerProductions[2..]),

                "sequence" => Production.Sequence(
                    ExtractProductionCardinality(productionElement),
                    innerProductions[0],
                    innerProductions[1],
                    innerProductions[2..]),

                "choice" => Production.Choice(
                    ExtractProductionCardinality(productionElement),
                    innerProductions[0],
                    innerProductions[1],
                    innerProductions[2..]),

                "non-terminal" => Production.Single(ImportNonTerminal(productionElement)),

                "string" => Production.Single(
                    ExtractProductionCardinality(productionElement),
                    ImportString(productionElement)),

                "pattern" => Production.Single(
                    ExtractProductionCardinality(productionElement),
                    ImportPattern(productionElement)),

                _ => throw new Exception($"Invalid production element name: {productionElement.Name}")
            };
        }

        public static NonTerminal ImportNonTerminal(XElement nonTerminalElement) => new NonTerminal(
            nonTerminalElement.Attribute(Legend.Enumerations.NonTerminalElement_Name).Value,
            ImportProduction(nonTerminalElement.FirstChild()));

        public static IRule ImportLanguage(XDocument language) => ImportNonTerminal(language.Root);

        public static Cardinality ExtractPatternMatchCardinality(XElement patternElement)
        {
            var minOccurs = patternElement.Attribute(Legend.Enumerations.PatterElement_MinMatch)?.Value;
            var maxOCcurs = patternElement.Attribute(Legend.Enumerations.PatterElement_MaxMatch)?.Value;

            return new Cardinality(
                int.Parse(minOccurs ?? "0"),
                maxOCcurs == null ? null : int.Parse(maxOCcurs));
        }

        public static Regex ExtractPatternRegex(XElement patternElement)
        {
            var regexPattern = patternElement.Attribute(Legend.Enumerations.PatterElement_Regex).Value;
            var options =
                !patternElement.TryAttribute(Legend.Enumerations.PatterElement_CaseSensitive, out var att) ? RegexOptions.IgnoreCase
                : !bool.Parse(att.Value) ? RegexOptions.IgnoreCase
                : RegexOptions.None;

            return new Regex(regexPattern, options);
        }

        public static bool ExtractStringCaseSensitivity(XElement element)
        {
            return element.TryAttribute(Legend.Enumerations.StringElement_CaseSensitive, out var att)
                && bool.Parse(att.Value);
        }

        public static Cardinality ExtractProductionCardinality(XElement productionElement)
        {
            var minOccurs = productionElement.Attribute(Legend.Enumerations.ProductionElement_MinOccurs)?.Value;
            var maxOCcurs = productionElement.Attribute(Legend.Enumerations.ProductionElement_MaxOccurs)?.Value;

            return new Cardinality(
                int.Parse(minOccurs ?? "0"),
                maxOCcurs == null ? null : int.Parse(maxOCcurs));
        }
    }
}
