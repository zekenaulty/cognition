using System.ComponentModel.DataAnnotations;
using Cognition.Data.Relational.Modules.Personas;

namespace Cognition.Api.Models.Personas;

public sealed class PersonaUpdateRequest
{
    [StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Name must contain non-whitespace characters when provided.")]
    public string? Name { get; init; }
    [StringLength(128)]
    public string? Nickname { get; init; }
    [StringLength(256)]
    public string? Role { get; init; }
    [StringLength(64)]
    public string? Gender { get; init; }
    [StringLength(4000)]
    public string? Essence { get; init; }
    [StringLength(4000)]
    public string? Beliefs { get; init; }
    [StringLength(4000)]
    public string? Background { get; init; }
    [StringLength(4000)]
    public string? CommunicationStyle { get; init; }
    [StringLength(4000)]
    public string? EmotionalDrivers { get; init; }
    public string[]? SignatureTraits { get; init; }
    public string[]? NarrativeThemes { get; init; }
    public string[]? DomainExpertise { get; init; }
    public bool? IsPublic { get; init; }
    [StringLength(256)]
    public string? Voice { get; init; }
    public PersonaType? Type { get; init; }

    public PersonaUpdateRequest() { }

    public PersonaUpdateRequest(
        string? name,
        string? nickname,
        string? role,
        string? gender,
        string? essence,
        string? beliefs,
        string? background,
        string? communicationStyle,
        string? emotionalDrivers,
        string[]? signatureTraits,
        string[]? narrativeThemes,
        string[]? domainExpertise,
        bool? isPublic,
        string? voice,
        PersonaType? type)
    {
        Name = name;
        Nickname = nickname;
        Role = role;
        Gender = gender;
        Essence = essence;
        Beliefs = beliefs;
        Background = background;
        CommunicationStyle = communicationStyle;
        EmotionalDrivers = emotionalDrivers;
        SignatureTraits = signatureTraits;
        NarrativeThemes = narrativeThemes;
        DomainExpertise = domainExpertise;
        IsPublic = isPublic;
        Voice = voice;
        Type = type;
    }
}
