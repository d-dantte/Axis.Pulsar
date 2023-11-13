using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Axis.Pulsar.Core.Utils
{
    internal abstract class SubstringMatcher
    {
        protected RollingHash.Hash PatternHash { get; }

        public string Source { get; }

        public int StartOffset { get; }

        public Tokens Pattern { get; }

        protected SubstringMatcher(
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

            PatternHash = RollingHash
                .Of(Pattern.Source!,
                    Pattern.Offset,
                    Pattern.Count)
                .WindowHash;
        }

        /// <summary>
        /// Consumes the next token from the source, from the current offset, then attempts to match the new window with the <see cref="SequenceMatcher.Pattern"/>.
        /// </summary>
        /// <param name="isMatch">True if the match succeeded, false otherwise</param>
        /// <returns>True if a new token could be consumed, false otherwise</returns>
        public abstract bool TryNextWindow(out bool isMatch);


        internal class LookAheadMatcher : SubstringMatcher
        {
            private readonly Lazy<RollingHash> _hasher;

            public LookAheadMatcher(
                Tokens patternSequence,
                string source,
                int startOffset)
                : base(patternSequence, source, startOffset)
            {
                if (startOffset + patternSequence.Count > source.Length)
                    throw new ArgumentException("Invalid args: source.Length < startOffset + patternSequence.Count");

                _hasher = new Lazy<RollingHash>(() => RollingHash.Of(
                    source,
                    startOffset,
                    patternSequence.Count));
            }

            public override bool TryNextWindow(out bool isMatch)
            {
                var advanced = _hasher.Value.TryNext(out var hash);
                isMatch = advanced &&  hash.Equals(PatternHash);

                return advanced;
            }
        }

        internal class LookBehindMatcher: SubstringMatcher
        {
            private readonly Lazy<RollingHash> _hasher;
            private int _consumedCharCount = 0;

            public LookBehindMatcher(
                Tokens patternSequence,
                string source,
                int startOffset)
                : base(patternSequence, source, startOffset)
            {
                _hasher = new Lazy<RollingHash>(() => RollingHash.Of(
                    source,
                    startOffset,
                    patternSequence.Count));
            }

            public override bool TryNextWindow(out bool isMatch)
            {
                isMatch = false;
                var newCount = _consumedCharCount + 1;

                if (StartOffset + newCount > Source.Length)
                    return false;

                _consumedCharCount = newCount;
                if (_consumedCharCount >= Pattern.Count)
                    isMatch =
                        _hasher.Value.TryNext(out var hash)
                        && hash.Equals(PatternHash);

                return true;
            }
        }
    }
}
