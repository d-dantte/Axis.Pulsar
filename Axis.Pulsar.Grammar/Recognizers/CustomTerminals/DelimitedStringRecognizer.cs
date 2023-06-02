using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Axis.Pulsar.Grammar.Language.Rules.CustomTerminals.DelimitedString;

namespace Axis.Pulsar.Grammar.Recognizers.CustomTerminals
{
    /// <summary>
    /// Recognizer for the <see cref="DelimitedString"/> rule.
    /// <para>
    /// The parse utilizes a state machine that has 4 states:
    /// <list type="number">
    ///     <item>Start-Delimiter state: recognizes the start delimiter</item>
    ///     <item>String-Character state: recognizes tokens found between the start and end delimiter</item>
    ///     <item>Escape Sequence state: recognizes tokens that are escaped within the start and end delimiter</item>
    ///     <item>End-Delimiter state: recognizes the end delimiter</item>
    /// </list>
    /// 
    /// Note:
    /// <list type="number">
    ///     <item>The IllegalSequence is technically never empty, as it will always contain at least the end-delimiter.</item>
    ///     <item>
    ///         If the LegalSequence is absent, single characters are read from the Reader, and all characters are deemed legal, unless
    ///         found in the IllegalSequence.
    ///     </item>
    ///     <item>
    ///         If escape matchers are present, automatically, the Illegal sequence will contain corresponding escape delimiters.
    ///     </item>
    /// </list>
    /// </para>
    /// </summary>
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
                return result switch
                {
                    SuccessResult => true,

                    _ => tokenReader
                        .Reset(position)
                        .ApplyTo(_ => false)
                };
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
            var startDelimiterState = new LambdaState<RecognitionContext>(
                StateNames.StartDelimiter.ToString(),
                RecognizeStartDelimiter);

            var endDelimiterState = new LambdaState<RecognitionContext>(
                StateNames.EndDelimiter.ToString(),
                RecognizeEndDelimiter);

            var stringCharacterState = new LambdaState<RecognitionContext>(
                StateNames.StringCharacters.ToString(),
                RecognizeStringCharacters);

            var escapeCharacterState = new LambdaState<RecognitionContext>(
                StateNames.EscapeCharacters.ToString(),
                RecognizeEscapeCharacters);

