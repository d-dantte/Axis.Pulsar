using Axis.Pulsar.Core.Grammar.Rules;

namespace Axis.Pulsar.Core.Grammar
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGrammar
    {
        /// <summary>
        /// 
        /// </summary>
        public string Root { get; }

        /// <summary>
        /// 
        /// </summary>
        public int ProductionCount { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Production this[string name] { get; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> ProductionSymbols { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbolName"></param>
        /// <returns></returns>
        public bool ContainsProduction(string symbolName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Production GetProduction(string name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="production"></param>
        /// <returns></returns>
        public bool TryGetProduction(string name, out Production? production);
    }
}
