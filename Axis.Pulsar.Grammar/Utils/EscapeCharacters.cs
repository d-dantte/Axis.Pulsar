using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Pulsar.Grammar.Utils
{
    /// <summary>
    /// see https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#string-escape-sequences
    /// </summary>
    [Flags]
    public enum EscapeCharacters
    {
        All = 4095,
        None = 0,

        NewLine = 1,
        SingleQuote = 2,
        DoubleQuote = 4,
        BackSlash = 8,
        Null = 16,
        Backspace = 32,
        FormFeed = 64,
        CarriageReturn = 128,
        Alert = 256,
        HorizontalTab = 512,
        VerticalTab = 1024,
        UTF16 = 2048
    }
}
