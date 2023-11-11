namespace Axis.Pulsar.Core.XBNF.Parsers.Models
{
    public class BlockComment : ISilentElement
    {
        public string Content { get; }

        public BlockComment(string comment)
        {
            Content = comment ?? throw new ArgumentNullException(nameof(comment));

            if (comment.Contains("*/", StringComparison.CurrentCulture))
                throw new ArgumentException($"Comment cannot contain the '*/' sequence");
        }
    }
}
