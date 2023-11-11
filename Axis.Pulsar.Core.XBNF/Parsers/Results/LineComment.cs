using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Core.XBNF.Parsers.Models
{
    internal class LineComment : ISilentElement
    {
        public string Content { get; }

        public LineComment(string comment)
        {
            Content = comment ?? throw new ArgumentNullException(nameof(comment));

            if (comment.IndexOfAny(new[] { '\n', '\r' }) >= 0)
                throw new ArgumentException($"Comment cannot contain '\\n' or '\\r' characters");
        }
    }
}
