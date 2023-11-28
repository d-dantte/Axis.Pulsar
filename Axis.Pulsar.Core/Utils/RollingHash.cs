using Axis.Luna.Common;
using System.Diagnostics.CodeAnalysis;

namespace Axis.Pulsar.Core.Utils
{
    /// <summary>
    /// Rolling polynomial hash implementation.
    /// <para/>
    /// 
    /// Instances of this type, upon creation, assume that the "current" offset is one step behind the provided
    /// offset value in the constructor. This means <see cref="RollingHash.WindowHash"/> will hold "null" until
    /// the first call to either of the <c>TryNext(...)</c> methods.
    /// </summary>
    abstract public class RollingHash
    {
        protected readonly Tokens _source;
        protected readonly int _windowLength;
        protected int _offset;

        public Hash? WindowHash { get; protected set; }

        public int WindowLength => _windowLength;

        public int Offset => _offset;

        public Tokens Source => _source;

        protected RollingHash(Tokens @string, int offset, int windowLength)
        {
            Validate(@string, offset, windowLength);

            _source = @string;
            _offset = offset - 1;
            _windowLength = windowLength;
        }

        public static RollingHash Of(Tokens @string, int offset, int windowLength)
        {
            if (windowLength == 1)
                return new RollingValueHash(@string, offset, windowLength);

            else return new RollingWindowHash(@string, offset, windowLength);
        }

        /// <summary>
        /// Moves the offset by 1, and calculates the new hash.
        /// <para/>
        /// If the new offset is beyond the end of the source string, then don't move the offset, and return false,
        /// and a default hash object.
        /// </summary>
        /// <param name="result">The has at the new offset</param>
        /// <returns>True if we were able to calculate the hash of the new offset, false otherwise</returns>
        abstract public bool TryNext(out Hash result);

        /// <summary>
        /// Slides the window by <paramref name="count"/> characters, calculating the hash along the way, and only
        /// returning the hash at the last offset calculated.
        /// <para/>
        /// If the final offset is beyond the end of the source string, stop at the end of the string, return false,
        /// and a default hash object.
        /// </summary>
        /// <param name="count">The number of characters to slide the window along</param>
        /// <param name="result">The hash at the final offset</param>
        /// <returns>True if we were able to calculate the hash of the final offset, false otherwise</returns>
        abstract public bool TryNext(int count, out Hash result);

        abstract public Hash ComputeHash(Tokens @string, int offset, int length);

