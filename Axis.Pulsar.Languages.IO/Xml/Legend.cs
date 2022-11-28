using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Pulsar.Languages.Xml
{
    public static class Legend
    {
        #region Language Element
        public const string LanguageElement = "language";
        public const string LanguageElement_Root= "root";
        #endregion

        #region Pattern Element
        public const string OpenPatternElement = "open-pattern";
        public const string ClosedPatternElement = "closed-pattern";
        public const string PatternElement_Name = "name";
        public const string PatternElement_Regex = "regex";
        public const string PatternElement_MinMatch = "min-match";
        public const string PatternElement_MaxMatch = "max-match";
        public const string PatternElement_MaxMismatch = "max-mismatch";
        public const string PatternElement_AllowsEmpty = "allows-empty";
        public const string PatternElement_CaseSensitive = "case-sensitive";
        public const string PatternElement_MultiLine = "multi-line";
        public const string PatternElement_SingleLine = "single-line";
        public const string PatternElement_ExplicitCapture = "explicit-capture";
        public const string PatternElement_IgnoreWhitespace = "ignore-whitespace";
        #endregion

        #region Literal Element
        public const string LiteralElement = "literal";
        public const string LiteralElement_Name = "name";
        public const string LiteralElement_Value = "value";
        public const string LiteralElement_CaseSensitive = "case-sensitive";
        #endregion

        #region EOF Element
        public const string EOFlement = "eof";
        #endregion

        #region NonTerminal Element
        public const string NonTerminalElement = "non-terminal";
        public const string NonTerminalElement_Name = "name";
        public const string NonTerminalElement_Threshold = "threshold";
        #endregion

        #region Symbol Element
        public const string SymbolElement = "symbol";
        public const string SymbolElement_Name = "name";
        #endregion

        #region Production Element
        public const string ProductionElement_MinOccurs = "min-occurs";
        public const string ProductionElement_MaxOccurs = "max-occurs";
        #endregion

        #region Set Element
        public const string SetElement = "set";
        public const string SetElement_MinRecognitionCount = "min-recognition-count";
        #endregion

        #region Sequence Element
        public const string SequenceElement = "sequence";
        #endregion

        #region Choice Element
        public const string ChoiceElement = "choice";
        #endregion
    }
}
