using Axis.Luna.Common;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;

namespace Axis.Pulsar.Core.Utils;

public static class NodeRecognitionAccumulator
{
    public delegate bool TryParse<TUnion, TResult, TSymbolID, TContext>(
        TokenReader reader,
        TSymbolID symbol,
        TContext context,
        out TUnion result)
        where TUnion : INodeRecognitionResult<TResult, TUnion>;

    public static NodeRecognitionAccumulator<TData, TSymbolID, TContext> Of<TData, TSymbolID, TContext>(
        TData data) => new(data);

    public static NodeRecognitionAccumulator<TData, TSymbolID, TContext> OfFailed<TData, TSymbolID, TContext>(
        FailedRecognitionError error) => new(default!, true, false, error);

    public static NodeRecognitionAccumulator<TData, TSymbolID, TContext> OfPartial<TData, TSymbolID, TContext>(
        PartialRecognitionError error) => new(default!, false, true, error);

    public static RecognitionArgs<TSymbolID, TContext> Args<TSymbolID, TContext>(
        TokenReader reader,
        TSymbolID symbol,
        TContext context)
        => new(reader, symbol, context);
}

public readonly struct RecognitionArgs<TSymbolID, TContext>(
    TokenReader reader,
    TSymbolID symbol,
    TContext context)
{
    public TokenReader Reader { get; } = reader;
    public TSymbolID Symbol { get; } = symbol;
    public TContext Context { get; } = context;
}

