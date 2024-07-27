using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Lang;
using System.Text;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Grammar.Rules.Composite;
using Axis.Pulsar.Core.Grammar.Rules;

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

        internal static string WriteProduction(Production production, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(production);

            return new StringBuilder()
                .Append("$").Append(production.Symbol)
                .Append(" -> ")
                .Append(WriteRule(production.Rule, context))
                .ToString();
        }

        internal static string WriteRule(Production.IRule rule, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);

            return rule switch
            {
                IAtomicRule ar => WriteAtomicRule(ar, context),
                CompositeRule cr => WriteCompositeRule(cr, context),
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

            var contentArg = args
                .Where(arg => arg.Argument is ContentArgument)
                .Select(arg => (ContentArgument) arg.Argument)
                .FirstOrDefault();

            return contentArg.Delimiter switch
            {
                ContentArgumentDelimiter.None => WriteRegularAtomicRule(rule.Id, args),
                ContentArgumentDelimiter delimiter => WriteContentAwareAtomicRule(delimiter, args)
            };
        }

        internal static string WriteRegularAtomicRule(string ruleId, Parameter[] args)
        {
            if (string.IsNullOrEmpty(ruleId))
                throw new ArgumentException($"Invalid {nameof(ruleId)}: null/empty");

            return new StringBuilder()
                .Append('@').Append(ruleId)
                .Append(WriteAtomicRuleArgs(args))
                .ToString();
        }

        internal static string WriteContentAwareAtomicRule(
            ContentArgumentDelimiter contentType,
            Parameter[] args)
        {
            if (!Enum.IsDefined(contentType))
                throw new ArgumentOutOfRangeException(nameof(contentType));

            var @char = contentType.DelimiterCharacter();

            var contentText = args
                .Where(arg => arg.Argument is ContentArgument)
                .Select(arg => $"{@char}{arg.RawValue}{@char}")
                .FirstOrDefault();

            var argsText = args
                .Where(arg => arg.Argument is not ContentArgument)
                .ApplyTo(WriteAtomicRuleArgs);

            return new StringBuilder()
                .Append(contentText)
                .Append(argsText)
                .ToString();
        }

        internal static string WriteAtomicRuleArgs(IEnumerable<Parameter> args)
        {
            ArgumentNullException.ThrowIfNull(args);

            return args
                .Aggregate(new StringBuilder('{'), (_sb, arg) =>
                {
                    return _sb
                        .Append(_sb.Length == 1 ? "" : ",")
                        .Append(" ").Append(arg.Argument)
                        .Append(": ")
                        .Append("'").Append(arg.RawValue).Append("'");
                })
                .Append(" }")
                .ToString();
        }

        internal static string WriteCompositeRule(CompositeRule rule, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentNullException.ThrowIfNull(context);

            if (rule is not CompositeRule nonTerminal)
                throw new InvalidOperationException(
                    $"Invalid composite rule: '{rule.GetType()}'");

            var rt = rule.RecognitionThreshold > 1
                ? $":{rule.RecognitionThreshold} "
                : "";

            return $"{rt}{WriteElement(nonTerminal.Element, context)}";
        }

        internal static string WriteElement(IAggregationElement element, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(element);

            return element switch
            {
                AtomicRuleRef @ref => WriteAtomicRuleRef(@ref, context),
                ProductionRef @ref => WriteProductionRef(@ref, context),
                Repetition rep => WriteRepetition(rep, context),
                Choice choice => WriteChoice(choice, context),
                Sequence seq => WriteSequence(seq, context),
                Set set => WriteSet(set, context),
                _ => throw new InvalidOperationException($"")
            };
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

        internal static string WriteRepetition(Repetition repetition, XBNFLanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(repetition);
            ArgumentNullException.ThrowIfNull(context);

            var cardinality = Cardinality.OccursOnlyOnce().Equals(repetition.Cardinality)
                ? "" : $"{repetition.Cardinality}";

            return $"{WriteElement(repetition.Element, context)}{cardinality}";
        }

        internal static string WriteGroup(IAggregation group, XBNFLanguageContext context)
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
            return $"%{(mrc > 1 ? mrc.ToString() : "")}{WriteGroup(set, context)}";
        }

        #region Nested types
        public class WriterSettings
        {

        }
        #endregion

    }
}
