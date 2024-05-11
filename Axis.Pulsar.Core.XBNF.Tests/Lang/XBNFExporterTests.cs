using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests.Lang;

[TestClass]
public class XBNFExporterTests
{
    [TestMethod]
    public void WriteGrammar()
    {
        // get language string
        using var langDefStream = ResourceLoader.Load("SampleGrammar.json.xbnf");
        var langText = new StreamReader(langDefStream!).ReadToEnd();

        // build importer
        var importer = XBNFImporter
            .NewBuilder()
            .WithDefaultAtomicRuleDefinitions()
            .Build();

        // import
        var lang = importer.ImportLanguage(langText);

        var exporter = new XBNFExporter();
        var grammar = exporter.ExportLanguage(lang);
        Console.WriteLine(grammar);
    }
}
