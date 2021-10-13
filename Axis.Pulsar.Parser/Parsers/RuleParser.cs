using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Linq;

namespace Axis.Pulsar.Parser.Parsers
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class RuleParser : IParser
    {
        private readonly IParser[] _children;


        /// <summary>
        /// 
        /// </summary>
        public Cardinality Cardinality { get; }

        /// <summary>
        /// 
        /// </summary>
        public IParser[] Children => _children.ToArray();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        public RuleParser(Cardinality cardinality, IParser[] children = null)
        {
            Cardinality = cardinality;

            if (children?.Any(p => p == null) == true)
                throw new ArgumentException($"Input array must not contain null elements");

            else
            {
                _children = children ?? Array.Empty<IParser>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public abstract bool TryParse(BufferedTokenReader tokenReader, out ParseResult result);


        /// <summary>
        /// Check if, given the completed repetitions, it is legal to repeat the parse cycle based on the cardinality
        /// </summary>
        /// <param name="completedRepetitions"></param>
        /// <returns>Value indicating if a repetition is legal</returns>
        protected bool CanRepeat(int completedRepetitions)
        {
            if (completedRepetitions < Cardinality.MinOccurence)
                return true;

            else if (Cardinality.MaxOccurence == null)
                return true;

            else if (completedRepetitions < Cardinality.MaxOccurence)
                return true;

            else return false;
        }
    }
}
