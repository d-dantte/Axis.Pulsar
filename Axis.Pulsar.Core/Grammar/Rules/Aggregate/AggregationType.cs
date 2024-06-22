namespace Axis.Pulsar.Core.Grammar.Rules.Aggregate
{
    public enum AggregationType
    {

        /// <summary>
        /// A single element (AtomicRef/ProductionRef)
        /// </summary>
        Unit = 0,

        /// <summary>
        /// 
        /// </summary>
        Repetition,

        /// <summary>
        /// A choice aggregation where only the first element to be recognized is chosen
        /// </summary>
        Choice,

        /// <summary>
        /// A sequence aggregation where all elements must be recognized in the order they appear for the aggregation to be valid
        /// </summary>
        Sequence,

        /// <summary>
        /// A set aggregation where all elements ust be recognized, but in any order, for the aggregation to be valid
        /// </summary>
        Set
    }
}
