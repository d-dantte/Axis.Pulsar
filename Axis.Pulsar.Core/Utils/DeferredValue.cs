namespace Axis.Pulsar.Core.Utils
{
    internal class DeferredValue<TValue>
    {
        private Func<TValue> _valueFactory;
        private TValue _value;

        /// <summary>
        /// 
        /// </summary>
        public bool IsGenerated { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public TValue Value
        {
            get
            {
                if (!IsGenerated)
                {
                    _value = _valueFactory.Invoke();
                    IsGenerated = true;
                }

                return _value;
            }
        }

        public DeferredValue(Func<TValue> valueFactory)
        {
            ArgumentNullException.ThrowIfNull(valueFactory);

            _valueFactory = valueFactory;
            IsGenerated = false;
            _value = default!;
        }

        public static implicit operator DeferredValue<TValue>(
            Func<TValue> valueFactory)
            => new(valueFactory);
    }
}
