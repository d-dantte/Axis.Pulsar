using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    public interface IGroup : IGroupElement
    {
        /// <summary>
        /// Elements within this group
        /// </summary>
        ImmutableArray<IGroupElement> Elements { get; }
    }
}
