using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Linq;

namespace Axis.Pulsar.Grammar.Exceptions
{
    public class PartialRecognitionException: Exception
    {
        private CST.CSTNode[] _partials;

        /// <summary>
        /// The partially recognized symbols
        /// </summary>
        public CST.CSTNode[] Symbols => _partials.ToArray();

        /// <summary>
        /// The failure reason
        /// </summary>
        public IReason FailureReason { get; }

        /// <summary>
        /// The expected symbol that was partially recognized
        /// </summary>
        public string ExpectedSymbol { get; }

        /// <summary>
        /// The position in the <see cref="BufferedTokenReader"/> where the recognition attempt began
        /// </summary>
        public int Position { get; }

        public PartialRecognitionException(
            string expectedSymbol,
            int position,
            IReason failureReason,
            params CST.CSTNode[] partials)
            :base($"Partial recognition at ({position}), for symbol: {expectedSymbol}")
        {
            _partials = partials;
            Position = position;
            ExpectedSymbol = expectedSymbol;
            FailureReason = failureReason;
        }
    }
}
