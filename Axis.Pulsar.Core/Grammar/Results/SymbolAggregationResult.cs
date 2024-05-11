using Axis.Luna.Unions;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Results
{
    public readonly struct SymbolAggregationResult :
        IUnion<ISymbolNodeAggregation, SymbolAggregationError, SymbolAggregationResult>,
        IUnionOf<ISymbolNodeAggregation, SymbolAggregationError, SymbolAggregationResult>
    {
        private readonly object? _value;

        object? IUnion<ISymbolNodeAggregation, SymbolAggregationError, SymbolAggregationResult>.Value => _value!;


        public SymbolAggregationResult(object value)
        {
            _value = value switch
            {
                null => null,
                SymbolAggregationError
                or ISymbolNodeAggregation => value,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"Invalid {nameof(value)} type: '{value.GetType()}'")
            };
        }

        public static SymbolAggregationResult Of(ISymbolNodeAggregation value) => value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            _ => new(value)
        };

        public static SymbolAggregationResult Of(SymbolAggregationError value) => new(value);

        public bool Is(out ISymbolNodeAggregation value)
        {
            if (_value is ISymbolNodeAggregation n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Is(out SymbolAggregationError value)
        {
            if (_value is SymbolAggregationError n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool IsNull() => _value is null;

        public TOut? MapMatch<TOut>(
            Func<ISymbolNodeAggregation, TOut> sequenceMapper,
            Func<SymbolAggregationError, TOut> groupErrorMapper,
            Func<TOut>? nullMapper = null!)
        {
            ArgumentNullException.ThrowIfNull(sequenceMapper);
            ArgumentNullException.ThrowIfNull(groupErrorMapper);

            if (_value is ISymbolNodeAggregation t1)
                return sequenceMapper.Invoke(t1);

            if (_value is SymbolAggregationError t2)
                return groupErrorMapper.Invoke(t2);

            // unknown type, assume null
            return nullMapper switch
            {
                null => default!,
                _ => nullMapper.Invoke()
            };
        }

        public void ConsumeMatch(
            Action<ISymbolNodeAggregation> sequenceConsumer,
            Action<SymbolAggregationError> groupErrorConsumer,
            Action? nullConsumer = null!)
        {
            ArgumentNullException.ThrowIfNull(sequenceConsumer);
            ArgumentNullException.ThrowIfNull(groupErrorConsumer);

            if (_value is ISymbolNodeAggregation t1)
                sequenceConsumer.Invoke(t1);

            else if (_value is SymbolAggregationError t2)
                groupErrorConsumer.Invoke(t2);

            else if (_value is null && nullConsumer is not null)
                nullConsumer.Invoke();
        }

        public SymbolAggregationResult WithMatch(
            Action<ISymbolNodeAggregation> sequenceConsumer,
            Action<SymbolAggregationError> groupErrorConsumer,
            Action? nullConsumer = null!)
        {
            ConsumeMatch(sequenceConsumer, groupErrorConsumer, nullConsumer);
            return this;
        }

        #region Map
        public TOut Map<TIn, TOut>(Func<TIn, TOut> seqFunc)
        {
            ArgumentNullException.ThrowIfNull(seqFunc);

            if (_value is TIn @in)
                return seqFunc.Invoke(@in);

            throw new InvalidOperationException($"Invalid map operation");
        }
        #endregion

        #region Get
        public TValue Get<TValue>()
        {
            if (_value is TValue value)
                return value;

            throw new InvalidOperationException($"Invalid call: value is not an aggregation");
        }
        #endregion

        #region Consume
        public void Consume<TValue>(Action<TValue> seqFunc)
        {
            ArgumentNullException.ThrowIfNull(seqFunc);

            if (_value is TValue value)
                seqFunc.Invoke(value);
            else
                throw new InvalidOperationException($"Invalid consume operation");
        }
        #endregion
    }
}