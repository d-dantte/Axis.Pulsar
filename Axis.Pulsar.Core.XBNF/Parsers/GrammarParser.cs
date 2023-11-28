using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Parsers.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Parsers;

internal static class GrammarParser
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

    internal static bool TryParseGrammar(
        TokenReader reader,
        MetaContext context,
        out IResult<IGrammar> result)
    {
        var position = reader.Position;

        try
        {
            var isEOF = false;
            var accumulator = ParserAccumulator
                .Of(reader, ProductionPath.Of("grammar"), context, new List<XBNFProduction>())

                // optional initial silent block
                .ThenTry<SilentBlock>(
                    TryParseSilentBlock,
                    (prods, _) => prods,
                    prods => prods)

                // initial production
                .ThenTry<XBNFProduction>(
                    TryParseProduction,
                    (prods, prod) => prods.AddItem(prod));

            do
            {
                accumulator = accumulator

                    // required silent block
                    .ThenTry<SilentBlock>(
                        TryParseSilentBlock,
                        (prods, _) => prods)

                    // or required end of file
                    .OrTry<Results.EOF>(
                        TryParseEOF,
                        (prods, prod) =>
                        {
                            isEOF = true;
                            return prods;
                        });

                if (!isEOF && !accumulator.IsErrored)
                { 
                    accumulator = accumulator

                        // required production
                        .ThenTry<XBNFProduction>(
                            TryParseProduction,
                            (prods, prod) => prods.AddItem(prod))

                        // or required end of file
                        .OrTry<Results.EOF>(
                            TryParseEOF,
                            (prods, prod) =>
                            {
                                isEOF = true;
                                return prods;
                            });
                }
            }
            while (!accumulator.IsErrored && !isEOF);

            result = accumulator.ToResult(prods => XBNFGrammar.Of(
                prods[0].Symbol,
                prods));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<IGrammar>(e);
            return false;
        }
    }

    internal static bool TryParseProduction(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<XBNFProduction> result)
    {
        var position = reader.Position;
        var productionPath = path.Next("production");

        try
        {
            var accummulator = ParserAccumulator
                .Of(reader,
                    productionPath,
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

                // composite rule
                .ThenTry<ICompositeRule>(
                    TryParseCompositeRule,
                    (kvp, rule) => kvp.Key.ValuePair((IRule)rule));

            result = accummulator.ToResult(kvp => XBNFProduction.Of(
                kvp.Key,
                kvp.Value));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<XBNFProduction>(e);
            return false;
        }
    }

    internal static bool TryParseCompositeSymbolName(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<string> result)
    {
        var position = reader.Position;
        var compositeSymbolPath = path.Next("composite-symbol-name");

        try
        {
            if (!reader.TryGetToken(out var token)
                || !'$'.Equals(token[0]))
            {
                reader.Reset(position);
                result = Result.Of<string>(FailedRecognitionError.Of(
                    compositeSymbolPath,
                    position));
                return false;
            }

            if (!reader.TryGetPattern(IProduction.SymbolPattern, out var tokens))
            {
                reader.Reset(position);
                result = Result.Of<string>(PartialRecognitionError.Of(
                    compositeSymbolPath,
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
            result = Result.Of<string>(e);
            return false;
        }
    }

    internal static bool TryParseAtomicSymbolName(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<string> result)
    {
        var position = reader.Position;
        var atomicSymbolPath = path.Next("atomic-symbol-name");

        try
        {
            if (!reader.TryGetToken(out var token)
                || !'@'.Equals(token[0]))
            {
                reader.Reset(position);
                result = Result.Of<string>(FailedRecognitionError.Of(
                    atomicSymbolPath,
                    position));
                return false;
            }

            if (!reader.TryGetPattern(IProduction.SymbolPattern, out var tokens))
            {
                reader.Reset(position);
                result = Result.Of<string>(PartialRecognitionError.Of(
                    atomicSymbolPath,
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
            result = Result.Of<string>(e);
            return false;
        }
    }

    internal static bool TryParseMapOperator(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<Tokens> result)
    {
        var position = reader.Position;
        var mapOpPath = path.Next("map-operator");

        try
        {
            if (!reader.TryGetTokens("->", out Tokens tokens))
            {
                result = Result.Of<Tokens>(new FailedRecognitionError(
                    mapOpPath,
                    position));
                return false;
            }

            result = Result.Of(tokens);
            return true;
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<Tokens>(e);
            return false;
        }
    }

    internal static bool TryParseEOF(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<Results.EOF> result)
    {
        var position = reader.Position;
        var eofPath = path.Next("EOF");

        try
        {
            if (reader.TryPeekToken(out _))
            {
                result = FailedRecognitionError
                    .Of(eofPath, position)
                    .ApplyTo(Result.Of<Results.EOF>);
                return false;
            }

            result = Result.Of(Results.EOF.Instance);
            return true;
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<Results.EOF>(e);
            return false;
        }
    }
    
    #endregion

    #region Composite

    internal static bool TryParseCompositeRule(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<ICompositeRule> result)
    {
        var position = reader.Position;
        var compositeRulePath = path.Next("composite-rule");

        try
        {
            var accumulator = ParserAccumulator
                .Of(reader,
                    compositeRulePath,
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
            result = Result.Of<ICompositeRule>(e);
            return false;
        }
    }

    internal static bool TryParseRecognitionThreshold(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<uint> result)
    {
        var position = reader.Position;
        var thresholdPath = path.Next("recognition-threshold");

        try
        {
            if (!reader.TryGetTokens(":", out var colonToken))
            {
                reader.Reset(position);
                result = Result.Of<uint>(FailedRecognitionError.Of(
                    thresholdPath,
                    position));
                return false;
            }

            if (!reader.TryGetPattern(DigitPattern, out var digitTokens))
            {
                result = Result.Of<uint>(PartialRecognitionError.Of(
                    thresholdPath,
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            if (!TryParseSilentBlock(reader, thresholdPath, context, out _))
            {
                result = Result.Of<uint>(PartialRecognitionError.Of(
                    thresholdPath,
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
            result = Result.Of<uint>(e);
            return false;
        }
    }

    internal static bool TryParseGroupElement(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<IGroupElement> result)
    {
        var position = reader.Position;
        var elementPath = path.Next("group-element");

        try
        {
            var accumulator = ParserAccumulator
                .Of(reader,
                    elementPath,
                    context,
                    default(IGroupElement)!)

                // atomic rule ref
                .ThenTry<AtomicRuleRef>(
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
            result = Result.Of<IGroupElement>(e);
            return false;
        }
    }

    internal static bool TryParseAtomicRuleRef(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<AtomicRuleRef> result)
    {
        var position = reader.Position;
        var atomicRulePath = path.Next("atomic-rule");

        try
        {
            var accumulator = ParserAccumulator
                .Of(reader,
                    atomicRulePath,
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
            result = Result.Of<AtomicRuleRef>(e);
            return false;
        }
    }

    internal static bool TryParseProductionRef(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<ProductionRef> result)
    {
        var position = reader.Position;
        var productionRefPath = path.Next("production-ref");

        try
        {
            var accumulator = ParserAccumulator
                .Of(reader,
                    productionRefPath,
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
            result = Result.Of<ProductionRef>(e);
            return false;
        }
    }

    internal static bool TryParseGroup(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<IGroup> result)
    {
        var position = reader.Position;
        var groupPath = path.Next("group");

        try
        {
            var accumulator = ParserAccumulator
                .Of(reader, groupPath, context, default(IGroup)!)

                // choice
                .ThenTry<Choice>(
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
            result = Result.Of<IGroup>(e);
            return false;
        }
    }

    internal static bool TryParseSet(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<Set> result)
    {
        var position = reader.Position;
        var setPath = path.Next("set");

        try
        {
            if (!reader.TryGetTokens("#", out var delimiterToken))
            {
                reader.Reset(position);
                result = Result.Of<Set>(FailedRecognitionError.Of(
                    setPath,
                    position));
                return false;
            }

            // optional min match count
            if (!reader.TryGetPattern(DigitPattern, out var minMatchCount))
                minMatchCount = Tokens.Empty;

            result = ParserAccumulator
                .Of(reader,
                    setPath,
                    context,
                    (list: default(IGroupElement[]), cardinality: default(Cardinality)))

                // required element list
                .ThenTry<IGroupElement[]>(
                    TryParseElementList,
                    (info, list) => (list, info.cardinality))

                // optional cardinality
                .ThenTry<Cardinality>(
                    TryParseCardinality,
                    (info, cardinality) => (info.list, cardinality),
                    info => (info.list, cardinality: Cardinality.OccursOnlyOnce()))

                // transform failed recognition errors
                .TransformError((data, error, matchCount) => error switch
                {
                    FailedRecognitionError => PartialRecognitionError.Of(
                        setPath,
                        position,
                        reader.Position - position),
                    _ => error
                })

                // map to result
                .ToResult(info => Set.Of(
                    cardinality: info.cardinality,
                    elements: info.list!,
                    minRecognitionCount: minMatchCount.IsEmpty switch
                    {
                        true => info.list!.Length,
                        false => int.Parse(minMatchCount.AsSpan())
                    }));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<Set>(e);
            return false;
        }
    }

    internal static bool TryParseChoice(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<Choice> result)
    {
        var position = reader.Position;
        var choicePath = path.Next("choice");

        try
        {
            if (!reader.TryGetTokens("?", out var delimiterToken))
            {
                reader.Reset(position);
                result = Result.Of<Choice>(FailedRecognitionError.Of(
                    choicePath,
                    position));
                return false;
            }

            result = ParserAccumulator
                .Of(reader,
                    choicePath,
                    context,
                    (list: default(IGroupElement[]), cardinality: default(Cardinality)))

                // required element list
                .ThenTry<IGroupElement[]>(
                    TryParseElementList,
                    (info, list) => (list, info.cardinality))

                // optional cardinality
                .ThenTry<Cardinality>(
                    TryParseCardinality,
                    (info, cardinality) => (info.list, cardinality),
                    info => (info.list, cardinality: Cardinality.OccursOnlyOnce()))

                // transform unmatched errors
                .TransformError((data, error, matchCount) => error switch
                {
                    FailedRecognitionError => PartialRecognitionError.Of(
                        choicePath,
                        position,
                        reader.Position - position),
                    _ => error
                })

                // map to result
                .ToResult(info => Choice.Of(info.cardinality, info.list!));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<Choice>(e);
            return false;
        }
    }

    internal static bool TryParseSequence(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<Sequence> result)
    {
        var position = reader.Position;
        var sequencePath = path.Next("sequence-group");

        try
        {
            if (!reader.TryGetTokens("+", out var delimiterToken))
            {
                reader.Reset(position);
                result = Result.Of<Sequence>(FailedRecognitionError.Of(
                    sequencePath,
                    position));
                return false;
            }

            result = ParserAccumulator
                .Of(reader,
                    sequencePath,
                    context,
                    (list: default(IGroupElement[]), cardinality: default(Cardinality)))

                // required element list
                .ThenTry<IGroupElement[]>(
                    TryParseElementList,
                    (info, list) => (list, info.cardinality))

                // optional cardinality
                .ThenTry<Cardinality>(
                    TryParseCardinality,
                    (info, cardinality) => (info.list, cardinality),
                    info => (info.list, cardinality: Cardinality.OccursOnlyOnce()))

                // transform unmatched errors
                .TransformError((data, error, matchCount) => error switch
                {
                    FailedRecognitionError => PartialRecognitionError.Of(
                        sequencePath,
                        position,
                        reader.Position - position),
                    _ => error
                })

                // map to result
                .ToResult(info => Sequence.Of(info.cardinality, info.list!));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<Sequence>(e);
            return false;
        }
    }

    internal static bool TryParseCardinality(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<Cardinality> result)
    {
        var position = reader.Position;
        var cardinalityPath = path.Next("cardinality");

        try
        {
            if (!reader.TryGetToken(out var delimiterToken)
                || !'.'.Equals(delimiterToken[0]))
            {
                reader.Reset(position);
                result = Result.Of<Cardinality>(FailedRecognitionError.Of(
                    cardinalityPath,
                    position));
                return false;
            }

            // min occurs value
            if (!reader.TryGetPattern(CardinalityMinOccurencePattern, out var minOccursTokens))
            {
                result = Result.Of<Cardinality>(PartialRecognitionError.Of(
                    cardinalityPath,
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
                    '*' or '?' or '+' => Result.Of<Cardinality>(PartialRecognitionError.Of(
                        cardinalityPath,
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
                    '*' or '?' or '+' => Result.Of<Cardinality>(PartialRecognitionError.Of(
                        cardinalityPath,
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
            result = Result.Of<Cardinality>(e);
            return false;
        }
    }

    internal static bool TryParseElementList(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<IGroupElement[]> result)
    {
        var position = reader.Position;
        var elementListPath = path.Next("element-list");

        try
        {
            // open bracket
            if (!reader.TryGetTokens("[", out var openBracket))
            {
                result = Result.Of<IGroupElement[]>(FailedRecognitionError.Of(
                    elementListPath,
                    position));
                reader.Reset(position);
                return false;
            }

            var accumulator = ParserAccumulator
                .Of(reader, elementListPath, context, new List<IGroupElement>())

                // optional whitespace
                .ThenTry<SilentBlock>(
                    TryParseSilentBlock,
                    (group, block) => group,
                    group => group)

                // required element
                .ThenTry<IGroupElement>(
                    TryParseGroupElement,
                    (group, element) => group.AddItem(element));

            // optional additional elements
            while (!accumulator.IsErrored)
            {
                _ = accumulator

                    // required whitespace
                    .ThenTry<SilentBlock>(
                        TryParseSilentBlock,
                        (group, block) => group)

                    // required element
                    .ThenTry<IGroupElement>(
                        TryParseGroupElement,
                        (group, element) => group.AddItem(element));
            }

            accumulator = accumulator

                // transform failed recognition if we have no previously recognized elements. This indicates an empty
                // list, and groups do not support empty lists
                .TransformError<FailedRecognitionError>((data, error, matchCount) => data.Count switch
                {
                    0 => PartialRecognitionError.Of(
                        elementListPath,
                        position,
                        reader.Position - position),

                    _ => error
                })
                
                // Map failed errors that do not signify empty lists
                .MapError<FailedRecognitionError>((data, error, matchCount) => data);

            if (accumulator.IsErrored
                || !reader.TryGetTokens("]", out var _))
            {
                result = Result.Of<IGroupElement[]>(PartialRecognitionError.Of(
                    elementListPath,
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            result = accumulator.ToResult(data => data.ToArray());
            return true;
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<IGroupElement[]>(e);
            return false;
        }
    }

    #endregion

    #region Atomic
    internal static bool TryParseAtomicRule(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<IAtomicRule> result)
    {
        var position = reader.Position;
        var atomicRulePath = path.Next("atomic-rule");

        try
        {
            result = ParserAccumulator
                .Of(reader,
                    atomicRulePath,
                    context,
                    (Name: string.Empty, Args: new List<ArgumentPair>()))

                // parse atomic symbol name
                .ThenTry<string>(
                    TryParseAtomicSymbolName, 
                    (r, name) => (Name: name, r.Args))

                // or parse atomic content, and derive rule name/Id
                .OrTry<AtomicContentArgumentInfo>(TryParseAtomicContent, (r, contentInfo) =>
                {
                    if (!context.AtomicContentTypeMap.TryGetValue(contentInfo.ContentType, out var symbol))
                        throw PartialRecognitionError.Of(
                            atomicRulePath,
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
                    defaultMapper: r => r,
                    mapper: (r, args) =>
                    {
                        r.Args.AddRange(args);
                        return r;
                    })

                // map to atomic rule
                .ToResult(r =>
                {
                    if (!context.AtomicFactoryMap.TryGetValue(r.Name, out var factoryDef))
                        throw PartialRecognitionError.Of(
                            atomicRulePath,
                            position,
                            reader.Position - position);

                    return factoryDef.Factory.NewRule(
                        r.Name,
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
            result = Result.Of<IAtomicRule>(e);
            return false;
        }
    }

    internal static bool TryParseAtomicContent(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<AtomicContentArgumentInfo> result)
    {
        var position = reader.Position;
        var atomicContentPath = path.Next("atomic-content");

        try
        {
            var accumulator = ParserAccumulator.Of(
                reader, 
                atomicContentPath,
                context,
                default(AtomicContentArgumentInfo)!);

            if (reader.TryPeekToken(out var delimToken))
            {
                var initialAlternative = true;
                foreach (var delimChar in AtomicContentDelimiterTypeExtensions.DelimiterCharacterSet)
                {
                    if (delimChar == delimToken[0])
                    {
                        accumulator = initialAlternative switch
                        {
                            true => accumulator.ThenTry(
                                (TokenReader tr, ProductionPath p, MetaContext mc, out IResult<Tokens> r) => TryParseDelimitedContent(
                                    tr,
                                    atomicContentPath,
                                    mc, delimChar, delimChar, out r),
                                (info, content) => new AtomicContentArgumentInfo
                                {
                                    Content = content,
                                    ContentType = delimChar.DelimiterType()
                                }),

                            false => accumulator.OrTry(
                                (TokenReader tr, ProductionPath p, MetaContext mc, out IResult<Tokens> r) => TryParseDelimitedContent(
                                    tr,
                                    atomicContentPath,
                                    mc, delimChar, delimChar, out r),
                                (info, content) => new AtomicContentArgumentInfo
                                {
                                    Content = content,
                                    ContentType = delimChar.DelimiterType()
                                })
                        };

                        // break if we already have a match
                        if (!accumulator.IsErrored)
                            break;

                        initialAlternative = false;
                    }
                }
            }

            result = accumulator.ToResult();

            if (result.IsDataResult(out var data) && data is null)
                result = Result.Of<AtomicContentArgumentInfo>(FailedRecognitionError.Of(
                    atomicContentPath,
                    position));

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<AtomicContentArgumentInfo>(e);
            return false;
        }
    }

    internal static bool TryParseDelimitedContent(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        char startDelimiter,
        char endDelimiter,
        out IResult<Tokens> result)
    {
        var position = reader.Position;
        var delimContentPath = path.Next("delimited-content");

        try
        {
            // start delim
            if (!reader.TryGetTokens(startDelimiter.ToString(), out var startDelimToken))
            {
                result = Result.Of<Tokens>(FailedRecognitionError.Of(
                    delimContentPath,
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
                result = Result.Of<Tokens>(PartialRecognitionError.Of(
                    delimContentPath,
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
            result = Result.Of<Tokens>(e);
            return false;
        }
    }

    internal static bool TryParseAtomicRuleArguments(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<ArgumentPair[]> result)
    {
        var position = reader.Position;
        var atomicRuleArgumentsPath = path.Next("atomic-rule-arguments");

        try
        {
            if (!reader.TryGetTokens("{", out var startDelimToken))
            {
                result = Result.Of<ArgumentPair[]>(FailedRecognitionError.Of(
                    atomicRuleArgumentsPath,
                    position));
                reader.Reset(position);
                return false;
            }

            var accumulator = ParserAccumulator
                .Of(reader,
                    atomicRuleArgumentsPath,
                    context,
                    new List<ArgumentPair>());

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
            while (!accumulator.IsErrored && reader.TryGetTokens(",", out _));

            if (accumulator.IsErrored)
            {
                // allow non-failed recognition errors to flow, else pass a faultymatch error.
                result = accumulator
                    .TransformError<FailedRecognitionError>(
                        (data, err, matchCount) => PartialRecognitionError.Of(
                            atomicRuleArgumentsPath,
                            position,
                            reader.Position - position))
                    .ToResult(data => data.ToArray());
                reader.Reset(position);
                return false;
            }

            if (!reader.TryGetTokens("}", out _))
            {
                result = Result.Of<ArgumentPair[]>(
                    PartialRecognitionError.Of(
                        atomicRuleArgumentsPath,
                        position,
                        reader.Position - position));
                reader.Reset(position);
                return false;
            }

            result = accumulator.ToResult(data => data.ToArray());
            return true;
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<ArgumentPair[]>(e);
            return false;
        }
    }

    internal static bool TryParseArgument(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<ArgumentPair> result)
    {
        var position = reader.Position;
        var argumentPath = path.Next("atomic-rule-argument");

        try
        {
            if (!reader.TryGetPattern(Argument.ArgumentPattern, out var argKey))
            {
                result = Result.Of<ArgumentPair>(FailedRecognitionError.Of(
                    argumentPath,
                    position));
                reader.Reset(position);
                return false;
            }

            // optional whitespace
            _ = TryParseSilentBlock(reader, argumentPath, context, out _);

            // optional value
            string? argValue = null;
            if (reader.TryGetTokens(":", out var argSeparator))
            {
                //optional whitespace
                _ = TryParseSilentBlock(reader, argumentPath, context, out _);

                var accumulator = ParserAccumulator
                    .Of(reader, argumentPath, context, "")

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
                        (TokenReader tr, ProductionPath p, MetaContext mc, out IResult<Tokens> r) => TryParseDelimitedContent(
                            tr,
                            argumentPath,
                            mc, '\'', '\'', out r),
                        (value, tokens) => tokens.ToString()!);

                if (accumulator.IsErrored)
                {
                    result = Result.Of<ArgumentPair>(PartialRecognitionError.Of(
                        argumentPath,
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
            result = Result.Of<ArgumentPair>(e);
            return false;
        }
    }

    internal static bool TryParseBooleanArgValue(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<bool> result)
    {
        var position = reader.Position;
        var boolArgPath = path.Next("bool-arg-value");

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
            else result = Result.Of<bool>(FailedRecognitionError.Of(boolArgPath, position));

            return result.IsDataResult();
        }
        catch(Exception e)
        {
            result = Result.Of<bool>(e);
            return false;
        }
    }

    internal static bool TryParseNumberArgValue(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<decimal> result)
    {
        var position = reader.Position;
        var numberArgPath = path.Next("number-arg-value");

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
                result = Result.Of<decimal>(FailedRecognitionError.Of(numberArgPath, position));
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

            result = Result.Of<decimal>(PartialRecognitionError.Of(
                "number-arg-value",
                position,
                reader.Position - position));
            reader.Reset(position);
            return false;
        }
        catch (Exception e)
        {
            reader.Reset(position);
            result = Result.Of<decimal>(e);
            return false;
        }
    }

    #endregion

    #region Silent Block

    internal static bool TryParseSilentBlock(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<SilentBlock> result)
    {
        var position = reader.Position;
        var blockPath = path.Next("silent-block");

        try
        {
            var accumulator = ParserAccumulator.Of(
                reader,
                blockPath,
                context,
                new List<ISilentElement>());

            do
            {
                _ = accumulator

                    // block comment
                    .ThenTry<BlockComment>(
                        TryParseBlockComment,
                        (list, comment) => list.AddItem((ISilentElement)comment))

                    // line comment
                    .OrTry<LineComment>(
                        TryParseLineComment,
                        (list, comment) => list.AddItem((ISilentElement)comment))

                    // tab/space/line-feed/carriage-return
                    .OrTry<Whitespace>(
                        TryParseWhitespace,
                        (list, whitespace) => list.AddItem((ISilentElement)whitespace));
            }
            while (!accumulator.IsErrored);

            result = accumulator

                // errors?
                .MapError((list, err, recognitionCount) => (err, recognitionCount) switch
                {
                    (FailedRecognitionError, >= 1) => list,
                    _ => err.Throw<List<ISilentElement>>()
                })

                // map to result
                .ToResult(SilentBlock.Of);

            return result.IsDataResult();
        }
        catch (Exception e)
        {
            result = Result.Of<SilentBlock>(e);
            return false;
        }
    }

    internal static bool TryParseBlockComment(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<BlockComment> result)
    {
        var position = reader.Position;
        var blockCommentPath = path.Next("block-comment");

        try
        {
            if (!reader.TryGetTokens("/*", out var delimiter))
            {
                result = Result.Of<BlockComment>(FailedRecognitionError.Of(
                    blockCommentPath,
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

            result = Result.Of<BlockComment>(PartialRecognitionError.Of(
                blockCommentPath,
                position,
                reader.Position - position));
            reader.Reset(position);
            return false;
        }
        catch (Exception e)
        {
            result = Result.Of<BlockComment>(e);
            return false;
        }
    }

    internal static bool TryParseLineComment(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<LineComment> result)
    {
        var position = reader.Position;
        var lineCommentPath = path.Next("line-comment");

        try
        {
            if (!reader.TryGetTokens("#", out var delimiter))
            {
                result = Result.Of<LineComment>(FailedRecognitionError.Of(
                    lineCommentPath,
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
            result = Result.Of<LineComment>(e);
            return false;
        }
    }

    internal static bool TryParseWhitespace(
        TokenReader reader,
        ProductionPath path,
        MetaContext context,
        out IResult<Whitespace> result)
    {
        var position = reader.Position;
        var whitespacePath = path.Next("whitespace");

        try
        {
            if (!reader.TryGetToken(out var whitespaceToken)
                || (' ' != whitespaceToken[0]
                && '\t' != whitespaceToken[0]
                && '\n' != whitespaceToken[0]
                && '\r' != whitespaceToken[0]))
            {
                result = Result.Of<Whitespace>(FailedRecognitionError.Of(
                    whitespacePath,
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
            result = Result.Of<Whitespace>(e);
            return false;
        }
    }

    #endregion

}
