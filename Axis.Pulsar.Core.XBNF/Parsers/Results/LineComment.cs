using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF.Parsers.Models
{
    public class LineComment : ISilentElement
    {
        public Tokens Content { get; }

        public LineComment(Tokens comment)
        {
            Content = comment.ThrowIfDefault(new ArgumentException("Invalid comment: default"));

            if (comment.ContainsAny('\n', '\r'))
                throw new ArgumentException($"Comment cannot contain '\\n' or '\\r' characters");
        }

        public static LineComment Of(Tokens comment) => new(comment);

        public static implicit operator LineComment(Tokens comment) => new(comment);
    }
}
