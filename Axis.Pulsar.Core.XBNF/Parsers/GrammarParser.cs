using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.XBNF;

public static class GrammarParser
{
    public static bool TryParseGrammar(
        TokenReader reader,
        MetaContext context,
        out IResult<IGrammar> result)
    {
        var position = reader.Position;
        var productions = new List<Production>();

        try
        {
            do
            {
                // silent block
                _ = TryParseSilentBlock(reader, context, out var sblockResult);

                if (sblockResult.IsErrorResult(out UnknownError uke)
                    || sblockResult.IsErrorResult(out FaultyMatchError fre))
                {
                    reader.Reset(position);
                    result = sblockResult.MapAs<IGrammar>();
                    return false;
                }

                // production
                if (TryParseProduction(reader, context, out var productionResult))
                    productionResult.Consume(productions.Add);

                else if (productionResult.IsErrorResult(out UnmatchedError ure))
                    break;

                else
                {
                    reader.Reset(position);
                    result = productionResult.MapAs<IGrammar>();
                    return false;
                }
            }
            while (true);

            result = productions
                .ApplyTo(prods => Grammar.Grammar.Of(
                    productions[0].Symbol,
                    productions))
                .ApplyTo(Result.Of);
            return true;
        }
        catch (Exception e)
        {
                    reader.Reset(position);
            result = Result.Of<IGrammar>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseProduction(
        TokenReader reader,
        MetaContext context,
        out IResult<Production> result)
    {
        var position = reader.Position;

        try
        {
            // symbol name
            if (!TryParseCompositeSymbolName(reader, context, out var symbolNameResult))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            // space
            if (!TryParseSilentBlock(reader, context, out _))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            // =>
            if (!TryParseMapOperator(reader, context, out _))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            // space
            if (!TryParseSilentBlock(reader, context, out _))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            // rule
            if (!TryParseCompositeRule(reader, context, out var ruleResult))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            result = symbolNameResult.Combine(ruleResult, Production.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<Production>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseCompositeSymbolName(
        TokenReader reader,
        MetaContext context,
        out IResult<string> result)
    {
        var position = reader.Position;

        try
        {
            if (!reader.TryGetToken(out var token)
                || !'$'.Equals(token[0]))
            {
                reader.Reset(position);
                result = Result.Of<string>(new UnmatchedError(
                    "composite-symbol-name",
                    position));
                return false;
            }

            if (!reader.TryGetPattern(IProduction.SymbolPattern, out var tokens))
            {
                reader.Reset(position);
                result = Result.Of<string>(new FaultyMatchError(
                    "composite-symbol-name",
                    reader.Position,
                    reader.Position - position));
                return false;
            }

            result = Result.Of(tokens.ToString()!);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<string>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseAtomicSymbolName(
        TokenReader reader,
        MetaContext context,
        out IResult<string> result)
    {
        var position = reader.Position;

        try
        {
            if (!reader.TryGetToken(out var token)
                || !'@'.Equals(token[0]))
            {
                reader.Reset(position);
                result = Result.Of<string>(new UnmatchedError(
                    "atomic-symbol-name",
                    position));
                return false;
            }

            if (!reader.TryGetPattern(IProduction.SymbolPattern, out var tokens))
            {
                reader.Reset(position);
                result = Result.Of<string>(new FaultyMatchError(
                    "atomic-symbol-name",
                    reader.Position,
                    reader.Position - position));
                return false;
            }

            result = Result.Of(tokens.ToString()!);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<string>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseMapOperator(
        TokenReader reader,
        MetaContext context,
        out IResult<Tokens> result)
    {
        var position = reader.Position;

        try
        {
            if (!reader.TryGetTokens("=>", out Tokens tokens))
            {
                result = Result.Of<Tokens>(new UnrecognizedTokens(
                    "map-operator",
                    position));
                return false;
            }

            result = Result.Of(tokens);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<Tokens>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseSilentBlock(
        TokenReader reader,
        MetaContext context,
        out IResult<ISilentBlock> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<ISilentBlock>(new UnknownError(e));
            return false;
        }
    }

    #region Composite
    public static bool TryParseCompositeRule(
        TokenReader reader,
        MetaContext context,
        out IResult<ICompositeRule> result)
    {
        var position = reader.Position;

        try
        {
            if (!TryParseRecognitionThreshold(reader, context, out var thresholdResult)
                && !thresholdResult.IsErrorResult(out UnmatchedError ume))
            {
                reader.Reset(position);
                result = thresholdResult.MapAs<ICompositeRule>();
                return false;
            }
            thresholdResult = thresholdResult.MapError(err => 1u);

            if (!TryParseGroupElement(reader, context, out var elementResult))
            {
                reader.Reset(position);
                result = elementResult.MapAs<ICompositeRule>();
                return false;
            }

            result = thresholdResult
                .Combine(elementResult, NonTerminal.Of)
                .MapAs<ICompositeRule>();
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<ICompositeRule>(new UnknownError(e));
            return false;
        }
    }


    private static readonly Regex DigitPattern = new Regex("^\\d+\\z", RegexOptions.Compiled);
    public static bool TryParseRecognitionThreshold(
        TokenReader reader,
        MetaContext context,
        out IResult<uint> result)
    {
        var position = reader.Position;

        try
        {
            if (!reader.TryGetTokens(":", out var colonToken))
            {
                reader.Reset(position);
                result = Result.Of<uint>(new UnmatchedError(
                    "recognition-threshold",
                    position));
                return false;
            }

            if (!reader.TryGetPattern(DigitPattern, out var digitTokens))
            {
                result = Result.Of<uint>(new FaultyMatchError(
                    "recognition-threshold",
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            result = uint
                .Parse(digitTokens.AsSpan())
                .ApplyTo(Result.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<uint>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseGroupElement(
        TokenReader reader,
        MetaContext context,
        out IResult<IGroupElement> result)
    {
        var position = reader.Position;

        try
        {
            #region Atomic Rule Ref
            if (TryParseAtomicRuleRef(reader, context, out var atomicRuleRefResult))
            {
                result = atomicRuleRefResult.MapAs<IGroupElement>();
                return true;
            }
            else if (atomicRuleRefResult.IsErrorResult(out FaultyMatchError fme)
                || atomicRuleRefResult.IsErrorResult(out UnknownError uke))
            {
                result = atomicRuleRefResult.MapAs<IGroupElement>();
                reader.Reset(position);
                return false;
            }
            #endregion

            #region or Production Ref
            if (TryParseProductionRef(reader, context, out var productionRefResult))
            {
                result = productionRefResult.MapAs<IGroupElement>();
                return true;
            }
            else if (productionRefResult.IsErrorResult(out FaultyMatchError fme)
                || productionRefResult.IsErrorResult(out UnknownError uke))
            {
                result = productionRefResult.MapAs<IGroupElement>();
                reader.Reset(position);
                return false;
            }
            #endregion

            #region or Group
            if (TryParseGroup(reader, context, out var groupResult))
            {
                result = groupResult.MapAs<IGroupElement>();
                return true;
            }
            else
            {
                result = groupResult.MapAs<IGroupElement>();
                reader.Reset(position);
                return false;
            }
            #endregion
        }
        catch (Exception e)
        {
            result = Result.Of<IGroupElement>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseAtomicRuleRef(
        TokenReader reader,
        MetaContext context,
        out IResult<AtomicRuleRef> result)
    {
        var position = reader.Position;

        try
        {
            if (!TryParseAtomicRule(reader, context, out var atomicRuleResult))
            {
                result = atomicRuleResult.MapAs<AtomicRuleRef>();
                reader.Reset(position);
                return false;
            }

            if (!TryParseCardinality(reader, context, out var cardinalityResult))
            {
                result = atomicRuleResult.MapAs<AtomicRuleRef>();
                reader.Reset(position);
                return false;
            }

            result = cardinalityResult.Combine(
                atomicRuleResult,
                AtomicRuleRef.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<AtomicRuleRef>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseProductionRef(
        TokenReader reader,
        MetaContext context,
        out IResult<ProductionRef> result)
    {
        var position = reader.Position;

        try
        {
            if (!TryParseCompositeSymbolName(reader, context, out var compositeSymbolNameResult))
            {
                result = compositeSymbolNameResult.MapAs<ProductionRef>();
                reader.Reset(position);
                return false;
            }

            if (!TryParseCardinality(reader, context, out var cardinalityResult))
            {
                result = cardinalityResult.MapAs<ProductionRef>();
                reader.Reset(position);
                return false;
            }

            result = cardinalityResult.Combine(
                compositeSymbolNameResult,
                ProductionRef.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<ProductionRef>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseGroup(
        TokenReader reader,
        MetaContext context,
        out IResult<IGroup> result)
    {
        var position = reader.Position;

        try
        {
            #region Choice
            if (TryParseChoice(reader, context, out var choiceResult))
            {
                result = choiceResult.MapAs<IGroup>();
                return true;
            }
            else if (choiceResult.IsErrorResult(out FaultyMatchError fme)
                || choiceResult.IsErrorResult(out UnknownError uke))
            {
                result = choiceResult.MapAs<IGroup>();
                reader.Reset(position);
                return false;
            }
            #endregion

            #region or Sequence
            if (TryParseSequence(reader, context, out var sequenceResult))
            {
                result = sequenceResult.MapAs<IGroup>();
                return true;
            }
            else if (sequenceResult.IsErrorResult(out FaultyMatchError fme)
                || sequenceResult.IsErrorResult(out UnknownError uke))
            {
                result = sequenceResult.MapAs<IGroup>();
                reader.Reset(position);
                return false;
            }
            #endregion

            #region or Group
            if (TryParseSet(reader, context, out var setResult))
            {
                result = setResult.MapAs<IGroup>();
                return true;
            }
            else
            {
                result = setResult.MapAs<IGroup>();
                reader.Reset(position);
                return false;
            }
            #endregion
        }
        catch (Exception e)
        {
            result = Result.Of<IGroup>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseSet(
        TokenReader reader,
        MetaContext context,
        out IResult<Set> result)
    {
        var position = reader.Position;

        try
        {
            if (!reader.TryGetTokens("#", out var delimiterToken))
            {
                reader.Reset(position);
                result = Result.Of<Set>(new UnmatchedError(
                    "set-group",
                    position));
                return false;
            }

            // optional min match count
            if (!reader.TryGetPattern(DigitPattern, out var minMatchCount))
                minMatchCount = Tokens.Empty;

            // element list
            if(!TryParseElementList(reader, context, out var elementListResult))
            {
                result = Result.Of<Set>(new FaultyMatchError(
                    "set-group",
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            // cardinality
            if (!TryParseCardinality(reader, context, out var cardinalityResult))
            {
                result = cardinalityResult.MapAs<Set>();
                reader.Reset(position);
                return false;
            }

            result = cardinalityResult.Combine(
                elementListResult,
                (cardinality, elements) => Set.Of(
                    cardinality,
                    minMatchCount.IsEmpty ? null : int.Parse(minMatchCount.AsSpan()),
                    elements));
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<Set>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseChoice(
        TokenReader reader,
        MetaContext context,
        out IResult<Choice> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<ProductionRef>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseSequence(
        TokenReader reader,
        MetaContext context,
        out IResult<Sequence> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<ProductionRef>(new UnknownError(e));
            return false;
        }
    }

    /// <summary>
    /// If cardinality is not found, return a "occurs only once" cardinality.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParseCardinality(
        TokenReader reader,
        MetaContext context,
        out IResult<Cardinality> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<Cardinality>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseElementList(
        TokenReader reader,
        MetaContext context,
        out IResult<IGroupElement[]> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<Production>(new UnknownError(e));
            return false;
        }
    }

    #endregion

    #region Atomic
    public static bool TryParseAtomicRule(
        TokenReader reader,
        MetaContext context,
        out IResult<IAtomicRule> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<IAtomicRule>(new UnknownError(e));
            return false;
        }
    }
    #endregion

    public static bool TryParse___(
        TokenReader reader,
        MetaContext context,
        out IResult<Production> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<Production>(new UnknownError(e));
            return false;
        }
    }
    
}
