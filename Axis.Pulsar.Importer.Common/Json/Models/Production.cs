namespace Axis.Pulsar.Importer.Common.Json.Models
{
    public record Production
    {
        public string Name { get; set; }

        public IRule Rule { get; set; }
    }
}
