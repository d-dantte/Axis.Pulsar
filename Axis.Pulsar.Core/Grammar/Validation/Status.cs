namespace Axis.Pulsar.Core.Grammar.Validation
{
    public enum Status
    {
        /// <summary>
        /// The symbol validation succeeded.
        /// </summary>
        Valid,

        /// <summary>
        /// The symbol is deemed invalid - results in a failed recognition.
        /// </summary>
        Invalid,

        /// <summary>
        /// The symbol is deemed FATALLY invalid, and all further recognition should cease.
        /// </summary>
        Fatal
    }
}
