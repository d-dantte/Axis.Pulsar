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
using System.Globalization;
using System.Text.RegularExpressions;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;


public static class GrammarParser
{
    private static readonly Regex DigitPattern = new Regex("^\\d+\\z", RegexOptions.Compiled);

    private static readonly Regex BoolPattern = new Regex(
        "^true|false",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CardinalityMinOccurencePattern = new Regex(
        "^\\*|\\?|\\+|\\d+\\z",
        RegexOptions.Compiled);

    private static readonly HashSet<char> NumberArgValueEndDelimiters = new HashSet<char>
    {
        ',', '}', '\n', '\r', '\t', ' ', '#', '/', '\''
    };

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
            var accumulator = ParserAccumulator
                .Of(reader, context, productions)

                // optional initial silent block
                .ThenTry<SilentBlock>(
                    TryParseSilentBlock,
                    (prods, _) => prods,
                    prods => prods)

                // initial production
                .ThenTry<Production>(
                    TryParseProduction,
                    (prods, prod) => prods.AddItem(prod));

            do
            {
                accumulator

                    // required silent block
                    .ThenTry<SilentBlock>(
                        TryParseSilentBlock,
                        (prods, _) => prods)

                    // required production
                    .ThenTry<Production>(
                        TryParseProduction,
                        (prods, prod) => prods.AddItem(prod));
            }
            while (!accumulator.IsPreviousOpErrored);

            if (accumulator.IsPreviousOpFaultyMatch
                || accumulator.IsPreviousOpRuntimeError)
                result = accumulator
                    .ToResult()
                    .MapAs<IGrammar>();

            else result = productions
                .ApplyTo(prods => XBNFGrammar.Of(
                    productions[0].Symbol,
                    productions))
                .ApplyTo(Result.Of);

            return result.IsDataResult();
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
            var accummulator = ParserAccumulator
                .Of(reader,
                    context,
                    KeyValuePair.Create<string, IRule>(null!, null!))

                // symbol name
                .ThenTry<string>(
                    TryParseCompositeSymbolName,
                    (kvp, name) => KeyValuePair.Create(name!, kvp.Value))

                // silent block
                .ThenTry<SilentBlock>(
                    TryParseSilentBlock,
                    (kvp, _) => kvp)

                // ->
                .ThenTry<Tokens>(
                    TryParseMapOperator,
                    (kvp, _) => kvp)

                // silent block
                .ThenTry<SilentBlock>(
                    TryParseSilentBlock,
                    (kvp, _) => kvp)

                // rule
                .ThenTry<ICompositeRule>(
                    TryParseCompositeRule,
                    (kvp, rule) => kvp.Key.ValuePair((IRule)rule));

            result = accummulator.ToResult(kvp => Production.Of(
                kvp.Key,
                kvp.Value));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
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
            reader.Reset(position);
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
            reader.Reset(position);
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
            if (!reader.TryGetTokens("->", out Tokens tokens))
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
            reader.Reset(position);
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
            var accumulator = ParserAccumulator
                .Of(reader,
                    context,
                    KeyValuePair.Create(0u, default(IGroupElement)!))

                // optional recognition threshold
                .ThenTry<uint>(
                    TryParseRecognitionThreshold,
                    (kvp, threshold) => threshold.ValuePair(kvp.Value),
                    kvp => 1u.ValuePair(kvp.Value))

                // required group element
                .ThenTry<IGroupElement>(
                    TryParseGroupElement,
                    (kvp, element) => kvp.Key.ValuePair(element));

            result = accumulator
                .ToResult(kvp => NonTerminal.Of(
                    kvp.Key,
                    kvp.Value))
                .MapAs<ICompositeRule>();

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
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
            reader.Reset(position);
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
            var accumulator = ParserAccumulator
                .OfAlternative(
                    reader,
                    context,
                    default(IGroupElement)!,
                    "group-element")

                // atomic rule ref
                .OrTry<AtomicRuleRef>(
                    TryParseAtomicRuleRef,
                    (_, ruleRef) => ruleRef)

                // production ref
                .OrTry<ProductionRef>(
                    TryParseProductionRef,
                    (_, prodRef) => prodRef)

                // group
                .OrTry<IGroup>(
                    TryParseGroup,
                    (_, group) => group);

            result = accumulator.ToResult();
            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
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
            var accumulator = ParserAccumulator
                .Of(reader,
                    context,
                    KeyValuePair.Create(default(IAtomicRule)!, Cardinality.OccursOnlyOnce()))

                // required atomic rule
                .ThenTry<IAtomicRule>(
                    TryParseAtomicRule,
                    (kvp, rule) => rule.ValuePair(kvp.Value))

                // optional cardinality
                .ThenTry<Cardinality>(
                    TryParseCardinality,
                    (kvp, cardinality) => kvp.Key.ValuePair(cardinality),
                    kvp => kvp);

            result = accumulator.ToResult(kvp => AtomicRuleRef.Of(
                kvp.Value,
                kvp.Key));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
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
            var accumulator = ParserAccumulator
                .Of(reader,
                    context,
                    KeyValuePair.Create(default(string)!, Cardinality.OccursOnlyOnce()))

                // required atomic rule
                .ThenTry<string>(
                    TryParseCompositeSymbolName,
                    (kvp, rule) => rule.ValuePair(kvp.Value))

                // optional cardinality
                .ThenTry<Cardinality>(
                    TryParseCardinality,
                    (kvp, cardinality) => kvp.Key.ValuePair(cardinality),
                    kvp => kvp);

            result = accumulator.ToResult(kvp => ProductionRef.Of(
                kvp.Value,
                kvp.Key));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
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
            var accumulator = ParserAccumulator
                .OfAlternative(
                    reader,
                    context,
                    default(IGroup)!,
                    "group")

                // choice
                .OrTry<Choice>(
                    TryParseChoice,
                    (group, choice) => choice)

                // sequence
                .OrTry<Sequence>(
                    TryParseSequence,
                    (group, sequence) => sequence)

                // set
                .OrTry<Set>(
                    TryParseSet,
                    (group, set) => set);

            result = accumulator.ToResult();
            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
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
            reader.Reset(position);
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
            reader.Reset(position);
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
            reader.Reset(position);
            result = Result.Of<Sequence>(new UnknownError(e));
            return false;
        }
    }

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
                result = Result.Of<Cardinality>(new UnmatchedError(
                    "cardinality",
                    position));
                return false;
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
            reader.Reset(position);
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
            reader.Reset(position);
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
                        r.Args.ToImmutableDictionary(arg => arg.Argument, arg => arg.Value!));
                });

            if (result.IsErrorResult())
                reader.Reset(position);

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
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
            var accumulator = ParserAccumulator.OfAlternative(
                reader, context,
                new AtomicContentArgumentInfo(),
                "atomic-content");

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
                            "atomic-content",
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
            reader.Reset(position);
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
                result = Result.Of<Tokens>(new UnmatchedError(
                    "atomic-rule",
                    position));
                reader.Reset(position);
                return false;
            }

            // content chars
            var contentTokens = Tokens.Empty;
            while (reader.TryGetToken(out var stringChar))
            {
                if (stringChar[0] == '\\'
                    && reader.TryPeekToken(out var nextToken)
                    && nextToken[0] == endDelimiter)
                {
                    stringChar += nextToken;
                    reader.Advance();
                }
                else if (stringChar[0] == endDelimiter)
                {
                    reader.Back();
                    break;
                }                
                
                contentTokens += stringChar; // contentTokens = contentTokens.Join(stringChar);
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
            reader.Reset(position);
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
            reader.Reset(position);
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
            string? argValue = null;
            if (reader.TryGetTokens(":", out var argSeparator))
            {
                //optional whitespace
                _ = TryParseSilentBlock(reader, context, out _);

                var accumulator = ParserAccumulator
                    .Of(reader, context, "")

                    // bool value?
                    .ThenTry<bool>(
                        TryParseBooleanArgValue,
                        (value, @bool) => @bool.ToString())

                    // number value?
                    .OrTry<decimal>(
                        TryParseNumberArgValue,
                        (value, @decimal) => @decimal.ToString(NumberFormatInfo.InvariantInfo))

                    // delimited content value?
                    .OrTry(
                        (TokenReader x, MetaContext y, out IResult<Tokens> z) => TryParseDelimitedContent(x, y, '\'', '\'', out z),
                        (value, tokens) => tokens.ToString()!);

                if (accumulator.IsPreviousOpErrored)
                {
                    result = Result.Of<ArgumentPair>(new FaultyMatchError(
                        "atomic-rule-argument",
                        position,
                        reader.Position - position));
                    reader.Reset(position);
                    return false;
                }
                else accumulator.Consume(v => argValue = v);
            }

            result = Result.Of(
                ArgumentPair.Of(
                    Argument.Of(argKey.ToString()!),
                    argValue));
            return true;
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<ArgumentPair>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseBooleanArgValue(
        TokenReader reader,
        MetaContext context,
        out IResult<bool> result)
    {
        var position = reader.Position;

        try
        {
            // false?
            if (reader.TryPeekTokens(5, true, out var falseTokens)
                && BoolPattern.IsMatch(falseTokens.AsSpan()))
            {
                reader.Advance(5);
                result = Result.Of(false);
            }
            else if (reader.TryPeekTokens(4, true, out var trueTokens)
                && BoolPattern.IsMatch(trueTokens.AsSpan()))
            {
                reader.Advance(4);
                result = Result.Of(true);
            }
            else result = Result.Of<bool>(new UnmatchedError("bool-arg-value", position));

            return result.IsDataResult();
        }
        catch(Exception e)
        {
            result = Result.Of<bool>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseNumberArgValue(
        TokenReader reader,
        MetaContext context,
        out IResult<decimal> result)
    {
        var position = reader.Position;
        try
        {
            var tokens = Tokens.Empty;
            while (reader.TryGetToken(out var token))
            {
                if (NumberArgValueEndDelimiters.Contains(token[0]))
                {
                    reader.Back();
                    break;
                }

                tokens += token;
            }

            if (tokens.IsEmpty)
            {
                reader.Reset(position);
                result = Result.Of<decimal>(new UnmatchedError("number-arg-value", position));
                return false;
            }

            if (decimal.TryParse(
                tokens.AsSpan(),
                NumberStyles.Any,
                NumberFormatInfo.InvariantInfo,
                out var @decimal))
            {
                Console.Write($"tokens: {tokens.AsSpan()}, parsed: {@decimal}\r\n");
                result = Result.Of(@decimal);
                return true;
            }

            result = Result.Of<decimal>(new FaultyMatchError(
                "number-arg-value",
                position,
                reader.Position - position));
            reader.Reset(position);
            return false;
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<decimal>(new UnknownError(e));
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
            var list = new List<ISilentElement>();
            ParserAccumulator<ISilentElement> accumulator = null!;

            do
            {
                accumulator = ParserAccumulator
                    .OfAlternative<ISilentElement>(reader,
                        context,
                        null!,
                        "silent-block")

                    // block comment
                    .OrTry<BlockComment>(
                        TryParseBlockComment,
                        (_, comment) => comment)

                    // line comment
                    .OrTry<LineComment>(
                        TryParseLineComment,
                        (_, comment) => comment)

                    // tab/space/line-feed/carriage-return
                    .OrTry<Whitespace>(
                        TryParseWhitespace,
                        (_, whitespace) => whitespace)

                    // if we parsed any of the silent elements
                    .Consume(list.Add);
            }
            while (!accumulator.IsPreviousOpErrored);


            if (accumulator.IsPreviousOpFaultyMatch
                || accumulator.IsPreviousOpUnknown)
            {
                if (accumulator.IsPreviousOpUnmatched && list.Count > 0)
                    result = Result.Of<SilentBlock>(new FaultyMatchError(
                        "silent-block",
                        position,
                        reader.Position - position));

                else result = accumulator
                    .ToResult()
                    .MapAs<SilentBlock>();

                reader.Reset(position);
                return false;
            }

            result = SilentBlock
                .Of(list)
                .ApplyTo(Result.Of);
            return true;
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
            if (!reader.TryGetTokens("/*", out var delimiter))
            {
                result = Result.Of<BlockComment>(new UnmatchedError(
                    "block-comment",
                    position));
                reader.Reset(position);
                return false;
            }

            // read content
            var contentToken = Tokens.Empty;
            while(reader.TryGetToken(out var token))
            {
                if ('*' == token[0]
                    && reader.TryPeekTokens("/", out var bsolToken))
                {
                    reader.Advance();
                    result = BlockComment
                        .Of(contentToken)
                        .ApplyTo(Result.Of);
                    return true;
                }

                contentToken += token;
            }

            result = Result.Of<BlockComment>(new FaultyMatchError(
                "block-comment",
                position,
                reader.Position - position));
            reader.Reset(position);
            return false;
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
            if (!reader.TryGetTokens("#", out var delimiter))
            {
                result = Result.Of<LineComment>(new UnmatchedError(
                    "line-comment",
                    position));
                reader.Reset(position);
                return false;
            }

            // read content
            var contentToken = Tokens.Empty;
            while (reader.TryGetToken(out var token))
            {
                if ('\n' == token[0]
                    || '\r' == token[0])
                {
                    reader.Back();
                    break;
                }

                contentToken += token;
            }

            result = LineComment
                .Of(contentToken)
                .ApplyTo(Result.Of);
            return true;
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
            if (!reader.TryGetToken(out var whitespaceToken)
                || (' ' != whitespaceToken[0]
                && '\t' != whitespaceToken[0]
                && '\n' != whitespaceToken[0]
                && '\r' != whitespaceToken[0]))
            {
                result = Result.Of<Whitespace>(new UnmatchedError(
                    "whitespace",
                    position));
                reader.Reset(position);
                return false;
            }

            result = Whitespace
                .Of(whitespaceToken)
                .ApplyTo(Result.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<Whitespace>(new UnknownError(e));
            return false;
        }
    }

    #endregion

}
