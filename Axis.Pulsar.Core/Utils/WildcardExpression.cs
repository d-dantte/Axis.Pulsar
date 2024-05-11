using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Luna.Result;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Utils
{
    /// <summary>
    /// Represents a fixed-length pattern used to match strings. It matches each character based on it's position, where the wildcard character
    /// matches any character of the input in the specific position.
    /// <para/>
    /// The textual representation of the wildcard expression is based on the following syntax rule:
    /// <list type="number">
    /// <item>All string characters are represented as-is</item>
    /// <item>Except for the '.' character, which represents the wildcard character</item>
    /// <item>...and the '\' which is used to escape the wildcard character</item>
    /// </list>
    /// 
    /// Examples:
    /// <code>
    /// "abcd" -> abcd
    /// "ab.d" -> ab{wildcard}d
    /// "abcd\.efgh" -> abcd.efgh
    /// "abcd\\efgh" -> abcd\efgh
    /// </code>
    /// </summary>
    public readonly struct WildcardExpression:
        IDefaultValueProvider<WildcardExpression>,
        IEquatable<WildcardExpression>
    {
        public static readonly int WILD_CARD_CHAR = char.MaxValue + 1;

        private readonly ImmutableArray<int> characters;
        private readonly bool isCaseSensitive;

        public int Length => IsDefault ? 0 : characters.Length;

        public bool IsCaseSensitive => isCaseSensitive;

        public static WildcardExpression Default => default;

        public bool IsDefault => characters.IsDefault;

        private WildcardExpression(int[] characters, bool isCaseSensitive)
        {
            this.isCaseSensitive = isCaseSensitive;
            this.characters = characters.ToImmutableArray();
        }

        public bool IsMatch(string literal)
        {
            if (literal == null)
                return IsDefault;

            return IsMatch(literal.AsSpan());
        }

        public bool IsMatch(ReadOnlySpan<char> literal)
        {
            if (literal.Length == 0)
                return !IsDefault && characters.Length == 0;

            if (IsDefault)
                return false;

            if (characters.Length != literal.Length)
                return false;

            for (int index = 0; index < characters.Length; index++)
            {
                var chr = characters[index];

                if (chr == WILD_CARD_CHAR)
                    continue;

                else if (isCaseSensitive && chr != literal[index])
                    return false;

                else if (!isCaseSensitive &&
                    char.ToLowerInvariant((char)chr) != char.ToLowerInvariant(literal[index]))
                    return false;
            }
            return true;
        }

        public static IResult<WildcardExpression> Parse(string literal) => Parse(literal, true);

        public static IResult<WildcardExpression> Parse(string literal, bool isCaseSensitive)
        {
            ArgumentNullException.ThrowIfNull(literal);

            return Result.Of(() =>
            {
                var characters = literal
                    .Aggregate(new List<int?> { -1 }, (list, chr) => (chr, list[^1]) switch
                    {
                        ('\\', int) => list.AddItem((int?)null),
                        ('\\', null) => list.InsertItem(^1, (int?)'\\'),

                        ('.', int) => list.AddItem((int?)WILD_CARD_CHAR),
                        ('.', null) => list.InsertItem(^1, (int?)'.'),

                        (_, null) => throw new FormatException($"Invalid escape sequence: \\{chr}"),
                        (_, _) => list.AddItem((int?)chr)
                    })
                    .Skip(1)
                    .Select(v => v!.Value)
                    .ToArray();

                return new WildcardExpression(characters, isCaseSensitive);
            });
        }

        public static implicit operator WildcardExpression(string literal) => Parse(literal, true).Resolve();

        public bool Equals(WildcardExpression other)
        {
            if (other.isCaseSensitive != isCaseSensitive)
                return false;

            if (other.characters.IsDefault && characters.IsDefault)
                return true;

            if (other.characters.Length != characters.Length)
                return false;

            return characters.SequenceEqual(other.characters);
        }

        public override bool Equals(object? obj)
        {
            return obj is WildcardExpression other && Equals(other);
        }

        public override int GetHashCode()
        {
            return characters.Aggregate(isCaseSensitive.GetHashCode(), HashCode.Combine);
        }

        public static bool operator ==(WildcardExpression left, WildcardExpression right) => left.Equals(right);
        public static bool operator !=(WildcardExpression left, WildcardExpression right) => !left.Equals(right);
    }
}
