using Axis.Pulsar.Grammar.CST;

namespace Axis.Pulsar.Grammar.Utils
{
    public interface IParsable<TValue>: System.IParsable<TValue>
    where TValue : IParsable<TValue>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        static abstract TValue Parse(CSTNode node);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        static abstract bool TryParse(CSTNode node, out TValue value);
    }
}
