using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.XBNF;

public static class Extensions
{
    public static TItems AddItem<TItems, TItem>(this TItems items, TItem item)
    where TItems : ICollection<TItem>
    {
        ArgumentNullException.ThrowIfNull(items);

        items.Add(item);
        return items;
    }
}
