namespace Axis.Pulsar.Grammar.Language
{
    internal interface IProductionAppender
    {
        /// <summary>
        /// Adds a new production to the underlying SET of productions.
        /// </summary>
        /// <param name="production"></param>
        /// <returns></returns>
        IProductionAppender AddProduction(Production production);

        /// <summary>
        /// Adds a new production to the underlying SET of productions, indicating a successful operation or otherwise.
        /// </summary>
        /// <param name="production">The production to append</param>
        /// <returns>true if the production was added, false if it already existed and was not added</returns>
        bool TryAddProduction(Production production);
    }
}
