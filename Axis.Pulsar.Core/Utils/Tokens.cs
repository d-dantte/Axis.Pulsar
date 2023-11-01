using Axis.Luna.Common;
using Axis.Luna.Common.Utils;

namespace Axis.Misc.Pulsar.Utils
{
    public readonly struct Tokens :
        IEquatable<Tokens>,
        IReadonlyIndexer<int, char>,
        IDefaultValueProvider<Tokens>
    {
        private readonly string _source;
        private readonly int _offset;
        private readonly int _count;

        /// <summary>
        /// Lazy-loaded hash of the individual characters of this token.
        /// <para/>
        /// NOTE: Lazy-loaded becaus the some performance will be shaved off if this is calculated each time an instance is created.
        /// </summary>
        private readonly Lazy<int> _valueHash;

        public int Count => _count;

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();

                return _source[_offset + index];
            }
        }

        #region Empty
        public bool IsEmpty => !IsDefault && _count == 0;

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
            int offset,
            int length)
        {
            _source = sourceString ?? throw new ArgumentNullException(nameof(sourceString));
            _offset = offset < 0
                ? throw new ArgumentException($"Invalid {nameof(offset)}: {offset}")
                : offset;
            _count = length < 0
                ? throw new ArgumentException($"Invalid {nameof(length)}: {length}")
                : length;

            if (offset > sourceString.Length
                || sourceString.Length < (offset + length))
                throw new ArgumentException(
                    $"Invalid args. source-length:{sourceString.Length}, "
                    + $"offset:{offset}, segment-length:{length}");

            _valueHash = new Lazy<int>(() => sourceString
                .Substring(offset, length)
                .Aggregate(0, HashCode.Combine));
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

        public static Tokens Of(string sourceString) => new(sourceString);
        #endregion

        #region Implicits
        public static implicit operator Tokens(string sourceString) => new(sourceString);
        #endregion

        #region Slice
        public Tokens Slice(
            int offset,
            int length)
            => new(_source, offset + _offset, length);

        public Tokens Slice(
            int offset)
            => new(_source, offset + _offset, _count - offset);
        #endregion

        public override int GetHashCode() => HashCode.Combine(_offset, _count, _valueHash?.Value ?? 0);

        public override string? ToString() => _source?.Substring(_offset, _count);

        /// <summary>
        /// <see cref="Tokens"/> instances can be combined if they are contiguous, or if the LHS segment is
        /// the default-empty segment (see <see cref="Empty"/>).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool CanCombine(Tokens other)
        {
            if (other.IsDefault)
                return false;

            if (IsConsecutiveTo(other))
                return true;

            if (IsEmpty && (_source.Length == 0 || IsSourceEqual(other)))
                return true;

            return false;
        }

        /// <summary>
        /// Combines two consecutive <see cref="Tokens"/> instances, or when the current instance is empty and the second isn't default, returns the second instance.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Tokens CombineWith(Tokens other)
        {
            if (!CanCombine(other))
                throw new InvalidOperationException($"Invalid segment: non-combinable");

            if (IsEmpty)
                return other;

            if (other.IsEmpty)
                return this;

            return new Tokens(_source, _offset, _count + other._count);
        }

        public Tokens ExpandBy(int length) => new(_source, _offset, length + length);

        /// <summary>
        /// Returns a span representation of this token
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<char> AsSpan() => _source.AsSpan(_offset, _count);

        /// <summary>
        /// Calls <see cref="Tokens.CombineWith(Tokens)"/> on each consecutive items in the given sequence.
        /// </summary>
        /// <param name="segmentTokens">A sequence of consecutively related <see cref="Tokens"/> instances</param>
        /// <returns>A new instance that is a combination of all the given consecutive instances</returns>
        internal static Tokens Combine(IEnumerable<Tokens> segmentTokens)
        {
            ArgumentNullException.ThrowIfNull(segmentTokens);

            return segmentTokens.Aggregate(
                Tokens.Empty,
                (segmentToken, next) => segmentToken.CombineWith(next));
        }

        #region Relationship Checks

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
        /// <item><c>_offset + _length</c> of this segment must be equal to <c>_offset</c> of <paramref name="other"/></item>
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

            return other._offset == _offset + _count;
        }

        /// <summary>
        /// Checks if the current instance overlaps the given instance.
        /// <para/>
        /// To overlap:
        /// <list type="number">
        /// <item>The source of both instances must be equal</item>
        /// <item>The first instance (lhs) must have an offset less than or equal to the offset of the second instance (rhs).</item>
        /// <item>The length + offset of the of the first instance (lhs) must be greater than or equal to the offset of the second instance (rhs).</item>
        /// <item>The offset + length of the second instance (rhs) must be greater than or equal to the offset + length of the first instance (lhs).</item>
        /// </list>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsOverlapping(Tokens other)
        {
            if (IsDefault || other.IsDefault)
                return false;

            if (!IsSourceEqual(other))
                return false;

            if (_offset > other._offset)
                return false;

            var lhsEnd = _offset + _count;
            if (lhsEnd < other._offset)
                return false;

            var rhsEnd = other._offset + other._count;
            if (rhsEnd < lhsEnd)
                return false;

            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is Tokens other && Equals(other);
        }

        public bool Equals(string? value)
        {
            if (this.IsDefault && value is null)
                return true;

            else if (this.IsDefault ^ value is null)
                return false;

            if (value!.Length != _count)
                return false;

            var comparer = EqualityComparer<string>.Default;
            if (_count == _source.Length
                && (ReferenceEquals(_source, value) || comparer.Equals(_source, value)))
                return true;

            for (int cnt = 0; cnt < _count; cnt++)
            {
                if (this[cnt] != value[cnt])
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

            if (value.Length != _count)
                return false;

            for (int cnt = 0; cnt < _count; cnt++)
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

            if (other.Count != _count)
                return false;

            if (IsSourceEqual(other) && _offset == other._offset)
                return true;

            for (int cnt = 0; cnt < _count; cnt++)
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
        #endregion
    }
}