using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Cognition.Data.Relational.Modules.Fiction;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public record FictionResponseValidationResult(
    FictionPhase Phase,
    FictionTranscriptValidationStatus Status,
    string Summary,
    string Details,
    IReadOnlyList<string> Errors,
    JToken? ParsedPayload = null,
    IReadOnlyList<string>? SalientTerms = null)
{
    public bool IsValid => Status == FictionTranscriptValidationStatus.Passed;
}
