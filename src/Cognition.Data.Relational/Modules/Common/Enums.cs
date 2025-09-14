namespace Cognition.Data.Relational.Modules.Common;

public enum InstructionKind
{
    MissionCritical,
    CoreRules,
    Tool,
    Persona,
    SystemInstruction,
    Other
}

public enum PromptType
{
    None,
    Dynamic,
    SystemMessage,
    SystemInstruction,
    GeneralQuery,
    ImageCreation,
    ImageDescription,
    Vision,
    NaturalQuery,
    GenericUserPrompt
}

public enum ToolParamDirection
{
    Input,
    Output
}

public enum DataSourceType
{
    JsonStore,
    Postgres,
    Vector,
    Blob,
    Other
}

public enum SupportLevel
{
    Full,
    Partial,
    Unsupported
}

public enum ChatRole
{
    System,
    User,
    Assistant
}

