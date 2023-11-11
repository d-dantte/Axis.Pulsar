using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.XBNF;

public class Whitespace :
    ISilentElement,
    IEquatable<Whitespace>
{
    public WhitespaceChar Char { get; }

    public string Content => Char switch
    {
        WhitespaceChar.Space => " ",
        WhitespaceChar.Tab => "\t",
        WhitespaceChar.LineFeed => "\n",
        WhitespaceChar.CarriageReturn => "\r",
        _ => throw new InvalidOperationException($"Invalid whitespace char: {Char}")
    };

    public Whitespace(WhitespaceChar @char)
    {
        Char = @char.ThrowIfNot(
            Enum.IsDefined,
            new ArgumentOutOfRangeException(nameof(@char)));
    }

    public static Whitespace Of(WhitespaceChar @char) => new(@char);

    public static implicit operator Whitespace(WhitespaceChar @char) => new(@char);

    public static implicit operator Whitespace(char @char) => new((WhitespaceChar)@char);

    public override string ToString() => Content;

    public override int GetHashCode() => HashCode.Combine(Char);

    public override bool Equals(object? obj)
    {
        return obj is Whitespace other && Equals(other);
    }

    public bool Equals(Whitespace? other)
    {
        return other is not null
            && other.Char == Char;
    }

    public static bool operator ==(Whitespace first, Whitespace second)
    {
        return
            first is null && second is null ? true :
            first is null ^ second is null ? false :
            first!.Equals(second);
    }

    public static bool operator !=(Whitespace first, Whitespace second)
    {
        return !(first == second);
    }

    #region Nested types
    public enum WhitespaceChar
    {
        Space = ' ',

        Tab = '\t',

        LineFeed = '\n',

        CarriageReturn = '\r'
    }
    #endregion
}
