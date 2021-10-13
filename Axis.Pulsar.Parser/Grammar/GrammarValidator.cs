using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.Grammar
{
    public static class GrammarValidator
    {
        ///// <summary>
        ///// Validates that the entire language tree is correct, and also makes sure that the rule given is the root rule.
        ///// </summary>
        ///// <param name="root"></param>
        ///// <param name="error"></param>
        ///// <returns></returns>
        //public static bool TryValidateRoot(IRule root, out Error error)
        //{
        //    if (!root.IsRoot)
        //    {
        //        error = new Error("Invalid root");
        //        return false;
        //    }
        //    else
        //    {
        //        return TryValidate(root, out error);
        //    }
        //}

        ///// <summary>
        ///// ensures that the language tree is valid
        ///// </summary>
        ///// <param name="root"></param>
        ///// <param name="error"></param>
        ///// <returns></returns>
        //public static bool TryValidate(IRule root, out Error error)
        //{
        //    throw new NotImplementedException();
        //}


        //public class Error
        //{
        //    private readonly List<string> _errors = new();

        //    public string[] Errors => _errors.ToArray();


        //    public Error(params string[] errors)
        //    {
        //        _errors.AddRange(errors ?? Array.Empty<string>());
        //    }

        //    public Error AddError(string error)
        //    {
        //        _errors.Add(error.ThrowIf(
        //            string.IsNullOrWhiteSpace,
        //            n => new ArgumentException("Invalid error message")));

        //        return this;
        //    }
        //}
    }

}
