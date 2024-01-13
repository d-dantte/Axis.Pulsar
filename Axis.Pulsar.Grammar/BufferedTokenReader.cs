using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Grammar
{
    /// <summary>
    /// Represents the entity that all parsers read tokens from.
    /// </summary>
    public class BufferedTokenReader
    {
        private readonly List<char> _buffer = new();
        private int _position = -1;
        private readonly IEnumerator<char> _source;

        /// <summary>
        /// The current position of the token reader
        /// </summary>
        public int Position => _position;

        /// <summary>
        /// Indicates that the end of the input source has been reached, and no more tokens can be read
        /// from the current position.
        /// </summary>
        public bool IsConsumed
        {
            get
            {
                if (TryNextToken(out _))
                {
                    Back();
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Creates a new instance of this class
        /// </summary>
        /// <param name="source">The original source of individual characters (tokens)</param>
        /// <exception cref="ArgumentNullException"></exception>
        public BufferedTokenReader(IEnumerable<char> source)
        {
            _source = source?.GetEnumerator() ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Try retrieving the next token from the current position.
        /// </summary>
        /// <param name="nextToken">the next token is set to this variable if it was successfully read</param>
        /// <returns>true if a token was read, false if not.</returns>
        public bool TryNextToken(out char nextToken)
        {
            if (TryNextTokens(1, out var nextChars))
            {
                nextToken = nextChars[0];
                return true;
            }

            //else
            nextToken = default;
            return false;
        }


        /// <summary>
        /// Reads a number of tokens from the current position. if <paramref name="failOnInsufficientTokens"/> is false,
        /// <paramref name="tokens"/> is populated with however many characters were read <c>{0 &lt;= x &lt;= tokenCount}</c>, and the method always return true.
        /// If however, <paramref name="failOnInsufficientTokens"/> is true, the method only return true if the requested number of tokens is read.
        /// </summary>
        /// <param name="tokenCount">the number of tokens to read</param>
        /// <param name="failOnInsufficientTokens">If the number of tokens read are not equal to <paramref name="tokenCount"/>, fail depending on this value</param>
        /// <param name="tokens">The successfully read tokens</param>
        /// <returns>true if a token was read, false if not. Note that partial reads are not supported - it's an all or nothing process.</returns>
        public bool TryNextTokens(int tokenCount, out char[] tokens, bool failOnInsufficientTokens = true)
        {
            try
            {
                tokens = new char[tokenCount];

                if (tokenCount == 0)
                {
                    tokens = Array.Empty<char>();
                    return true;
                }

                int index = 0;

                //get as many characters as are available in the buffer
                for (int bufferOffset = _position + 1;
                    bufferOffset < _buffer.Count && index < tokenCount;
                    index++, bufferOffset++)
                {
                    tokens[index] = _buffer[bufferOffset];
                }

                //get the remaining characters from the enumerator if possible
                for (; index < tokenCount && _source.MoveNext(); index++)
                {
                    tokens[index] = _source.Current;
                    _buffer.Add(_source.Current);
                }

                // If we couldn't read the desired number of characters, exit without updating the position
                // ps - throwing and catching exceptions is costly, so i use a condition here to decide if we exit
                // pps - note that 'index' currently holds the number of items read
                if (failOnInsufficientTokens && index < tokenCount)
                {
                    tokens = null;
                    return false;
                }

                //else update the position and return the read characters.
                else
                {
                    if (index == 0)
                        tokens = Array.Empty<char>();

                    else if (index < tokenCount)
                        tokens = tokens[..index];

                    _position += index;
                    return true;
                }
            }
            catch
            {
                tokens = null;
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public bool TryNextPattern(Regex regex, out string tokens)
        {
            var sb = new StringBuilder();
            while(TryNextToken(out char next))
            {
                if (regex.IsMatch($"{sb}{next}"))
                    sb.Append(next);

                else
                {
                    Back(1);
                    break;
                }
            }

            tokens = sb.ToString();
            return sb.Length > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public bool TryNextPattern(HashSet<char> chars, out string tokens)
        {
            var sb = new StringBuilder();
            while (TryNextToken(out char next))
            {
                if (chars.Contains(next))
                    sb.Append(next);

                else
                {
                    Back(1);
                    break;
                }
            }

            tokens = sb.ToString();
            return sb.Length > 0;
        }

        /// <summary>
        /// Resets the position to -1, the starting position.
        /// </summary>
        /// <returns>The current token reader</returns>
        public BufferedTokenReader Reset()
        {
            _position = -1;
            return this;
        }

        /// <summary>
        /// Resets the position to the value indicated, if it is valid.
        /// </summary>
        /// <param name="position">the value to reset the position to.</param>
        /// <returns>The current token reader</returns>
        /// <exception cref="ArgumentException"></exception>
        public BufferedTokenReader Reset(int position)
        {
            if (position < -1
                || position >= _buffer.Count)
                throw new ArgumentException($"Invalid position: {position}");

            else
            {
                _position = position;
                return this;
            }
        }

        /// <summary>
        /// Moves the position backwards by however many spaces specified
        /// </summary>
        /// <param name="offset">The offset. This should be a positive number - if it is negative, an exception is thrown</param>
        public BufferedTokenReader Back(int offset)
            => Reset(Position - offset.ThrowIf(
                Extensions.IsNegative,
                _ => new ArgumentException("Negative offsets are invalid")));

        /// <summary>
        /// Moves the position backwards by one space
        /// </summary>
        public BufferedTokenReader Back() => Back(1);


        public static implicit operator BufferedTokenReader(string input) => new(input);
    }
}
