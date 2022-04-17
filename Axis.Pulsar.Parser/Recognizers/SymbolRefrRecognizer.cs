using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    public class SymbolRefRecognizer : IRecognizer
    {
        public static readonly string PSEUDO_NAME = "#Ref";

        private readonly Grammar.Grammar _grammar;

        public Cardinality Cardinality { get; }

        public string SymbolRef { get; }

        public SymbolRefRecognizer(string symbolRef, Grammar.Grammar grammar)
            :this(symbolRef, Cardinality.OccursOnlyOnce(), grammar)
        {
        }

        public SymbolRefRecognizer(string symbolRef, Cardinality cardinality, Grammar.Grammar grammar)
        {
            Cardinality = cardinality;
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            SymbolRef = symbolRef.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentNullException(nameof(symbolRef)));
        }

        public bool TryRecognize(BufferedTokenReader tokenReader, out RecognizerResult result)
        {
            var position = tokenReader.Position;
            var _parser = _grammar.GetParser(SymbolRef) ?? throw new InvalidOperationException($"This recognizer represents an invalid symbol-ref: {SymbolRef}");

            try
            {
                var results = new List<ParseResult>();
                ParseResult current = null;
                int cycleCount = 0;
                do
                {
                    if (_parser.TryParse(tokenReader, out current))
                        results.Add(current);
                }
                while (current.Succeeded && Cardinality.CanRepeat(++cycleCount));

                var cycles = results.Count;
                if (cycles == 0 && Cardinality.MinOccurence == 0)
                {
                    result = new RecognizerResult(new Syntax.Symbol(ToString(), ""));
                    return true;
                }
                else if (cycles >= Cardinality.MinOccurence && (Cardinality.MaxOccurence == null || cycles <= Cardinality.MaxOccurence))
                {
                    result = results
                        .Select(result => result.Symbol)
                        .ToArray()
                        .Map(symbols => new RecognizerResult(symbols));

                    return true;
                }
                else
                {
                    tokenReader.Reset(position);
                    result = new(new ParseError(PSEUDO_NAME, position + 1, current.Error));
                    return false;
                }
            }
            catch(Exception e)
            {
                tokenReader.Reset(position);
                result = new(new ParseError(PSEUDO_NAME, position + 1, e.Message));
                return false;
            }
        }

        public override string ToString() => $"${SymbolRef}";
    }
}
