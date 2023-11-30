using Axis.Luna.Extensions;
using Axis.Pulsar.Core.XBNF.Lang;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Parsers
{
    internal class ParserContext
    {
        private readonly Dictionary<string, ArgumentPair[]> _atomicRuleArgs = new();

        internal IReadOnlyDictionary<string, ArgumentPair[]> AtomicRuleArguments => _atomicRuleArgs.AsReadOnly();

        internal LanguageMetadata Metadata { get; }

        internal ParserContext(LanguageMetadata metadata)
        {
            Metadata = metadata.ThrowIfNull(new ArgumentNullException(nameof(metadata)));
        }

        internal void AppendAtomicRuleArguments(string id, ArgumentPair[] arguments)
        {
            if (!_atomicRuleArgs.TryAdd(id, arguments))
                throw new InvalidOperationException(
                    $"Invalid rule args: duplicate key '{id}'");
        }
    }
}
