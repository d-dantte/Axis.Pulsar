using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.IO;
using Axis.Pulsar.Core.XBNF.Definitions;
using System.Text;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Lang
{
    public class XBNFExporter : ILanguageExporter
    {
        public string ExportLanguage(ILanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not XBNFLanguageContext xbnfContext)
                throw new ArgumentException($"Invalid context type: '{context.GetType()}'");

            return context.Grammar.ProductionSymbols
                .Select(symbol => context.Grammar[symbol])
                .Select(production => WriteProduction(production, xbnfContext))
                .Aggregate(new StringBuilder(), (sb, productionText) => sb.Append(productionText))
                .ToString();
        }

        public string WriteProduction(IProduction production, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(production);

            return new StringBuilder()
                .Append(production.Symbol)
                .Append(" -> ")
                .Append(WriteRule(production.Rule, context))
                .ToString();
        }

        private string WriteRule(IRule rule, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);

            return rule switch
            {
                IAtomicRule ar => WriteAtomicRule(ar, context),
                ICompositeRule cr => WriteCompositeRule(cr, context),
                _ => throw new InvalidOperationException($"Invalid rule type: {rule.GetType()}")
            };
        }

        private string WriteAtomicRule(IAtomicRule rule, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);

            var argMap = context.AtomicRuleArguments;
            var args = argMap.TryGetValue(rule.Id, out var value)
                ? value : throw new InvalidOperationException(
                    $"Invalid atomic rule: rule id '{rule.Id}' not found in argument map");

            var ruleMap = context.Metadata.AtomicRuleDefinitionMap;
            var contentType = ruleMap.TryGetValue(rule.Id, out var ruleDef)
                ? ruleDef.ContentDelimiterType : throw new InvalidOperationException(
                    $"Invalid atomic rule: rule id '{rule.Id}' not found in argument map");

            return contentType switch
            {
                AtomicContentDelimiterType.None => WriteRegularAtomicRule(rule.Id, args),
                _ => WriteContentAwareAtomicRule(contentType, args)
            };
        }

        private string WriteRegularAtomicRule(string ruleId, ArgumentPair[] args)
        {
            if (string.IsNullOrEmpty(ruleId))
                throw new ArgumentException($"Invalid {nameof(ruleId)}: null/empty");

        }

        private string WriteContentAwareAtomicRule(
            AtomicContentDelimiterType contentType,
            ArgumentPair[] args)
        {
            if (!Enum.IsDefined(contentType))
                throw new ArgumentOutOfRangeException(nameof(contentType));
        }

        private string WriteAtomicRuleArgs(ArgumentPair[] args)
        {

        }

        private string WriteCompositeRule(ICompositeRule rule, XBNFLanguageContext context)
        {

        }


        #region Nested types
        public class WriterSettings
        {

        }
        #endregion

    }
}
