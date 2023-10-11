using Axis.Luna.Common;

namespace Axis.Misc.Pulsar.Utils
{
    public readonly struct Tokens :
        IEquatable<Tokens>,
        IDefaultValueProvider<Tokens>
    {
        private readonly string _source;
        private readonly int _offset;
        private readonly int _length;
        private readonly Lazy<int> _valueHash;

        public int Length => _length;

        #region Empty
        public bool IsEmpty => _source?.Length == 0 || _source?.Length == _offset || _length == 0;

        /// <summary>
        /// Returns the DEFAULT-EMPTY segment: a segment whose source-string is the empty string (see <see cref="string.Empty"/>)
        /// </summary>
        public static Tokens Empty { get; } = new(string.Empty, 0, 0);
        #endregion

        #region DefaultValueProvider
        public bool IsDefault => _source is null && _offset == 0 && _length == 0 && _valueHash is null;

        public static Tokens Default => default;
        #endregion

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new IndexOutOfRangeException();

                return _source[_offset + index];
            }
        }

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
            _length = length < 0
                ? throw new ArgumentException($"Invalid {nameof(length)}: {length}")
                : length;

            if (offset > sourceString.Length
                || sourceString.Length < (offset + length))
                throw new ArgumentException(
                    $"Invalid args. source-length:{sourceString.Length}, "
                    + $"offset:{offset}, segment-length:{length}");

            _valueHash = new Lazy<int>(() => sourceString.Aggregate(0, HashCode.Combine));
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
            => new(_source, offset + _offset, _length - offset);
        #endregion

        public override int GetHashCode() => HashCode.Combine(_offset, _length, _valueHash.Value);

        public override string ToString() => _source?.Substring(_offset, _length) ?? string.Empty;

        public bool IsRelative(Tokens other) => ReferenceEquals(_source, other._source);

        /// <summary>
        /// Checks if the given segment is contiguous with the current one - meaning <paramref name="other"/> continues right where this
        /// segment leaves off. 
        /// <para/>
        /// To be contiguous, the following needs to be true:
        /// <list type="number">
        /// <item>Both segments must have equivalent backing strings, i.e, <see cref="SourceEquals(Tokens)"/> must return true</item>
        /// <item><c>_offset + _length</c> of this segment must be equal to <c>_offset</c> of <paramref name="other"/></item>
        /// </list>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsContiguousWith(Tokens other)
        {
            if (IsDefault || other.IsDefault)
                return false;

            if (!SourceEquals(other))
                return false;

            return other._offset == _offset + _length;
        }

        /// <summary>
        /// <see cref="Tokens"/> instances can be combined if they are contiguous, or if the LHS segment is
        /// the default-empty segment (see <see cref="Empty"/>).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool CanCombine(Tokens other)
        {
            if (IsContiguousWith(other))
                return true;

            else if (IsEmpty && _source.Length == 0)
                return true;

            return false;
        }

        /// <summary>
        /// Combines two consecutive <see cref="Tokens"/> instances.
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

            return new Tokens(_source, _offset, _length + other._length);
        }

        public Tokens ExpandBy(int length) => new Tokens(_source, _offset, length + length);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<char> AsSpan() => _source.AsSpan(_offset, _length);

        #region Equality
        public override bool Equals(object? obj)
        {
            return obj is Tokens other
                && this.Equals(other);
        }

        public bool Equals(string value) => Equals(value.ToCharArray());

        public bool Equals(char value) => Equals(new[] { value });

        public bool Equals(char[] value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (this.IsDefault)
                return false;

            if (value.Length != _length)
                return false;

            for (int cnt = 0; cnt < _length; cnt++)
            {
                if (this[cnt] != value[cnt])
                    return false;
            }

            return true;
        }

        public bool Equals(Tokens other)
        {
            if (other.Length != _length)
                return false;

            if ((SourceEquals(other) || _valueHash.Value == other._valueHash.Value)
                && _offset == other._offset)
                return true;

            for (int cnt = 0; cnt < _length; cnt++)
            {
                if (this[cnt + _offset] != other[cnt])
                    return false;
            }

            return true;
        }

        public bool SourceEquals(Tokens other)
        {
            return ReferenceEquals(_source, other._source)
                || _source.Equals(other._source);
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