using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Parsers.Models;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;


public static class GrammarParser
{
    private static readonly Regex DigitPattern = new Regex("^\\d+\\z", RegexOptions.Compiled);

    private static readonly Regex CardinalityMinOccurencePattern = new Regex(
        "^\\*|\\?|\\+|\\d+\\z",
        RegexOptions.Compiled);

    #region Production
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
                .ApplyTo(prods => XBNFGrammar.Of(
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
    #endregion

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

            #region or Set
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
            if (!TryParseElementList(reader, context, out var elementListResult))
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
            if (!reader.TryGetTokens("?", out var delimiterToken))
            {
                reader.Reset(position);
                result = Result.Of<Choice>(new UnmatchedError(
                    "choice-group",
                    position));
                return false;
            }

            // element list
            if (!TryParseElementList(reader, context, out var elementListResult))
            {
                result = Result.Of<Choice>(new FaultyMatchError(
                    "choice-group",
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            // cardinality
            if (!TryParseCardinality(reader, context, out var cardinalityResult))
            {
                result = cardinalityResult.MapAs<Choice>();
                reader.Reset(position);
                return false;
            }

            result = cardinalityResult.Combine(elementListResult, Choice.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<Choice>(new UnknownError(e));
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
            if (!reader.TryGetTokens("+", out var delimiterToken))
            {
                reader.Reset(position);
                result = Result.Of<Sequence>(new UnmatchedError(
                    "sequence-group",
                    position));
                return false;
            }

            // element list
            if (!TryParseElementList(reader, context, out var elementListResult))
            {
                result = Result.Of<Sequence>(new FaultyMatchError(
                    "sequence-group",
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            // cardinality
            if (!TryParseCardinality(reader, context, out var cardinalityResult))
            {
                result = cardinalityResult.MapAs<Sequence>();
                reader.Reset(position);
                return false;
            }

            result = cardinalityResult.Combine(elementListResult, Sequence.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<Sequence>(new UnknownError(e));
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
            if (!reader.TryGetToken(out var delimiterToken)
                || !'.'.Equals(delimiterToken[0]))
            {
                reader.Reset(position);
                result = Result.Of(Cardinality.OccursOnlyOnce());
                return true;
            }

            // min occurs value
            if (!reader.TryGetPattern(CardinalityMinOccurencePattern, out var minOccursTokens))
            {
                result = Result.Of<Cardinality>(new FaultyMatchError(
                    "cardinality",
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            // value separator
            if (!reader.TryGetTokens(",", out var separatorTokens))
            {
                result = Result.Of(minOccursTokens[0] switch
                {
                    '*' => Cardinality.OccursNeverOrMore(),
                    '?' => Cardinality.OccursOptionally(),
                    '+' => Cardinality.OccursAtLeastOnce(),
                    _ => Cardinality.OccursOnly(int.Parse(minOccursTokens.AsSpan()))
                });
                return true;
            }

            // max occurs value
            if (reader.TryGetPattern(DigitPattern, out var maxOccursTokens))
            {
                result = minOccursTokens[0] switch
                {
                    '*' or '?' or '+' => Result.Of<Cardinality>(new FaultyMatchError(
                        "cardinality",
                        position,
                        reader.Position - position)),

                    _ => Result.Of(Cardinality.Occurs(
                        int.Parse(minOccursTokens.AsSpan()),
                        int.Parse(maxOccursTokens.AsSpan())))
                };
                return true;
            }
            else
            {
                result = minOccursTokens[0] switch
                {
                    '*' or '?' or '+' => Result.Of<Cardinality>(new FaultyMatchError(
                        "cardinality",
                        position,
                        reader.Position - position)),

                    _ => Result.Of(Cardinality.OccursAtLeast(
                        int.Parse(minOccursTokens.AsSpan())))
                };
                return true;
            }
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
            // open bracket
            if (!reader.TryGetTokens("[", out var openBracket))
            {
                result = Result.Of<IGroupElement[]>(new UnmatchedError(
                    "element-list",
                    position));
                reader.Reset(position);
                return false;
            }

            var elementList = new List<IGroupElement>();

            do
            {
                // whitespace
                _ = TryParseSilentBlock(reader, context, out _);

                if (TryParseGroupElement(reader, context, out var elementResult))
                    elementResult.Consume(elementList.Add);

                else
                {
                    result = Result.Of<IGroupElement[]>(new FaultyMatchError(
                        "element-list",
                        position,
                        reader.Position - position));
                    reader.Reset(position);
                    return false;
                }

                // possible whitespace after element
                _ = TryParseSilentBlock(reader, context, out _);
            }
            while (reader.TryGetToken(out var commaToken) && ','.Equals(commaToken[0]));

            reader.Back(); // back one space for the failed ',' match
            if (!reader.TryGetTokens("]", out var closeBracket))
            {
                result = Result.Of<IGroupElement[]>(new FaultyMatchError(
                    "element-list",
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            result = elementList
                .ToArray()
                .ApplyTo(Result.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<IGroupElement[]>(new UnknownError(e));
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
            result = ParserAccumulator
                .Of(reader, context, (Name: string.Empty, Args: new List<ArgumentPair>()))

                // parse atomic symbol name
                .ThenTry<string>(TryParseAtomicSymbolName, (r, name) =>
                {
                    r.Name = name;
                    return r;
                })

                // or parse atomic content, and derive symbol name
                .OrTry<AtomicContentArgumentInfo>(TryParseAtomicContent, (r, contentInfo) =>
                {
                    if (!context.AtomicContentTypeMap.TryGetValue(contentInfo.ContentType, out var symbol))
                        throw new FaultyMatchError(
                            "atomic-rule",
                            position,
                            reader.Position - position);

                    r.Name = symbol;
                    r.Args.Add(ArgumentPair.Of(
                        IAtomicRuleFactory.ContentArgument,
                        contentInfo.Content.ToString()!));

                    return r;
                })

                // parse optional arguments
                .ThenTry<ArgumentPair[]>(
                    tryParse: TryParseAtomicRuleArguments,
                    optionalValueAggregatorFunction: r => r,
                    aggregatorFunction: (r, args) =>
                    {
                        r.Args.AddRange(args);
                        return r;
                    })

                // map to atomic rule
                .ToResult(r =>
                {
                    if (!context.AtomicFactoryMap.TryGetValue(r.Name, out var factoryDef))
                        throw new FaultyMatchError(
                            "atomic-rule",
                            position,
                            reader.Position - position);

                    return factoryDef.Factory.NewRule(
                        context,
                        r.Args.ToImmutableDictionary(arg => arg.Argument, arg => arg.Value));
                });

            if (result.IsErrorResult())
                reader.Reset(position);

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            result = Result.Of<IAtomicRule>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseAtomicContent(
        TokenReader reader,
        MetaContext context,
        out IResult<AtomicContentArgumentInfo> result)
    {
        var position = reader.Position;

        try
        {
            var accumulator = ParserAccumulator.Of(
                reader, context,
                new AtomicContentArgumentInfo(),
                new UnmatchedError("atomic-content", position)); // <-- error so we can start off with "OrTry"

            foreach (var delimChar in AtomicContentDelimiterTypeExtensions.DelimiterCharacterSet)
            {
                accumulator = accumulator.OrTry(
                    (TokenReader x, MetaContext y, out IResult<Tokens> z) => TryParseDelimitedContent(x, y, delimChar, delimChar, out z),
                    (info, content) => info with
                    {
                        Content = content,
                        ContentType = delimChar.DelimiterType()
                    });

                // break if we already have a match
                if (!accumulator.IsPreviousOpErrored)
                    break;
            }

            if (accumulator.IsPreviousOpErrored)
            {
                result = !accumulator.IsPreviousOpUnmatched
                    ? accumulator.ToResult<AtomicContentArgumentInfo>()
                    : Result.Of<AtomicContentArgumentInfo>(
                        new FaultyMatchError(
                            "atomic-rule-arguments",
                            position,
                            reader.Position - position));
                reader.Reset(position);
                return false;
            }

            result = accumulator.ToResult();
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<AtomicContentArgumentInfo>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseDelimitedContent(
        TokenReader reader,
        MetaContext context,
        char startDelimiter,
        char endDelimiter,
        out IResult<Tokens> result)
    {
        var position = reader.Position;

        try
        {
            // start delim
            if (!reader.TryGetTokens(startDelimiter.ToString(), out var startDelimToken))
            {
                result = Result.Of<Tokens>(new UnrecognizedTokens(
                    "atomic-rule",
                    position));
                reader.Reset(position);
                return false;
            }

            // content chars
            var contentTokens = Tokens.Empty;
            while (reader.TryGetToken(out var stringChar))
            {
                if (stringChar[0] == startDelimToken[0]
                    && contentTokens.Count > 0 && contentTokens[^1] != '\\')
                {
                    reader.Back();
                    break;
                }
                else contentTokens += stringChar; // contentTokens = contentTokens.Join(stringChar);
            }

            // end delim
            if (!reader.TryGetTokens(endDelimiter.ToString(), out var endDelimToken))
            {
                result = Result.Of<Tokens>(new FaultyMatchError(
                    "atomic-rule",
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            result = Result.Of(contentTokens);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<Tokens>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseAtomicRuleArguments(
        TokenReader reader,
        MetaContext context,
        out IResult<ArgumentPair[]> result)
    {
        var position = reader.Position;

        try
        {
            if (!reader.TryGetTokens("{", out var startDelimToken))
            {
                result = Result.Of<ArgumentPair[]>(new UnmatchedError(
                    "atomic-rule-arguments",
                    position));
                reader.Reset(position);
                return false;
            }

            var argSeparatorToken = Tokens.Default;
            var accumulator = ParserAccumulator
                .Of(reader, context, new List<ArgumentPair>());

            do
            {
                accumulator = accumulator

                    // optional silent block
                    .ThenTry<SilentBlock>(
                        TryParseSilentBlock,
                        (args, silentBlock) => args,
                        args => args)

                    // required argument-pair
                    .ThenTry<ArgumentPair>(
                        TryParseArgument,
                        (args, arg) => args.AddItem(arg))

                    // optional silent block
                    .ThenTry<SilentBlock>(
                        TryParseSilentBlock,
                        (args, silentBlock) => args,
                        args => args);
            }
            while (!accumulator.IsPreviousOpErrored && reader.TryGetTokens(",", out argSeparatorToken));

            // back up from the mis-matched separator
            reader.Back();

            if (accumulator.IsPreviousOpErrored)
            {
                result = !accumulator.IsPreviousOpUnmatched
                    ? accumulator.ToResult<ArgumentPair[]>()
                    : Result.Of<ArgumentPair[]>(
                        new FaultyMatchError(
                            "atomic-rule-arguments",
                            position,
                            reader.Position - position));
                reader.Reset(position);
                return false;
            }

            result = accumulator.Data
                .ToArray()
                .ApplyTo(Result.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<ArgumentPair[]>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseArgument(
        TokenReader reader,
        MetaContext context,
        out IResult<ArgumentPair> result)
    {
        var position = reader.Position;

        try
        {
            if (!reader.TryGetPattern(Argument.ArgumentPattern, out var argKey))
            {
                result = Result.Of<ArgumentPair>(new UnmatchedError(
                    "atomic-rule-argument",
                    position));
                reader.Reset(position);
                return false;
            }

            // optional whitespace
            _ = TryParseSilentBlock(reader, context, out _);

            // optional value
            var argValue = Tokens.Default;
            if (reader.TryGetTokens(":", out var argSeparator))
            {
                //optional whitespace
                _ = TryParseSilentBlock(reader, context, out _);

                // required delimited content
                if (!TryParseDelimitedContent(reader, context, '\'', '\'', out var argValueResult))
                {
                    result = Result.Of<ArgumentPair>(new FaultyMatchError(
                        "atomic-rule-argument",
                        position,
                        reader.Position - position));
                    reader.Reset(position);
                    return false;
                }

                argValueResult.Consume(value => argValue = value);
            }

            result = Result.Of(
                ArgumentPair.Of(
                    Argument.Of(argKey.ToString()!),
                    argValue.ToString()!));
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<ArgumentPair>(new UnknownError(e));
            return false;
        }
    }

    #endregion

    #region Silent Block

    public static bool TryParseSilentBlock(
        TokenReader reader,
        MetaContext context,
        out IResult<SilentBlock> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<SilentBlock>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseBlockComment(
        TokenReader reader,
        MetaContext context,
        out IResult<BlockComment> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<BlockComment>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseLineComment(
        TokenReader reader,
        MetaContext context,
        out IResult<LineComment> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<LineComment>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseWhitespace(
        TokenReader reader,
        MetaContext context,
        out IResult<Whitespace> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<Whitespace>(new UnknownError(e));
            return false;
        }
    }

    #endregion

}
