using Axis.Pulsar.Production;
using System.Collections.Generic;

namespace Axis.Pulsar
{
    public interface ILanguageSource
    {
        string Id { get; }
        ProductionMap Grammar { get; }
        IEnumerable<ImportRef> Imports { get; }
    }
}
