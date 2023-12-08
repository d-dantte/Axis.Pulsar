using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using System.Collections.Immutable;
using System.Text;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// Defines the path through a <see cref="ICSTNode"/> tree instance, to search for other nodes.
    /// </summary>
    public class NodePath
    {
        private readonly ImmutableArray<PathSegment> _segments;
        private readonly Lazy<string> _text;
        private readonly Lazy<int> _hashCode;

        public ImmutableArray<PathSegment> Segments => _segments;

        public NodePath(params PathSegment[] segments)
        {
            ArgumentNullException.ThrowIfNull(segments);

            _segments = segments
                .ThrowIfAny(s => s is null, new ArgumentException("Invalid segment: null"))
                .ToImmutableArray();

            // text
            _text = new Lazy<string>(() =>
            {
                return _segments
                    .Select(segment => segment.ToString())
                    .JoinUsing("/")
                    ?? "";
            });

            // hash code
            _hashCode = new Lazy<int>(() => _segments.Aggregate(
                func: (prev, segment) => HashCode.Combine(prev, segment),
                seed: 0));
        }

        public static NodePath Of(params PathSegment[] segments) => new NodePath(segments);

        public static NodePath Of(IEnumerable<PathSegment> segments) => new NodePath(segments.ToArray());

        public override bool Equals(object? obj)
        {
            return obj is NodePath other
                && Enumerable.SequenceEqual(_segments, other._segments);
        }

        public override int GetHashCode() => _hashCode.Value;

        public override string ToString() => _text.Value;

        public static bool operator ==(NodePath left, NodePath right) => left.Equals(right);

        public static bool operator !=(NodePath left, NodePath right) => !(left == right);

        public static implicit operator NodePath(string path) => PathParser.Parse(path).Resolve();
    }

    /// <summary>
    /// Defines a segment of the node path. Segments are used to define (alternative) filters for matching nodes at a given branch in
    /// the Concrete Syntax Tree.
    /// </summary>
    public class PathSegment
    {
        private readonly ImmutableArray<NodeFilter> _filters;
        private readonly Lazy<string> _text;
        private readonly Lazy<int> _hashCode;

        public ImmutableArray<NodeFilter> NodeFilters => _filters;

        public PathSegment(params NodeFilter[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            _filters = filters
                .ThrowIfAny(f => f is null, new ArgumentException("Invalid filter: null"))
                .ToImmutableArray();

            // text
            _text = new Lazy<string>(() =>
            {
                return _filters
                        .Select(segment => segment.ToString())
                        .JoinUsing("|")
                        ?? "";
            });

            // hash code
            _hashCode = new Lazy<int>(() => _filters.Aggregate(
                func: (prev, filter) => HashCode.Combine(prev, filter),
                seed: 0));
        }

        public static PathSegment Of(params NodeFilter[] filters) => new(filters);

        public static PathSegment Of(IEnumerable<NodeFilter> filters) => new(filters.ToArray());

        public override bool Equals(object? obj)
        {
            return obj is PathSegment other
                && Enumerable.SequenceEqual(_filters, other._filters);
        }

        public override int GetHashCode() => _hashCode.Value;

        public override string ToString() => _text.Value;

        public static bool operator ==(PathSegment left, PathSegment right) => left.Equals(right);

        public static bool operator !=(PathSegment left, PathSegment right) => !(left == right);

        /// <summary>
        /// Match the given node against the alternative filters of this segment
        /// </summary>
        /// <param name="node">The node to match</param>
        /// <returns>True if the node matches any of the filters, false otherwise</returns>
        public bool Matches(ICSTNode node)
        {
            return _filters.Any(filter => filter.Matches(node));
        }
    }

    /// <summary>
    /// Defines type of the node being sought
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// Unspecified - when the node type is not considered in the match operation
        /// </summary>
        Unspecified = 'U',

        /// <summary>
        /// NonTerminal - Indicates that the node type being sought is a <see cref="ICSTNode.NonTerminal"/> 
        /// </summary>
        NonTerminal = 'N',

        /// <summary>
        /// Terminal - Indicates that the node type being sought is a <see cref="ICSTNode.Terminal"/> 
        /// </summary>
        Terminal = 'T'
    }

    /// <summary>
    /// Defines the predicate by which a node is chosen
    /// </summary>
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

        /// <summary>
        /// Executes the predicate/filter on the given node
        /// </summary>
        /// <param name="node">The node</param>
        /// <returns>True if the node matches, false otherwise</returns>
        /// <exception cref="InvalidOperationException">If the given node is not of a recognized type.</exception>
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
