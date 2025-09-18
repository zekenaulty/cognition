using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Personas;

public class Persona : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = false;
    public PersonaType Type { get; set; } = PersonaType.Assistant;
    public OwnedBy OwnedBy { get; set; } = OwnedBy.System;
    public string Gender { get; set; } = string.Empty;
    public string Essence { get; set; } = string.Empty;
    public string Beliefs { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string CommunicationStyle { get; set; } = string.Empty;
    public string EmotionalDrivers { get; set; } = string.Empty;
    public string Voice { get; set; } = string.Empty;

    public string[]? SignatureTraits { get; set; }
    public string[]? NarrativeThemes { get; set; }
    public string[]? DomainExpertise { get; set; }

    public Guid[]? KnownPersonas { get; set; }

    public List<PersonaLink> OutboundLinks { get; set; } = [];
    public List<PersonaLink> InboundLinks { get; set; } = [];
}

public enum PersonaType
{
    User,
    Assistant
}

public enum OwnedBy
{
    System,
    User,
    Persona
}
