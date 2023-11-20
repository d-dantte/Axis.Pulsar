using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF;

public class ParserAccumulator<TData>
{
    private readonly int _initialPosition;
    private Exception? _aggregationException = null;

    public TData Data { get; private set; }

    public MetaContext Context { get; }

    public TokenReader Reader { get; }

    public string SymbolName { get; }

    public int MatchCount { get; private set; }

    public int UnmatchedThreshold { get; }

    #region Error status checks
    public bool IsPreviousOpErrored => _aggregationException is not null;

    public bool IsPreviousOpUnmatched => _aggregationException is UnmatchedError;

    public bool IsPreviousOpFaultyMatch => _aggregationException is FaultyMatchError;

    public bool IsPreviousOpUnknown => _aggregationException is UnknownError;

    public bool IsPreviousOpRuntimeError => IsPreviousOpErrored && !IsPreviousOpFaultyMatch && !IsPreviousOpUnknown && !IsPreviousOpUnmatched;
    #endregion

    internal ParserAccumulator(
        TokenReader reader,
        MetaContext context,
        TData data,
        string symbolName,
        int unmatchedThreshold = 1)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(context);

        Reader = reader;
        Context = context;
        Data = data;
        MatchCount = 0;

        UnmatchedThreshold = unmatchedThreshold.ThrowIf(
            rt => rt < 1,
            new ArgumentOutOfRangeException(nameof(unmatchedThreshold)));