            return new StateMachine<RecognitionContext>(
                context,
                StateNames.StartDelimiter.ToString(),
                startDelimiterState,
                endDelimiterState,
                stringCharacterState,
                escapeCharacterState);
        }

        private static string RecognizeStartDelimiter(RecognitionContext context)
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

        private static string RecognizeEndDelimiter(RecognitionContext context)
        {
            if (!context.TokenReader.TryNextTokens(
                context.Rule.EndDelimiter.Length,
                out var tokens)
                || !context.Rule.EndDelimiter.Equals(new string(tokens)))
            {
                context.Result = new FailureResult(
                    context.StartPosition + 1,
                    IReason.Of(context.Rule.EndDelimiter));
            }
            else
            {
                context.TokenBuffer.Append(tokens);
                context.Result = new SuccessResult(
                    context.StartPosition + 1,
                    CST.CSTNode.Of(
                        context.Rule.SymbolName,
                        context.TokenBuffer.ToString()));
            }

            return null;
        }

        private static string RecognizeStringCharacters(RecognitionContext context)
        {
            while (true)
            {
                // read tokens
                var tokenCount = context.LegalSequences.OrderedSequenceLengths[0];
                if (context.TokenReader.TryNextTokens(tokenCount, out var tokens, false) && tokens.Length <= 0)
                {
                    context.Result = new FailureResult(
                        context.StartPosition + 1,
                        IReason.Of("EOF error. Expected legal/allowed tokens."));
                    return null;
                }

                // legal tokens
                if (!TryVerifyLegalSequence(context, tokens, out int length))
                {
                    context.TokenReader.Back(tokens.Length);
                    return StateNames.EscapeCharacters.ToString();
                }

                // reset any excess tokens
                if (tokens.Length > length)
                {
                    context.TokenReader.Back(tokens.Length - length);
                    tokens = tokens[..length];
                }

                // illegal sequence?
                if (TryVerifyIllegalSequence(context, tokens, out _))
                {
                    context.TokenReader.Back(tokens.Length);
                    return StateNames.EscapeCharacters.ToString();
                }

                // finally, add the tokens
                _ = context.TokenBuffer.Append(tokens);
            }
        }

        private static string RecognizeEscapeCharacters(RecognitionContext context)
        {
            // no escape matchers?
            if (context.Rule.EscapeMatchers.Count <= 0)
                return StateNames.EndDelimiter.ToString();

            // read escape
            var delimLengths = context.Rule.EscapeMatchers.Keys
                .Select(delim => delim.Length)
                .OrderByDescending(l => l)
                .Distinct()
                .ToArray();

            if (context.TokenReader.TryNextTokens(delimLengths[0], out var tokens, false) && tokens.Length <= 0)
            {
                context.Result = new FailureResult(
                    context.TokenReader.Position + 1,
                    IReason.Of("EOF error. Expected escape tokens."));
                return null;
            }

            var matcher = delimLengths
                .Select(length => context.Rule.EscapeMatchers
                    .TryGetValue(new string(tokens[..length]), out var _matcher)
                    ? _matcher : null)
                .FirstOrDefault(m => m is not null);

            if (matcher is null)
            {
                context.TokenReader.Back(tokens.Length);
                return StateNames.EndDelimiter.ToString();
            }

            var escapeBuffer = new StringBuilder();
            while (true)
            {
                if (!context.TokenReader.TryNextToken(out var token))
                {
                    // fail
                    context.Result = new FailureResult(
                        context.TokenReader.Position + 1,
                        IReason.Of("EOF error. Expected escape tokens."));
                    return null;
                }

                _ = escapeBuffer.Append(token);
                var escapeSequence = escapeBuffer.ToString();

                if (matcher.IsSubMatch(escapeSequence))
                    continue;

                else if (matcher.IsMatch(escapeSequence.AsSpan()[..^1]))
                {
                    escapeBuffer.RemoveLast();
                    context.TokenReader.Back();
                    context.TokenBuffer.Append(tokens).Append(escapeBuffer);
                    return StateNames.StringCharacters.ToString();
                }

                else // fail
                {
                    context.Result = new FailureResult(
                        context.TokenReader.Position + 1,
                        IReason.Of($"Invalid escape characters: {escapeSequence}"));
                    return null;
                }
            }
        }

        /// <summary>
        /// verifies that the given <paramref name="tokens"/> contains at least one subset that is present in the LegalSequence set.
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="tokens">the tokens</param>
        /// <param name="validLength">the length of the valid token subset</param>
        /// <returns>true if a subset is found in LegalSequence, false otherwise</returns>
        private static bool TryVerifyLegalSequence(
            RecognitionContext context,
            char[] tokens,
            out int validLength)
        {
            foreach(var length in context.LegalSequences.OrderedSequenceLengths)
            {
                if (context.LegalSequences.Matches(new string(tokens[..length])))
                {
                    validLength = length;
                    return true;
                }
            }

            validLength = 0;
            return false;
        }

        /// <summary>
        /// verifies that the given concatenation of the <c>context.TokenBuffer</c> and the<paramref name="tokens"/> contains at least one
        /// right-most subset that is present in the IllegalSequence set.
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="tokens">the tokens</param>
        /// <returns>true if no illegal subsets are found, false otherwise</returns>
        private static bool TryVerifyIllegalSequence(
            RecognitionContext context,
            char[] tokens,
            out int illegalLength)
        {
            var tbuff = context.TokenBuffer.ToString() + new string(tokens);

            foreach(var length in context.IllegalSequences.OrderedSequenceLengths)
            {
                if (tbuff.Length < length)
                    continue;

                var index = tbuff.Length - length;
                var potentialIllegalSequence = tbuff[index..];

                if (context.IllegalSequences.Matches(potentialIllegalSequence))
                {
                    illegalLength = length;
                    return true;
                }
            }

            illegalLength = -1;
            return false;
        }


        #region Nested types
        internal enum StateNames
        {
            StartDelimiter,
            EndDelimiter,
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

            public SequenceInfo LegalSequences { get; }

            public SequenceInfo IllegalSequences { get; }

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

                IllegalSequences = new SequenceInfo(rule.IllegalSequences
                    .Append(rule.EndDelimiter)
                    .Concat(rule.EscapeMatchers.Values.Select(m => m.EscapeDelimiter))
                    .ToArray());

                // null represents "any character", meaning all characters are valid
                LegalSequences = new SequenceInfo(rule.LegalSequences.Length == 0
                    ? new string[] { null }
                    : rule.LegalSequences);
            }
        }

        /// <summary>
        /// Specialized structure for recognizing sequences of tokens. If this sequence contains a null in it's sequence set,
        /// it means it will recognize any single character.
        /// </summary>
        internal record SequenceInfo
        {
            public IReadOnlySet<string> Sequences { get; }

            public int[] OrderedSequenceLengths { get; }

            public bool HasSequences => Sequences.Count > 0;

            public SequenceInfo(string[] sequences)
            {
                Sequences = new HashSet<string>(sequences);
                OrderedSequenceLengths = sequences
                    .Select(seq => seq?.Length ?? 1)
                    .OrderByDescending(l => l)
                    .Distinct()
                    .ToArray();
            }

            public bool Matches(string sequence)
            {
                if (sequence.Length == 1 && Sequences.Contains(null))
                    return true;

                return Sequences.Contains(sequence);
            }
        }
        #endregion
    }
}
