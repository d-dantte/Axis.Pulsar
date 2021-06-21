using Axis.Pulsar.Parser.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Language
{
    public class Production
    {
        private readonly Production[] _members;

        public Cardinality Cardinality { get; }

        public IRule Rule { get; }

        public GroupingMode Mode { get; }

        public Production[] Members => _members?.ToArray();


        private Production(GroupingMode mode, Cardinality cardinality, IEnumerable<Production> members)
        {
            Mode = mode;
            Cardinality = cardinality;
            Rule = null;
            _members = members?.ToArray() ?? throw new System.ArgumentNullException(nameof(members));

            if (_members.Any(t => t == null) == true)
                throw new System.Exception($"Cannot contain a null {nameof(Production)} Member");
        }

        private Production(Cardinality cardinality, IRule rule)
        {
            Mode = GroupingMode.Single;
            Cardinality = cardinality;
            _members = null;
            Rule = rule;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static Production Set(
            Cardinality cardinality,
            Production first,
            Production second,
            params Production[] others)
            => new(GroupingMode.Set, cardinality, first.Concat(second).Concat(others));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static Production Set(
            Production first,
            Production second,
            params Production[] others)
            => new(GroupingMode.Set, Cardinality.OccursOnlyOnce(), first.Concat(second).Concat(others));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static Production Sequence(
            Cardinality cardinality,
            Production first,
            Production second,
            params Production[] others)
            => new(GroupingMode.Sequence, cardinality, first.Concat(second).Concat(others));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static Production Sequence(
            Production first,
            Production second,
            params Production[] others)
            => new(GroupingMode.Sequence, Cardinality.OccursOnlyOnce(), first.Concat(second).Concat(others));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static Production Choice(
            Cardinality cardinality,
            Production first,
            Production second,
            params Production[] others)
            => new(GroupingMode.Choice, cardinality, first.Concat(second).Concat(others));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static Production Choice(
            Production first,
            Production second,
            params Production[] others)
            => new(GroupingMode.Choice, Cardinality.OccursOnlyOnce(), first.Concat(second).Concat(others));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static Production Single(Cardinality cardinality, IRule rule) => new(cardinality, rule);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static Production Single(IRule rule) => new(Cardinality.OccursOnlyOnce(), rule);

    }

    public enum GroupingMode
    {
        Single,
        Set,
        Sequence,
        Choice
    }
}
