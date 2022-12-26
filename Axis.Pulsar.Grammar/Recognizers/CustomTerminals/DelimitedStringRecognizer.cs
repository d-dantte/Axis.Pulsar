using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Text;
using static Axis.Pulsar.Grammar.Language.Rules.CustomTerminals.DelimitedString;

namespace Axis.Pulsar.Grammar.Recognizers.CustomTerminals
{
    public class DelimitedStringRecognizer : IRecognizer
    {
        private readonly DelimitedString _rule;

        public Language.Grammar Grammar { get; }

        public IRule Rule => _rule;

        public DelimitedStringRecognizer(DelimitedString rule, Language.Grammar grammar)
        {
            _rule = rule.ThrowIfDefault(new ArgumentException(nameof(rule)));
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        }

        public IRecognitionResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        public bool TryRecognize(BufferedTokenReader tokenReader, out IRecognitionResult result)
        {
            var position = tokenReader.Position;
            try
            {
                var context = new RecognitionContext(
                    position,
                    stringBuilder: new StringBuilder(),
                    tokenReader: tokenReader,
                    rule: _rule);
                var parseMachine = CreateStateMachine(context);

                while (parseMachine.TryAct()) ;

                result = context.Result;
                switch(result)
                {
                    case SuccessResult:
                        return true;

                    default:
                        tokenReader.Reset(position);
                        return false;
                }
            }
            catch (Exception ex)
            {
                #region Error
                _ = tokenReader.Reset(position);
                result = new ErrorResult(position + 1, ex);
                return false;
                #endregion
            }
        }

        private static StateMachine<RecognitionContext> CreateStateMachine(RecognitionContext context)
        {
            var leftDelimiterState = new LambdaState<RecognitionContext>(
                StateNames.LeftDelimiter.ToString(),
                RecognizeLeftDelimiter);

            //var rightDelimiterState = new LambdaState<RecognitionContext>(
            //    StateNames.RightDelimiter.ToString(),
            //    RecognizeRightDelimiter);

            var stringCharacterState = new LambdaState<RecognitionContext>(
                StateNames.StringCharacters.ToString(),
                RecognizeStringCharacters);

            var escapeCharacterState = new LambdaState<RecognitionContext>(
                StateNames.EscapeCharacters.ToString(),
                RecognizeEscapeCharacters);

            return new StateMachine<RecognitionContext>(
                context,
                StateNames.LeftDelimiter.ToString(),
                leftDelimiterState,
                //rightDelimiterState,
                stringCharacterState,
                escapeCharacterState);
        }

        private static string RecognizeLeftDelimiter(RecognitionContext context)
        {
            if (!context.TokenReader.TryNextTokens(
                context.Rule.StartDelimiter.Length,
                out var tokens)
                || !context.Rule.StartDelimiter.Equals(new string(tokens)))
            {
                context.Result = new FailureResult(
                    context.StartPosition + 1,
                    IReason.Of(context.Rule.StartDelimiter));

                return null;
            }

            context.TokenBuffer.Append(tokens);
            return StateNames.StringCharacters.ToString();
        }

