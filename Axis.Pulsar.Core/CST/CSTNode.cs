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
        Tokens Tokens { get; }

        string Name { get; }

        #region Of
        public static ICSTNode Of(string name, params ICSTNode[] nodes) => new NonTerminal(name, nodes);

        public static ICSTNode Of(string name, NodeSequence nodes) => new NonTerminal(name, nodes);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The production symbol name</param>
        /// <param name="tokens">The tokens</param>
        /// <returns></returns>
        public static ICSTNode Of(string name, Tokens tokens) => new Terminal(name, tokens);
        #endregion

        /// <summary>
        /// Non-Terminal symbol node
        /// </summary>
        public readonly struct NonTerminal :
            ICSTNode,
            IDefaultValueProvider<NonTerminal>
        {
            private readonly string _name;
            private readonly NodeSequence _nodes;
            private readonly Lazy<string> _text;

            #region DefaultValueProvider
            public static NonTerminal Default => default;

            public bool IsDefault => Default.Equals(this);
            #endregion

            public string Name => _name;

            public NodeSequence Nodes => _nodes;

            public Tokens Tokens => _nodes?.Tokens ?? Tokens.Default;

            public NonTerminal(string name, NodeSequence nodes)
            {
                _name = name.ThrowIf(string.IsNullOrWhiteSpace, new ArgumentNullException(nameof(name)));
                _nodes = nodes.ThrowIfNull(new ArgumentNullException(nameof(nodes)));

                var node = this;
                _text = new Lazy<string>(() =>
                {
                    var tokenString = node.Tokens.Count > 20
                        ? $"{node.Tokens[..20]}..."
                        : node.Tokens.ToString();

                    return $"[@N name: {node.Name}; NodeCount: {node.Nodes.Count}; Tokens: {tokenString}]";
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
                _tokens = tokens.ThrowIfDefault(new ArgumentException(
                    $"Invalid {nameof(tokens)}: default"));

                _name = name.ThrowIf(
                    n => !IProduction.SymbolPattern.IsMatch(n),
                    new ArgumentException($"Invalid {nameof(name)}: '{name}'"));
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
