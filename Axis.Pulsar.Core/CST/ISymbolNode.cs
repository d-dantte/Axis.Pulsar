using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// TODO: Add <see cref="ProductionPath"/> as a member of all nodes
    /// </summary>
    public interface ISymbolNode
    {
        /// <summary>
        /// The tokens this node is comprised of
        /// </summary>
        Tokens Tokens { get; }

        /// <summary>
        /// The symbol name of this node
        /// </summary>
        string Symbol { get; }

        #region Of

        /// <summary>
        /// Creates an instance of a <see cref="Composite"/> node
        /// </summary>
        /// <param name="name">The node name</param>
        /// <param name="nodes">The list of comprising nodes</param>
        /// <returns></returns>
        public static ISymbolNode Of(
            string name,
            params ISymbolNode[] nodes)
            => new Composite(name, nodes);

        /// <summary>
        /// Creates an instance of a <see cref="Composite"/> node
        /// </summary>
        /// <param name="name">The node name</param>
        /// <param name="nodes">The list of comprising nodes</param>
        /// <returns></returns>
        public static ISymbolNode Of(
            string name,
            IEnumerable<ISymbolNode> nodes)
            => new Composite(name, nodes);

        /// <summary>
        /// Creates an instance of a <see cref="Aggregate"/> node
        /// </summary>
        /// <param name="name">The node name</param>
        /// <param name="nodes">The list of comprising nodes</param>
        /// <returns></returns>
        public static ISymbolNode Of(
            AggregationType type,
            bool isOptional,
            params ISymbolNode[] nodes)
            => new Aggregate(type, isOptional, nodes);

        /// <summary>
        /// Creates an instance of a <see cref="Aggregate"/> node
        /// </summary>
        /// <param name="name">The node name</param>
        /// <param name="nodes">The list of comprising nodes</param>
        /// <returns></returns>
        public static ISymbolNode Of(
            AggregationType type,
            bool isOptional,
            IEnumerable<ISymbolNode> nodes)
            => new Aggregate(type, isOptional, nodes);

        /// <summary>
        /// Creates a new Atomic node
        /// </summary>
        /// <param name="name">The production symbol name</param>
        /// <param name="tokens">The tokens</param>
        /// <returns></returns>
        public static ISymbolNode Of(
            string name,
            Tokens tokens)
            => new Atom(name, tokens);

        #endregion

        public interface INodeContainer
        {
            ImmutableArray<ISymbolNode> Nodes { get; }
        }

        /// <summary>
        /// Terminal symbol node - represents recognition of tokens based on an atomic rule.
        /// </summary>
        public readonly struct Atom :
            ISymbolNode,
            IDefaultValueProvider<Atom>,
            IEquatable<Atom>
        {
            private readonly Tokens _tokens;
            private readonly string _symbol;

            #region DefaultValueProvider
            public static Atom Default => default;

            public bool IsDefault => _tokens.IsDefault && _symbol is null;
            #endregion

            #region ISymbolNode
            public Tokens Tokens => _tokens;

            public string Symbol => _symbol;
            #endregion

            public Atom(string name, Tokens tokens)
            {
                _tokens = tokens;

                _symbol = name.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    _ => new ArgumentException($"Invalid {nameof(name)}: '{name}'"));
            }

            public static bool operator ==(Atom left, Atom right) => left.Equals(right);

            public static bool operator !=(Atom left, Atom right) => !left.Equals(right);

            public override bool Equals(object? obj)
            {
                return obj is Atom other && Equals(other);
            }

            public bool Equals(Atom other)
            {
                return _tokens.Equals(other.Tokens)
                    && EqualityComparer<string>.Default.Equals(_symbol, other._symbol);
            }

            public override int GetHashCode() => HashCode.Combine(_tokens, _symbol);

            public override string ToString() => $"[@T Name: {_symbol}; Tokens: {_tokens};]";
        }

        /// <summary>
        /// Composite symbol node - represents a rule-determined composition of symbol nodes.
        /// Used to implement Choice, Sequence, etc.
        /// </summary>
        public readonly struct Composite :
            ISymbolNode,
            INodeContainer,
            IEquatable<Composite>,
            IDefaultValueProvider<Composite>
        {
            private readonly string _name;
            private readonly ImmutableArray<ISymbolNode> _nodes;
            private readonly DeferredValue<string> _text;
            private readonly DeferredValue<Tokens> _tokens;

            #region DefaultValueProvider
            public static Composite Default => default;

            public bool IsDefault =>
                _name is null
                && _text is null
                && _tokens is null
                && _nodes.IsDefault;

            #endregion

            #region ISymbolNode
            public string Symbol => _name;

            public Tokens Tokens => _tokens?.Value ?? Tokens.Default;
            #endregion

            /// <summary>
            /// The list of composing nodes
            /// </summary>
            public ImmutableArray<ISymbolNode> Nodes => _nodes;

            public Composite(
                string name,
                params ISymbolNode[] nodes)
                : this(name, (IEnumerable<ISymbolNode>)nodes)
            { }

            public Composite(
                string name,
                IEnumerable<ISymbolNode> nodes)
            {
                _name = name.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    _ => new ArgumentNullException(nameof(name)));

                var __nodes = _nodes = nodes
                    .ThrowIfNull(
                        () => new ArgumentNullException(nameof(nodes)))
                    .ThrowIfAny(
                        node => node is null,
                        _ => new InvalidOperationException($"Invalid node: null"))
                    .ToImmutableArray();

                var tokenProvider = _tokens = new DeferredValue<Tokens>(() =>
                {
                    return __nodes.Length switch
                    {
                        0 => Tokens.Default,
                        1 => nodes.First().Tokens,
                        _ => nodes.Aggregate(
                            func: (tok, next) => tok.MergeWith(next.Tokens),
                            seed: Tokens.Default)
                    };
                });

                _text = new DeferredValue<string>(() =>
                {
                    var tokenString = tokenProvider.Value.Segment.Count > 20
                        ? $"{tokenProvider.Value[..20]}..."
                        : tokenProvider.Value.ToString();

                    return $"[@N name: {name}; NodeCount: {__nodes.Length}; Tokens: {tokenString};]";
                });
            }

            public override string ToString() => _text?.Value!;

            public static bool operator ==(Composite left, Composite right) => left.Equals(right);

            public static bool operator !=(Composite left, Composite right) => !left.Equals(right);

            public override bool Equals(object? obj)
            {
                return obj is Composite other && Equals(other);
            }

            public bool Equals(Composite other)
            {
                return Common.NullOrEquals(_name, other._name)
                    && Common.NullOrEquals(_tokens, other._tokens)
                    && _nodes.DefaultOrSequenceEqual(other._nodes);
            }

            public override int GetHashCode() => HashCode.Combine(_tokens.Value, _name, _nodes);
        }

        /// <summary>
        /// Aggregate symbol node - represents cardinality-based repetition of symbol nodes.
        /// </summary>
        public readonly struct Aggregate :
            ISymbolNode,
            INodeContainer,
            IEquatable<Aggregate>,
            IDefaultValueProvider<Aggregate>
        {
            private readonly bool _isOptional;
            private readonly AggregationType _type;
            private readonly ImmutableArray<ISymbolNode> _nodes;
            private readonly DeferredValue<string> _text;
            private readonly DeferredValue<Tokens> _tokens;

            #region ISymbolNode
            public Tokens Tokens => _tokens?.Value ?? Tokens.Default;

            public string Symbol => _type.ToString();
            #endregion

            #region DefaultValueProvider
            public bool IsDefault =>
                _type == 0
                && _text is null
                && _tokens is null
                && _nodes.IsDefault
                && _isOptional == false;

            public static Aggregate Default => default;
            #endregion

            public ImmutableArray<ISymbolNode> Nodes => _nodes;

            public bool IsOptional => _isOptional;

            public AggregationType Type => _type;

            public Aggregate(
                AggregationType type,
                bool isOptional,
                params ISymbolNode[] nodes)
                : this(type, isOptional, (IEnumerable<ISymbolNode>)nodes)
            { }

            public Aggregate(
                AggregationType type,
                bool isOptional,
                IEnumerable<ISymbolNode> nodes)
            {
                _type = type;
                _isOptional = isOptional;

                var __nodes = _nodes = nodes
                    .ThrowIfNull(
                        () => new ArgumentNullException(nameof(nodes)))
                    .ThrowIfAny(
                        node => node is null,
                        _ => new InvalidOperationException($"Invalid node: null"))
                    .ToImmutableArray();

                var tokenProvider = _tokens = new DeferredValue<Tokens>(() =>
                {
                    return __nodes.Length switch
                    {
                        0 => Tokens.Default,
                        1 => __nodes[0].Tokens,
                        _ => __nodes.Aggregate(
                            func: (tok, next) => tok.MergeWith(next.Tokens),
                            seed: Tokens.Default)
                    };
                });

                _text = new DeferredValue<string>(() =>
                {
                    var tokenString = tokenProvider.Value.Segment.Count > 20
                        ? $"{tokenProvider.Value[..20]}..."
                        : tokenProvider.Value.ToString();

                    return $"<@N type: {type}; NodeCount: {__nodes.Length}; Tokens: {tokenString};>";
                });
            }

            public bool Equals(Aggregate other)
            {
                return _type == other._type
                    && _isOptional == other._isOptional
                    && _nodes.DefaultOrSequenceEqual(other._nodes);
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj is Aggregate other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = HashCode.Combine(_isOptional, _type);
                return _nodes.Aggregate(hash, HashCode.Combine);
            }

            public override string ToString() => _text?.Value!;

            public static bool operator ==(Aggregate left, Aggregate right) => left.Equals(right);

            public static bool operator !=(Aggregate left, Aggregate right) => !left.Equals(right);
        }
    }

    internal static class SymbolNodeExtensions
    {
        public static IEnumerable<ISymbolNode> FlattenAggregates(this ISymbolNode node)
        {
            return node switch
            {
                ISymbolNode.Atom
                or ISymbolNode.Composite => EnumerableExtensions.Enumerate(node),

                ISymbolNode.Aggregate aggregate => aggregate.Nodes.SelectMany(FlattenAggregates),

                null => throw new ArgumentNullException(nameof(node)),

                _ => throw new InvalidOperationException($"Invalid node-type: {node.GetType()}")
            };
        }

        public static int RequiredNodeCount(this ISymbolNode node)
        {
            return node switch
            {
                ISymbolNode.Composite
                or ISymbolNode.Atom => 1,
                ISymbolNode.Aggregate aggregate => aggregate.IsOptional switch
                {
                    true => 0,
                    false => aggregate.Nodes
                    .Select(RequiredNodeCount)
                    .Sum()
                },
                _ => throw new InvalidOperationException(
                    $"Invalid node type: {node?.GetType()}")
            };
        }
    }
}
