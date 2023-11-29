using Axis.Luna.Common.Results;

namespace Axis.Pulsar.Core.Utils;

public class ParserAccumulator<TData, TSymbolID, TContext>
{
    private TData _data;
    private Exception? _error = null;
    private int _recognitionCount = 0;
    private bool _isRecentlyFailed = false;

    private readonly TokenReader _reader;
    private readonly TContext _context;
    private readonly TSymbolID _symbolId;

    public bool IsErrored => _error is not null;

    public bool IsFailedRecognitionError => _error is FailedRecognitionError;

    public ParserAccumulator(
        TokenReader reader,
        TSymbolID symbolID,
        TContext context,
        TData data = default!)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(symbolID);

        _data = data;
        _reader = reader;
        _context = context;
        _symbolId = symbolID;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TTempData"></typeparam>
    /// <param name="symbol"></param>
    /// <param name="tryParse"></param>
    /// <param name="mapper"></param>
    /// <param name="defaultMapper"></param>
    /// <returns></returns>
    public ParserAccumulator<TData, TSymbolID, TContext> ThenTry<TTempData>(
        ParserAccumulator.TryParse<TTempData, TSymbolID, TContext> tryParse,
        Func<TData, TTempData, TData> mapper,
        Func<TData, TData>? defaultMapper = null)
    {
        ArgumentNullException.ThrowIfNull(tryParse);
        ArgumentNullException.ThrowIfNull(mapper);

        if (!IsErrored)
        {
            var position = _reader.Position;

            if (tryParse.Invoke(_reader, _symbolId, _context, out var tresult))
            {
                tresult
                    .Map(tdata => mapper.Invoke(_data, tdata))
                    .Consume(data =>
                    {
                        _data = data;
                        _recognitionCount++;
                    });
            }
            else if (tresult.IsErrorResult(out FailedRecognitionError fre)
                && defaultMapper is not null)
            {
                _reader.Reset(position);

                try
                {
                    _data = defaultMapper!.Invoke(_data);
                    _recognitionCount++; // ????
                }
                catch(Exception e)
                {
                    _error = e;
                }
            }
            else
            {
                _reader.Reset(position);
                tresult.ConsumeError(e =>
                {
                    _error = e;
                    _isRecentlyFailed = true;
                });
            }
        }
        else
        {
            _isRecentlyFailed = false;
        }

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TTempData"></typeparam>
    /// <param name="symbol"></param>
    /// <param name="tryParse"></param>
    /// <param name="mapper"></param>
    /// <param name="defaultMapper"></param>
    /// <returns></returns>
    public ParserAccumulator<TData, TSymbolID, TContext> OrTry<TTempData>(
        ParserAccumulator.TryParse<TTempData, TSymbolID, TContext> tryParse,
        Func<TData, TTempData, TData> mapper,
        Func<TData, TData>? defaultMapper = null)
    {
        ArgumentNullException.ThrowIfNull(tryParse);
        ArgumentNullException.ThrowIfNull(mapper);

        if (IsFailedRecognitionError && _isRecentlyFailed)
        {
            var position = _reader.Position;

            if (tryParse.Invoke(_reader, _symbolId, _context, out var tresult))
            {
                tresult
                    .Map(tdata => mapper.Invoke(_data, tdata))
                    .Consume(data => _data = data);
                _recognitionCount++;
                _error = null;
            }
            else if (tresult.IsErrorResult(out FailedRecognitionError fre)
                && defaultMapper is not null)
            {
                _reader.Reset(position);

                try
                {
                    _data = defaultMapper!.Invoke(_data);
                    _recognitionCount++; // ????
                    _error = null;
                }
                catch (Exception e)
                {
                    _error = e;
                }
            }
            else
            {
                _reader.Reset(position);
                tresult.ConsumeError(e => _error = e);
            }
        }

        return this;
    }


    public IResult<TData> ToResult()
    {
        if (IsErrored)
            return Result.Of<TData>(_error);

        return Result.Of(_data);
    }

    public IResult<TOtherData> ToResult<TOtherData>(Func<TData, TOtherData> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return this
            .ToResult()
            .Map(mapper);
    }


    public ParserAccumulator<TData, TSymbolID, TContext> Consume(Action<TData> consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        if (!IsErrored)
            consumer.Invoke(_data);

        return this;
    }

    public ParserAccumulator<TData, TSymbolID, TContext> MapError(
        Func<TData, Exception, int, TData> mapper)
        => MapError((a, b, c) => true, mapper);

    public ParserAccumulator<TData, TSymbolID, TContext> MapError(
        Func<TData, Exception, int, bool> predicate,
        Func<TData, Exception, int, TData> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(predicate);

        if (IsErrored && predicate.Invoke(_data, _error!, _recognitionCount))
        {
            try
            {
                _data = mapper.Invoke(_data, _error!, _recognitionCount);
                _error = null;
            }
            catch (Exception e)
            {
                _error = e;
            }
        }

        return this;
    }

    public ParserAccumulator<TData, TSymbolID, TContext> MapError<TError>(
        Func<TData, TError, int, TData> mapper)
        where TError : Exception
        => MapError((a, b, c) => true, mapper);

    public ParserAccumulator<TData, TSymbolID, TContext> MapError<TError>(
        Func<TData, TError, int, bool> predicate,
        Func<TData, TError, int, TData> mapper)
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(predicate);

        if (_error is TError terror && predicate.Invoke(_data, terror!, _recognitionCount))
        {
            try
            {
                _data = mapper.Invoke(_data, terror!, _recognitionCount);
                _error = null;
            }
            catch (Exception e)
            {
                _error = e;
            }
        }

        return this;
    }

