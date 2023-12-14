using Axis.Pulsar.Core.Grammar.Groups;

namespace Axis.Pulsar.Core.Grammar.Nodes
{
    /// <summary>
    /// Rules that comprise groupings of other rules.
    /// <para/>
    /// Aggregate rules implement the concept of a recognition threshold. This represents the minimum number of
    /// INITIAL sub-rules that must be parsed for the rule to be established. A typical example of this is with delimited
    /// tokens, e.g a CLASSIC c# string literal. The recognition threshold will be 1, because if the initial double-quote
    /// is recognized, we are fairly certain we are meant to recognize a string literal.
    /// </summary>
    public interface ICompositeRule : INodeRule
    {
        /// <summary>
        /// The primary group
        /// </summary>
        IGroupElement Element { get; }

        /// <summary>
        /// The recognition threshold.
        /// </summary>
        uint RecognitionThreshold { get; }
    }
}
