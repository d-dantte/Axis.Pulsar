using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Errors
{
    internal static class NodeErrorMapper
    {
        internal static IResult<ICSTNode> MapNodeError(
            this IResult<ICSTNode>.ErrorResult result,
            Func<UnrecognizedTokens, INodeError> unrecognizedTokensErrorMapper,
            Func<PartiallyRecognizedTokens, INodeError> partiallyRecognizedTokensErrorMapper)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(unrecognizedTokensErrorMapper);
            ArgumentNullException.ThrowIfNull(partiallyRecognizedTokensErrorMapper);

            try
            {
                if (result.ActualCause() is RecognitionRuntimeError)
                    return result;

                var nodeError = result.ActualCause() switch
                {
                    UnrecognizedTokens ute => unrecognizedTokensErrorMapper.Invoke(ute),
                    PartiallyRecognizedTokens pte => partiallyRecognizedTokensErrorMapper.Invoke(pte),
                    _ => result.ActualCause().Throw<INodeError>()
                };

                return Result.Of<ICSTNode>((Exception)nodeError);
            }
            catch (RecognitionRuntimeError rre)
            {
                return Result.Of<ICSTNode>(rre);
            }
            catch (Exception e)
            {
                return Result.Of<ICSTNode>(RecognitionRuntimeError.Of(e));
            }
        }

        internal static IResult<NodeSequence> MapGroupError(
            this IResult<ICSTNode>.ErrorResult result,
            Func<UnrecognizedTokens, GroupError> unrecognizedTokensErrorMapper,
            Func<PartiallyRecognizedTokens, GroupError> partiallyRecognizedTokensErrorMapper)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(unrecognizedTokensErrorMapper);
            ArgumentNullException.ThrowIfNull(partiallyRecognizedTokensErrorMapper);

            try
            {
                if (result.ActualCause() is RecognitionRuntimeError)
                    return result.MapAs<NodeSequence>();

                var groupError = result.ActualCause() switch
                {
                    UnrecognizedTokens ute => unrecognizedTokensErrorMapper.Invoke(ute),
                    PartiallyRecognizedTokens pte => partiallyRecognizedTokensErrorMapper.Invoke(pte),
                    _ => result.ActualCause().Throw<GroupError>()
                };

                return Result.Of<NodeSequence>(groupError);
            }
            catch (RecognitionRuntimeError rre)
            {
                return Result.Of<NodeSequence>(rre);
            }
            catch (Exception e)
            {
                return Result.Of<NodeSequence>(RecognitionRuntimeError.Of(e));
            }
        }
    }

    internal static class GroupErrorMapper
    {
        internal static IResult<ICSTNode> MapNodeError(
            this IResult<NodeSequence>.ErrorResult result,
            Func<GroupError, UnrecognizedTokens, INodeError> unrecognizedTokensErrorMapper,
            Func<GroupError, PartiallyRecognizedTokens, INodeError> partiallyRecognizedTokensErrorMapper)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(unrecognizedTokensErrorMapper);
            ArgumentNullException.ThrowIfNull(partiallyRecognizedTokensErrorMapper);

            try
            {
                if (result.ActualCause() is RecognitionRuntimeError)
                    return result.MapAs<ICSTNode>();

                var nodeError = result.ActualCause() switch
                {
                    GroupError ge => ge.NodeError switch
                    {
                        UnrecognizedTokens ute => unrecognizedTokensErrorMapper.Invoke(ge, ute),
                        PartiallyRecognizedTokens pte => partiallyRecognizedTokensErrorMapper.Invoke(ge, pte),
                        _ => (ge.NodeError as Exception ?? new InvalidOperationException("null error")).Throw<INodeError>()
                    },
                    _ => result.ActualCause().Throw<INodeError>()
                };

                return Result.Of<ICSTNode>((Exception)nodeError);
            }
            catch (RecognitionRuntimeError rre)
            {
                return Result.Of<ICSTNode>(rre);
            }
            catch (Exception e)
            {
                return Result.Of<ICSTNode>(RecognitionRuntimeError.Of(e));
            }
        }

        internal static IResult<NodeSequence> MapGroupError(
            this IResult<NodeSequence>.ErrorResult result,
            Func<GroupError, UnrecognizedTokens, GroupError> unrecognizedTokensErrorMapper,
            Func<GroupError, PartiallyRecognizedTokens, GroupError> partiallyRecognizedTokensErrorMapper)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(unrecognizedTokensErrorMapper);
            ArgumentNullException.ThrowIfNull(partiallyRecognizedTokensErrorMapper);

            try
            {
                if (result.ActualCause() is RecognitionRuntimeError)
                    return result;

                var groupError = result.ActualCause() switch
                {
                    GroupError ge => ge.NodeError switch
                    {
                        UnrecognizedTokens ute => unrecognizedTokensErrorMapper.Invoke(ge, ute),
                        PartiallyRecognizedTokens pte => partiallyRecognizedTokensErrorMapper.Invoke(ge, pte),
                        _ => (ge.NodeError as Exception ?? new InvalidOperationException("null error")).Throw<GroupError>()
                    },
                    _ => result.ActualCause().Throw<GroupError>()
                };

                return Result.Of<NodeSequence>(groupError);
            }
            catch (RecognitionRuntimeError rre)
            {
                return Result.Of<NodeSequence>(rre);
            }
            catch (Exception e)
            {
                return Result.Of<NodeSequence>(RecognitionRuntimeError.Of(e));
            }
        }
    }
}