    public ParserAccumulator<TData, TSymbolID, TContext> ConsumeError(
        Action<TData, Exception, int> consumer)
        => ConsumeError((a,b,c) => true, consumer);

    public ParserAccumulator<TData, TSymbolID, TContext> ConsumeError(
        Func<TData, Exception, int, bool> predicate,
        Action<TData, Exception, int> consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(predicate);

        if (IsErrored && predicate.Invoke(_data, _error!, _recognitionCount))
            consumer.Invoke(_data, _error!, _recognitionCount);

        return this;
    }

    public ParserAccumulator<TData, TSymbolID, TContext> ConsumeError<TError>(
        Action<TData, TError, int> consumer)
        where TError : Exception
        => ConsumeError((a, b, c) => true, consumer);

    public ParserAccumulator<TData, TSymbolID, TContext> ConsumeError<TError>(
        Func<TData, TError, int, bool> predicate,
        Action<TData, TError, int> consumer)
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(predicate);

        if (_error is TError terror && predicate.Invoke(_data, terror, _recognitionCount))
            consumer.Invoke(_data, terror, _recognitionCount);

        return this;
    }

    public ParserAccumulator<TData, TSymbolID, TContext> TransformError(
        Func<TData, Exception, int, Exception> mapper)
        => TransformError((a, b, c) => true, mapper);


    public ParserAccumulator<TData, TSymbolID, TContext> TransformError(
        Func<TData, Exception, int, bool> predicate,
        Func<TData, Exception, int, Exception> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(predicate);

        if (IsErrored && predicate.Invoke(_data, _error!, _recognitionCount))
            _error = mapper.Invoke(_data, _error!, _recognitionCount);

        return this;
    }

    public ParserAccumulator<TData, TSymbolID, TContext> TransformError<TError>(
        Func<TData, TError, int, Exception> mapper)
        where TError : Exception
        => TransformError((a, b, c) => true, mapper);

    public ParserAccumulator<TData, TSymbolID, TContext> TransformError<TError>(
        Func<TData, TError, int, bool> predicate,
        Func<TData, TError, int, Exception> mapper)
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(predicate);

        if (_error is TError terror && predicate.Invoke(_data, terror, _recognitionCount))
            _error = mapper.Invoke(_data, terror, _recognitionCount);

        return this;
    }
}

public static class ParserAccumulator
{
    public delegate bool TryParse<TData, TSymbolID, TContext>(
        TokenReader reader,
        TSymbolID symbol,
        TContext context,
        out IResult<TData> result);

    public static ParserAccumulator<TData, TSymbolID, TContext> Of<TData, TSymbolID, TContext>(
        TokenReader reader,
        TSymbolID symbolID,
        TContext context)
        => new(reader, symbolID, context);

    public static ParserAccumulator<TData, TSymbolID, TContext> Of<TData, TSymbolID, TContext>(
        TokenReader reader,
        TSymbolID symbolID,
        TContext context,
        TData data)
        => new(reader, symbolID, context, data);
}

