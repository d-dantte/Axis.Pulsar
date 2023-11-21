using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using System.Collections.Immutable;
using System.Text;

namespace Axis.Pulsar.Core.CST
{
    public class NodePath
    {
        private readonly ImmutableArray<Segment> _segments;
        private readonly Lazy<string> _text;

        public ImmutableArray<Segment> Segments => _segments;

        public NodePath(params Segment[] segments)
        {
            ArgumentNullException.ThrowIfNull(segments);

            _segments = segments
                .ThrowIfAny(s => s is null, new ArgumentException("Invalid segment: null"))
                .ToImmutableArray();

            _text = new Lazy<string>(() =>
            {
                return _segments
                    .Select(segment => segment.ToString())
                    .JoinUsing("/")
                    ?? "";
            });
        }

        public static NodePath Of(params Segment[] segments) => new NodePath(segments);

        public static NodePath Of(IEnumerable<Segment> segments) => new NodePath(segments.ToArray());

        public override bool Equals(object? obj)
        {
            return obj is NodePath other
                && Enumerable.SequenceEqual(_segments, other._segments);
        }

        public override int GetHashCode()
        {
            return _segments.Aggregate(0, (prev, segment) => HashCode.Combine(prev, segment));
        }

        public override string ToString() => _text.Value;

        public static bool operator ==(NodePath left, NodePath right) => left.Equals(right);

        public static bool operator !=(NodePath left, NodePath right) => !(left == right);

        public static implicit operator NodePath(string path) => PathParser.Parse(path).Resolve();
    }

    public class Segment
    {
        private readonly ImmutableArray<NodeFilter> _filters;
        private readonly Lazy<string> _text;

        public ImmutableArray<NodeFilter> NodeFilters => _filters;

        public Segment(params NodeFilter[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            _filters = filters
                .ThrowIfAny(f => f is null, new ArgumentException("Invalid filter: null"))
                .ToImmutableArray();

            _text = new Lazy<string>(() =>
            {
                return _filters
                        .Select(segment => segment.ToString())
                        .JoinUsing("|")
                        ?? "";
            });
        }

        public static Segment Of(params NodeFilter[] filters) => new(filters);

        public static Segment Of(IEnumerable<NodeFilter> filters) => new(filters.ToArray());

        public override bool Equals(object? obj)
        {
            return obj is Segment other
                && Enumerable.SequenceEqual(_filters, other._filters);
        }

        public override int GetHashCode()
        {
            return _filters.Aggregate(0, (prev, segment) => HashCode.Combine(prev, segment));
        }

        public override string ToString() => _text.Value;

        public static bool operator ==(Segment left, Segment right) => left.Equals(right);

        public static bool operator !=(Segment left, Segment right) => !(left == right);

        public bool Matches(ICSTNode node)
        {
            return _filters.Any(filter => filter.Matches(node));
        }

    }

    public enum NodeType
    {
        Unspecified = 'u',
        NonTerminal = 'n',
        Terminal = 't'
    }

    public record NodeFilter
    {
        private readonly Lazy<string> _text;

        public string? SymbolName { get; }

        public string? Tokens { get; }

        public NodeType NodeType { get; }

        public NodeFilter(NodeType nodeType, string? symbolName, string? tokens)
        {
            NodeType = nodeType;
            SymbolName = symbolName;
            Tokens = tokens;

            if (symbolName is null && tokens is null && nodeType == NodeType.Unspecified)
                throw new ArgumentException(
                    $"Invalid arguments: '{nameof(symbolName)}' & '{nameof(tokens)}' cannot both be null");

            _text = new Lazy<string>(() =>
            {
                var sb = new StringBuilder()
                    .Append('@')
                    .Append(NodeType.ToString()[0]);

                if (SymbolName is not null)
                    sb.Append(':').Append(SymbolName);

                if (Tokens is not null)
                    sb.Append('<').Append(Tokens).Append('>');

                return sb.ToString();
            });
        }

        public static NodeFilter Of(
            NodeType nodeType,
            string? symbolName,
            string? tokens)
            => new(nodeType, symbolName, tokens);

        public bool Matches(ICSTNode node)
        {
            var isNodeTypeMatch = NodeType == NodeType.Unspecified || node switch
            {
                ICSTNode.NonTerminal => NodeType.NonTerminal.Equals(NodeType),
                ICSTNode.Terminal => NodeType.Terminal.Equals(NodeType),
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
                ICSTNode.Terminal t => SymbolName.Equals(t.Name),
                _ => false
            };
        }

        private bool IsTokenMatch(ICSTNode node)
        {
            return string.IsNullOrEmpty(Tokens) || node.Tokens.Equals(Tokens);
        }

        public override string ToString() => _text.Value;
    }
}
