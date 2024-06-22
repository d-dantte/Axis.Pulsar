using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Parsers.Models;
using Axis.Pulsar.Core.XBNF.Parsers.Results;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Axis.Pulsar.Core.Grammar.Errors;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Grammar.Rules.Composite;
using Axis.Pulsar.Core.Grammar.Rules;

namespace Axis.Pulsar.Core.XBNF.Parsers;

/// <summary>
/// TODO: Add DETAILED comments on all parse methods.
/// </summary>
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
        ParserContext context,
        out XBNFResult<IGrammar> result)
    {
        var position = reader.Position;

        var isEOF = false;
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, SymbolPath.Of("grammar"), context);
        var accumulator = NodeRecognitionAccumulator
            .Of<List<Production>, SymbolPath, ParserContext>(new List<Production>())

            // optional initial silent block
            .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                TryParseSilentBlock,
                accumulatorArgs,
                (prods, _) => prods,
                (prods, error) => prods)

            // initial production
            .ThenTry<Production, XBNFResult<Production>>(
                TryParseProduction,
                accumulatorArgs,
                (prods, prod) => prods.AddItem(prod));

        do
        {
            accumulator = accumulator

                // required silent block
                .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                    TryParseSilentBlock,
                    accumulatorArgs,
                    (prods, _) => prods)

                // or required end of file
                .OrTry<Results.EOF, XBNFResult<Results.EOF>>(
                    TryParseEOF,
                    accumulatorArgs,
                    (prods, prod) =>
                    {
                        isEOF = true;
                        return prods;
                    });

            if (!isEOF && accumulator.CanTryRequired)
            {
                accumulator = accumulator

                    // required production
                    .ThenTry<Production, XBNFResult<Production>>(
                        TryParseProduction,
                        accumulatorArgs,
                        (prods, prod) => prods.AddItem(prod))

                    // or required end of file
                    .OrTry<Results.EOF, XBNFResult<Results.EOF>>(
                        TryParseEOF,
                        accumulatorArgs,
                        (prods, prod) =>
                        {
                            isEOF = true;
                            return prods;
                        });
            }
        }
        while (accumulator.CanTryRequired && !isEOF);

        result = accumulator.MapAll(
            prods => XBNFGrammar
                .Of(prods[0].Symbol, prods)
                .ApplyTo(XBNFResult<IGrammar>.Of),
            (fre, prods) => XBNFGrammar
                .Of(prods[0].Symbol, prods)
                .ApplyTo(XBNFResult<IGrammar>.Of),
            (pre, prods) => XBNFResult<IGrammar>.Of(pre));

        if (!result.Is(out IGrammar _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseProduction(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Production> result)
    {
        var position = reader.Position;
        var productionPath = path.Next("production");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, productionPath, context);

        var accummulator = NodeRecognitionAccumulator
            .Of<KeyValuePair<string, IRule>, SymbolPath, ParserContext>(
                KeyValuePair.Create<string, IRule>(null!, null!))

            // symbol name
            .ThenTry<string, XBNFResult<string>>(
                TryParseCompositeSymbolName,
                accumulatorArgs,
                (kvp, name) => KeyValuePair.Create(name!, kvp.Value))

            // silent block
            .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                TryParseSilentBlock,
                accumulatorArgs,
                (kvp, _) => kvp)

            // ->
            .ThenTry<Tokens, XBNFResult<Tokens>>(
                TryParseMapOperator,
                accumulatorArgs,
                (kvp, _) => kvp)

            // silent block
            .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                TryParseSilentBlock,
                accumulatorArgs,
                (kvp, _) => kvp)

            // composite rule
            .ThenTry<CompositeRule, XBNFResult<CompositeRule>>(
                TryParseCompositeRule,
                accumulatorArgs,
                (kvp, rule) => kvp.Key.ValuePair((IRule)rule));

        result = accummulator.MapAll(
            prod => Production
                .Of(prod.Key, prod.Value)
                .ApplyTo(XBNFResult<Production>.Of),
            (fre, _) => XBNFResult<Production>.Of(fre),
            (pre, _) => XBNFResult<Production>.Of(pre));

        if (!result.Is(out Production _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseCompositeSymbolName(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<string> result)
    {
        var position = reader.Position;
        var compositeSymbolPath = path.Next("composite-symbol-name");

        if (!reader.TryGetToken(out var token)
            || !'$'.Equals(token[0]))
        {
            reader.Reset(position);
            result = XBNFResult<string>.Of(FailedRecognitionError.Of(
                compositeSymbolPath,
                position));
            return false;
        }

        if (!reader.TryGetPattern(Production.SymbolPattern, out var tokens))
        {
            reader.Reset(position);
            result = XBNFResult<string>.Of(PartialRecognitionError.Of(
                compositeSymbolPath,
                reader.Position,
                reader.Position - position));
            return false;
        }

        result = XBNFResult<string>.Of(tokens.ToString()!);
        return true;
    }

    internal static bool TryParseAtomicSymbolName(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<string> result)
    {
        var position = reader.Position;
        var atomicSymbolPath = path.Next("atomic-symbol-name");

        if (!reader.TryGetToken(out var token)
            || !'@'.Equals(token[0]))
        {
            reader.Reset(position);
            result = XBNFResult<string>.Of(FailedRecognitionError.Of(
                atomicSymbolPath,
                position));
            return false;
        }

        if (!reader.TryGetPattern(Production.SymbolPattern, out var tokens))
        {
            reader.Reset(position);
            result = XBNFResult<string>.Of(PartialRecognitionError.Of(
                atomicSymbolPath,
                reader.Position,
                reader.Position - position));
            return false;
        }

        result = XBNFResult<string>.Of(tokens.ToString()!);
        return true;
    }

    internal static bool TryParseMapOperator(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Tokens> result)
    {
        var position = reader.Position;
        var mapOpPath = path.Next("map-operator");

        if (!reader.TryGetTokens("->", out Tokens tokens))
        {
            result = XBNFResult<Tokens>.Of(new FailedRecognitionError(
                mapOpPath,
                position));
            return false;
        }

        result = XBNFResult<Tokens>.Of(tokens);
        return true;
    }

    internal static bool TryParseEOF(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Results.EOF> result)
    {
        var position = reader.Position;
        var eofPath = path.Next("EOF");

        if (reader.TryPeekToken(out _))
        {
            result = FailedRecognitionError
                .Of(eofPath, position)
                .ApplyTo(XBNFResult<Results.EOF>.Of);
            return false;
        }

        result = XBNFResult<Results.EOF>.Of(Results.EOF.Instance);
        return true;
    }
    
    #endregion

    #region Composite

    internal static bool TryParseCompositeRule(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<CompositeRule> result)
    {
        var position = reader.Position;
        var compositeRulePath = path.Next("composite-rule");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, compositeRulePath, context);

        result = NodeRecognitionAccumulator
            .Of<KeyValuePair<uint?, IAggregationElement>, SymbolPath, ParserContext>(
                KeyValuePair.Create(default(uint?), default(IAggregationElement)!))

            // optional recognition threshold
            .ThenTry<uint, XBNFResult<uint>>(
                TryParseRecognitionThreshold,
                accumulatorArgs,
                (kvp, threshold) => KeyValuePair.Create((uint?)threshold, kvp.Value),
                (kvp, err) => default(uint?).ValuePair(kvp.Value))

            // required group element
            .ThenTry<IAggregationElement, XBNFResult<IAggregationElement>>(
                TryParseGroupElement,
                accumulatorArgs,
                (kvp, element) => kvp.Key.ValuePair(element))

            .MapAll(
                kvp => CompositeRule
                    .Of(kvp.Key, kvp.Value)
                    .ApplyTo(XBNFResult<CompositeRule>.Of),
                (fre, d) => XBNFResult<CompositeRule>.Of(fre),
                (pre, d) => XBNFResult<CompositeRule>.Of(pre));

        if (!result.Is(out CompositeRule _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseRecognitionThreshold(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<uint> result)
    {
        var position = reader.Position;
        var thresholdPath = path.Next("recognition-threshold");

        if (!reader.TryGetTokens(":", out var colonToken))
        {
            reader.Reset(position);
            result = XBNFResult<uint>.Of(FailedRecognitionError.Of(
                thresholdPath,
                position));
            return false;
        }

        if (!reader.TryGetPattern(DigitPattern, out var digitTokens))
        {
            result = XBNFResult<uint>.Of(PartialRecognitionError.Of(
                thresholdPath,
                position,
                reader.Position - position));
            reader.Reset(position);
            return false;
        }

        if (!TryParseSilentBlock(reader, thresholdPath, context, out _))
        {
            result = XBNFResult<uint>.Of(PartialRecognitionError.Of(
                thresholdPath,
                position,
                reader.Position - position));
            reader.Reset(position);
            return false;
        }

        result = uint
            .Parse(digitTokens.AsSpan())
            .ApplyTo(XBNFResult<uint>.Of);
        return true;
    }

    internal static bool TryParseGroupElement(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<IAggregationElement> result)
    {
        var position = reader.Position;
        var elementPath = path.Next("group-element");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, elementPath, context);

        result = NodeRecognitionAccumulator
            .Of<IAggregationElement, SymbolPath, ParserContext>(default!)

            // atomic rule ref
            .ThenTry<AtomicRuleRef, XBNFResult<AtomicRuleRef>>(
                TryParseAtomicRuleRef,
                accumulatorArgs,
                (_, ruleRef) => ruleRef)

            // production ref
            .OrTry<ProductionRef, XBNFResult<ProductionRef>>(
                TryParseProductionRef,
                accumulatorArgs,
                (_, prodRef) => prodRef)

            // group
            .OrTry<IAggregation, XBNFResult<IAggregation>>(
                TryParseGroup,
                accumulatorArgs,
                (_, group) => group)

            // map
            .MapAll(
                XBNFResult<IAggregationElement>.Of,
                (fre, _) => XBNFResult<IAggregationElement>.Of(fre),
                (pre, _) => XBNFResult<IAggregationElement>.Of(pre));

        if (!result.Is(out IAggregationElement _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseAtomicRuleRef(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<AtomicRuleRef> result)
    {
        var position = reader.Position;
        var atomicRulePath = path.Next("atomic-rule");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, atomicRulePath, context);

        result = NodeRecognitionAccumulator
            .Of<KeyValuePair<IAtomicRule, Cardinality>, SymbolPath, ParserContext>(
            KeyValuePair.Create(default(IAtomicRule)!, Cardinality.OccursOnlyOnce()))

            // required atomic rule
            .ThenTry<IAtomicRule, XBNFResult<IAtomicRule>>(
                TryParseAtomicRule,
                accumulatorArgs,
                (kvp, rule) => rule.ValuePair(kvp.Value))

            // optional cardinality
            .ThenTry<Cardinality, XBNFResult<Cardinality>>(
                TryParseCardinality,
                accumulatorArgs,
                (kvp, cardinality) => kvp.Key.ValuePair(cardinality),
                (kvp, err) => kvp)

            // map
            .MapAll(
                kvp => AtomicRuleRef
                    .Of(kvp.Value, kvp.Key)
                    .ApplyTo(XBNFResult<AtomicRuleRef>.Of),
                (fre, _) => XBNFResult<AtomicRuleRef>.Of(fre),
                (pre, _) => XBNFResult<AtomicRuleRef>.Of(pre));

        if (!result.Is(out AtomicRuleRef _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseProductionRef(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<ProductionRef> result)
    {
        var position = reader.Position;
        var productionRefPath = path.Next("production-ref");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, productionRefPath, context);

        result = NodeRecognitionAccumulator
            .Of<KeyValuePair<string, Cardinality>, SymbolPath, ParserContext>(
                KeyValuePair.Create(default(string)!, Cardinality.OccursOnlyOnce()))

            // required atomic rule
            .ThenTry<string, XBNFResult<string>>(
                TryParseCompositeSymbolName,
                accumulatorArgs,
                (kvp, rule) => rule.ValuePair(kvp.Value))

            // optional cardinality
            .ThenTry<Cardinality, XBNFResult<Cardinality>>(
                TryParseCardinality,
                accumulatorArgs,
                (kvp, cardinality) => kvp.Key.ValuePair(cardinality),
                (kvp, _) => kvp)

            // map
            .MapAll(
                kvp => ProductionRef
                    .Of(kvp.Value, kvp.Key)
                    .ApplyTo(XBNFResult<ProductionRef>.Of),
                (fre, _) => XBNFResult<ProductionRef>.Of(fre),
                (pre, _) => XBNFResult<ProductionRef>.Of(pre));

        if (!result.Is(out ProductionRef _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseGroup(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<IAggregation> result)
    {
        var position = reader.Position;
        var groupPath = path.Next("group");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, groupPath, context);

        result = NodeRecognitionAccumulator
            .Of<IAggregation, SymbolPath, ParserContext>(default!)

            // choice
            .ThenTry<Choice, XBNFResult<Choice>>(
                TryParseChoice,
                accumulatorArgs,
                (_, choice) => choice)

            // sequence
            .OrTry<Sequence, XBNFResult<Sequence>>(
                TryParseSequence,
                accumulatorArgs,
                (_, sequence) => sequence)

            // set
            .OrTry<Set, XBNFResult<Set>>(
                TryParseSet,
                accumulatorArgs,
                (_, set) => set)

            // map
            .MapAll(
                XBNFResult<IAggregation>.Of,
                (fre, _) => XBNFResult<IAggregation>.Of(fre),
                (pre, _) => XBNFResult<IAggregation>.Of(pre));

        if (!result.Is(out IAggregation _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseSet(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Set> result)
    {
        var position = reader.Position;
        var setPath = path.Next("set");

        if (!reader.TryGetTokens("#", out var delimiterToken))
        {
            reader.Reset(position);
            result = XBNFResult<Set>.Of(FailedRecognitionError.Of(
                setPath,
                position));
            return false;
        }

        // optional min match count
        if (!reader.TryGetPattern(DigitPattern, out var minMatchCount))
            minMatchCount = default;

        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, setPath, context);
        result = NodeRecognitionAccumulator
            .Of<(IAggregationElement[] list, Cardinality cardinality), SymbolPath, ParserContext>(
                (list: default(IAggregationElement[])!, cardinality: default(Cardinality)))

            // required element list
            .ThenTry<IAggregationElement[], XBNFResult<IAggregationElement[]>>(
                TryParseElementList,
                accumulatorArgs,
                (info, list) => (list, info.cardinality))

            // optional cardinality
            .ThenTry<Cardinality, XBNFResult<Cardinality>>(
                TryParseCardinality,
                accumulatorArgs,
                (info, cardinality) => (info.list, cardinality),
                (info, _) => (info.list, cardinality: Cardinality.OccursOnlyOnce()))

            // map
            .MapAll(
                info => Set
                    .Of(cardinality: info.cardinality,
                        elements: info.list!,
                        minRecognitionCount: minMatchCount.IsEmpty switch
                        {
                            true => info.list!.Length,
                            false => int.Parse(minMatchCount.AsSpan())
                        })
                    .ApplyTo(XBNFResult<Set>.Of),

                (fre, _) => PartialRecognitionError
                    .Of(setPath, position, reader.Position - position)
                    .ApplyTo(XBNFResult<Set>.Of),

                (pre, _) => XBNFResult<Set>.Of(pre));

        if (!result.Is(out Set _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseChoice(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Choice> result)
    {
        var position = reader.Position;
        var choicePath = path.Next("choice");

        if (!reader.TryGetTokens("?", out var delimiterToken))
        {
            reader.Reset(position);
            result = XBNFResult<Choice>.Of(FailedRecognitionError.Of(
                choicePath,
                position));
            return false;
        }

        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, choicePath, context);
        result = NodeRecognitionAccumulator
            .Of<(IAggregationElement[] list, Cardinality cardinality), SymbolPath, ParserContext>(
                (list: default(IAggregationElement[])!, cardinality: default(Cardinality)))

            // required element list
            .ThenTry<IAggregationElement[], XBNFResult<IAggregationElement[]>>(
                TryParseElementList,
                accumulatorArgs,
                (info, list) => (list, info.cardinality))

            // optional cardinality
            .ThenTry<Cardinality, XBNFResult<Cardinality>>(
                TryParseCardinality,
                accumulatorArgs,
                (info, cardinality) => (info.list, cardinality),
                (info, _) => (info.list, cardinality: Cardinality.OccursOnlyOnce()))

            // map
            .MapAll(
                info => Choice
                    .Of(info.cardinality, info.list)
                    .ApplyTo(XBNFResult<Choice>.Of),

                (fre, _) => PartialRecognitionError
                    .Of(choicePath, position, reader.Position - position)
                    .ApplyTo(XBNFResult<Choice>.Of),

                (pre, _) => XBNFResult<Choice>.Of(pre));

        if (!result.Is(out Choice _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseSequence(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Sequence> result)
    {
        var position = reader.Position;
        var sequencePath = path.Next("sequence-group");

        if (!reader.TryGetTokens("+", out var delimiterToken))
        {
            reader.Reset(position);
            result = XBNFResult<Sequence>.Of(FailedRecognitionError.Of(
                sequencePath,
                position));
            return false;
        }

        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, sequencePath, context);
        result = NodeRecognitionAccumulator
            .Of<(IAggregationElement[] list, Cardinality cardinality), SymbolPath, ParserContext>(
                (list: default(IAggregationElement[])!, cardinality: default(Cardinality)))

            // required element list
            .ThenTry<IAggregationElement[], XBNFResult<IAggregationElement[]>>(
                TryParseElementList,
                accumulatorArgs,
                (info, list) => (list, info.cardinality))

            // optional cardinality
            .ThenTry<Cardinality, XBNFResult<Cardinality>>(
                TryParseCardinality,
                accumulatorArgs,
                (info, cardinality) => (info.list, cardinality),
                (info, _) => (info.list, cardinality: Cardinality.OccursOnlyOnce()))

            // map
            .MapAll(
                info => Sequence
                    .Of(info.cardinality, info.list)
                    .ApplyTo(XBNFResult<Sequence>.Of),

                (fre, _) => PartialRecognitionError
                    .Of(sequencePath, position, reader.Position - position)
                    .ApplyTo(XBNFResult<Sequence>.Of),

                (pre, _) => XBNFResult<Sequence>.Of(pre));

        if (!result.Is(out Sequence _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseCardinality(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Cardinality> result)
    {
        var position = reader.Position;
        var cardinalityPath = path.Next("cardinality");

        if (!reader.TryGetToken(out var delimiterToken)
            || !'.'.Equals(delimiterToken[0]))
        {
            reader.Reset(position);
            result = XBNFResult<Cardinality>.Of(FailedRecognitionError.Of(
                cardinalityPath,
                position));
            return false;
        }

        // min occurs value
        if (!reader.TryGetPattern(CardinalityMinOccurencePattern, out var minOccursTokens))
        {
            result = XBNFResult<Cardinality>.Of(PartialRecognitionError.Of(
                cardinalityPath,
                position,
                reader.Position - position));
            reader.Reset(position);
            return false;
        }

        // value separator
        if (!reader.TryGetTokens(",", out var separatorTokens))
        {
            result = XBNFResult<Cardinality>.Of(minOccursTokens[0] switch
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
                '*' or '?' or '+' => XBNFResult<Cardinality>.Of(PartialRecognitionError.Of(
                    cardinalityPath,
                    position,
                    reader.Position - position)),

                _ => XBNFResult<Cardinality>.Of(Cardinality.Occurs(
                    int.Parse(minOccursTokens.AsSpan()),
                    int.Parse(maxOccursTokens.AsSpan())))
            };
            return true;
        }
        else
        {
            result = minOccursTokens[0] switch
            {
                '*' or '?' or '+' => XBNFResult<Cardinality>.Of(PartialRecognitionError.Of(
                    cardinalityPath,
                    position,
                    reader.Position - position)),

                _ => XBNFResult<Cardinality>.Of(Cardinality.OccursAtLeast(
                    int.Parse(minOccursTokens.AsSpan())))
            };
            return true;
        }
    }

    internal static bool TryParseElementList(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<IAggregationElement[]> result)
    {
        var position = reader.Position;
        var elementListPath = path.Next("element-list");

        // open bracket
        if (!reader.TryGetTokens("[", out var openBracket))
        {
            result = XBNFResult<IAggregationElement[]>.Of(FailedRecognitionError.Of(
                elementListPath,
                position));
            reader.Reset(position);
            return false;
        }

        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, elementListPath, context);
        var accumulator = NodeRecognitionAccumulator
            .Of<List<IAggregationElement>, SymbolPath, ParserContext>(new List<IAggregationElement>())

            // optional whitespace
            .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                TryParseSilentBlock,
                accumulatorArgs,
                (group, _) => group,
                (group, _) => group)

            // required element
            .ThenTry<IAggregationElement, XBNFResult<IAggregationElement>>(
                TryParseGroupElement,
                accumulatorArgs,
                (group, element) => group.AddItem(element));

        // optional additional elements
        while (accumulator.CanTryRequired)
        {
            accumulator = accumulator

                // required whitespace
                .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                    TryParseSilentBlock,
                    accumulatorArgs,
                    (group, _) => group)

                // required element
                .ThenTry<IAggregationElement, XBNFResult<IAggregationElement>>(
                    TryParseGroupElement,
                    accumulatorArgs,
                    (group, element) => group.AddItem(element));
        }

        result = accumulator
            
            // required closing bracket
            .TryIf<Tokens, XBNFResult<Tokens>>(
                TryParseClosingBracket,
                accumulatorArgs,
                (_, _, _, err) => err switch
                {
                    PartialRecognitionError => false,
                    FailedRecognitionError => true,
                    _ => throw new InvalidOperationException($"Invalid error: {err}")
                },
                (data, bracketToken) => data)
            
            // map
            .MapAll(
                data => XBNFResult<IAggregationElement[]>.Of(data.ToArray()),
                (fre, _) => PartialRecognitionError
                    .Of(elementListPath,
                        position,
                        reader.Position - position)
                    .ApplyTo(XBNFResult<IAggregationElement[]>.Of),
                (pre, _) => XBNFResult<IAggregationElement[]>.Of(pre));

        if (!result.Is(out IAggregationElement[] _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseClosingBracket(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Tokens> result)
    {
        var position = reader.Position;
        var closingBracketPath = path.Next("closing-bracket");

        if (!reader.TryGetTokens("]", out var token))
        {
            result = FailedRecognitionError
                .Of(closingBracketPath, position)
                .ApplyTo(XBNFResult<Tokens>.Of);
            reader.Reset(position);
            return false;
        }

        result = XBNFResult<Tokens>.Of(token);
        return true;
    }

    #endregion

    #region Atomic

    internal static bool TryParseAtomicRule(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<IAtomicRule> result)
    {
        var position = reader.Position;
        var atomicRulePath = path.Next("atomic-rule");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, atomicRulePath, context);

        result = NodeRecognitionAccumulator
            .Of<(string Name, List<Parameter> Args), SymbolPath, ParserContext>(
                (Name: string.Empty, Args: new List<Parameter>()))

            // parse atomic symbol name
            .ThenTry<string, XBNFResult<string>>(
                TryParseAtomicSymbolName,
                accumulatorArgs,
                (r, name) => (Name: name, r.Args))

            // or parse atomic content, and derive rule name/Id
            .OrTry<Parameter, XBNFResult<Parameter>>(
                TryParseAtomicContent,
                accumulatorArgs,
                (r, param) =>
                {
                    var contentArg = (ContentArgument)param.Argument;
                    if (!context.Metadata.AtomicContentTypeMap.TryGetValue(contentArg.Delimiter, out var symbol))
                        throw new InvalidOperationException(
                            $"Invalid atomic content: no atomic rule registered for '{contentArg.Delimiter}'");

                    r.Name = symbol;
                    r.Args.Add(param);
                    return r;
                })

            // parse optional arguments
            .ThenTry<Parameter[], XBNFResult<Parameter[]>>(
                tryParse: TryParseAtomicRuleArguments,
                args: accumulatorArgs,
                failedRecognitionMapper: (info, _) => info,
                mapper: (info, args) =>
                {
                    info.Args.AddRange(args);
                    return info;
                })

            // map to atomic rule
            .MapAll(

                // errors
                failedRecognitionMapper: (err, _) => XBNFResult<IAtomicRule>.Of(err),
                partialRecognitionMapper: (err, _) => XBNFResult<IAtomicRule>.Of(err),

                // data
                dataMapper: info =>
                {
                    if (!context.Metadata.AtomicRuleDefinitionMap.TryGetValue(info.Name, out var factoryDef))
                        throw new InvalidOperationException(
                            $"Invalid atomic rule name/Id: no atomic rule registered for Id: {info.Name}");

                    var ruleId = $"{info.Name}-{context.AtomicRuleArguments.Count}";

                    // create the rule
                    var rule = factoryDef.Factory.NewRule(
                        ruleId,
                        context.Metadata,
                        info.Args.ToImmutableDictionary(
                            arg => arg.Argument,
                            arg => arg.EscapedValue!,
                            ArgumentKeyComparer.Default)); // <-- the key comparer that makes comparing regular and content arguments possible.

                    // append the args to the context
                    context.AppendAtomicRuleArguments(ruleId, info.Args.ToArray());

                    return XBNFResult<IAtomicRule>.Of(rule);
                });

        if (!result.Is(out IAtomicRule _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseAtomicContent(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Parameter> result)
    {
        var position = reader.Position;
        var atomicContentPath = path.Next("atomic-content");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, atomicContentPath, context);

        var accumulator = NodeRecognitionAccumulator.Of<Parameter, SymbolPath, ParserContext>(default);

        if (reader.TryPeekToken(out var delimToken))
        {
            foreach (var delimChar in ContentArgumentDelimiterExtensions.DelimiterCharacterSet)
            {
                if (delimChar == delimToken[0])
                {
                    accumulator = accumulator.OrTry(
                        DelimitedContentParserDelegate(delimChar, delimChar),
                        accumulatorArgs,
                        (info, content) => Parameter.Of(
                            IArgument.Of(delimChar.DelimiterType()),
                            content));

                    // break if we already have a match
                    if (accumulator.CanTryRequired)
                        break;
                }
            }
        }

        result = accumulator.MapAll(
            // data
            @param => !@param.Equals(default)
                ? XBNFResult<Parameter>.Of(@param)
                : FailedRecognitionError
                    .Of(atomicContentPath, position)
                    .ApplyTo(XBNFResult<Parameter>.Of),
            // errors
            (fre, _) => XBNFResult<Parameter>.Of(fre),
            (pre, _) => XBNFResult<Parameter>.Of(pre));

        if (!result.Is(out Parameter _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static NodeRecognitionAccumulator.TryParse<XBNFResult<string>, string, SymbolPath, ParserContext>DelimitedContentParserDelegate(
        char startDelimiter,
        char endDelimiter)
    {
        return (TokenReader reader, SymbolPath path, ParserContext context, out XBNFResult<string> result) =>
        {
            var position = reader.Position;
            var delimContentPath = path.Next("delimited-content");
            var tryParseDelimitedContentSegment = DelimitedContentSegmentParserDelegate(startDelimiter, endDelimiter);
            var content = new StringBuilder();

            if (!tryParseDelimitedContentSegment(reader, delimContentPath, context, out var contentResult))
            {
                reader.Reset(position);
                result = contentResult.MapMatch(
                    tokens => throw new InvalidOperationException("Invalid contentResult: should be error"),
                    XBNFResult<string>.Of,
                    XBNFResult<string>.Of);
                return false;
            }

            content = !contentResult.Is(out Tokens contentTokens)
                ? content
                : contentTokens
                    .ToString()!
                    .ApplyTo(content.Append);

            // optional segments
            var tryParseAdditionalContentSegment = AdditionalDelimitedContentSegmentParserDelegate(tryParseDelimitedContentSegment);

            while (tryParseAdditionalContentSegment(reader, delimContentPath, context, out contentResult))
            {
                content = !contentResult.Is(out contentTokens)
                    ? content
                    : contentTokens
                        .ToString()!
                        .ApplyTo(content.Append);
            }

            result = !contentResult.Is(out PartialRecognitionError pre)
                ? XBNFResult<string>.Of(content.ToString())
                : XBNFResult<string>.Of(pre);

            if (result.Is(out string _))
                return true;

            reader.Reset(position);
            return false;
        };
    }

    internal static NodeRecognitionAccumulator.TryParse<XBNFResult<Tokens>, Tokens, SymbolPath, ParserContext> DelimitedContentSegmentParserDelegate(
        char startDelimiter,
        char endDelimiter)
    {
        return (TokenReader reader, SymbolPath path, ParserContext context, out XBNFResult<Tokens> result) =>
        {
            var position = reader.Position;
            var delimContentPath = path.Next("delimited-content-segment");

            // start delim
            if (!reader.TryGetTokens(startDelimiter.ToString(), out var startDelimToken))
            {
                result = XBNFResult<Tokens>.Of(FailedRecognitionError.Of(
                    delimContentPath,
                    position));
                reader.Reset(position);
                return false;
            }

            // content chars
            var contentTokens = Tokens.EmptyAt(reader.Source, reader.Position);
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

                contentTokens += stringChar;
            }

            // end delim
            if (!reader.TryGetTokens(endDelimiter.ToString(), out var endDelimToken))
            {
                result = XBNFResult<Tokens>.Of(PartialRecognitionError.Of(
                    delimContentPath,
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }

            result = XBNFResult<Tokens>.Of(contentTokens);
            return true;
        };
    }

    internal static NodeRecognitionAccumulator.TryParse<XBNFResult<Tokens>, Tokens, SymbolPath, ParserContext> AdditionalDelimitedContentSegmentParserDelegate(
        NodeRecognitionAccumulator.TryParse<XBNFResult<Tokens>, Tokens, SymbolPath, ParserContext> tryParseSegment)
    {
        return (TokenReader reader, SymbolPath path, ParserContext context, out XBNFResult<Tokens> result) =>
        {
            int position = reader.Position;
            var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, path, context);
            var segmentTokens = Tokens.Default;

            result = NodeRecognitionAccumulator
                .Of<Tokens, SymbolPath, ParserContext>(segmentTokens)

                // optional silent block
                .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                    TryParseSilentBlock,
                    accumulatorArgs,
                    (contentSegments, _) => contentSegments,
                    (contentSegments, _) => contentSegments)

                // mandatory concatenation operator
                .ThenTry<ContentConcatenationOperator, XBNFResult<ContentConcatenationOperator>>(
                    TryParseContentConcatenationOperator,
                    accumulatorArgs,
                    (contentSegments, _) => contentSegments)

                // optional silent block
                .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                    TryParseSilentBlock,
                    accumulatorArgs,
                    (contentSegments, _) => contentSegments,
                    (contentSegments, _) => contentSegments)

                // mandatory segment
                .ThenTry(
                    tryParseSegment,
                    accumulatorArgs,
                    (contentSegments, segment) => segment)

                // result
                .MapAll(
                    dataMapper: XBNFResult<Tokens>.Of,
                    failedRecognitionMapper: (err, data) => XBNFResult<Tokens>.Of(err),
                    partialRecognitionMapper: (err, data) => XBNFResult<Tokens>.Of(err));


            if (!result.Is(out Tokens _))
            {
                reader.Reset(position);
                return false;
            }

            return true;
        };
    }

    internal static bool TryParseAtomicRuleArguments(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Parameter[]> result)
    {
        var position = reader.Position;
        var atomicRuleArgumentsPath = path.Next("atomic-rule-arguments");

        if (!reader.TryGetTokens("{", out var startDelimToken))
        {
            result = XBNFResult<Parameter[]>.Of(FailedRecognitionError.Of(
                atomicRuleArgumentsPath,
                position));
            reader.Reset(position);
            return false;
        }

        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, atomicRuleArgumentsPath, context);
        var accumulator = NodeRecognitionAccumulator.Of<List<Parameter>, SymbolPath, ParserContext>(new List<Parameter>());

        do
        {
            accumulator = accumulator

                // optional silent block
                .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                    TryParseSilentBlock,
                    accumulatorArgs,
                    (args, _) => args,
                    (args, _) => args)

                // required parameter
                .ThenTry<Parameter, XBNFResult<Parameter>>(
                    TryParseArgument,
                    accumulatorArgs,
                    (args, arg) => args.AddItem(arg))

                // optional silent block
                .ThenTry<SilentBlock, XBNFResult<SilentBlock>>(
                    TryParseSilentBlock,
                    accumulatorArgs,
                    (args, _) => args,
                    (args, _) => args);
        }
        while (accumulator.CanTryRequired && reader.TryGetTokens(",", out _));

        result = accumulator

            // closing brace
            .ThenTry<Tokens, XBNFResult<Tokens>>(
                TryParseClosingBrace,
                accumulatorArgs,
                (args, _) => args)

            // map to result
            .MapAll(
                list => XBNFResult<Parameter[]>.Of(list.ToArray()),
                (fre, _) => PartialRecognitionError
                    .Of(atomicRuleArgumentsPath, position, reader.Position - position)
                    .ApplyTo(XBNFResult<Parameter[]>.Of),
                (pre, _) => XBNFResult<Parameter[]>.Of(pre));

        if (!result.Is(out Parameter[] _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseClosingBrace(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Tokens> result)
    {
        var position = reader.Position;
        var closingBracketPath = path.Next("closing-brace");

        if (!reader.TryGetTokens("}", out var token))
        {
            result = FailedRecognitionError
                .Of(closingBracketPath, position)
                .ApplyTo(XBNFResult<Tokens>.Of);
            reader.Reset(position);
            return false;
        }

        result = XBNFResult<Tokens>.Of(token);
        return true;
    }

    internal static bool TryParseArgument(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Parameter> result)
    {
        var position = reader.Position;
        var argumentPath = path.Next("atomic-rule-argument");

        if (!reader.TryGetPattern(IArgument.RegularArgumentPattern, out var argKey))
        {
            result = XBNFResult<Parameter>.Of(FailedRecognitionError.Of(
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

            var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, argumentPath, context);
            var accumulator = NodeRecognitionAccumulator
                .Of<string, SymbolPath, ParserContext>(string.Empty)

                // bool value?
                .ThenTry<bool, XBNFResult<bool>>(
                    TryParseBooleanArgValue,
                    accumulatorArgs,
                    (value, @bool) => @bool.ToString())

                // number value?
                .OrTry<decimal, XBNFResult<decimal>>(
                    TryParseNumberArgValue,
                    accumulatorArgs,
                    (value, @decimal) => @decimal.ToString(NumberFormatInfo.InvariantInfo))

                // delimited content value?
                .OrTry(
                    DelimitedContentParserDelegate('\'', '\''),
                    accumulatorArgs,
                    (value, tokens) => tokens.ToString()!);

            if (!accumulator.CanTryRequired)
            {
                result = XBNFResult<Parameter>.Of(PartialRecognitionError.Of(
                    argumentPath,
                    position,
                    reader.Position - position));
                reader.Reset(position);
                return false;
            }
            else argValue = accumulator.Data;
        }

        result = XBNFResult<Parameter>.Of(
            Parameter.Of(
                IArgument.Of(argKey.ToString()!),
                argValue));
        return true;
    }

    internal static bool TryParseContentConcatenationOperator(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<ContentConcatenationOperator> result)
    {
        var position = reader.Position;
        var concatPath = path.Next("content-concatenation-operator");

        result = reader.TryGetTokens("+", out _)
            ? XBNFResult<ContentConcatenationOperator>.Of(ContentConcatenationOperator.Instance)
            : FailedRecognitionError
                .Of(concatPath, position)
                .ApplyTo(XBNFResult<ContentConcatenationOperator>.Of);

        return result.Is(out ContentConcatenationOperator _);
    }

    internal static bool TryParseBooleanArgValue(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<bool> result)
    {
        var position = reader.Position;
        var boolArgPath = path.Next("bool-arg-value");

        // false?
        if (reader.TryPeekTokens(5, true, out var falseTokens)
            && BoolPattern.IsMatch(falseTokens.AsSpan()))
        {
            reader.Advance(5);
            result = XBNFResult<bool>.Of(false);
        }
        else if (reader.TryPeekTokens(4, true, out var trueTokens)
            && BoolPattern.IsMatch(trueTokens.AsSpan()))
        {
            reader.Advance(4);
            result = XBNFResult<bool>.Of(true);
        }
        else result = XBNFResult<bool>.Of(FailedRecognitionError.Of(boolArgPath, position));

        return result.Is(out bool _);
    }

    internal static bool TryParseNumberArgValue(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<decimal> result)
    {
        var position = reader.Position;
        var numberArgPath = path.Next("number-arg-value");
        var tokens = Tokens.EmptyAt(reader.Source, position);

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
            result = XBNFResult<decimal>.Of(FailedRecognitionError.Of(numberArgPath, position));
            return false;
        }

        if (decimal.TryParse(
            tokens.AsSpan(),
            NumberStyles.Any,
            NumberFormatInfo.InvariantInfo,
            out var @decimal))
        {
            Console.Write($"tokens: {tokens.AsSpan()}, parsed: {@decimal}\r\n");
            result = XBNFResult<decimal>.Of(@decimal);
            return true;
        }

        result = XBNFResult<decimal>.Of(PartialRecognitionError.Of(
            "number-arg-value",
            position,
            reader.Position - position));
        reader.Reset(position);
        return false;
    }

    #endregion

    #region Silent Block

    internal static bool TryParseSilentBlock(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<SilentBlock> result)
    {
        var position = reader.Position;
        var blockPath = path.Next("silent-block");
        var accumulatorArgs = NodeRecognitionAccumulator.Args(reader, blockPath, context);
        var accumulator = NodeRecognitionAccumulator.Of<List<ISilentElement>, SymbolPath, ParserContext>(
            new List<ISilentElement>());

        do
        {
            accumulator = accumulator

                // block comment
                .ThenTry<BlockComment, XBNFResult<BlockComment>>(
                    TryParseBlockComment,
                    accumulatorArgs,
                    (list, comment) => list.AddItem((ISilentElement)comment))

                // line comment
                .OrTry<LineComment, XBNFResult<LineComment>>(
                    TryParseLineComment,
                    accumulatorArgs,
                    (list, comment) => list.AddItem((ISilentElement)comment))

                // tab/space/line-feed/carriage-return
                .OrTry<Whitespace, XBNFResult<Whitespace>>(
                    TryParseWhitespace,
                    accumulatorArgs,
                    (list, whitespace) => list.AddItem((ISilentElement)whitespace));
        }
        while (accumulator.CanTryRequired);

        result = accumulator.MapAll(
            dataMapper: list => XBNFResult<SilentBlock>.Of(SilentBlock.Of(list)),

            // errors?
            partialRecognitionMapper: (pre, _) => XBNFResult<SilentBlock>.Of(pre),
            failedRecognitionMapper: (fre, list) => list.Count switch
            {
                >= 1 => XBNFResult<SilentBlock>.Of(SilentBlock.Of(list)),
                _ => XBNFResult<SilentBlock>.Of(fre)
            });

        if (!result.Is(out SilentBlock _))
        {
            reader.Reset(position);
            return false;
        }

        return true;
    }

    internal static bool TryParseBlockComment(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<BlockComment> result)
    {
        var position = reader.Position;
        var blockCommentPath = path.Next("block-comment");

        if (!reader.TryGetTokens("/*", out var delimiter))
        {
            result = XBNFResult<BlockComment>.Of(FailedRecognitionError.Of(
                blockCommentPath,
                position));
            reader.Reset(position);
            return false;
        }

        // read content
        var contentToken = Tokens.EmptyAt(reader.Source, reader.Position);
        while (reader.TryGetToken(out var token))
        {
            if ('*' == token[0]
                && reader.TryPeekTokens("/", out var bsolToken))
            {
                reader.Advance();
                result = BlockComment
                    .Of(contentToken)
                    .ApplyTo(XBNFResult<BlockComment>.Of);
                return true;
            }

            contentToken += token;
        }

        result = XBNFResult<BlockComment>.Of(PartialRecognitionError.Of(
            blockCommentPath,
            position,
            reader.Position - position));
        reader.Reset(position);
        return false;
    }

    internal static bool TryParseLineComment(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<LineComment> result)
    {
        var position = reader.Position;
        var lineCommentPath = path.Next("line-comment");

        if (!reader.TryGetTokens("#", out var delimiter))
        {
            result = XBNFResult<LineComment>.Of(FailedRecognitionError.Of(
                lineCommentPath,
                position));
            reader.Reset(position);
            return false;
        }

        // read content
        var contentToken = Tokens.EmptyAt(reader.Source, reader.Position);
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
            .ApplyTo(XBNFResult<LineComment>.Of);
        return true;
    }

    internal static bool TryParseWhitespace(
        TokenReader reader,
        SymbolPath path,
        ParserContext context,
        out XBNFResult<Whitespace> result)
    {
        var position = reader.Position;
        var whitespacePath = path.Next("whitespace");

        if (!reader.TryGetToken(out var whitespaceToken)
            || (' ' != whitespaceToken[0]
            && '\t' != whitespaceToken[0]
            && '\n' != whitespaceToken[0]
            && '\r' != whitespaceToken[0]))
        {
            result = XBNFResult<Whitespace>.Of(FailedRecognitionError.Of(
                whitespacePath,
                position));
            reader.Reset(position);
            return false;
        }

        result = Whitespace
            .Of(whitespaceToken)
            .ApplyTo(XBNFResult<Whitespace>.Of);
        return true;
    }

    #endregion

    #region helpes

    private static string Flatten(this
        IEnumerable<Tokens> tokens)
        => tokens
            .Aggregate(new StringBuilder(), (builder, tokens) => builder.Append(tokens))
            .ToString();

    #endregion

}
