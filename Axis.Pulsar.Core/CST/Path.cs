using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using System.Text;

namespace Axis.Pulsar.Core.CST
{
    public class Path
    {
        private readonly Segment[] segments;

        public Segment[] Segments => segments?.ToArray() ?? Array.Empty<Segment>();

        public Path(params Segment[] segments)
        {
            ArgumentNullException.ThrowIfNull(segments);

            this.segments = segments;
        }

        public static Path Of(params Segment[] segments) => new Path(segments);

        public static Path Of(IEnumerable<Segment> segments) => new Path(segments.ToArray());

        public override bool Equals(object? obj)
        {
            return obj is Path other
                && Enumerable.SequenceEqual(segments, other.segments);
        }

        public override int GetHashCode()
        {
            return segments?
                .Aggregate(0, (prev, segment) => HashCode.Combine(prev, segment))
                ?? 0;
        }

        public override string ToString()
        {
            return segments?
                .Select(segment => segment.ToString())
                .JoinUsing("/")
                ?? "";
        }

        public static bool operator ==(Path left, Path right) => left.Equals(right);

        public static bool operator !=(Path left, Path right) => !(left == right);

        public static implicit operator Path(string path) => PathParser.Parse(path).Resolve();
    }

    public class Segment
    {
        private readonly NodeFilter[] filters;

        public NodeFilter[] NodeFilters => filters?.ToArray() ?? Array.Empty<NodeFilter>();

        public Segment(params NodeFilter[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            this.filters = filters;
        }

        public static Segment Of(params NodeFilter[] filters) => new Segment(filters);

        public static Segment Of(IEnumerable<NodeFilter> filters) => new Segment(filters.ToArray());

        public override bool Equals(object? obj)
        {
            return obj is Segment other
                && Enumerable.SequenceEqual(filters, other.filters);
        }

        public override int GetHashCode()
        {
            return filters?
                .Aggregate(0, (prev, segment) => HashCode.Combine(prev, segment))
                ?? 0;
        }

        public override string ToString()
        {
            return filters?
                .Select(segment => segment.ToString())
                .JoinUsing("|")
                ?? "";
        }

        public static bool operator ==(Segment left, Segment right) => left.Equals(right);

        public static bool operator !=(Segment left, Segment right) => !(left == right);

        public bool Matches(ICSTNode node)
        {
            return filters.Any(filter => filter.Matches(node));
        }

    }

    public enum NodeType
    {
        None,
        Ref,
        Custom,
        Literal,
        Pattern
    }

    public record NodeFilter
    {
        public string SymbolName { get; }

        public string Tokens { get; }

        public NodeType NodeType { get; }

        public NodeFilter(NodeType nodeType, string symbolName, string tokens)
        {
            NodeType = nodeType;
            SymbolName = symbolName;
            Tokens = tokens;
        }

        public bool Matches(ICSTNode node)
        {
            var isNodeTypeMatch = NodeType == NodeType.None || node switch
            {
                ICSTNode.NonTerminal => NodeType.Ref.Equals(NodeType),
                ICSTNode.Pattern => NodeType.Pattern.Equals(NodeType),
                ICSTNode.Literal => NodeType.Literal.Equals(NodeType),
                ICSTNode.CustomTerminal => NodeType.Custom.Equals(NodeType),
                _ => throw new InvalidOperationException($"Invalid ICSTNode type: '{node?.GetType()}'")
            };

            return isNodeTypeMatch
                && IsNameMatch(node)
                && IsTokenMatch(node);
        }

        private bool IsNameMatch(ICSTNode node)
        {
            return SymbolName is null || node switch
            {
                ICSTNode.NonTerminal nt => SymbolName.Equals(nt.Name),
                ICSTNode.CustomTerminal ct => SymbolName.Equals(ct.Name),
                _ => false
            };
        }

        private bool IsTokenMatch(ICSTNode node)
        {
            return string.IsNullOrEmpty(Tokens) || node.Tokens.Equals(Tokens);
        }

        public override string ToString()
        {
            var sb = new StringBuilder()
                .Append('@')
                .Append(NodeType.ToString()[0]);

            if (SymbolName is not null)
                sb.Append(':').Append(SymbolName);

            if (Tokens is not null)
                sb.Append('<').Append(Tokens).Append('>');

            return sb.ToString();
        }
    }
}
