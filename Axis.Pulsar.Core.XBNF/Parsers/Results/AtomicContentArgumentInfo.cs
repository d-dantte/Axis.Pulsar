using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Definitions;

namespace Axis.Pulsar.Core.XBNF;

public record AtomicContentArgumentInfo
{
    public AtomicContentDelimiterType ContentType { get; set; }

    public Tokens Content { get; set; }
}
