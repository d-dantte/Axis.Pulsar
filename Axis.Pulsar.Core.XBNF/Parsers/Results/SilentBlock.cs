﻿using System.Collections.Immutable;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.XBNF;

public class SilentBlock
{
    public ImmutableArray<ISilentElement> Elements{get;}

    public SilentBlock(IEnumerable<ISilentElement> elements)
    {
        Elements = elements
            .ThrowIfNull(new ArgumentNullException(nameof(elements)))
            .ThrowIfAny(
                t => t is null,
                new ArgumentException("Invalid element: null"))
            .ToImmutableArray();
    }

    public static SilentBlock Of(
        IEnumerable<ISilentElement> elements)
        => new(elements);
}