        private static string RecognizeStringCharacters(RecognitionContext context)
        {
            if (!context.TokenReader.TryNextToken(out var token))
            {
                context.Result = new FailureResult(
                    context.StartPosition + 1,
                    IReason.Of(context.Rule.ToString()));
                return null;
            }

            _ = context.TokenBuffer.Append(token);

            // escape?
            foreach(var matcher in context.Rule.EscapeMatchers.Values)
            {
                var escapeDelimiterLength = matcher.EscapeDelimiter.Length;
                if (context.TokenBuffer.Length < escapeDelimiterLength)
                    continue;

                var index = context.TokenBuffer.Length - escapeDelimiterLength;
                var potentialEscapeDelimiter = context.TokenBuffer.ToString(
                    index,
                    escapeDelimiterLength);

                if (matcher.EscapeDelimiter.Equals(potentialEscapeDelimiter))
                {
                    context.EscapeMatcher = matcher;
                    context.EscapeDelimiterIndex = index;
                    return StateNames.EscapeCharacters.ToString();
                }
            }

            // end-delimiter?
            var delimiterLength = context.Rule.EndDelimiter.Length;
            if (context.TokenBuffer.Length > delimiterLength)
            {
                var potentialEndDelimiter = context.TokenBuffer.ToString(
                    context.TokenBuffer.Length - delimiterLength,
                    delimiterLength);

                if (context.Rule.EndDelimiter.Equals(potentialEndDelimiter))
                {
                    context.Result = new SuccessResult(
                        context.StartPosition + 1,
                        CST.CSTNode.Of(
                            context.Rule.SymbolName,
                            context.TokenBuffer.ToString()));

                    return null;
                }
            }

            return StateNames.StringCharacters.ToString();
        }

        private static string RecognizeEscapeCharacters(RecognitionContext context)
        {
            if (!context.TokenReader.TryNextToken(out var token))
            {
                // fail
                context.Result = new FailureResult(
                    context.EscapeDelimiterIndex.Value,
                    IReason.Of("{Invalid escape characters}"));
                return null;
            }

            context.TokenBuffer.Append(token);
            var index = context.EscapeDelimiterIndex.Value + context.EscapeMatcher.EscapeDelimiter.Length;
            var escapeSequence = context.TokenBuffer.ToString(index);

            if (context.EscapeMatcher.IsSubMatch(escapeSequence))
                return StateNames.EscapeCharacters.ToString();

            else if (context.EscapeMatcher.IsMatch(escapeSequence[..^1]))
            {
                context.TokenReader.Back();
                context.EscapeMatcher = null;
                context.EscapeDelimiterIndex = null;
                context.TokenBuffer.RemoveLast();
                return StateNames.StringCharacters.ToString();
            }

            else // fail
            {
                context.Result = new FailureResult(
                    context.EscapeDelimiterIndex.Value,
                    IReason.Of("{Invalid escape characters}"));
                return null;
            }
        }


        #region Nested types
        internal enum StateNames
        {
            LeftDelimiter,
            RightDelimiter,
            StringCharacters,
            EscapeCharacters
        }

        internal class LambdaState<TData> : IState<TData> where TData : class
        {
            private readonly Func<TData, string> _act;
            private readonly Action<string, TData> _entering;
            private readonly Action<string, TData> _leaving;

            public string StateName { get; }

            public LambdaState(
                string stateName,
                Func<TData, string> act,
                Action<string, TData> entering = null,
                Action<string, TData> leaving = null)
            {
                StateName = stateName;
                _act = act ?? throw new ArgumentNullException(nameof(act));
                _entering = entering;
                _leaving = leaving;
            }

            public string Act(TData data) => _act.Invoke(data);

            public void Entering(string previousState, TData data) => _entering?.Invoke(previousState, data);

            public void Leaving(string nextState, TData data) => _leaving?.Invoke(nextState, data);
        }

        internal record RecognitionContext
        {
            public StringBuilder TokenBuffer { get; }

            public BufferedTokenReader TokenReader { get; }

            public DelimitedString Rule { get; }

            public int StartPosition { get; }

            public IRecognitionResult Result { get; set; }

            public IEscapeSequenceMatcher EscapeMatcher { get; set; }

            public int? EscapeDelimiterIndex { get; set; }

            public RecognitionContext(
                int startPosition,
                DelimitedString rule,
                StringBuilder stringBuilder,
                BufferedTokenReader tokenReader)
            {
                Rule = rule;
                StartPosition = startPosition;
                TokenBuffer = stringBuilder ?? throw new ArgumentNullException(nameof(stringBuilder));
                TokenReader = tokenReader ?? throw new ArgumentNullException(nameof(tokenReader));
            }
        }
        #endregion
    }
}
