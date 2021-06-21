using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Linq;

namespace Axis.Pulsar.Parser.Builder
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ProductionParser : IParser
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
        public ProductionParser(Cardinality cardinality, IParser[] children)
        {
            Cardinality = cardinality;

            if (children == null)
                throw new ArgumentNullException(nameof(children));

            else if (children.Length < 1)
                throw new ArgumentException($"Input array must have at least one element");

            else if (children.Any(p => p == null))
                throw new ArgumentException($"Input array must not contain null elements");

            else
            {
                _children = children;
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
