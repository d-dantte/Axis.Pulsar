using System.Collections.Immutable;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;

public static class ContentArgumentDelimiterExtensions
{
    public static ImmutableHashSet<char> DelimiterCharacterSet { get; } = Enum
        .GetValues<ContentArgumentDelimiter>()
        .Where(t => t != ContentArgumentDelimiter.None)
        .Select(t => t.DelimiterCharacter())
        .ToImmutableHashSet();

    public static char DelimiterCharacter(this ContentArgumentDelimiter type)
    {
        return type switch
        {
            ContentArgumentDelimiter.Quote => '\'',
            ContentArgumentDelimiter.DoubleQuote => '"',
            ContentArgumentDelimiter.Grave => '`',
            ContentArgumentDelimiter.Sol => '/',
            ContentArgumentDelimiter.BackSol => '\\',
            ContentArgumentDelimiter.VerticalBar => '|',
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public static ContentArgumentDelimiter DelimiterType(this char @char)
    {
        return @char switch
        {
            '\'' => ContentArgumentDelimiter.Quote,
            '"' => ContentArgumentDelimiter.DoubleQuote,
            '`' => ContentArgumentDelimiter.Grave,
            '/' => ContentArgumentDelimiter.Sol,
            '\\' => ContentArgumentDelimiter.BackSol,
            '|' => ContentArgumentDelimiter.VerticalBar,
            _ => ContentArgumentDelimiter.None
        };
    }
}
