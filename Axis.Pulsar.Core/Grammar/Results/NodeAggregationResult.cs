using Axis.Luna.Unions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Results
{
    public readonly struct NodeAggregationResult :
        IUnion<ISymbolNode, AggregateRecognitionError, NodeAggregationResult>,
        IUnionOf<ISymbolNode, AggregateRecognitionError, NodeAggregationResult>
    {
        private readonly object? _value;

        object? IUnion<ISymbolNode, AggregateRecognitionError, NodeAggregationResult>.Value => _value;


        private NodeAggregationResult(object value)
        {
            _value = value;
        }

        /// <summary>
        /// Rejects null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static NodeAggregationResult Of(ISymbolNode value) => value switch
        {
            ISymbolNode => new(value),
            null => throw new ArgumentNullException(nameof(value))
        };

        public static NodeAggregationResult Of(AggregateRecognitionError value) => new(value);

        public bool Is(out ISymbolNode value)
        {
            if (_value is ISymbolNode n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Is(out AggregateRecognitionError value)
        {
            if (_value is AggregateRecognitionError n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool IsNull() => _value is null;

        public TOut? MapMatch<TOut>(
            Func<ISymbolNode, TOut> sequenceMapper,
            Func<AggregateRecognitionError, TOut> aggregateErrorMapper,
            Func<TOut>? nullMapper = null!)
        {
            ArgumentNullException.ThrowIfNull(sequenceMapper);
            ArgumentNullException.ThrowIfNull(aggregateErrorMapper);

            if (_value is ISymbolNode t1)
                return sequenceMapper.Invoke(t1);

            if (_value is AggregateRecognitionError t2)
                return aggregateErrorMapper.Invoke(t2);

            // unknown type, assume null
            return nullMapper switch
            {
                null => default!,
                _ => nullMapper.Invoke()
            };
        }

        public void ConsumeMatch(
            Action<ISymbolNode> sequenceConsumer,
            Action<AggregateRecognitionError> aggregateErrorConsumer,
            Action? nullConsumer = null!)
        {
            ArgumentNullException.ThrowIfNull(sequenceConsumer);
            ArgumentNullException.ThrowIfNull(aggregateErrorConsumer);

            if (_value is ISymbolNode t1)
                sequenceConsumer.Invoke(t1);

            else if (_value is AggregateRecognitionError t2)
                aggregateErrorConsumer.Invoke(t2);

            else nullConsumer?.Invoke();
        }

        public NodeAggregationResult WithMatch(
            Action<ISymbolNode> sequenceConsumer,
            Action<AggregateRecognitionError> aggregateErrorConsumer,
            Action? nullConsumer = null!)
        {
            ConsumeMatch(sequenceConsumer, aggregateErrorConsumer, nullConsumer);
            return this;
        }

        #region Map
        public TOut Map<TIn, TOut>(Func<TIn, TOut> func)
        {
            ArgumentNullException.ThrowIfNull(func);

            if (_value is TIn @in)
                return func.Invoke(@in);

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
        public void Consume<TValue>(Action<TValue> func)
        {
            ArgumentNullException.ThrowIfNull(func);

            if (_value is TValue value)
                func.Invoke(value);
            else
                throw new InvalidOperationException($"Invalid consume operation");
        }
        #endregion
    }
}