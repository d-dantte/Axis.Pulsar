namespace Axis.Pulsar.Importer.Common.Xml.Legend
{
    public static class Enumerations
    {
        #region Pattern Element
        public static readonly string PatterElement_Name = "name";
        public static readonly string PatterElement_Regex = "regex";
        public static readonly string PatterElement_MinMatch = "min-match";
        public static readonly string PatterElement_MaxMatch = "max-match";
        public static readonly string PatterElement_CaseSensitive = "case-sensitive";
        #endregion

        #region String Element
        public static readonly string StringElement_Name = "name";
        public static readonly string StringElement_Value = "value";
        public static readonly string StringElement_CaseSensitive = "case-sensitive";
        #endregion

        #region NonTerminal Element
        public static readonly string NonTerminalElement_Name = "name";
        #endregion

        #region Production Element
        public static readonly string ProductionElement_MinOccurs = "min-occurs";
        public static readonly string ProductionElement_MaxOccurs = "max-occurs";
        #endregion
    }
}
