using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Utils
{
    internal abstract class SubstringMatcher
    {
        protected Tokens _source;
        
        /// <summary>
        /// The index of the next window. This is the index that the next call to
        /// <see cref="SubstringMatcher.TryNextWindow(out bool)"/> will use to search for
        /// the search pattern.
        /// </summary>
        public int Index { get; protected set; }

        public Tokens Pattern { get; }

        /// <summary>
        /// Initializes the instance
        /// </summary>
        /// <param name="pattern">The pattern to search for</param>
        /// <param name="source">The source string within which the pattern is sought</param>
        /// <param name="initialOffset">The offset to start searching from</param>
        protected SubstringMatcher(
            Tokens pattern,
            Tokens source,
            int initialOffset)
        {
            Index = initialOffset;

            Pattern = pattern.ThrowIf(
                p => p.IsDefaultOrEmpty,
                _ => new ArgumentException($"Invalid {nameof(pattern)}: default/empty"));

            _source = source.ThrowIf(
                s => s.IsDefaultOrEmpty,
                _ => new ArgumentException($"Invalid {nameof(source)}: null/empty"));
        }

        /// <summary>
        /// Advances the index as far as it will go - which is the length of the source + 1.
        /// </summary>
        /// <param name="skipCount">The number of characters to skip</param>
        /// <returns>The number of characters skipped</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public abstract int Advance(int skipCount);

        public bool TryNextWindow(out bool isMatch) => TryNextWindow(
            StringComparison.InvariantCulture,
            out isMatch);

        /// <summary>
        /// Consumes the next token from the source, from the current offset, then attempts to match the new window 
        /// with the <see cref="SubstringMatcher.Pattern"/>.
        /// </summary>
        /// <param name="isMatch">True if the match succeeded, false otherwise</param>
        /// <returns>True if a new token could be consumed, false otherwise</returns>
        public abstract bool TryNextWindow(
            StringComparison comparison,
            out bool isMatch);

        /// <summary>
        /// Indicates if the current window is valid.
        /// <para/>
        /// A valid window is one where a search pattern can safely fit. The window is defined by
        /// the current index, and the count of characters in the <see cref="SubstringMatcher.Pattern"/>.
        /// </summary>
        /// <returns>True if the current window is valid, false otherwise</returns>
        public abstract bool IsValidWindow { get; }

        /// <summary>
        /// The pattern at the next index within the source string.
        /// </summary>
        public abstract Tokens NextPattern { get; }


        #region Nested Types

        /// <summary>
        /// 
        /// </summary>
        public class LookAheadMatcher : SubstringMatcher
        {
            public LookAheadMatcher(
                Tokens pattern,
                Tokens source, 
                int initialOffset)
                : base(pattern, source, initialOffset)
            {
                if (initialOffset < 0 || initialOffset >= source.Segment.Count)
                    throw new ArgumentOutOfRangeException(nameof(initialOffset));
            }

            public static LookAheadMatcher Of(
                Tokens pattern,
                Tokens source,
                int initialOffset)
                => new(pattern, source, initialOffset);

            public override Tokens NextPattern => _source.Slice(
                length: Math.Min(Pattern.Segment.Count, _source.Segment.Count - Index),
                offset: Index);

            public override bool IsValidWindow => Index + Pattern.Segment.Count <= _source.Segment.Count;

            public override int Advance(int skipCount)
            {
                if (skipCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(skipCount));

                var auxIndex = Index + skipCount;

                if (auxIndex + Pattern.Segment.Count > _source.Segment.Count)
                {
                    auxIndex = Index;
                    Index = (_source.Segment.Count + 1) - Pattern.Segment.Count;
                    return Index - auxIndex;
                }
                else
                {
                    Index = auxIndex;
                    return skipCount;
                }
            }

            public override bool TryNextWindow(
                StringComparison comparison,
                out bool isMatch)
            {
                if (!IsValidWindow)
                    return isMatch = false;

                isMatch = Pattern.AsSpan().Equals(
                    NextPattern.AsSpan(),
                    comparison);
                Index++;

                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class LookBehindMatcher: SubstringMatcher
        {
            public LookBehindMatcher(
                Tokens pattern,
                Tokens source,
                int initialOffset)
                : base(pattern, source, initialOffset)
            {
                if (initialOffset < 0 || initialOffset >= source.Segment.Count)
                    throw new ArgumentOutOfRangeException(nameof(initialOffset));
            }

            public static LookBehindMatcher Of(
                Tokens pattern,
                Tokens source,
                int initialOffset)
                => new(pattern, source, initialOffset);

            public override bool IsValidWindow
            {
                get => Index < _source.Segment.Count
                    && Index + 1 - Pattern.Segment.Count >= 0;
            }

            public override Tokens NextPattern
            {
                get 
                {
                    var startIndex = Index - Pattern.Segment.Count + 1;

                    if (startIndex < 0)
                        return _source.Slice(0, Index + 1);

                    if (Index >= _source.Segment.Count)
                        return _source.Slice(
                            Math.Min(startIndex, _source.Segment.Count - 1),
                            Math.Max(0, _source.Segment.Count - startIndex));

                    return _source.Slice(startIndex, Pattern.Segment.Count);
                }
            }

            public override int Advance(int skipCount)
            {
                if (skipCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(skipCount));

                var auxIndex = Index + skipCount;

                if (auxIndex >= _source.Segment.Count)
                {
                    auxIndex = Index;
                    Index = _source.Segment.Count;
                    return Index - auxIndex;
                }
                else
                {
                    Index = auxIndex;
                    return skipCount;
                }
            }

            public override bool TryNextWindow(
                StringComparison comparison,
                out bool isMatch)
            {
                if (Index >= _source.Segment.Count)
                    return isMatch = false;

                var currentPattern = NextPattern;
                Index++;

                isMatch =
                    currentPattern.Segment.Count == Pattern.Segment.Count
                    && currentPattern.AsSpan().Equals(
                        Pattern.AsSpan(),
                        comparison);

                return true;
            }
        }

        #endregion
    }
}
