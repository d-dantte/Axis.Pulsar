using System;

namespace Axis.Pulsar.Importer.Common.Json.Models
{
    public record Literal : IRule
    {
        public RuleType Type => RuleType.Literal;

        public string Value { get; set; }

        public bool IsCaseSensitive { get; set; }
    }

    public record Pattern : IRule
    {
        public RuleType Type => RuleType.Pattern;

        public string Regex { get; set; }
        public bool IsCaseSensitive { get; set; }

        public IMatchType MatchType { get; set; }
    }


    public interface IMatchType
    {
        public record OpenMatchType : IMatchType
        {
            public int MaxMismatch { get; set; } = 1;

            public bool AllowsEmpty { get; set; } = false;
        }

        public record ClosedMatchType : IMatchType
        {
            public int MaxMatch { get; set; } = 1;
            public int MinMatch { get; set; } = 1;
        }
    }

    public record Ref : IRule
    {
        public RuleType Type => RuleType.Ref;

        public string Symbol { get; set; }
        public int? MaxOCcurs { get; set; } = 1;
        public int MinOccurs { get; set; } = 1;
    }

    public record EOF : IRule
    {
        public RuleType Type => RuleType.EOF;
    }

    public enum GroupMode
    {
        Set,
        Sequence,
        Choice
    }

    public record Grouping: IRule
    {
        private IRule[] _rules = Array.Empty<IRule>();

        public RuleType Type => RuleType.Grouping;

        public GroupMode Mode { get; set; }

        public int? MaxOccurs { get; set; } = 1;
        public int MinOccurs { get; set; } = 1;
        public int? MinContentCount { get; set; } = 1;
        public IRule[] Rules
        {
            get => _rules;
            set => _rules = value ?? Array.Empty<IRule>();
        }
    }

    public record Expression : IRule
    {
        public RuleType Type => RuleType.Expression;

        public int? RecognitionThreshold { get; set; }

        public Grouping Grouping { get; set; }
    }
}
