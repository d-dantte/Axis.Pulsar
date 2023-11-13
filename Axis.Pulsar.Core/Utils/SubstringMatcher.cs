using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Utils
{
    internal class SubstringMatcher
    {
        private readonly Lazy<RollingHash> _hasher;

        private readonly RollingHash.Hash _patternHash;

        public string Source { get; }

        public int StartOffset { get; }

        public Tokens Pattern { get; }

        internal SubstringMatcher(
            Tokens patternSequence,
            string source,
            int startOffset)
        {
            Pattern = patternSequence.ThrowIf(
                seq => seq.IsEmpty || seq.IsDefault,
                new ArgumentException($"Invalid {nameof(patternSequence)}: default/empty"));

            Source = source.ThrowIf(
                string.IsNullOrEmpty,
                new ArgumentException($"Invalid {nameof(source)}: null/empty"));

            StartOffset = startOffset.ThrowIf(
                offset => offset < 0 || offset >= source.Length,
                new ArgumentOutOfRangeException(
                    nameof(startOffset),
                    $"Value '{startOffset}' is < 0 or > {nameof(source)}.Length"));

            _hasher = new Lazy<RollingHash>(() => RollingHash.Of(
                Source,
                StartOffset,
                Pattern.Count));

            var patternHasher = RollingHash.Of(
                Pattern.Source!,
                Pattern.Offset,
                Pattern.Count);

            _patternHash = patternHasher.TryNext(out var hash)
                ? hash
                : throw new InvalidOperationException($"Failed to calculate pattern hash");
        }

        /// <summary>
        /// Consumes the next token from the source, from the current offset, then attempts to match the new window with the <see cref="SequenceMatcher.Pattern"/>.
        /// </summary>
        /// <param name="isMatch">True if the match succeeded, false otherwise</param>
        /// <returns>True if a new token could be consumed, false otherwise</returns>
        public bool TryNextWindow(out bool isMatch)
        {
            var advanced = _hasher.Value.TryNext(out var hash);
            isMatch = advanced && hash.Equals(PatternHash);

            return advanced;
        }

    }
}
