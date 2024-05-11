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

    public static IEnumerable<T> ThrowIfDuplicate<T>(this
        IEnumerable<T> items,
        IEqualityComparer<T> equalityComparer,
        Func<T, Exception> exceptionProvider)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(equalityComparer);
        ArgumentNullException.ThrowIfNull(exceptionProvider);

        var hashSet = new HashSet<T>();
        foreach (var item in items)
        {
            if (!hashSet.Add(item))
                throw exceptionProvider.Invoke(item);

            else yield return item;
        }
    }

    public static IEnumerable<T> ThrowIfDuplicate<T>(this
        IEnumerable<T> items,
        Func<T, Exception> exceptionProvider)
        => ThrowIfDuplicate(items, EqualityComparer<T>.Default, exceptionProvider);
}
