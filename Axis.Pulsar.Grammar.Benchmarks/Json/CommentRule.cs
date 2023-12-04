using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Grammar.Recognizers;

namespace Axis.Pulsar.Grammar.Benchmarks.Json
{
    internal class CommentRule : ICustomTerminal
    {
        public string SymbolName { get; }

        public CommentRule(string symbolName)
        {
            SymbolName = symbolName ?? throw new ArgumentNullException(nameof(symbolName));
        }

        public IRecognizer ToRecognizer(Language.Grammar grammar) => new CommentRecognizer(grammar, this);
    }
}
