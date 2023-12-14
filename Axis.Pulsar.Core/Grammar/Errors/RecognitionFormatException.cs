using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Errors
{
    public class RecognitionFormatException: Exception
    {
        public int Line { get; }

        public int Column { get; }

        public Tokens ErrorSegment { get; }

        public override string Message => $"Recognition error at line: {Line}, column: {Column}, of the input tokens: '{ErrorSegment}'.";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="source">The entire input source token</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public RecognitionFormatException(
            int position,
            int length,
            Tokens source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            if(length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            var lines = source.Split(
                0,
                position + 1,
                "\r\n",
                "\r",
                "\n");

            Line = lines.Length;
            Column = lines[^1].Tokens.Segment.Count;
            ErrorSegment = Tokens.Of(
                source,
                position,
                length);
        }

        public static RecognitionFormatException Of(
            int index,
            int count,
            Tokens source)
            => new(index, count, source);
    }
}
