using System;

namespace Axis.Pulsar.Grammar.Builders
{
    public abstract class AbstractBuiler<Target>
    {
        private bool _built;

        /// <summary>
        /// Perform validation on the target instance, throwing exceptions if validation fails.
        /// </summary>
        protected abstract void ValidateTarget();

        /// <summary>
        /// Called to build the target instance
        /// </summary>
        protected abstract Target BuildTarget();

        public Target Build()
        {
            if(!_built)
            {
                ValidateTarget();
                _built = true;
            }

            return BuildTarget();
        }

        /// <summary>
        /// Ensures that the builder hasn't yet successfully built the target
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        protected void AssertNotBuilt()
        {
            if (_built)
                throw new InvalidOperationException($"Cannot modify the builder after it has built the target");
        }
    }
}
