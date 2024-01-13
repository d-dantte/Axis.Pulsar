using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Axis.Pulsar.Grammar.CST
{
    /// <summary>
    /// 
    /// </summary>
    public class Path
    {
        private readonly ImmutableArray<Segment> _segments;

        public ImmutableArray<Segment> Segments => _segments;

        public Path(IEnumerable<Segment> segments)
        {
            _segments = segments
                .ThrowIfNull(() => new ArgumentNullException(nameof(segments)))
                .ThrowIfAny(f => f is null, _ => new ArgumentException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public Path(params Segment[] segments)
            :this(segments.AsEnumerable())
        { }

        public static Path Of(params Segment[] segments) => new Path(segments);

        public static Path Of(IEnumerable<Segment> segments) => new Path(segments.ToArray());

        public override bool Equals(object obj)
        {
            return obj is Path other
                && Enumerable.SequenceEqual(_segments, other._segments);
        }

        public override int GetHashCode()
        {
            return !_segments.IsDefault
                ? _segments.Aggregate(0, (prev, segment) => HashCode.Combine(prev, segment))
                : 0;
        }

        public override string ToString()
        {
            return !_segments.IsDefault
                ? _segments
                    .Select(segment => segment.ToString())
                    .JoinUsing("/")
                : "";
        }

        public static bool operator ==(Path left, Path right) => left.Equals(right);

        public static bool operator !=(Path left, Path right) => !(left == right);

        public static implicit operator Path(string path) => PathParser.Parse(path).Resolve();
    }

    /// <summary>
    /// 
    /// </summary>
    public class Segment
    {
        private readonly ImmutableArray<NodeFilter> _filters;

        public ImmutableArray<NodeFilter> NodeFilters => _filters;

        public Segment(params NodeFilter[] filters)
        {
            _filters = filters
                .ThrowIfNull(() => new ArgumentNullException(nameof(filters)))
                .ThrowIfAny(f => f is null, _ => new ArgumentException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Segment Of(params NodeFilter[] filters) => new Segment(filters);

        public static Segment Of(IEnumerable<NodeFilter> filters) => new Segment(filters.ToArray());

        public override bool Equals(object obj)
        {
            return obj is Segment other
                && Enumerable.SequenceEqual(_filters, other._filters);
        }

        public override int GetHashCode()
        {
            return !_filters.IsDefault
                ? _filters.Aggregate(0, (prev, segment) => HashCode.Combine(prev, segment))
                : 0;
        }

        public override string ToString()
        {
            return !_filters.IsDefault
                ? _filters
                    .Select(segment => segment.ToString())
                    .JoinUsing("|")
                : "";
        }

        public static bool operator ==(Segment left, Segment right) => left.Equals(right);

        public static bool operator !=(Segment left, Segment right) => !(left == right);

        public bool Matches(CSTNode node)
        {
            return _filters.Any(filter => filter.Matches(node));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum NodeType
    {
        None,
        Ref,
        Custom,
        Literal,
        Pattern
    }

    /// <summary>
    /// 
    /// </summary>
    public class NodeFilter
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

        public bool Matches(CSTNode node)
        {
            var isNodeTypeMatch = NodeType == NodeType.None || node switch
            {
                CSTNode.BranchNode => NodeType.Ref.Equals(NodeType),
                CSTNode.LeafNode leaf => leaf.TerminalType switch
                {
                    CSTNode.TerminalType.Pattern => NodeType.Pattern.Equals(NodeType),
                    CSTNode.TerminalType.Custom => NodeType.Custom.Equals(NodeType),
                    CSTNode.TerminalType.Literal => NodeType.Literal.Equals(NodeType),
                    _ => throw new InvalidOperationException($"Invalid terminal type: {leaf.TerminalType}")
                },
                _ => throw new InvalidOperationException($"Invalid CSTNode type: '{node?.GetType()}'")
            };

            return isNodeTypeMatch
                && (SymbolName is null || SymbolName.Equals(node.SymbolName))
                && (Tokens is null || Tokens.Equals(CSTNodeUtils.TokenValue(node)));
        }

        public override string ToString()
        {
            var sb = new StringBuilder()
                .Append('@')
                .Append(NodeType.ToString().ToLower()[0]);

            if (SymbolName is not null)
                sb.Append(':').Append(SymbolName);

            if (Tokens is not null)
                sb.Append('<').Append(Tokens).Append('>');

            return sb.ToString();
        }

        public override int GetHashCode() => HashCode.Combine(SymbolName, Tokens, NodeType);

        public override bool Equals(object obj)
        {
            return obj is NodeFilter other
                && other.NodeType == NodeType
                && EqualityComparer<string>.Default.Equals(other.SymbolName, SymbolName)
                && EqualityComparer<string>.Default.Equals(other.Tokens, Tokens);
        }

        public static bool operator ==(NodeFilter left, NodeFilter right) => left.Equals(right);

        public static bool operator !=(NodeFilter left, NodeFilter right) => !(left == right);
    }
}
