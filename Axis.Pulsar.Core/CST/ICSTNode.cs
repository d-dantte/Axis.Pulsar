using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// TODO: Add <see cref="ProductionPath"/> as a member of all nodes
    /// </summary>
    public interface ICSTNode
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
        public static ICSTNode Of(
            string name,
            params ICSTNode[] nodes)
            => new Composite(name, INodeSequence.Of(nodes));

        /// <summary>
        /// Creates an instance of a <see cref="Composite"/> node
        /// </summary>
        /// <param name="name">The node name</param>
        /// <param name="nodes">The list of comprising nodes</param>
        /// <returns></returns>
        public static ICSTNode Of(
            string name,
            INodeSequence nodes)
            => new Composite(name, nodes);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The production symbol name</param>
        /// <param name="tokens">The tokens</param>
        /// <returns></returns>
        public static ICSTNode Of(
            string name,
            Tokens tokens)
            => new Atom(name, tokens);

        #endregion

        /// <summary>
        /// Non-Terminal symbol node
        /// </summary>
        public readonly struct Composite :
            ICSTNode,
            IDefaultValueProvider<Composite>
        {
            private readonly string _name;
            private readonly INodeSequence _nodes;
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
            public INodeSequence Nodes => _nodes;

            public Tokens Tokens => _tokens.Value;

            public Composite(string name, INodeSequence nodes)
            {
                _name = name.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    _ => new ArgumentNullException(nameof(name)));
                _nodes = nodes.ThrowIfNull(() => new ArgumentNullException(nameof(nodes)));

                var tokenProvider = _tokens = new DeferredValue<Tokens>(() =>
                {
                    return nodes.Count switch
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

                    return $"[@N name: {name}; NodeCount: {nodes.Count}; Tokens: {tokenString}]";
                });
            }

            public override string ToString() => _text?.Value!;
        }

        /// <summary>
        /// Terminal symbol node
        /// </summary>
        public readonly struct Atom :
            ICSTNode,
            IDefaultValueProvider<Atom>,
            IEquatable<Atom>
        {
            private readonly Tokens _tokens;
            private readonly string _name;

            #region DefaultValueProvider
            public static Atom Default => default;

            public bool IsDefault => _tokens.IsDefault && _name is null;
            #endregion

            public Tokens Tokens => _tokens;

            public string Symbol => _name;

            public Atom(string name, Tokens tokens)
            {
                _tokens = tokens;

                _name = name.ThrowIf(
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
                    && EqualityComparer<string>.Default.Equals(_name, other._name);
            }

            public override int GetHashCode() => HashCode.Combine(_tokens, _name);

            public override string ToString() => $"[@T Name: {Symbol}; Tokens: {Tokens}]";
        }
    }
}
