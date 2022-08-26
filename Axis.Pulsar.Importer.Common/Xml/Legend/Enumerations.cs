using System.Xml.Linq;

namespace Axis.Pulsar.Importer.Common.Xml.Legend
{
    public static class Enumerations
    {
        #region Pattern Element
        public static readonly string PatternElement_Name = "name";
        public static readonly string PatternElement_Regex = "regex";
        public static readonly string PatternElement_MinMatch = "min-match";
        public static readonly string PatternElement_MaxMatch = "max-match";
        public static readonly string PatternElement_CaseSensitive = "case-sensitive";
        #endregion

        #region Literal Element
        public static readonly string LiteralElement_Name = "name";
        public static readonly string LiteralElement_Value = "value";
        public static readonly string LiteralElement_CaseSensitive = "case-sensitive";
        #endregion

        #region NonTerminal Element
        public static readonly string NonTerminalElement_Name = "name";
        public static readonly string NonTerminalElement_Threshold = "threshold";
        #endregion

        #region Symbol Element
        public static readonly string SymbolElement_Name = "name";
        #endregion

        #region Production Element
        public static readonly string ProductionElement_MinOccurs = "min-occurs";
        public static readonly string ProductionElement_MaxOccurs = "max-occurs";
        #endregion
    }
}