        SymbolName = symbolName
            .ThrowIf(
                string.IsNullOrEmpty,
                new ArgumentException($"Invalid argument: '{nameof(symbolName)}' is null/empty"))
            .ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                new ArgumentException($"Invalid '{nameof(symbolName)}' format: {symbolName} "));

        _aggregationException = null;
        _initialPosition = Reader.Position;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOther"></typeparam>
    /// <param name="ignoreUnmatchedThresholdCheck">
    ///     False indicates that <see cref="FaultyMatchError"/>s emanating from the operation are transformed to <see cref="UnmatchedError"/> instances
    ///     if the <see cref="ParserAccumulator{TData}.UnmatchedThreshold"/> is reached.
    ///     <para/>
    ///     True indicates that all errors from the operations are retained as-is.
    /// </param>
    /// <param name="tryParse">The parse function</param>
    /// <param name="aggregatorFunction">The aggregation function</param>
    /// <param name="optionalValueAggregatorFunction"></param>
    /// <returns></returns>
    public ParserAccumulator<TData> ThenTry<TOther>(
        bool ignoreUnmatchedThresholdCheck,
        ParserAccumulator.TryParse<TOther> tryParse,
        Func<TData, TOther, TData> aggregatorFunction,
        Func<TData, TData>? optionalValueAggregatorFunction = null)
    {
        ArgumentNullException.ThrowIfNull(tryParse);
        ArgumentNullException.ThrowIfNull(aggregatorFunction);

        if (!IsPreviousOpErrored)
        {
            try
            {
                _ = tryParse.Invoke(Reader, Context, out var newResult);

                if (newResult.IsDataResult())
                {
                    Data = newResult
                        .Map(newData => aggregatorFunction.Invoke(Data, newData))
                        .Resolve();

                    MatchCount++;
                }
                else if (newResult.IsErrorResult(out UnmatchedError ume)
                    && optionalValueAggregatorFunction is not null)
                {
                    Data = optionalValueAggregatorFunction.Invoke(Data);
                }
                else
                {
                    if (newResult.IsErrorResult(out ume)
                        && !ignoreUnmatchedThresholdCheck
                        && MatchCount >= UnmatchedThreshold)
                    {
                        _aggregationException = new FaultyMatchError(
                            SymbolName,
                            _initialPosition,
                            Reader.Position - _initialPosition);
                    }
                    else
                    {
                        _aggregationException = newResult.AsError().ActualCause();
                    }
                }
            }
            catch (Exception e)
            {
                _aggregationException = new UnknownError(e);
            }
        }
        return this;
    }

    public ParserAccumulator<TData> ThenTry<TOther>(
        ParserAccumulator.TryParse<TOther> tryParse,
        Func<TData, TOther, TData> aggregatorFunction,
        Func<TData, TData>? optionalValueAggregatorFunction = null)
        => ThenTry(false, tryParse, aggregatorFunction, optionalValueAggregatorFunction);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOther"></typeparam>
    /// <param name="ignorePreviousUnmatchedCheck">Set to true if this or should ignore the success status of the previous operation</param>
    /// <param name="tryParse"></param>
    /// <param name="aggregatorFunction"></param>
    /// <param name="optionalValueAggregatorFunction"></param>
    /// <returns></returns>
    public ParserAccumulator<TData> OrTry<TOther>(
        bool ignorePreviousUnmatchedCheck,
        ParserAccumulator.TryParse<TOther> tryParse,
        Func<TData, TOther, TData> aggregatorFunction,
        Func<TData, TData>? optionalValueAggregatorFunction = null)
    {
        ArgumentNullException.ThrowIfNull(tryParse);
        ArgumentNullException.ThrowIfNull(aggregatorFunction);

        if ((!IsPreviousOpErrored && ignorePreviousUnmatchedCheck) || IsPreviousOpUnmatched)
        {
            try
            {
                _ = tryParse.Invoke(Reader, Context, out var newResult);

                if (newResult.IsDataResult())
                {
                    Data = newResult
                        .Map(newData => aggregatorFunction.Invoke(Data, newData))
                        .Resolve();
                    _aggregationException = null;
                }

                // Be careful of this branch: it shunts the or-chain.
                else if (newResult.IsErrorResult(out UnmatchedError ume)
                    && optionalValueAggregatorFunction is not null)
                {
                    Data = optionalValueAggregatorFunction.Invoke(Data);
                    _aggregationException = null;
                }

                else _aggregationException = newResult.AsError().ActualCause();
            }
            catch (Exception e)
            {
                _aggregationException = new UnknownError(e);
            }
        }

        return this;
    }

    public ParserAccumulator<TData> OrTry<TOther>(
        ParserAccumulator.TryParse<TOther> tryParse,
        Func<TData, TOther, TData> aggregatorFunction,
        Func<TData, TData>? optionalValueAggregatorFunction = null)
        => OrTry(false, tryParse, aggregatorFunction, optionalValueAggregatorFunction);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IResult<TData> ToResult() => _aggregationException switch
    {
        null => Result.Of(Data),
        Exception e => Result.Of<TData>(e)
    };

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOther"></typeparam>
    /// <param name="mapper"></param>
    /// <returns></returns>
    public IResult<TOther> ToResult<TOther>(
        Func<TData, TOther>? mapper = null)
    {
        if (mapper is null)
            return ToResult().MapAs<TOther>();

        else return ToResult().Map(mapper);
    }


    public ParserAccumulator<TData> Consume(Action<TData> consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        if (!IsPreviousOpErrored)
            consumer.Invoke(Data);

        return this;
    }

    public ParserAccumulator<TData> ConsumeError(Action<Exception> consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        if (IsPreviousOpErrored)
            consumer.Invoke(_aggregationException!);

        return this;
    }

    public ParserAccumulator<TData> TransformError(Func<Exception, Exception> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);

        if (IsPreviousOpErrored)
            _aggregationException = transformer.Invoke(_aggregationException!);

        return this;
    }
}

public static class ParserAccumulator
{
    public delegate bool TryParse<TData>(
        TokenReader reader,
        MetaContext context,
        out IResult<TData> result);

    public static ParserAccumulator<TData> Of<TData>(
        TokenReader reader,
        MetaContext context,
        TData data,
        string symbolName,
        int recognitionThreshold = 1)
        => new(reader, context, data, symbolName, recognitionThreshold);
}
