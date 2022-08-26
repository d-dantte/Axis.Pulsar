namespace Axis.Pulsar.Importer.Common.Json.Models
{
    public record Grammar
    {
        public string Language { get; set; }

        public Production[] Productions { get; set; }
    }
}
