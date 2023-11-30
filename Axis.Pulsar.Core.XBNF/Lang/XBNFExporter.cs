using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar.Rules;
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
                .Aggregate(new StringBuilder(), (sb, productionText) => sb.AppendLine(productionText))
                .ToString();
        }

        internal static string WriteProduction(IProduction production, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(production);

            return new StringBuilder()
                .Append("$").Append(production.Symbol)
                .Append(" -> ")
                .Append(WriteRule(production.Rule, context))
                .ToString();
        }

        internal static string WriteRule(IRule rule, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);

            return rule switch
            {
                IAtomicRule ar => WriteAtomicRule(ar, context),
                ICompositeRule cr => WriteCompositeRule(cr, context),
                _ => throw new InvalidOperationException($"Invalid rule type: {rule.GetType()}")
            };
        }

        internal static string WriteAtomicRule(IAtomicRule rule, XBNFLanguageContext context)
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

        internal static string WriteRegularAtomicRule(string ruleId, ArgumentPair[] args)
        {
            if (string.IsNullOrEmpty(ruleId))
                throw new ArgumentException($"Invalid {nameof(ruleId)}: null/empty");

            return new StringBuilder()
                .Append('@').Append(ruleId)
                .Append(WriteAtomicRuleArgs(args))
                .ToString();
        }

        internal static string WriteContentAwareAtomicRule(
            AtomicContentDelimiterType contentType,
            ArgumentPair[] args)
        {
            if (!Enum.IsDefined(contentType))
                throw new ArgumentOutOfRangeException(nameof(contentType));

            var @char = contentType.DelimiterCharacter();
            var content = args
                .Where(arg => arg.Argument.Equals(IAtomicRuleFactory.ContentArgument))
                .Select(arg => $"{@char}{arg.Value}{@char}")
                .FirstOrDefault();

            return new StringBuilder()
                .Append(content)
                .Append(WriteAtomicRuleArgs(
                    args.Where(arg => !arg.Argument.Equals(IAtomicRuleFactory.ContentArgument))))
                .ToString();
        }

        internal static string WriteAtomicRuleArgs(IEnumerable<ArgumentPair> args)
        {
            ArgumentNullException.ThrowIfNull(args);

            return args
                .Aggregate(new StringBuilder('{'), (_sb, arg) =>
                {
                    return _sb
                        .Append(_sb.Length == 1 ? "" : ",")
                        .Append(" ").Append(arg.Argument)
                        .Append(": ")
                        .Append("'").Append(arg.Value).Append("'");
                })
                .Append(" }")
                .ToString();
        }

        internal static string WriteCompositeRule(ICompositeRule rule, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentNullException.ThrowIfNull(context);

            if (rule is not NonTerminal nonTerminal)
                throw new InvalidOperationException(
                    $"Invalid composite rule: '{rule.GetType()}'");

            var rt = rule.RecognitionThreshold > 1
                ? $":{rule.RecognitionThreshold} "
                : "";

            return $"{rt}{WriteElement(nonTerminal.Element, context)}";
        }

        internal static string WriteElement(IGroupElement element, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(element);

            var eltString = element switch
            {
                AtomicRuleRef @ref => WriteAtomicRuleRef(@ref, context),
                ProductionRef @ref => WriteProductionRef(@ref, context),
                Choice choice => WriteChoice(choice, context),
                Sequence seq => WriteSequence(seq, context),
                Set set => WriteSet(set, context),
                _ => throw new InvalidOperationException($"")
            };

            return $"{eltString}{element.Cardinality}";
        }

        internal static string WriteAtomicRuleRef(AtomicRuleRef ruleRef, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(ruleRef);

            return WriteAtomicRule(ruleRef.Ref, context);
        }

        internal static string WriteProductionRef(ProductionRef prodRef, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(prodRef);
            ArgumentNullException.ThrowIfNull(context);

            return $"${prodRef.Ref}";
        }

        internal static string WriteGroup(IGroup group, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(context);

            return group.Elements
                .Aggregate(new StringBuilder("["), (sb, element) =>
                {
                    return sb
                        .Append(" ")
                        .Append(WriteElement(element, context));
                })
                .Append(" ]")
                .ToString();
        }

        internal static string WriteChoice(Choice choice, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(choice);
            ArgumentNullException.ThrowIfNull(context);

            return $"?{WriteGroup(choice, context)}";
        }

        internal static string WriteSequence(Sequence seq, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(seq);
            ArgumentNullException.ThrowIfNull(context);

            return $"+{WriteGroup(seq, context)}";
        }

        internal static string WriteSet(Set set, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(set);
            ArgumentNullException.ThrowIfNull(context);

            var mrc = set.MinRecognitionCount;
            return $"#{(mrc > 1 ? mrc.ToString() : "")}{WriteGroup(set, context)}";
        }

        #region Nested types
        public class WriterSettings
        {

        }
        #endregion

    }
}
