using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.XBNF;

public static class Extensions
{
    public static IResult<TOut> TransformError<TOut, TError>(this
        IResult<TOut> result,
        Func<Exception, TError> errorTransformer)
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(errorTransformer);

        if (result.IsDataResult())
            return result;

        return result
            .AsError()
            .ActualCause()
            .ApplyTo(errorTransformer.Invoke)
            .ApplyTo(Result.Of<TOut>);
    }
}
