using Axis.Luna.Common;
using Axis.Luna.Common.Utils;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Utils
{
    public readonly struct Tokens :
        IEquatable<Tokens>,
        IReadonlyIndexer<int, char>,
        IDefaultValueProvider<Tokens>
    {
        private readonly string? _source;
        private readonly Segment _sourceSegment;

        /// <summary>
        /// Lazy-loaded hash of the individual characters of this token.
        /// <para/>
        /// NOTE: Lazy-loaded becaus the some performance will be shaved off if this is calculated each time an instance is created.
        /// </summary>
        private readonly Lazy<int> _valueHash;

        public Segment SourceSegment => _sourceSegment;

        public string? Source => _source;

        public bool IsDefaultOrEmpty => IsDefault || IsEmpty;

        #region Indexers

        public char this[int index]
        {
            get
            {
                if (IsDefault)
                    throw new InvalidOperationException($"Invalid token instance: default");

                if (index < 0 || index >= _sourceSegment.Length)
                    throw new IndexOutOfRangeException();

                return _source![_sourceSegment.Offset + index];
            }
        }

        public char this[Index index] => index.IsFromEnd switch
        {
            true => this[_sourceSegment.Length - index.Value],
            false => this[index.Value]
        };

        public Tokens this[Range range] => range
            .GetOffsetAndLength(_sourceSegment.Length)
            .ApplyTo(Slice);

        #endregion

        #region Empty
        public bool IsEmpty => !IsDefault && _sourceSegment.Length == 0;

        /// <summary>
        /// Returns the DEFAULT-EMPTY segment: a segment whose source-string is the empty string (see <see cref="string.Empty"/>)
        /// </summary>
        public static Tokens Empty { get; } = new(string.Empty, 0, 0);
        #endregion

        #region DefaultValueProvider
        public bool IsDefault => _source is null;

        public static Tokens Default => default;
        #endregion

        #region Constructors

        public Tokens(
            string sourceString,
            Segment tokenSegment)
        {
            _source = sourceString ?? throw new ArgumentNullException(nameof(sourceString));
            _sourceSegment = tokenSegment;

            if (tokenSegment.Offset > sourceString.Length
                || sourceString.Length < (tokenSegment.Offset + tokenSegment.Length))
                throw new ArgumentException(
                    $"Invalid args. source-length:{sourceString.Length}, "
                    + $"offset:{tokenSegment.Offset}, segment-length:{tokenSegment.Length}");

            _valueHash = new Lazy<int>(() => sourceString
                .Substring(tokenSegment.Offset, tokenSegment.Length)
                .Aggregate(0, HashCode.Combine));
        }

        public Tokens(
            string sourceString,
            int offset,
            int length)
            : this(sourceString, Segment.Of(
                offset < 0
                    ? throw new ArgumentException($"Invalid {nameof(offset)}: {offset}")
                    : offset,
                length < 0
                    ? throw new ArgumentException($"Invalid {nameof(length)}: {length}")
                    : length))
        {
        }

        public Tokens(string sourceString, int offset)
            : this(sourceString, offset, sourceString?.Length - offset ?? 0)
        {
        }

        public Tokens(string sourceString)
            : this(sourceString, 0, sourceString?.Length ?? 0)
        {
        }
        #endregion

        #region Of
        public static Tokens Of(
            string sourceString,
            int offset,
            int length)
            => new(sourceString, offset, length);

        public static Tokens Of(
            string sourceString,
            int offset)
            => new(sourceString, offset);

        public static Tokens Of(
            string sourceString,
            Segment tokenSegment)
            => new(sourceString, tokenSegment);

        public static Tokens Of(string sourceString) => new(sourceString);
        #endregion

        #region Implicits
        public static implicit operator Tokens(string sourceString) => new(sourceString);

        public static implicit operator string(Tokens tokens) => tokens.ToString()!;
        #endregion

        #region Slice
        public Tokens Slice(
            int offset,
            int length)
            => new(_source!, offset + _sourceSegment.Offset, length);

        public Tokens Slice(
            int offset)
            => new(_source!, offset + _sourceSegment.Offset, _sourceSegment.Length - offset);
        #endregion

        public override int GetHashCode() => HashCode.Combine(_sourceSegment.Offset, _sourceSegment.Length, _valueHash?.Value ?? 0);

        public override string? ToString()
        {
            if (_source is null)
                return null;

            if (_sourceSegment.Offset == 0 && _sourceSegment.Length == _source.Length)
                return _source;

            else return _source.Substring(_sourceSegment.Offset, _sourceSegment.Length);
        }

        public char[]? ToArray()
        {
            return !IsDefault
                ? AsSpan().ToArray()
                : null;
        }

        /// <summary>
        /// Merge two intersecting <see cref="Tokens"/> instances.
        /// </summary>
        /// <param name="other">The token instance to merge with</param>
        /// <returns></returns>
        public Tokens Merge(Tokens other)
        {
            if (IsDefault || other.IsDefault)
                throw new ArgumentException("Cannot merge default tokens");

            if (IsEmpty)
                return other;

            if (other.IsEmpty)
                return this;

            if (!Intersects(this, other))
                throw new ArgumentException("Cannot merge non-intersecting tokens");

            return Of(_source!, _sourceSegment + other._sourceSegment);
        }

        /// <summary>
        /// Joins two consecutive <see cref="Tokens"/> instances, non-default instances. See <see cref="Tokens.IsConsecutiveTo(Tokens)"/>
        /// <para/>Note that this method is commutative. i.e:
        /// <code>
        /// Token x = ...;
        /// Token y = ...;
        /// 
        /// // assume both are consecutive.
        /// Token z1 = x.Join(y);
        /// Token z2 = y.Join(x);
        /// z1.Equals(z2) // &lt;-- will be true.
        /// </code>
        /// </summary>
        /// <param name="other">The token instance to concatenate</param>
        /// <returns>The new token created from joining both tokens</returns>
        /// <exception cref="InvalidOperationException">If the tokens cannot be joined</exception>
        public Tokens ConJoin(Tokens other)
        {
            if (!CanJoin(other))
                throw new InvalidOperationException($"Invalid segment: non-combinable");

            if (IsEmpty)
                return other;

            if (other.IsEmpty)
                return this;

            return new Tokens(
                _source!,
                SourceSegment + other.SourceSegment);
        }

        /// <summary>
        /// Expand this token by the given number of characters to the right
        /// </summary>
        /// <param name="count">Number of characters by which we will expand this token instance</param>
        /// <returns></returns>
        public Tokens ExpandBy(int count) => new(_source!, _sourceSegment.Offset, count + count);

        /// <summary>
        /// Returns a span representation of this token
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<char> AsSpan()
            => !IsDefault
                ? _source.AsSpan(_sourceSegment.Offset, _sourceSegment.Length)
                : default;

        /// <summary>
        /// Calls <see cref="Tokens.ConJoin(Tokens)"/> on each consecutive items in the given sequence.
        /// </summary>
        /// <param name="segmentTokens">A sequence of consecutively related <see cref="Tokens"/> instances</param>
        /// <returns>A new instance that is a combination of all the given consecutive instances</returns>
        internal static Tokens Join(IEnumerable<Tokens> segmentTokens)
        {
            ArgumentNullException.ThrowIfNull(segmentTokens);

            return segmentTokens.Aggregate(
                Tokens.Empty,
                (segmentToken, next) => segmentToken.ConJoin(next));
        }

        #region Relationship Checks

        /// <summary>
        /// Returns true if the tokens are consecutive, or either one is empty.
        /// </summary>
        private bool CanJoin(Tokens other)
        {
            if (IsDefault || other.IsDefault)
                return false;

            if (IsEmpty || other.IsEmpty)
                return true;

            if (IsConsecutiveTo(other) || other.IsConsecutiveTo(this))
                return true;

            return false;
        }

        public bool IsSourceRefEqual(Tokens other) => ReferenceEquals(_source, other._source);

        public bool IsSourceEqual(Tokens other)
        {
            var stringComparer = EqualityComparer<string>.Default;
            return IsSourceRefEqual(other) || stringComparer.Equals(_source, other._source);
        }

        public bool IsValueHashEqual(Tokens other)
        {
            // both tokens are default
            if (_valueHash is null && other._valueHash is null)
                return true;

            else if (_valueHash is null ^ other._valueHash is null)
                return false;

            else return (_valueHash?.Value ?? 0) == (other._valueHash?.Value ?? 0);
        }

        /// <summary>
        /// Checks if the given token directly succeeds this instance.
        /// <para/>
        /// To be consecutive, the following needs to be true:
        /// <list type="number">
        /// <item>Both segments must have equivalent backing strings, i.e, <see cref="IsSourceEqual(Tokens)"/> must return true</item>
        /// <item><c>_sourceSegment.Offset + _length</c> of this segment must be equal to <c>_sourceSegment.Offset</c> of <paramref name="other"/></item>
        /// </list>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsConsecutiveTo(Tokens other)
        {
            if (IsDefault || other.IsDefault)
                return false;

            if (!IsSourceEqual(other))
                return false;

            return other._sourceSegment.Offset == _sourceSegment.Offset + _sourceSegment.Length;
        }

        /// <summary>
        /// Checks if the current instance intersects with the given instance.
        /// <para/>
        /// To overlap:
        /// <list type="number">
        /// <item>Either token may be empty. </item>
        /// <item>The source of both instances must be equal</item>
        /// <item>Some element (index) of one instance must be found as an element in the other instance</item>
        /// </list>
        /// </summary>
        /// <param name="second">The instance to check for intersection</param>
        /// <returns>True if both instances intersect, false otherwise</returns>
        public static bool Intersects(Tokens first, Tokens second)
        {
            if (first.IsDefault || second.IsDefault)
                return false;

            if (first.IsEmpty || second.IsEmpty)
                return true;

            if (!first.IsSourceEqual(second))
                return false;

            return first._sourceSegment.Intersects(second._sourceSegment);
        }

        public override bool Equals(object? obj)
        {
            return obj is Tokens other && Equals(other);
        }

        public bool Equals(string? value)
        {
            var other = value is null
               ? Tokens.Default
               : Tokens.Of(value!);

            return Equals(other);
        }

        public bool Equals(string? value, bool isCaseSensitive)
        {
            if (isCaseSensitive)
                return Equals(value);

            if (IsDefault && value is null)
                return true;

            if (IsEmpty && string.IsNullOrEmpty(value))
                return true;

            if (value!.Length != _sourceSegment.Length)
                return false;

            for (int index = 0; index < _sourceSegment.Length; index++)
            {
                if (char.ToLowerInvariant(value[index]) != char.ToLowerInvariant(this[index]))
                    return false;
            }
            return true;
        }

        public bool Equals(char value) => Equals(new[] { value });

        public bool Equals(char[] value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (this.IsDefault)
                return false;

            if (value.Length != _sourceSegment.Length)
                return false;

            for (int cnt = 0; cnt < _sourceSegment.Length; cnt++)
            {
                if (this[cnt] != value[cnt])
                    return false;
            }

            return true;
        }

        public bool Equals(Tokens other)
        {
            if (IsDefault && other.IsDefault)
                return true;

            if (_source is null ^ other._source is null)
                return false;

            if (IsEmpty && other.IsEmpty)
                return true;

            if (other.SourceSegment.Length != _sourceSegment.Length)
                return false;

            if (IsSourceEqual(other) && _sourceSegment.Offset == other._sourceSegment.Offset)
                return true;

            for (int cnt = 0; cnt < _sourceSegment.Length; cnt++)
            {
                if (this[cnt] != other[cnt])
                    return false;
            }

            return true;
        }

        public static bool operator ==(Tokens left, Tokens right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Tokens left, Tokens right)
        {
            return !(left == right);
        }

        public static Tokens operator +(Tokens left, Tokens right) => left.ConJoin(right);
        #endregion

        public bool Contains(string substring)
        {
            if (IsDefault)
                return false;

            if (substring is null)
                return false;

            return _source!.IndexOf(substring, _sourceSegment.Offset, _sourceSegment.Length) >= 0;
        }

        public bool Contains(Tokens subtokens)
        {
            if (IsDefault ^ subtokens.IsDefault)
                return false;

            if (subtokens.SourceSegment.Length > SourceSegment.Length)
                return false;

            return AsSpan().Contains(subtokens.AsSpan(), StringComparison.InvariantCulture);
        }

        public bool Contains(char c) => ContainsAny(c);

        public bool ContainsAny(params char[] chars)
        {
            if (IsDefault)
                return false;

            return _source!.IndexOfAny(chars, _sourceSegment.Offset, _sourceSegment.Length) >= 0;
        }
    }
}