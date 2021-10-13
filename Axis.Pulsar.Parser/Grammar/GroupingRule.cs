using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// 
    /// </summary>
    public enum GroupingMode
    {
        Choice,
        Set,
        Sequence
    }


    /// <summary>
    /// 
    /// </summary>
    public class GroupingRule : Rule
    {
        private readonly List<Rule> _rules = new();

        /// <summary>
        /// 
        /// </summary>
        public Rule[] Rules => _rules.ToArray();

        /// <summary>
        /// 
        /// </summary>
        public GroupingMode GroupingMode { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="cardinality"></param>
        /// <param name="rules"></param>
        private GroupingRule(
            GroupingMode mode,
            Cardinality cardinality,
            params Rule[] rules)
            : base(cardinality)
        {
            if (rules == null || rules.Length == 0)
                throw new ArgumentException($"Invalid {rules} array");

            _rules.AddRange(rules);
            GroupingMode = mode;
        }


        #region Set
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        public static GroupingRule Set(Cardinality cardinality, params Rule[] rules)
        {
            return new(GroupingMode.Set, cardinality, rules);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public static GroupingRule Set(params Rule[] rules)
        {
            return new(GroupingMode.Set, Cardinality.OccursOnlyOnce(), rules);
        }
        #endregion

        #region Sequence
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        public static GroupingRule Sequence(Cardinality cardinality, params Rule[] rules)
        {
            return new(GroupingMode.Sequence, cardinality, rules);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public static GroupingRule Sequence(params Rule[] rules)
        {
            return new(GroupingMode.Sequence, Cardinality.OccursOnlyOnce(), rules);
        }
        #endregion

        #region Choice
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        public static GroupingRule Choice(Cardinality cardinality, params Rule[] rules)
        {
            return new(GroupingMode.Choice, cardinality, rules);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public static GroupingRule Choice(params Rule[] rules)
        {
            return new(GroupingMode.Choice, Cardinality.OccursOnlyOnce(), rules);
        }
        #endregion
    }
}
