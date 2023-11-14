﻿using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IRecognizer<TResult>
    {
        /// <summary>
        /// Attempt to recognize tokens according to the logic represented by the implementing rule.
        /// </summary>
        /// <param name="reader">the reader from which tokens are read</param>
        /// <param name="productionPath">the logical symbol-path of the parent rule, or null if this is the root rule</param>
        /// <param name="result">the result of the recognition</param>
        /// <returns>True if this rule successfully recognized tokens from the <paramref name="reader"/>, false otherwise</returns>
        bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            ILanguageContext context,
            out IResult<TResult> result);
    }
}