using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.Input
{
    public class BufferedTokenReader
    {
        private readonly List<char> _buffer = new();
        private int _position = -1;
        private readonly IEnumerator<char> _source;

        public int Position => _position;

        public BufferedTokenReader(IEnumerable<char> source)
        {
            _source = source?.GetEnumerator() ?? throw new ArgumentNullException(nameof(source));
        }

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

        public bool TryNextTokens(int tokenCount, out char[] tokens)
        {
            try
            {
                tokens = new char[tokenCount];

                if(tokenCount == 0)
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

                //If we couldn't read the desired number of characters, exit without updating the position
                //ps - throwing and catching exceptions is costly, so i use a condition here to decide if we exit
                if (index < tokenCount)
                {
                    tokens = null;
                    return false;
                }

                //else update the position and return the read characters.
                else
                {
                    _position += tokenCount;
                    return true;
                }
            }
            catch
            {
                tokens = null;
                return false;
            }
        }

        public BufferedTokenReader Reset()
        {
            _position = -1;
            return this;
        }

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
            => Reset(Position - offset.ThrowIf(Extensions.IsNegative, n => new ArgumentException("Negative offsets are invalid")));

        /// <summary>
        /// Moves the position backwards by one space
        /// </summary>
        public BufferedTokenReader Back() => Back(1);
    }
}
