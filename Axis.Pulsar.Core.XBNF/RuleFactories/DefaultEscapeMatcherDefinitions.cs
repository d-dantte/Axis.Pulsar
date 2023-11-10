using Axis.Pulsar.Core.Utils.EscapeMatchers;
using Axis.Pulsar.Core.XBNF.Definitions;

namespace Axis.Pulsar.Core.XBNF;

public static class DefaultEscapeMatcherDefinitions
{
    /// <summary>
    /// 
    /// </summary>
    public static readonly EscapeMatcherDefinition BSolBasic = EscapeMatcherDefinition.Of(
        "BSolBasic",
        new BSolBasicEscapeMatcher());
        
    /// <summary>
    /// 
    /// </summary>
    public static readonly EscapeMatcherDefinition BSolAscii = EscapeMatcherDefinition.Of(
        "BSolAscii",
        new BSolAsciiEscapeMatcher());

    /// <summary>
    /// 
    /// </summary>
    public static readonly EscapeMatcherDefinition BSolUTF = EscapeMatcherDefinition.Of(
        "BSolUTF",
        new BSolUTFEscapeMatcher());
}
