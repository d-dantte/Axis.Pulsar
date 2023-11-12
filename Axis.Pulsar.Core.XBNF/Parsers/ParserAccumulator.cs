using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF;

public class ParserAccumulator<TData>
{
    private Exception? _aggregationException = null;

    public TData Data { get; private set; }

    public MetaContext Context { get; }

    public TokenReader Reader { get; }

    public bool IsPreviousOpErrored => _aggregationException is not null;

    public bool IsPreviousOpUnmatched => _aggregationException is UnmatchedError;

    public bool IsPreviousOpFaultyMatch => _aggregationException is FaultyMatchError;

    public bool IsPreviousOpUnknown => _aggregationException is UnknownError;

    public bool IsPreviousOpRuntimeError => IsPreviousOpErrored && !IsPreviousOpFaultyMatch && !IsPreviousOpUnknown && !IsPreviousOpUnmatched;

    internal ParserAccumulator(
        TokenReader reader,
        MetaContext context,
        TData data,
        Exception? error = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(context);

        Reader = reader;
        Context = context;
        Data = data;
        _aggregationException = error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOther"></typeparam>
    /// <param name="tryParse"></param>
    /// <param name="aggregatorFunction"></param>
    /// <param name="optionalValueAggregatorFunction"></param>
    /// <returns></returns>
    public ParserAccumulator<TData> ThenTry<TOther>(
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
                    Data = newResult
                        .Map(newData => aggregatorFunction.Invoke(Data, newData))
                        .Resolve();

                else if (newResult.IsErrorResult(out UnmatchedError ume)
                    && optionalValueAggregatorFunction is not null)
                    Data = optionalValueAggregatorFunction.Invoke(Data);

                else _aggregationException = newResult.AsError().ActualCause();
            }
            catch (Exception e)
            {
                _aggregationException = new UnknownError(e);
            }
        }
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOther"></typeparam>
    /// <param name="tryParse"></param>
    /// <param name="aggregatorFunction"></param>
    /// <param name="optionalValueAggregatorFunction"></param>
    /// <returns></returns>
    public ParserAccumulator<TData> OrTry<TOther>(
        ParserAccumulator.TryParse<TOther> tryParse,
        Func<TData, TOther, TData> aggregatorFunction,
        Func<TData, TData>? optionalValueAggregatorFunction = null)
    {
        ArgumentNullException.ThrowIfNull(tryParse);
        ArgumentNullException.ThrowIfNull(aggregatorFunction);

        if (IsPreviousOpUnmatched)
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
        TData data)
        => new(reader, context, data, null);

    public static ParserAccumulator<TData> OfAlternative<TData>(
        TokenReader reader,
        MetaContext context,
        TData data,
        string symbolName = null!)
        => new(reader, context, data, new UnmatchedError(
            symbolName ?? "$_alternative", 
            reader.Position));
}