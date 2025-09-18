using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Personas;

namespace Cognition.Data.Relational.Modules.Users;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }

    // Password storage (hash + salt); hashing performed at the application layer
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public string PasswordAlgo { get; set; } = "argon2id"; // e.g., argon2id, pbkdf2, bcrypt
    public int PasswordHashVersion { get; set; } = 1;
    public DateTime? PasswordUpdatedAtUtc { get; set; }

    public string? SecurityStamp { get; set; } // random string to invalidate sessions when changed
    public bool IsActive { get; set; } = true;

    public Guid? PrimaryPersonaId { get; set; }
    public Persona? PrimaryPersona { get; set; }

    public List<UserPersonas> UserPersonas { get; set; } = [];
}

public enum UserRole
{
    Viewer,
    User,
    Administrator
}
