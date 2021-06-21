using System;

namespace Axis.Pulsar.Parser.Utils
{
    public readonly struct Slice<T>
    {
        private readonly T[] _source;
        private readonly int _offset;
        private readonly int _length;

        public Slice(T[] source, int offset, int? length = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (offset < 0 || offset >= source.Length)
                throw new ArgumentException($"Invalid offset: {offset}");

            var maxlength = source.Length - offset;
            if (length > maxlength)
                throw new ArgumentException($"Invalid length: {length}");

            _length = length ?? maxlength;
            _offset = offset;
            _source = source;
        }

        public T this[int index]
        {
            get => _source[_offset + index];
        }

        public int Length => _length;

        public Slice<T> Subslice(int offset, int? length = null) => new(_source, _offset + offset, length);
    }

    public static class SliceExtensions
    {
        public static Slice<T> Slice<T>(this T[] array, int offset, int? length = null) => new(array, offset, length);
    }
}
