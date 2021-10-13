using System;

namespace Axis.Pulsar.Importer.Common.Json.Models
{
    public class Literal : IRule
    {
        public RuleType Type => RuleType.Literal;

        public string Value { get; set; }
        public bool IsCaseSensitive { get; set; }
    }

    public class Pattern : IRule
    {
        public RuleType Type => RuleType.Pattern;

        public string Regex { get; set; }
        public bool IsCaseSensitive { get; set; }
        public int? MaxMatch { get; set; }
        public int MinMatch { get; set; } = 1;
    }

    public class Ref : IRule
    {
        public RuleType Type => RuleType.Ref;

        public string Symbol { get; set; }
        public int? MaxOCcurs { get; set; } = 1;
        public int MinOccurs { get; set; } = 1;
    }


    public enum GroupMode
    {
        Set,
        Sequence,
        Choice
    }

    public class Grouping : IRule
    {
        private IRule[] _rules = Array.Empty<IRule>();

        public RuleType Type => RuleType.Grouping;

        public GroupMode Mode { get; set; }

        public int? MaxOccurs { get; set; } = 1;
        public int MinOccurs { get; set; } = 1;
        public IRule[] Rules
        {
            get => _rules;
            set => _rules = value ?? Array.Empty<IRule>();
        }
    }
}
