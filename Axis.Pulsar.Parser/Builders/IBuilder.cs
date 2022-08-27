using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Pulsar.Parser.Builders
{
    /// <summary>
    /// Represents the contract for a builder
    /// </summary>
    /// <typeparam name="T">The encapsulated type to be built</typeparam>
    public interface IBuilder<T>
    {
        /// <summary>
        /// Build the encapsulated type
        /// </summary>
        T Build();
    }
}
