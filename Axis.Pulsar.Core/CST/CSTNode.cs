using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
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
        string Name { get; }

        #region Of

        /// <summary>
        /// Creates an instance of a <see cref="NonTerminal"/> node
        /// </summary>
        /// <param name="name">The node name</param>
        /// <param name="nodes">The list of comprising nodes</param>
        /// <returns></returns>
        public static ICSTNode Of(
            string name,
            params ICSTNode[] nodes)
            => new NonTerminal(name, INodeSequence.Of(nodes));

        /// <summary>
        /// Creates an instance of a <see cref="NonTerminal"/> node
        /// </summary>
        /// <param name="name">The node name</param>
        /// <param name="nodes">The list of comprising nodes</param>
        /// <returns></returns>
        public static ICSTNode Of(
            string name,
            INodeSequence nodes)
            => new NonTerminal(name, nodes);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The production symbol name</param>
        /// <param name="tokens">The tokens</param>
        /// <returns></returns>
        public static ICSTNode Of(
            string name,
            Tokens tokens)
            => new Terminal(name, tokens);

        #endregion

        /// <summary>
        /// Non-Terminal symbol node
        /// </summary>
        public readonly struct NonTerminal :
            ICSTNode,
            IDefaultValueProvider<NonTerminal>
        {
            private readonly string _name;
            private readonly INodeSequence _nodes;
            private readonly DeferredValue<string> _text;
            private readonly DeferredValue<Tokens> _tokens;

            #region DefaultValueProvider
            public static NonTerminal Default => default;

            public bool IsDefault => Default.Equals(this);
            #endregion

            public string Name => _name;

            /// <summary>
            /// The list of composing nodes
            /// </summary>
            public INodeSequence Nodes => _nodes;

            public Tokens Tokens => _tokens.Value;

            public NonTerminal(string name, INodeSequence nodes)
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
        public readonly struct Terminal :
            ICSTNode,
            IDefaultValueProvider<Terminal>
        {
            private readonly Tokens _tokens;
            private readonly string _name;

            #region DefaultValueProvider
            public static Terminal Default => default;

            public bool IsDefault => _tokens.IsDefault && _name is null;
            #endregion

            public Tokens Tokens => _tokens;

            public string Name => _name;

            public Terminal(string name, Tokens tokens)
            {
                _tokens = tokens;

                _name = name.ThrowIf(
                    n => !IProduction.SymbolPattern.IsMatch(n),
                    _ => new ArgumentException($"Invalid {nameof(name)}: '{name}'"));
            }

            public static bool operator ==(Terminal left, Terminal right) => left.Equals(right);

            public static bool operator !=(Terminal left, Terminal right) => !left.Equals(right);

            public override bool Equals(object? obj)
            {
                return obj is Terminal other
                    && _tokens.Equals(other.Tokens)
                    && EqualityComparer<string>.Default.Equals(_name, other._name);
            }

            public override int GetHashCode() => HashCode.Combine(_tokens, _name);

            public override string ToString() => $"[@T Name: {Name}; Tokens: {Tokens}]";
        }
    }
}
