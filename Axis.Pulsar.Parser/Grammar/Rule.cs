using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// Parse rule
    /// 
    /// NOTE: flirting with the idea of adding a Func delegate that represents semantic validation of the symbol that a rule parses. 
    /// This will be given the symbol, which should have access to ancestors; the validation logic can then use the entire syntax tree to do
    /// some semantic validations on the symbol. This validation logic can be injected while the rules are built.
    /// </summary>
    public abstract class Rule
    {
        public Cardinality Cardinality { get; }

        public Rule(Cardinality cardinality = default)
        {
            Cardinality = cardinality;
        }
    }
}
