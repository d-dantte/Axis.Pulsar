using Axis.Luna.Common;
using Axis.Luna.Common.Indexers;
using Axis.Luna.Extensions;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace Axis.Pulsar.Core.Utils
{
    using LunaSegment = Luna.Common.Segments.Segment;

    public readonly struct Tokens :
        IEnumerable<char>,
        IEquatable<Tokens>,
        IReadonlyIndexer<int, char>,
        IDefaultValueProvider<Tokens>
    {
        private readonly LunaSegment _segment;
        private readonly string? _source;
        private readonly DeferredValue<int>? _sourceHash;

        #region Construction
        public Tokens(string source)
            : this(source, LunaSegment.Of(0, source.Length))
        { }

        public Tokens(string source, LunaSegment segment)
        {
            _source = source;
            _segment = segment;
            _sourceHash = source is null ? null : new DeferredValue<int>(() =>
            {
                var hash = 0;
                for (int cnt = segment.Offset; cnt < segment.Count; cnt++)
                    hash = HashCode.Combine(hash, source[cnt]);

                return hash;
            });

            // validate
            if (source is null && !segment.IsDefault)
                throw new InvalidOperationException(
                    $"Invalid argument combination: "
                    + $"[{nameof(source)}: null, {nameof(segment)}: non-default] ");

            var soffset = segment.Offset;
            if (soffset < 0)
                throw new ArgumentOutOfRangeException(nameof(segment));

            var sourceLength = source?.Length ?? 0;
            if (sourceLength > 0
                && soffset + segment.Count > sourceLength)
                throw new ArgumentOutOfRangeException(nameof(segment));
        }

        public static implicit operator Tokens(string @string) => new(
            segment: LunaSegment.Of(0, @string?.Length ?? 0),
            source: @string!);

        public static implicit operator string(Tokens tokens) => tokens.ToString()!;

        public static Tokens Of(string @string) => @string;

        public static Tokens Of(string @string, int offset) => new(
            segment: LunaSegment.Of(offset, (@string?.Length ?? 0) - offset),
            source: @string!);

        public static Tokens Of(string @string, int offset, int count) => new(
            segment: LunaSegment.Of(offset, count),
            source: @string);

        /// <summary>
        /// Creates an empty token from the source, at the given offset.
        /// </summary>
        /// <param name="string">The source string</param>
        /// <param name="offset">The start offset</param>
        public static Tokens EmptyAt(string @string, int offset) => new(
            segment: LunaSegment.Of(offset, 0),
            source: @string);

        public static Tokens Empty { get; } = Tokens.EmptyAt(string.Empty, 0);

        public static Tokens Of(string @string, LunaSegment segment) => new(
            segment: segment,
            source: @string);

        #endregion

        #region Properties

        /// <summary>
        /// The source string
        /// </summary>
        public string? Source => _source;

        /// <summary>
        /// The segment instance
        /// </summary>
        public LunaSegment Segment => _segment;

        /// <summary>
        /// Indicates if this instance's <see cref="Tokens.Segment"/>.Count has a value of zero, irrespective of the <see cref="Tokens.Source"/>.
        /// </summary>
        public bool IsEmpty => _segment.Count == 0;

        /// <summary>
        /// Indicates if this instance is default, or empty, according to the definitions of both states.
        /// </summary>
        public bool IsDefaultOrEmpty => IsDefault || IsEmpty;

        #endregion

        #region DefaultValueProvider

        /// <summary>
        /// Indicates if this instance is the default value
        /// </summary>
        public bool IsDefault => _source is null && _segment.IsDefault;

        /// <summary>
        /// Returns the default <see cref="Tokens"/> value.
        /// </summary>
        public static Tokens Default => default;

        #endregion

        #region IReadonlyIndexer

        public char this[int index]
        {
            get
            {
                if (IsEmpty)
                    throw new InvalidOperationException($"Invalid token instance: empty");

                if (index < 0 || index >= _segment.Count)
                    throw new IndexOutOfRangeException();

                return _source![_segment.Offset + index];
            }
        }

        #endregion

        #region Index & Range

        public char this[Index index] => index.IsFromEnd switch
        {
            true => this[_segment.Count - index.Value],
            false => this[index.Value]
        };

        public Tokens this[Range range] => range
            .GetOffsetAndLength(_segment.Count)
            .ApplyTo(Slice);

        public Tokens Slice(
            int offset,
            int length)
            => new(_source!, LunaSegment.Of(offset + _segment.Offset, length));

        public Tokens Slice(
            int offset)
            => new(_source!, LunaSegment.Of(offset + _segment.Offset, _segment.Count - offset));

        #endregion

        #region object Overrides

        public override int GetHashCode() => HashCode.Combine(
            _segment,
            _sourceHash?.Value ?? 0);

        public override string? ToString()
        {
            if (_source is null)
                return null;

            if (_segment.Offset == 0 && _segment.Count == _source.Length)
                return _source;

            else return _source.Substring(_segment.Offset, _segment.Count);
        }

        public override bool Equals(
            [NotNullWhen(true)] object? obj)
            => obj is Tokens other && Equals(other);

        public static bool operator==(Tokens left, Tokens right) => left.Equals(right);

        public static bool operator!=(Tokens left, Tokens right) => !left.Equals(right);

        #endregion

        #region IEnumerable

        public IEnumerator<char> GetEnumerator() => new TokenEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region IEquatable

        public bool Equals(Tokens other) => Equals(other, true);

        #endregion

        #region API

        public ReadOnlySpan<char> AsSpan()
        {
            if (IsDefault)
                throw new InvalidOperationException("Invalid state: default");

            return _source.AsSpan(_segment.Offset, _segment.Count);
        }

        public char[] ToArray() => AsSpan().ToArray();

        #region Expand
        /// <summary>
        /// Grow or shrink this token by the given number of characters.
        /// </summary>
        /// <param name="count">Number of characters to add to or remove from the total number of characters in this instance</param>
        /// <returns></returns>
        public Tokens ExpandBy(int count) => Of(_source!, _segment.Offset, _segment.Count + count);

        public static Tokens operator +(Tokens left, int right) => left.ExpandBy(right);

        public static Tokens operator -(Tokens left, int right) => left.ExpandBy(-right);

        #endregion

        #region Merge

        /// <summary>
        /// Merge two <see cref="Tokens_old"/> instances.
        /// <para/>
        /// Note: merging a default Token with a non-default token yields the non-default token.
        /// Note: merging any non-default token with an empty token yields the non-default token.
        /// </summary>
        /// <param name="other">The token instance to merge with.</param>
        /// <returns></returns>
        public Tokens MergeWith(Tokens other)
        {
            if (IsDefault && other.IsDefault)
                return this;

            if (IsDefault)
                return other;

            if (other.IsDefault)
                return this;

            if (IsEmpty)
                return other;

            if (other.IsEmpty)
                return this;

            if (!EqualityComparer<string>.Default.Equals(_source, other._source))
                throw new InvalidOperationException("Invalid merge: unequal sources");

            return Of(_source!, _segment + other._segment);
        }

        public static Tokens operator +(Tokens left, Tokens right) => left.MergeWith(right);

        public static Tokens Merge(Tokens first, Tokens second) => first.MergeWith(second);

        #endregion

        #region Contains
        public bool Contains(string substring)
        {
            if (substring is null)
                return false;

            if (IsDefaultOrEmpty)
                return false;

            return 0 <= _source!.IndexOf(
                substring,
                _segment.Offset,
                _segment.Count,
                StringComparison.InvariantCulture);
        }

        public bool Contains(Tokens subtokens)
        {
            if (subtokens.IsDefault)
                return false;

            if (IsDefaultOrEmpty)
                return false;

            if (subtokens._segment.Count > _segment.Count)
                return false;

            return AsSpan().Contains(subtokens.AsSpan(), StringComparison.InvariantCulture);
        }
         
        public bool Contains(char c) => ContainsAny(c);

        public bool ContainsAny(params char[] chars)
        {
            if (IsDefault)
                return false;

            return _source!.IndexOfAny(chars, _segment.Offset, _segment.Count) >= 0;
        }
        #endregion

        #region Split
        /// <summary>
        /// Splits this token into an array of tokens seprated by the given delimiters
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="delimiters">The delimiters to split with</param>
        /// <returns></returns>
        public (Tokens Delimiter, Tokens Tokens)[] Split(
            int offset,
            int length,
            params Tokens[] delimiters)
        {
            #region Validate
            if (offset < 0 || offset >= _segment.Count)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (length + offset > _segment.Count)
                throw new ArgumentOutOfRangeException(nameof(length));

            var delims = delimiters
                .ThrowIfNull(
                    () => new ArgumentNullException(nameof(delimiters)))
                .ThrowIf(
                    d => d.IsEmpty(),
                    _ => new ArgumentException($"Invalid '{nameof(delimiters)}': empty array"))
                .ThrowIfAny(
                    d => d.IsDefaultOrEmpty,
                    _ => new ArgumentException($"Invalid delimiter: default/empty"));
            #endregion

            var auxTokens = Slice(offset, length);
            var parts = new List<(Tokens Delimiter, Tokens Tokens)>();

            // delimiter matchers
            var delimMatchers = delims
                .OrderByDescending(delim => delim._segment.Count)
                .Select(delim => SubstringMatcher.LookAheadMatcher.Of(delim, auxTokens, 0))
                .ToArray();

            for (int cnt = 0; cnt < length; cnt++)
            {
                (bool isMatch, Tokens delim) match = default;
                foreach (var matcher in delimMatchers)
                {
                    if (matcher.TryNextWindow(out var matched)
                        && matched && !match.isMatch)
                        match = (true, matcher.Pattern);
                }

                if (match.isMatch)
                {
                    parts.Add((
                        Delimiter: match.delim,
                        Tokens: Tokens.EmptyAt(_source!, offset + cnt + match.delim.Segment.Count)));

                    // skip all matchers by match.delim.SourceSegment.Length characters
                    var skipCount = match.delim._segment.Count - 1;
                    delimMatchers.ForEvery(m => m.Advance(skipCount));
                    cnt += skipCount;
                }

                else if (parts.Count == 0)
                    parts.Add((
                        Delimiter: string.Empty,
                        Tokens: auxTokens.Slice(cnt, 1)));

                else parts[^1] = (
                    parts[^1].Delimiter,
                    Tokens: parts[^1].Tokens + 1);
            }

            return parts.ToArray();
        }

        public (Tokens Delimiter, Tokens Tokens)[] Split(
            int offset,
            params Tokens[] delimiters)
            => Split(offset, _segment.Count - offset, delimiters);

        public (Tokens Delimiter, Tokens Tokens)[] Split(
            params Tokens[] delimiters)
            => Split(0, _segment.Count, delimiters);
        #endregion

        /// <summary>
        /// Indicates that the tokens have equivalent sources, and their segments overlap
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IntersectsWith(Tokens other)
        {
            if (IsDefault && other.IsDefault)
                return true;

            return
                EqualityComparer<string>.Default.Equals(_source, other._source)
                && _segment.Intersects(other._segment);
        }

        public static bool Intersects(Tokens first, Tokens second) => first.IntersectsWith(second);

        /// <summary>
        /// Indicates that the sources of both instances are equal, and this instance is directly preceeding the given instance.
        /// <para/>
        /// Note: directly preceeding means this token ends exactly one character before
        /// the start of the <paramref name="successor"/> instance.
        /// </summary>
        /// <param name="successor">The successor instance</param>
        /// <returns>True if this instance preceeds the given instance, false otherwise</returns>
        public bool Preceeds(Tokens successor)
        {
            if (IsDefault || successor.IsDefault)
                return false;

            return
                EqualityComparer<string>.Default.Equals(_source, successor._source)
                && _segment.EndOffset + 1 == successor._segment.Offset;
        }

        /// <summary>
        /// Indicates that the sources of both instances are equal, and this instance is directly succeeding the given instance.
        /// <para/>
        /// Note: directly succeeding means this token starts from the very next character
        /// after the <paramref name="predecessor"/> instance ends.
        /// </summary>
        /// <param name="predecessor">The predecessor instance</param>
        /// <returns>True if this instance succeeds the given instance, false otherwise</returns>
        public bool Succeeds(Tokens predecessor) => predecessor.Preceeds(this);

        public bool Equals(string other)
        {
            if (IsDefault && other is null)
                return true;

            if (IsEmpty && string.Empty.Equals(other))
                return true;

            if (_source is not null && other is not null)
                return AsSpan().Equals(other.AsSpan(), StringComparison.InvariantCulture);

            return false;
        }

        public bool Equals(char[] value)
        {
            if (IsDefault && value is null)
                return true;

            if (IsEmpty && value?.Length == 0)
                return true;

            if (_source is not null && value is not null)
                return AsSpan().Equals(new ReadOnlySpan<char>(value), StringComparison.InvariantCulture);

            return false;
        }

        public bool Equals(Tokens other, bool isCaseSensitive)
        {
            if (IsDefault && other.IsDefault)
                return true;

            if (IsDefault ^ other.IsDefault)
                return false;

            if (_segment.Count != other._segment.Count)
                return false;

            var flag = isCaseSensitive switch
            {
                true => StringComparison.InvariantCulture,
                false => StringComparison.InvariantCultureIgnoreCase
            };

            return
                ((_segment.Offset == other._segment.Offset) && ReferenceEquals(_source, other._source))
                || AsSpan().Equals(other.AsSpan(), flag);
        }

        public bool StartsWith(Tokens tokens)
        {
            if (IsDefault && tokens.IsDefault)
                return true;

            if (IsDefault ^ tokens.IsDefault)
                return false;

            if (IsEmpty && tokens.IsEmpty)
                return true;

            if (Segment.Count < tokens.Segment.Count)
                return false;

            return AsSpan().StartsWith(tokens.AsSpan());
        }

        public bool EndsWith(Tokens tokens)
        {
            if (IsDefault && tokens.IsDefault)
                return true;

            if (IsDefault ^ tokens.IsDefault)
                return false;

            if (IsEmpty && tokens.IsEmpty)
                return true;

            if (Segment.Count < tokens.Segment.Count)
                return false;

            return AsSpan().EndsWith(tokens.AsSpan());
        }

        #endregion


        #region Nested types
        internal class TokenEnumerator : IEnumerator<char>
        {
            private readonly Tokens _tokens;
            private int _index;
            private bool _disposed;

            internal TokenEnumerator(Tokens tokens)
            {
                _disposed = false;
                _index = -1;
                _tokens = tokens.ThrowIfDefault(
                    _ => new ArgumentException($"Invalid {nameof(tokens)}: default instance"));
            }

            public char Current => AssertDisposed(() => _tokens[_index]);

            object IEnumerator.Current => Current;

            public void Dispose() => _disposed = true;

            public bool MoveNext() => AssertDisposed(() =>
            {
                var newIndex = _index + 1;

                if (newIndex > _tokens.Segment.Count)
                    return false;

                _index = newIndex;
                return _index < _tokens.Segment.Count;
            });

            public void Reset() => AssertDisposed(() => _index = -1);

            private T AssertDisposed<T>(Func<T> func)
            {
                if (_disposed)
                    throw new InvalidOperationException("Enumerator is disposed");

                return func.Invoke();
            }
        }
        #endregion
    }

}