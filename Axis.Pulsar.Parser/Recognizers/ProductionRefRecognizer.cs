using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    /// <summary>
    /// Recognizes tokens for a given <see cref="Grammar.ProductionRef"/>
    /// </summary>
    public class ProductionRefRecognizer : IRecognizer
    {
        /// <summary>
        /// The name given to this node in the Concrete Syntax Tree node. (<see  cref="CST.CSTNode"/>)
        /// </summary>
        public static readonly string PSEUDO_NAME = "#Ref";

        private readonly Grammar.IGrammar _grammar;

        ///<inheritdoc/>
        public Cardinality Cardinality { get; }

        /// <summary>
        /// The symbol name for the production that this instance refers to.
        /// </summary>
        public string SymbolRef { get; }

        public ProductionRefRecognizer(
            string symbolRef,
            Cardinality cardinality,
            Grammar.IGrammar grammar)
        {
            Cardinality = cardinality;
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            SymbolRef = symbolRef
                .ThrowIf(
                    string.IsNullOrWhiteSpace,
                    new ArgumentNullException(nameof(symbolRef)))
                .ThrowIf(
                    v => !grammar.HasProduction(v),
                    new ArgumentException($"Invalid {nameof(symbolRef)}: {symbolRef}"));
        }

        ///<inheritdoc/>
        public bool TryRecognize(BufferedTokenReader tokenReader, out IResult result)
        {
            var position = tokenReader.Position;
            var _parser = _grammar.GetParser(SymbolRef) ?? throw new InvalidOperationException($"This recognizer represents an invalid symbol-ref: {SymbolRef}");

            try
            {
                var results = new List<Parsers.IResult.Success>();
                Parsers.IResult currentResult;
                int cycleCount = 0;
                do
                {
                    currentResult = _parser.Parse(tokenReader);
                    if (currentResult is Parsers.IResult.Success success)
                        results.Add(success);

                    else break;
                }
                while (Cardinality.CanRepeat(++cycleCount));

                #region Success
                if (Cardinality.IsValidRange(results.Count))
                {
                    result = results
                        .Select(result => result.Symbol)
                        .Map(symbols => new IResult.Success(symbols));
                    return true;
                }
                #endregion

                #region Failed
                // else - Not enough symbols were recognized; this was a failed attempt
                var currentPosition = tokenReader.Position + 1;
                _ = tokenReader.Reset(position);
                result = currentResult switch
                {
                    Parsers.IResult.PartialRecognition @partial => new IResult.FailedRecognition(
                        results.Count,
                        currentPosition,
                        @partial),

                    Parsers.IResult.FailedRecognition failed => new IResult.FailedRecognition(
                        results.Count,
                        currentPosition,
                        failed),

                    Parsers.IResult.Exception exception => new IResult.Exception(
                        exception.Error,
                        exception.InputPosition),

                    _ => new IResult.Exception(
                        new InvalidOperationException($"Invalid result type: {currentResult?.GetType()}"),
                        currentPosition)
                };
                return false;

                #endregion
            }
            catch (Exception e)
            {
                #region Failed
                _ = tokenReader.Reset(position);
                result = new IResult.Exception(e, position + 1);
                return false;
                #endregion
            }
        }

        ///<inheritdoc/>
        public IResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        ///<inheritdoc/>
        public override string ToString() => $"${SymbolRef}.Ref";
    }
}
