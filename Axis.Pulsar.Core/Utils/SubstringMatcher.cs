using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Utils
{
    internal abstract class SubstringMatcher
    {
        protected readonly Lazy<RollingHash> _hasher;

        protected RollingHash.Hash PatternHash { get; }

        public Tokens Source { get; }

        public int StartOffset { get; }

        public int PatternLength { get; }

        public int CurrentOffset
        {
            get
            {
                if (_hasher.IsValueCreated)
                    return _hasher.Value.Offset;

                return - 1;
            }
        }

        protected SubstringMatcher(
            Tokens patternSequence,
            Tokens source,
            int startOffset = 0)
        {
            var pattern = patternSequence.ThrowIf(
                seq => seq.IsEmpty || seq.IsDefault,
                new ArgumentException($"Invalid {nameof(patternSequence)}: default/empty"));

            PatternLength = pattern.Count;

            Source = source.ThrowIf(
                s => s.IsDefaultOrEmpty,
                new ArgumentException($"Invalid {nameof(source)}: null/empty"));

            StartOffset = startOffset.ThrowIf(
                offset => offset < 0 || offset >= source.Count,
                new ArgumentOutOfRangeException(
                    nameof(startOffset),
                    $"Value '{startOffset}' is < 0 or > {nameof(source)}.Length"));

            var patternHasher =  RollingHash.Of(
                pattern.Source!,
                pattern.Offset,
                pattern.Count);
            
            PatternHash = !patternHasher.TryNext(out var hash)
                ? throw new InvalidOperationException($"Failed to calculate hash for pattern: {pattern}")
                : hash;

            _hasher = new Lazy<RollingHash>(() => RollingHash.Of(
                source.Source!,
                startOffset + source.Offset,
                patternSequence.Count));
        }

        public static SubstringMatcher OfLookAhead(
            Tokens patternSequence,
            Tokens source,
            int startOffset = 0)
            => new LookAheadMatcher(patternSequence, source, startOffset);

        public static SubstringMatcher OfLookBehind(
            Tokens patternSequence,
            Tokens source,
            int startOffset = 0)
            => new LookBehindMatcher(patternSequence, source, startOffset);

        /// <summary>
        /// Consumes the next token from the source, from the current offset, then attempts to match the new window with the <see cref="SequenceMatcher.Pattern"/>.
        /// </summary>
        /// <param name="isMatch">True if the match succeeded, false otherwise</param>
        /// <returns>True if a new token could be consumed, false otherwise</returns>
        public abstract bool TryNextWindow(out bool isMatch);


        internal class LookAheadMatcher : SubstringMatcher
        {
            internal LookAheadMatcher(
                Tokens patternSequence,
                Tokens source,
                int startOffset)
                : base(patternSequence, source, startOffset)
            {
                if (startOffset + patternSequence.Count > source.Count)
                    throw new ArgumentException("Invalid args: source.Count < startOffset + patternSequence.Count");
            }

            public override bool TryNextWindow(out bool isMatch)
            {
                var advanced = _hasher.Value.TryNext(out var hash);
                isMatch = advanced && hash.Equals(PatternHash);

                return advanced;
            }
        }

        /// <summary>
        /// Matcher that "skips" a number of input characters from the startOffset equal to the
        /// patternSequence's length, before it starts searching for matches. What this means is,
        /// when the current index is at "x", for example, the search window will effectively be
        /// <c>(Offset: x - patternSequence.Count, Length: patternSequence.Count)</c>
        /// </summary>
        internal class LookBehindMatcher : SubstringMatcher
        {
            private int _consumedCharCount = 0;

            internal LookBehindMatcher(
                Tokens patternSequence,
                Tokens source,
                int startOffset)
                : base(patternSequence, source, startOffset)
            {
            }

            public override bool TryNextWindow(out bool isMatch)
            {
                isMatch = false;
                var newCount = _consumedCharCount + 1;

                if (StartOffset + newCount > Source.Count)
                    return false;

                _consumedCharCount = newCount;
                if (_consumedCharCount >= PatternLength)
                    isMatch =
                        _hasher.Value.TryNext(out var hash)
                        && hash.Equals(PatternHash);

                return true;
            }
        }
    }
}
