namespace Axis.Pulsar.Core.IO
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
        string ExportLanguage(ILanguageContext context);
    }
}
