using System;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public class FictionResponseValidationException : Exception
{
    public FictionResponseValidationException(FictionResponseValidationResult result)
        : base(result.Details)
    {
        Result = result;
    }

    public FictionResponseValidationResult Result { get; }
}
