using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar
{
    public interface IGrammar
    {
        public string Root { get; }

        public int ProductionCount { get; }

        public IProduction this[string name] { get; }

        public IEnumerable<string> ProductionSymbols { get; }


        public bool ContainsProduction(string symbolName);

        public IProduction GetProduction(string name);

        public bool TryGetProduction(string name, out IProduction? production);
    }
}
