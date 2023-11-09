using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Lang;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core
{
    public interface ILanguageContext
    {
        IGrammar Grammar { get; }

        ImmutableDictionary<string, IProductionValidator> ProductionValidators { get; }
    }
}
