using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF.Parsers.Models
{
    public class BlockComment : ISilentElement
    {
        public Tokens Content { get; }

        public BlockComment(Tokens comment)
        {
            Content = comment.ThrowIfDefault(new ArgumentException("Invalid comment: default"));

            if (comment.Contains("*/"))
                throw new ArgumentException($"Comment cannot contain the '*/' sequence");
        }

        public static BlockComment Of(Tokens comment) => new(comment);

        public static implicit operator BlockComment(Tokens comment) => new(comment);
    }
}
