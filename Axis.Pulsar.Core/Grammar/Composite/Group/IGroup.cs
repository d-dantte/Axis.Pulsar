using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    public interface IGroup : IGroupRule
    {
        /// <summary>
        /// Elements within this group
        /// </summary>
        ImmutableArray<IGroupRule> Elements { get; }
    }
}
