using Axis.Luna.Extensions;
using Axis.Pulsar.Core.XBNF.Lang;
using System.Collections.Immutable;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Parsers
{
    internal class ParserContext
    {
        private readonly Dictionary<string, Parameter[]> _atomicRuleArgs = new();

        internal IImmutableDictionary<string, Parameter[]> AtomicRuleArguments => _atomicRuleArgs.ToImmutableDictionary();

        internal LanguageMetadata Metadata { get; }

        internal ParserContext(LanguageMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);

            Metadata = metadata;
        }

        internal void AppendAtomicRuleArguments(string id, params Parameter[] arguments)
        {
            if (!_atomicRuleArgs.TryAdd(id, arguments))
                throw new InvalidOperationException(
                    $"Invalid rule args: duplicate key '{id}'");
        }
    }
}
