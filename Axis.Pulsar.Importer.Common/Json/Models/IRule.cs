namespace Axis.Pulsar.Importer.Common.Json.Models
{
    public enum RuleType
    {
        Literal,
        Pattern,
        Ref,
        Grouping
    }

    public interface IRule
    {
        RuleType Type { get; }
    }
}
