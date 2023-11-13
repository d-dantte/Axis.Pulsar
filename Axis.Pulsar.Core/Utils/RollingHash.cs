using Axis.Luna.Common;
using System.Diagnostics.CodeAnalysis;

namespace Axis.Pulsar.Core.Utils
{
    abstract public class RollingHash
    {
        protected readonly string _source;
        protected readonly int _length;
        protected int _offset;

        public Hash WindowHash { get; protected set; }

        public int Length => _length;

        public int Offset => _offset;

        public string Source => _source;

        protected RollingHash(string @string, int offset, int length)
        {
            Validate(@string, offset, length);

            _source = @string;
            _offset = offset;
            _length = length;
        }

        public static RollingHash Of(string @string, int offset, int length)
        {
            if (length == 1)
                return new RollingValueHash(@string, offset, length);

            else return new RollingWindowHash(@string, offset, length);
        }

        abstract public bool TryNext(out Hash result);

        abstract public bool TryNext(int count, out Hash result);

        abstract public Hash ComputeHash(string @string, int offset, int length);

        protected static void Validate(string @string, int offset, int length)
        {
            if (string.IsNullOrEmpty(@string))
                throw new ArgumentException($"Invalid string: null/empty");

            if (offset < 0 || offset >= @string.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (offset + length > @string.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
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

            public RollingWindowHash(string @string, int offset, int length)
            : base(@string, offset, length)
            {
                _factor1 = ComputeFactor(_Base1, _Mod1, _length);
                _factor2 = ComputeFactor(_Base2, _Mod2, _length);
                WindowHash = ComputeHash(_source, _offset, _length);
            }

            override public bool TryNext(out Hash result)
            {
                var newOffset = _offset + 1;
                if (newOffset + _length > _source.Length)
                {
                    result = default;
                    return false;
                }

                WindowHash = result = NextHash(WindowHash, _source, _offset, _length, (_factor1, _factor2));
                _offset = newOffset;
                return true;
            }

            override public bool TryNext(int count, out Hash result)
            {
                var finalOffset = _offset + count;
                if (finalOffset + _length > _source.Length)
                {
                    result = default;
                    return false;
                }

                result = WindowHash = Enumerable
                    .Range(0, count)
                    .Aggregate(WindowHash, (hash, next) => NextHash(
                        hash,
                        _source, _offset + next,
                        _length,
                        (_factor1, _factor2)));
                _offset = finalOffset;
                return true;
            }

            override public Hash ComputeHash(string @string, int offset, int length)
            {
                return Hash.Of(
                    ComputeHash(@string, offset, length, _Mod1, _Base1),
                    ComputeHash(@string, offset, length, _Mod2, _Base2));
            }

            #region Static helpers

            public static Hash NextHash(
                Hash previous,
                string @string,
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
                string @string,
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
                string @string,
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
            internal RollingValueHash(string @string, int offset, int length)
            : base(@string, offset, length)
            {
                WindowHash = ComputeHash(@string, offset, length);
            }

            override public bool TryNext(out Hash result)
            {
                var newOffset = _offset + 1;
                if (newOffset + _length > _source.Length)
                {
                    result = default;
                    return false;
                }

                WindowHash = result = ComputeHash(_source, newOffset, _length);
                _offset = newOffset;
                return true;
            }

            override public bool TryNext(int count, out Hash result)
            {
                var finalOffset = _offset + count;
                if (finalOffset + _length > _source.Length)
                {
                    result = default;
                    return false;
                }

                WindowHash = result = ComputeHash(_source, finalOffset, _length);
                _offset = finalOffset;
                return true;
            }

            override public Hash ComputeHash(string @string, int offset, int length)
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
