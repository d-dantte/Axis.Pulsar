using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF;

public static class GrammarParser
{
    public static bool TryParseGrammar(
        TokenReader reader,
        LanguageContext context,
        out IResult<IGrammar> result)
    {
        var position = reader.Position;
        var productions = new List<Production>();

        try
        {
            do
            {
                // silent block
                _ = TryParseSilentBlock(reader, context, out var sblockResult);

                if (sblockResult.IsErrorResult(out UnknownError uke))
                {
                    reader.Reset(position);
                    result = Result.Of<IGrammar>(uke);
                    return false;
                }
                else if (sblockResult.IsErrorResult(out FailedRecognitionError fre))
                {
                    reader.Reset(position);
                    result = Result.Of<IGrammar>(fre);
                    return false;
                }

                // production
                if (TryParseProduction(reader, context, out var productionResult))
                    productionResult.Consume(productions.Add);

                else if (productionResult.IsErrorResult(out UnrecognizedError ure))
                    break;

                else
                {
                    reader.Reset(position);
                    result = productionResult.MapAs<IGrammar>();
                    return false;
                }
            }
            while (true);

            result = productions
                .ApplyTo(prods => Grammar.Grammar.Of(
                    productions[0].Symbol,
                    productions))
                .ApplyTo(Result.Of);
            return true;
        }
        catch (Exception e)
        {
                    reader.Reset(position);
            result = Result.Of<IGrammar>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseProduction(
        TokenReader reader,
        LanguageContext context,
        out IResult<Production> result)
    {
        var position = reader.Position;

        try
        {
            // symbol name
            if (!TryParseSymbolName(reader, context, out var symbolNameResult))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            // space
            if(!TryParseSilentBlock(reader, context, out _))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            // =>
            if(!TryParseMapOperator(reader, context, out _))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            // space
            if(!TryParseSilentBlock(reader, context, out _))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            // rule
            if(!TryParseRule(reader, context, out var ruleResult))
            {
                reader.Reset(position);
                result = symbolNameResult.MapAs<Production>();
                return false;
            }

            result = symbolNameResult.Combine(ruleResult, Production.Of);
            return true;
        }
        catch (Exception e)
        {
            result = Result.Of<Production>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseSymbolName(
        TokenReader reader,
        LanguageContext context,
        out IResult<string> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<string>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseRule(
        TokenReader reader,
        LanguageContext context,
        out IResult<IRule> result)
    {
        var position = reader.Position;

        try
        {
            // Composite rule
            if(TryParseCompositeRule(reader, context, out var compositeRuleResult))
            {
                result = compositeRuleResult.MapAs<IRule>();
                return true;
            }
            else if(compositeRuleResult)
        }
        catch (Exception e)
        {
            result = Result.Of<IRule>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseMapOperator(
        TokenReader reader,
        LanguageContext context,
        out IResult<Tokens> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<Tokens>(new UnknownError(e));
            return false;
        }
    }

    public static bool TryParseSilentBlock(
        TokenReader reader,
        LanguageContext context,
        out IResult<ISilentBlock> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<ISilentBlock>(new UnknownError(e));
            return false;
        }
    }
        
    public static bool TryParseCompositeRule(
        TokenReader reader,
        LanguageContext context,
        out IResult<ICompositeRule> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<ICompositeRule>(new UnknownError(e));
            return false;
        }
    }
    
    public static bool TryParseAtomicRule(
        TokenReader reader,
        LanguageContext context,
        out IResult<IAtomicRule> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<IAtomicRule>(new UnknownError(e));
            return false;
        }
    }
    
    
    public static bool TryParse___(
        TokenReader reader,
        LanguageContext context,
        out IResult<Production> result)
    {
        var position = reader.Position;

        try
        {
        }
        catch (Exception e)
        {
            result = Result.Of<Production>(new UnknownError(e));
            return false;
        }
    }
    
}
