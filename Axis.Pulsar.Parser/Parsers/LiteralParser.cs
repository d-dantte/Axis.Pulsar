using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using System;
using Axis.Pulsar.Parser.CST;

namespace Axis.Pulsar.Parser.Parsers
{
    /// <summary>
    /// Parser for <see cref="LiteralRule"/>
    /// </summary>
    public class LiteralParser: IParser
    {
        private readonly LiteralRule _literalRule;

        /// <inheritdoc/>
        public string SymbolName { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold => _literalRule.RecognitionThreshold;

        public LiteralParser(string symbolName, LiteralRule literalRule)
        {
            _literalRule = literalRule ?? throw new ArgumentNullException(nameof(literalRule));
            SymbolName = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException("Invalid symbol name"));
        }

        /// <inheritdoc/>
        public bool TryParse(BufferedTokenReader tokenReader, out IResult result)
        {
            var position = tokenReader.Position;
            try
            {
                if(tokenReader.TryNextTokens(_literalRule.Value.Length, out var tokens)
                    && _literalRule.Value.Equals(
                        new string(tokens),
                        _literalRule.IsCaseSensitive
                            ? StringComparison.InvariantCulture
                            : StringComparison.InvariantCultureIgnoreCase))
                {
                    result = new IResult.Success(
                        ICSTNode.Of(
                            SymbolName,
                            new string(tokens)));

                    return true;
                }

                //add relevant information into the parse error
                result = new IResult.FailedRecognition(
                    SymbolName,
                    position + 1);
                tokenReader.Reset(position);
                return false;
            }
            catch(Exception ex)
            {
                //add relevant information into the parse error
                result = new IResult.Exception(ex, position + 1);
                tokenReader.Reset(position);
                return false;
            }
        }
        
        /// <inheritdoc/>
        public IResult Parse(BufferedTokenReader tokenReader)
        {
            _ = TryParse(tokenReader, out var result);
            return result;
        }

        public override string ToString() => $"'{_literalRule.Value}'";
    }
}
