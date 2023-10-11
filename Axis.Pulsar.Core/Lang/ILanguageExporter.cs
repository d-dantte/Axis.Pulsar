namespace Axis.Pulsar.Core.Lang
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILanguageExporter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="grammar"></param>
        /// <returns></returns>
        string ExportGrammar(Grammar.IGrammar grammar);
    }
}
