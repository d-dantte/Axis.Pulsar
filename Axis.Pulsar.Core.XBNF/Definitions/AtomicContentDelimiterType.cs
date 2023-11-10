using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.Definitions;


public enum AtomicContentDelimiterType
{
    None,

    /// <summary>
    /// '
    /// </summary>
    Quote,

    /// <summary>
    /// "
    /// </summary>
    DoubleQuote,

    /// <summary>
    /// `
    /// </summary>
    Grave,

    /// <summary>
    /// /
    /// </summary>
    Sol,

    /// <summary>
    /// \
    /// </summary>
    BackSol,

    /// <summary>
    /// |
    /// </summary>
    VerticalBar
}

public static class AtomicContentDelimiterTypeExtensions
{
    public static ImmutableHashSet<char> DelimiterCharacterSet { get; } = Enum
        .GetValues<AtomicContentDelimiterType>()
        .Where(t => t != AtomicContentDelimiterType.None)
        .Select(t => t.DelimiterCharacter())
        .ToImmutableHashSet();

    public static char DelimiterCharacter(this AtomicContentDelimiterType type)
    {
        return type switch
        {
            AtomicContentDelimiterType.Quote => '\'',
            AtomicContentDelimiterType.DoubleQuote => '"',
            AtomicContentDelimiterType.Grave => '`',
            AtomicContentDelimiterType.Sol => '/',
            AtomicContentDelimiterType.BackSol => '\\',
            AtomicContentDelimiterType.VerticalBar => '|',
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public static AtomicContentDelimiterType DelimiterType(this char @char)
    {
        return @char switch
        {
            '\'' => AtomicContentDelimiterType.Quote,
            '"' => AtomicContentDelimiterType.DoubleQuote,
            '`' => AtomicContentDelimiterType.Grave,
            '/' => AtomicContentDelimiterType.Sol,
            '\\' => AtomicContentDelimiterType.BackSol,
            '|' => AtomicContentDelimiterType.VerticalBar,
            _ => AtomicContentDelimiterType.None
        };
    }
}