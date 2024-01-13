using Axis.Luna.Extensions;
using System;

namespace Axis.Pulsar.Grammar.CST
{
    internal static class Errors
    {
        internal interface IParseError { }

        /// <summary>
        /// Indicates that the end of the stream of input tokens was reached BEFORE
        /// the parsing could start. Typically, this happens when on the first attempt to read
        /// a token for a symbol/production, the reader announces it has no more tokens to give.
        /// </summary>
        internal class EndOfStream: Exception, IParseError
        {
        }

        /// <summary>
        /// Indicates that the first set of tokens read while trying to recognize a symbol did not
        /// match the symbols rules. E.g, trying to recognize an identifier, and a digit is the first
        /// character the reader returns.
        /// </summary>
        internal class UnrecognizedTokens: Exception, IParseError
        {
        }

        /// <summary>
        /// Indicates that enough characters have been recognized to anticipate the correct symbol, but
        /// an unrecognized set of characters were read subsequently. This usually happens while
        /// recognizing/parsing non-terminals.
        /// <para>
        /// E.g: trying to recognize a c# method signature, if the modifiers, return type, and name have
        /// all been recognized, but a '{' is read instead of a '(' while trying to recognize the parameter
        /// list, then a partial recognition has occured
        /// </para>
        /// </summary>
        internal class PartiallyRecognizedTokens: Exception, IParseError
        {
            public int Position { get; }

            public int Length { get; }

            public string PartialTokens { get; }

            public PartiallyRecognizedTokens(
                int position,
                int length,
                string partialTokens = null)
            {
                Position = position.ThrowIf(
                    p => p < 0,
                    _ => new ArgumentException($"Invalid position: {position}"));

                Length = length.ThrowIf(
                    p => p <= 0,
                    _ => new ArgumentException($"Invalid length: {length}"));

                PartialTokens = partialTokens;
            }
        }

        /// <summary>
        /// Some other error happens, e.g, divide by zero, failed cast, etc.
        /// </summary>
        internal class RuntimeError: Exception, IParseError
        {
            public RuntimeError(Exception cause)
            : base("", cause)
            { }
        }
    }
}
