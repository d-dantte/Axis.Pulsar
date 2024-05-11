using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

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

        /// <summary>
        /// Non-Terminal symbol node
        /// </summary>
        public readonly struct Composite :
            ISymbolNode,
            IEquatable<Composite>,
            IDefaultValueProvider<Composite>
        {
            private readonly string _name;
            private readonly ImmutableArray<ISymbolNode> _nodes;
            private readonly DeferredValue<string> _text;
            private readonly DeferredValue<Tokens> _tokens;

            #region DefaultValueProvider
            public static Composite Default => default;

            public bool IsDefault => Default.Equals(this);
            #endregion

            public string Symbol => _name;

            /// <summary>
            /// The list of composing nodes
            /// </summary>
            public ImmutableArray<ISymbolNode> Nodes => _nodes;

            public Tokens Tokens => _tokens.Value;

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
                    .ThrowIf(
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
                    && Common.NullOrEquals(_nodes, other._nodes);
            }

            public override int GetHashCode() => HashCode.Combine(_tokens.Value, _name, _nodes);
        }

        /// <summary>
        /// Terminal symbol node
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

            public Tokens Tokens => _tokens;

            public string Symbol => _symbol;

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
    }
}
