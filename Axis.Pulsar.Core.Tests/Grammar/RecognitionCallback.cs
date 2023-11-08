﻿using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar
{
    internal delegate bool TryRecognizeNode(
        TokenReader reader,
        ProductionPath? path,
        out IResult<ICSTNode> result);

    internal delegate bool TryRecognizeNodeSequence(
        TokenReader reader,
        ProductionPath? path,
        out IResult<NodeSequence> result);
}