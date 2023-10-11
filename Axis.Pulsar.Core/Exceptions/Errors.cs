using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Misc.Pulsar.Utils;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Exceptions
{
    public static class Errors
    {
        public interface IRecognitionError
        {
            ProductionPath ProductionPath { get; }
        }

        /// <summary>
        /// Indicates that the end of the stream of input tokens was reached BEFORE
        /// the parsing could start. Typically, this happens when on the first attempt to read
        /// a token for a symbol/production, the reader announces it has no more tokens to give.
        /// </summary>
        //public class EndOfStream : Exception, IRecognitionError
        //{
        //    public ProductionPath ProductionPath { get; }

        //    public EndOfStream(ProductionPath productionPath)
        //    {
        //        ProductionPath = productionPath ?? throw new ArgumentNullException(nameof(productionPath));
        //    }
        //}

        /// <summary>
        /// Indicates that the first set of tokens read while trying to recognize a symbol did not
        /// match the symbols rules. E.g, trying to recognize an identifier, and a digit is the first
        /// character the reader returns.
        /// <para/>
        /// In other words, where applicable, the <c>RecognitionThreshold</c> was not reached
        /// </summary>
        public class UnrecognizedTokens : Exception, IRecognitionError
        {
            public ProductionPath ProductionPath { get; }

            public int Position { get; }

            public UnrecognizedTokens(ProductionPath productionPath, int position)
            {
                ProductionPath = productionPath ?? throw new ArgumentNullException(nameof(productionPath));
                Position = position.ThrowIf(
                    i => i < 0,
                    new ArgumentOutOfRangeException($"Invalid {nameof(position)}: {position}"));
            }

            public static UnrecognizedTokens Of(
                ProductionPath productionPath,
                int position)
                => new(productionPath, position);
        }

        /// <summary>
        /// Indicates that enough characters have been recognized to anticipate the correct symbol, but
        /// an unrecognized set of characters were read subsequently. This usually happens while
        /// recognizing/parsing non-terminals.
        /// <para/>
        /// E.g: trying to recognize a c# method signature, if the modifiers, return type, and name have
        /// all been recognized, but a '{' is read instead of a '(' while trying to recognize the parameter
        /// list, then a partial recognition has occured.
        /// </summary>
        public class PartiallyRecognizedTokens: Exception, IRecognitionError
        {
            private readonly Lazy<Tokens> _tokens;

            public int Position { get; }

            public Tokens PartialTokens => _tokens.Value;

            public ProductionPath ProductionPath { get; }

            public PartiallyRecognizedTokens(
                ProductionPath productionPath,
                int position,
                Func<Tokens> tokenProvider)
            {
                ProductionPath = productionPath ?? throw new ArgumentNullException(nameof(productionPath));
                Position = position.ThrowIf(
                    p => p < 0,
                    new ArgumentException($"Invalid position: {position}"));

                _tokens = new Lazy<Tokens>(tokenProvider);
            }

            public PartiallyRecognizedTokens(
                ProductionPath productionPath,
                int position,
                Tokens partialTokens)
                :this(productionPath, position, () => partialTokens)
            {
            }

            public static PartiallyRecognizedTokens Of(
                ProductionPath productionPath,
                int position,
                Tokens partialTokens)
                => new(productionPath, position, partialTokens);

            public static PartiallyRecognizedTokens Of(
                ProductionPath productionPath,
                int position,
                Func<Tokens> tokenProvider)
                => new(productionPath, position, tokenProvider);
        }

        /// <summary>
        /// Some other error happens, e.g, divide by zero, failed cast, etc.
        /// </summary>
        public class RuntimeError: Exception, IRecognitionError
        {
            public ProductionPath ProductionPath { get; }

            public RuntimeError(ProductionPath productionPath, Exception cause)
            : base("", cause)
            {
                ProductionPath = productionPath ?? throw new ArgumentNullException(nameof(productionPath));
            }

            public static RuntimeError Of(
                ProductionPath productionPath,
                Exception cause)
                => new(productionPath, cause);
        }


        #region extensions
        public static IResult<TOut> MapUnrecognizedTokens<TIn, TOut>(this
            IResult<TIn>.ErrorResult result,
            ProductionPath path,
            int tokenPosition)
        {
            var error = result.ActualCause() as IRecognitionError;
            return error.MapUnrecognizedTokens<TOut>(path, tokenPosition);
        }

        public static IResult<TOut> MapUnrecognizedTokens<TOut>(this
            IRecognitionError? error,
            ProductionPath path,
            int tokenPosition)
        {
            return error switch
            {
                RuntimeError e => Result.Of<TOut>(e),
                PartiallyRecognizedTokens
                or UnrecognizedTokens => Result.Of<TOut>(
                    new UnrecognizedTokens(path, tokenPosition)),
                _ => Result.Of<TOut>(new RuntimeError(
                    path,
                    (error as Exception) ?? new InvalidOperationException($"Invalid error: {error}")))
            };
        }

        public static IResult<TOut> MapPartiallyRecognizedTokens<TIn, TOut>(this
            IResult<TIn>.ErrorResult result,
            ProductionPath path,
            int tokenPosition,
            params Tokens[] recognizedTokens)
        {
            return result.MapPartiallyRecognizedTokens<TIn, TOut>(
                path,
                tokenPosition,
                recognizedTokens.AsEnumerable());
        }

        public static IResult<TOut> MapPartiallyRecognizedTokens<TIn, TOut>(this
            IResult<TIn>.ErrorResult result,
            ProductionPath path,
            int tokenPosition,
            IEnumerable<Tokens> recognizedTokens)
        {
            var error = result.ActualCause() as IRecognitionError;
            return error.MapPartiallyRecognizedTokens<TOut>(
                path,
                tokenPosition,
                recognizedTokens);
        }

        public static IResult<TOut> MapPartiallyRecognizedTokens<TOut>(this
            IRecognitionError? error,
            ProductionPath path,
            int tokenPosition,
            IEnumerable<Tokens> recognizedTokens)
        {
            return error switch
            {
                PartiallyRecognizedTokens
                or RuntimeError => Result.Of<TOut>((Exception)error),
                UnrecognizedTokens => Result.Of<TOut>(
                    new PartiallyRecognizedTokens(
                        path,
                        tokenPosition,
                        recognizedTokens.Combine)),

                _ => Result.Of<TOut>(new RuntimeError(
                    path,
                    (error as Exception) ?? new InvalidOperationException($"Invalid error: {error}")))
            };
        }
        #endregion
    }
}
