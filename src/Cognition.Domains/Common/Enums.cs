namespace Cognition.Domains.Common;

public enum DomainKind
{
    Business = 0,
    Technical = 1
}

public enum DomainStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

public enum IndexIsolationPolicy
{
    Shared = 0,
    Dedicated = 1,
    Hybrid = 2
}

public enum KnowledgeAssetType
{
    Doc = 0,
    Code = 1,
    Data = 2,
    Decision = 3,
    Log = 4,
    TestResult = 5,
    Plan = 6,
    SchemaSnapshot = 7,
    Other = 8
}

public enum ToolCategory
{
    ReadOnly = 0,
    Compute = 1,
    SideEffect = 2
}

public enum SideEffectProfile
{
    None = 0,
    Low = 1,
    High = 2
}
