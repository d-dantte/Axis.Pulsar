using Axis.Pulsar.Parser.CST;

namespace Axis.Pulsar.Parser.Grammar
{
    public interface IRuleValidator<in TRule> where TRule : IRule
    {
        bool IsValidCSTNode(TRule rule, ICSTNode node);
    }
}
