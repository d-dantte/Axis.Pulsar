using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar
{
    /// <summary>
    /// This interface is meant as an extensibility point into the parsig library.
    /// </summary>
    public interface ICustomRule: IRule
    {
        /// <summary>
        /// Name of the default argument
        /// </summary>
        const string DefaultArgumentKey = "DefaultArg";

        /// <summary>
        /// Arguments used to build this custom rule instance
        /// </summary>
        ImmutableDictionary<string, string> Arguments { get; }
    }
}
