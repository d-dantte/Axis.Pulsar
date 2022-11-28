using System.Linq;

namespace Axis.Pulsar.Grammar.Language
{
    /// <summary>
    /// Represents a repeatable encapsulation of a group of other rules, that enforces special processing semantics for the contained rules.
    /// </summary>
    public interface IRuleExpression : IAggregateRule, IRepeatable
    {
    }
}