        protected static void Validate(Tokens @string, int offset, int length)
        {
            if (@string.IsDefaultOrEmpty)
                throw new ArgumentException($"Invalid tokens: null/empty");

            if (offset < 0 || offset >= @string.SourceSegment.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (offset + length > @string.SourceSegment.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
        }

        public static Hash ComputeHash(Tokens @string)
        {
            var impl = Of(@string, 0, @string.SourceSegment.Length);
            if (impl.TryNext(out var hash))
                return hash;

            throw new InvalidOperationException(
                $"Invalid string: could not calculate hash of '{@string}'");
        }

        #region Nested types
        public readonly struct Hash :
            IEquatable<Hash>,
            IDefaultValueProvider<Hash>
        {
            private readonly long _hash1;
            private readonly long _hash2;

            internal long Hash1 => _hash1;
            internal long Hash2 => _hash2;

            public bool IsDefault => _hash1 == 0 && _hash2 == 0;

            public static Hash Default => default;

            public Hash(long hash1, long hash2)
            {
                _hash1 = hash1;
                _hash2 = hash2;
            }

            public static Hash Of(long hash1, long hash2) => new Hash(hash1, hash2);

            public override string ToString() => $"[{_hash1:x}:{_hash2:x}]";

            public override int GetHashCode() => HashCode.Combine(_hash1, _hash2);

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj is Hash other && Equals(other);
            }

            public bool Equals(Hash other) => other._hash1 == _hash1 && other._hash2 == _hash2;

            public static bool operator ==(Hash left, Hash right) => left.Equals(right);

            public static bool operator !=(Hash left, Hash right) => !(left == right);
        }

        internal class RollingWindowHash : RollingHash
        {
            private static readonly long _Base1 = 65537;
            private static readonly long _Base2 = 65539;
            private static readonly long _Mod1 = 1000000007;
            private static readonly long _Mod2 = 1000000009;

            private readonly long _factor1;
            private readonly long _factor2;

            public RollingWindowHash(Tokens @string, int offset, int length)
            : base(@string, offset, length)
            {
                _factor1 = ComputeFactor(_Base1, _Mod1, _windowLength);
                _factor2 = ComputeFactor(_Base2, _Mod2, _windowLength);
            }

            override public bool TryNext(out Hash result)
            {
                var newOffset = _offset + 1;
                if (newOffset + _windowLength > _source.SourceSegment.Length)
                {
                    result = default;
                    return false;
                }

                WindowHash = result = WindowHash is null
                    ? ComputeHash(_source, newOffset, _windowLength)
                    : NextHash(WindowHash.Value, _source, _offset, _windowLength, (_factor1, _factor2));
                _offset = newOffset;
                return true;
            }

            override public bool TryNext(int count, out Hash result)
            {
                result = default;
                for (int cnt = 1; cnt <= count; cnt++)
                {
                    var moved = cnt == count
                        ? TryNext(out result)
                        : TryNext(out _);

                    if (!moved)
                        return false;
                }

                return true;
            }

            override public Hash ComputeHash(Tokens @string, int offset, int length)
            {
                return Hash.Of(
                    ComputeHash(@string, offset, length, _Mod1, _Base1),
                    ComputeHash(@string, offset, length, _Mod2, _Base2));
            }

            #region Static helpers

            public static Hash NextHash(
                Hash previous,
                Tokens @string,
                int oldOffset,
                int length,
                (long factor1, long factor2) factors)
            {
                return Hash.Of(
                    NextHash(previous.Hash1, factors.factor1, @string, oldOffset, length, _Mod1, _Base1),
                    NextHash(previous.Hash2, factors.factor2, @string, oldOffset, length, _Mod2, _Base2));
            }

            private static long NextHash(
                long previousHash,
                long factor,
                Tokens @string,
                int oldOffset,
                int length,
                long mod,
                long @base)
            {
                Validate(@string, oldOffset + 1, length);

                // Remove hash of left-most character, and refactor hash
                var hash = (previousHash + mod - factor * @string[oldOffset] % mod) % mod;

                // Add hash of new right-most character
                hash = (hash * @base + @string[oldOffset + length]) % mod;

                return hash;
            }

            private static long ComputeHash(
                Tokens @string,
                int offset,
                int length,
                long mod,
                long @base)
            {
                Validate(@string, offset, length);

                long hash = 0;
                var limit = offset + length;
                for (int index = offset; index < limit; index++)
                {
                    hash = (@base * hash + @string[index]) % mod;
                }
                return hash;
            }
            
            private static long ComputeFactor(long @base, long mod, long length)
            {
                var factor = 1L;
                for (int cnt = 1; cnt < length; cnt++)
                {
                    factor = (@base * factor) % mod;
                }

                return factor;
            }
            #endregion
        }

        internal class RollingValueHash : RollingHash
        {
            internal RollingValueHash(Tokens @string, int offset, int length)
            : base(@string, offset, length)
            {
            }

            override public bool TryNext(out Hash result)
            {
                var newOffset = _offset + 1;
                if (newOffset + _windowLength > _source.SourceSegment.Length)
                {
                    result = default;
                    return false;
                }

                WindowHash = result = ComputeHash(_source, newOffset, _windowLength);
                _offset = newOffset;
                return true;
            }

            override public bool TryNext(int count, out Hash result)
            {
                var finalOffset = _offset + count;
                if (finalOffset + _windowLength > _source.SourceSegment.Length)
                {
                    result = default;
                    return false;
                }

                WindowHash = result = ComputeHash(_source, finalOffset, _windowLength);
                _offset = finalOffset;
                return true;
            }

            override public Hash ComputeHash(Tokens @string, int offset, int length)
            {
                Validate(@string, offset, length);

                return Hash.Of(
                    @string[offset..(offset + 1)][0],
                    0);
            }


        }

        #endregion
    }
}
