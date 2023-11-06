using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Utils
{
    public class TokenReader
    {
        private int _position = 0;
        private readonly string _source;

        /// <summary>
        /// The source string
        /// </summary>
        public string Source => _source;

        /// <summary>
        /// The current position. This always represents the position of the next token to be peeked/read/consumed
        /// </summary>
        public int Position => _position;

        /// <summary>
        /// Indicates if the source string has been completely consumed
        /// </summary>
        public bool IsConsumed => _position == _source.Length;

        public TokenReader(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public static implicit operator TokenReader(string source) => new(source);

        public Tokens GetTokens(int tokenCount, bool failOnInsufficientTokens = false)
        {
            if (!TryGetTokens(tokenCount, failOnInsufficientTokens, out var tokens))
                throw new EndOfStreamException("Could not read requested tokens");

            return tokens;
        }

        #region TryGetTokens
        public bool TryGetTokens(string expectedTokens, out Tokens tokens)
        {
            if (TryPeekTokens(expectedTokens, out tokens))
            {
                _position += expectedTokens.Length;
                return true;
            }

            return false;
        }

        public bool TryGetTokens(int tokenCount, bool failOnInsufficientTokens, out Tokens tokens)
        {
            if (TryPeekTokens(tokenCount, failOnInsufficientTokens, out tokens))
            {
                _position += tokens.Count;
                return true;
            }

            return false;
        }

        public bool TryGetTokens(int tokenCount, out Tokens tokens)
            => TryGetTokens(tokenCount, false, out tokens);

        public bool TryGetToken(out Tokens tokens) => TryGetTokens(1, true, out tokens);

        /// <summary>
        /// Read as many characters from the reader, till the regex no longer matches.
        /// This method ONLY fails if the first call to <see cref="Regex.IsMatch(ReadOnlySpan{char})"/> returns false.
        /// <para/>
        /// Note that patterns that match empty strings are redundant here, as this method always first reads a character
        /// before attempting to match the regex.
        /// </summary>
        /// <param name="regex">The regex to match tokens with</param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public bool TryGetPattern(Regex regex, out Tokens tokens)
        {
            ArgumentNullException.ThrowIfNull(regex);

            var count = 1;
            tokens = Tokens.Empty;
            while (TryPeekTokens(count, true, out var _tokens))
            {
                var match = regex.Match(
                    _tokens.Source!,
                    _tokens.Offset,
                    _tokens.Count);

                if (match.Success && match.Length > tokens.Count)
                {
                    tokens = _tokens;
                    count++;
                }

                else break;
            }

            if (count == 1)
                return false;

            Reset(_position + tokens.Count);
            return true;
        }
        #endregion

        #region TryPeekTokens
        public bool TryPeekTokens(string expectedTokens, out Tokens tokens)
        {
            if (string.IsNullOrEmpty(expectedTokens))
                throw new ArgumentException($"Invalid {nameof(expectedTokens)}: null/empty");

            if (TryPeekTokens(expectedTokens.Length, true, out tokens)
                && tokens.Equals(expectedTokens))
                return true;

            return false;
        }

        public bool TryPeekTokens(int tokenCount, bool failOnInsufficientTokens, out Tokens tokens)
        {
            if (tokenCount < 0)
                throw new ArgumentOutOfRangeException(nameof(tokenCount));

            if (failOnInsufficientTokens && (_position + tokenCount) > _source.Length)
            {
                tokens = default;
                return false;
            }

            tokens = Tokens.Of(
                _source,
                _position,
                Math.Min(tokenCount, _source.Length - _position));

            return true;
        }

        public bool TryPeekTokens(int tokenCount, out Tokens tokens)
            => TryPeekTokens(tokenCount, false, out tokens);

        public bool TryPeekToken(out Tokens tokens) => TryPeekTokens(1, false, out tokens);
        #endregion

        #region Back
        public TokenReader Back(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var newPosition = _position - count;

            if (newPosition < 0)
                throw new InvalidOperationException($"Invalid calculated position: {newPosition}");

            _position = newPosition;
            return this;
        }

        public TokenReader Back() => Back(1);
        #endregion

        #region Reset
        public TokenReader Reset(int newPosition)
        {
            if (newPosition < 0 || newPosition > _source.Length)
                throw new ArgumentOutOfRangeException(nameof(newPosition));

            _position = newPosition;
            return this;
        }

        public TokenReader Reset() => Reset(0);
        #endregion
    }
}