public readonly struct NodeRecognitionAccumulator<TData, TSymbolID, TContext> :
    IDefaultValueProvider<NodeRecognitionAccumulator<TData, TSymbolID, TContext>>
{
    private readonly TData _data;
    private readonly State _state;
    private readonly INodeRecognitionError? _error;

    public bool CanTryAlternatives => _state.HasFlag(State.CanTryAlternative);

    public bool CanTryRequired => _state.HasFlag(State.CanTryRrequired);

    public TData Data => _data;

    #region IDefaultValueProvider<>
    public bool IsDefault => 
        EqualityComparer<TData>.Default.Equals(default, _data)
        && _state == State.None
        && _error is null;

    public static NodeRecognitionAccumulator<TData, TSymbolID, TContext> Default => default;
    #endregion

    public NodeRecognitionAccumulator(
        TData data)
        : this(data, true, true, null)
    {
    }

    internal NodeRecognitionAccumulator(
        TData data,
        bool canTryRequired,
        bool canTryAlternative,
        INodeRecognitionError? error = null)
    {
        _data = data;
        _error = error;
        _state = (canTryRequired, canTryAlternative) switch
        {
            (true, true) => State.All,
            (true, false) => State.CanTryRrequired,
            (false, true) => State.CanTryAlternative,
            _ => State.None
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOutResult"></typeparam>
    /// <typeparam name="TOutUnion"></typeparam>
    /// <param name="tryParse"></param>
    /// <param name="args"></param>
    /// <param name="mapper"></param>
    /// <param name="failedRecognitionMapper"></param>
    /// <returns></returns>
    public NodeRecognitionAccumulator<TData, TSymbolID, TContext> ThenTry<TOutResult, TOutUnion>(
        NodeRecognitionAccumulator.TryParse<TOutUnion, TOutResult, TSymbolID, TContext> tryParse,
        RecognitionArgs<TSymbolID, TContext> args,
        Func<TData, TOutResult, TData> mapper,
        Func<TData, FailedRecognitionError, TData>? failedRecognitionMapper = null)
        where TOutUnion : INodeRecognitionResult<TOutResult, TOutUnion>
    {
        ArgumentNullException.ThrowIfNull(tryParse);
        ArgumentNullException.ThrowIfNull(mapper);

        // Try required recognition/parse operation?
        if (CanTryRequired)
            return TryOperation(tryParse, args, mapper, failedRecognitionMapper);

        // Previous recognition failed, and since we cant try required, propagate fatal state.
        return new NodeRecognitionAccumulator<TData, TSymbolID, TContext>(_data, false, false, _error);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOutResult"></typeparam>
    /// <typeparam name="TOutUnion"></typeparam>
    /// <param name="tryParse"></param>
    /// <param name="args"></param>
    /// <param name="mapper"></param>
    /// <param name="failedRecognitionMapper"></param>
    /// <returns></returns>
    public NodeRecognitionAccumulator<TData, TSymbolID, TContext> OrTry<TOutResult, TOutUnion>(
        NodeRecognitionAccumulator.TryParse<TOutUnion, TOutResult, TSymbolID, TContext> tryParse,
        RecognitionArgs<TSymbolID, TContext> args,
        Func<TData, TOutResult, TData> mapper,
        Func<TData, FailedRecognitionError, TData>? failedRecognitionMapper = null)
        where TOutUnion : INodeRecognitionResult<TOutResult, TOutUnion>
    {
        ArgumentNullException.ThrowIfNull(tryParse);
        ArgumentNullException.ThrowIfNull(mapper);

        // Try alternative recognition/parse operation?
        if (CanTryAlternatives)
            return TryOperation(tryParse, args, mapper, failedRecognitionMapper);

        // If we can't try alternatives, we may be able to try required. Return this instance
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOutResult"></typeparam>
    /// <typeparam name="TOutUnion"></typeparam>
    /// <param name="tryParse"></param>
    /// <param name="args"></param>
    /// <param name="predicate"></param>
    /// <param name="mapper"></param>
    /// <param name="freMapper"></param>
    /// <returns></returns>
    public NodeRecognitionAccumulator<TData, TSymbolID, TContext> TryIf<TOutResult, TOutUnion>(
        NodeRecognitionAccumulator.TryParse<TOutUnion, TOutResult, TSymbolID, TContext> tryParse,
        RecognitionArgs<TSymbolID, TContext> args,
        Func<TData, bool, bool, INodeRecognitionError?, bool> predicate,
        Func<TData, TOutResult, TData> mapper,
        Func<TData, FailedRecognitionError, TData>? freMapper = null)
        where TOutUnion : INodeRecognitionResult<TOutResult, TOutUnion>
    {
        ArgumentNullException.ThrowIfNull(tryParse);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(predicate);

        // Try the operation based on the given predicate
        if (predicate.Invoke(_data, CanTryRequired, CanTryAlternatives, _error))
            return TryOperation(tryParse, args, mapper, freMapper);

        // If we can't try based on the predicate, leave things as they are
        return this;
    }


    private NodeRecognitionAccumulator<TData, TSymbolID, TContext> TryOperation<TOutResult, TOutUnion>(
        NodeRecognitionAccumulator.TryParse<TOutUnion, TOutResult, TSymbolID, TContext> tryParse,
        RecognitionArgs<TSymbolID, TContext> args,
        Func<TData, TOutResult, TData> mapper,
        Func<TData, FailedRecognitionError, TData>? freMapper)
        where TOutUnion : INodeRecognitionResult<TOutResult, TOutUnion>
    {
        _ = tryParse.Invoke(args.Reader, args.Symbol, args.Context, out var tresult);

        var localData = _data;
        var overrideError = freMapper is not null;
        return tresult.MapMatch(

            // recognition/parsing was successful, map the data
            result => new NodeRecognitionAccumulator<TData, TSymbolID, TContext>(
                mapper.Invoke(localData, result),
                true,
                false),

            // recognition failed. Override?
            fre => overrideError switch
            {
                // Yes: override the error and prepare for REQUIRED tries.
                true => new NodeRecognitionAccumulator<TData, TSymbolID, TContext>(
                    freMapper!.Invoke(localData, fre),
                    true,
                    false),

                // No: prepare for ALTERNATIVE tries.
                false => new NodeRecognitionAccumulator<TData, TSymbolID, TContext>(localData, false, true, fre)
            },

            // recognition/parsing was partial. Propagate the fatal state
            pre => new NodeRecognitionAccumulator<TData, TSymbolID, TContext>(localData, false, false, pre),

            // null/default result. If we will allow accumulation of null results, then 
            // map this same way we map "result", but with null
            () => throw new InvalidOperationException($"Invalid result: null"));
    }

    #region Transformations

    #region Map

    public TOut MapAll<TOut>(
        Func<TData, TOut> dataMapper,
        Func<FailedRecognitionError, TData, TOut> failedRecognitionMapper,
        Func<PartialRecognitionError, TData, TOut> partialRecognitionMapper)
    {
        ArgumentNullException.ThrowIfNull(dataMapper);
        ArgumentNullException.ThrowIfNull(failedRecognitionMapper);
        ArgumentNullException.ThrowIfNull(partialRecognitionMapper);

        return _error switch
        {
            null => dataMapper.Invoke(_data),
            FailedRecognitionError fre => failedRecognitionMapper.Invoke(fre, _data),
            //PartialRecognitionError pre
            _ => partialRecognitionMapper.Invoke((PartialRecognitionError)_error, _data),
        };
    }
    #endregion

    #region Consume

    public void ConsumeAll(
        Action<TData> dataMapper,
        Action<FailedRecognitionError, TData> failedRecognitionMapper,
        Action<PartialRecognitionError, TData> partialRecognitionMapper)
    {
        ArgumentNullException.ThrowIfNull(dataMapper);
        ArgumentNullException.ThrowIfNull(failedRecognitionMapper);
        ArgumentNullException.ThrowIfNull(partialRecognitionMapper);

        if (_error is null)
            dataMapper.Invoke(_data);

        else if (_error is FailedRecognitionError fre)
            failedRecognitionMapper.Invoke(fre, _data);

        else
            partialRecognitionMapper.Invoke((PartialRecognitionError) _error, _data);
    }
    #endregion

    #endregion

    #region Nested Types

    [Flags]
    internal enum State
    {
        None = 0,
        CanTryAlternative = 0x1,
        CanTryRrequired = 0x2,
        All = CanTryAlternative | CanTryRrequired
    }
    #endregion
}
