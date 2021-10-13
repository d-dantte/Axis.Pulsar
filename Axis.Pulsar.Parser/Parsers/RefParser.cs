using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Parsers
{
    public class RefParser : RuleParser
    {
        public static readonly string PSEUDO_NAME = "#Ref";


        private IGrammarContext _context;

        public string Ref { get; }

        
        public RefParser(RuleRef @ref)
            : this(Cardinality.OccursOnlyOnce(), @ref)
        { }

        public RefParser(string @ref)
            : this(Cardinality.OccursOnlyOnce(), @ref)
        { }

        public RefParser(Cardinality cardinality, RuleRef @ref)
            : base(cardinality)
        {
            Ref = @ref?.Symbol ?? throw new ArgumentNullException(nameof(@ref));
        }

        public RefParser(Cardinality cardinality, string @ref)
            : base(cardinality)
        {
            Ref = @ref ?? throw new ArgumentNullException(nameof(@ref));
        }

        public override bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var _parser = _context.GetParser(Ref);
            var position = tokenReader.Position;
            try
            {
                var results = new List<ParseResult>();
                var cycleCount = 0;
                ParseResult cycleResult = null;
                do
                {
                    if (_parser.TryParse(tokenReader, out cycleResult))
                        results.Add(cycleResult);
                }
                while (cycleResult.Succeeded && CanRepeat(++cycleCount));


                var cycles = results.Count;
                if ((cycles == 0 && Cardinality.MinOccurence == 0)
                    || (cycles >= Cardinality.MinOccurence && (Cardinality.MaxOccurence == null || cycles <= Cardinality.MaxOccurence)))
                {
                    result = new(new Syntax.Symbol(
                        PSEUDO_NAME,
                        results.Select(r => r.Symbol).ToArray()));

                    return true;
                }
                else
                {
                    tokenReader.Reset(position);
                    result = new(new ParseError(PSEUDO_NAME, position + 1, cycleResult.Error));
                    return false;
                }
            }
            catch
            {
                tokenReader.Reset(position);
                result = new(new ParseError(PSEUDO_NAME, position + 1));
                return false;
            }
        }

        public override string ToString() => $"Ref[{Ref}]";

        internal RefParser SetGrammarContext(IGrammarContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            return this;
        }
    }
}
