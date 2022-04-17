using Axis.Pulsar.Parser.Input;

namespace Axis.Pulsar.Parser
{
    public interface IParser
    {
        string SymbolName { get; }

        bool TryParse(BufferedTokenReader tokenReader, out ParseResult result);
    }
}
