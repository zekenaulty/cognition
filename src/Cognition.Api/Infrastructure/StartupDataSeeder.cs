using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Infrastructure;

public static class StartupDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CognitionDbContext>();
        // Apply EF Core migrations to create/update schema
        await db.Database.MigrateAsync();

        var user = await EnsureUserAsync(db, logger, username: "Zythis", password: "root");

        // Backfill: ensure primary persona types are correct (User) and owner set
        var users = await db.Users.AsNoTracking().ToListAsync();
        foreach (var u in users)
        {
            if (u.PrimaryPersonaId.HasValue)
            {
                var p = await db.Personas.FirstOrDefaultAsync(x => x.Id == u.PrimaryPersonaId.Value);
                if (p != null)
                {
                    bool changed = false;
                    if (p.Type != PersonaType.User)
                    {
                        p.Type = PersonaType.User;
                        changed = true;
                    }
                    if (p.OwnerUserId != u.Id)
                    {
                        p.OwnerUserId = u.Id;
                        changed = true;
                    }
                    if (changed)
                    {
                        p.UpdatedAtUtc = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                        logger.LogInformation("Backfilled persona {PersonaId} for user {UserId} as Type=User with owner.", p.Id, u.Id);
                    }
                }
            }
        }

        // Seed default client profiles
        await EnsureClientProfilesAsync(db, logger);

        // Seed personas from reference files if present
        var contentRoot = scope.ServiceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath;
        var candidates = new[]
        {
            Path.Combine(contentRoot, "..", "..", "reference", "ai.console", "personal.agents"),
            Path.Combine(contentRoot, "reference", "ai.console", "personal.agents"),
            Path.Combine(AppContext.BaseDirectory, "reference", "ai.console", "personal.agents"),
        };
        var folder = candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists) ?? string.Empty;
        if (string.IsNullOrEmpty(folder))
        {
            logger.LogWarning("Persona seed folder not found. Skipping persona seeding.");
            return;
        }

        var files = new[]
        {
            "ed2ba6f4-f33d-4676-9df7-b2ae87cafa66.json",
            "ed2ba6f4-f33d-4676-9df7-b2ae87cafa61.json",
            "58e6ad64-c4b1-4f06-9907-17793221d1fc.json"
        }
        .Select(f => Path.Combine(folder, f))
        .Where(File.Exists)
        .ToList();

        Guid? primaryPersonaId = null;
        foreach (var path in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                Guid id = Guid.TryParse(root.GetProperty("Id").GetString(), out var gid) ? gid : Guid.NewGuid();
                string nickname = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
                string role = root.TryGetProperty("role", out var roleProp) ? roleProp.GetString() ?? string.Empty : string.Empty;
                string personaBlob = root.TryGetProperty("properties", out var props) && props.TryGetProperty("persona", out var personaProp) && personaProp.TryGetProperty("value", out var valueProp)
                    ? valueProp.GetString() ?? string.Empty
                    : string.Empty;

                var parsed = ParsePersonaText(personaBlob);
                var fullName = string.IsNullOrWhiteSpace(parsed.Name) ? nickname : parsed.Name;
                var existing = await db.Personas.FirstOrDefaultAsync(p => p.Id == id);
                if (existing == null)
                {
                    var p = new Persona
                    {
                        Id = id,
                        Name = fullName,
                        Nickname = string.IsNullOrWhiteSpace(parsed.Nickname) ? nickname : parsed.Nickname,
                        Role = string.IsNullOrWhiteSpace(parsed.Role) ? role : parsed.Role,
                        Gender = parsed.Gender ?? string.Empty,
                        Essence = parsed.Essence ?? string.Empty,
                        Beliefs = parsed.Beliefs ?? string.Empty,
                        Background = parsed.Background ?? string.Empty,
                        CommunicationStyle = parsed.CommunicationStyle ?? string.Empty,
                        EmotionalDrivers = parsed.EmotionalDrivers ?? string.Empty,
                        SignatureTraits = parsed.SignatureTraits,
                        NarrativeThemes = parsed.NarrativeThemes,
                        DomainExpertise = parsed.DomainExpertise,
                        IsPublic = false
                    };
                    db.Personas.Add(p);
                    await db.SaveChangesAsync();
                    existing = p;
                    logger.LogInformation("Seeded persona {Name} ({Id}) from {Path}", p.Name, p.Id, path);
                }

                // Link to user
                if (!await db.UserPersonas.AnyAsync(up => up.UserId == user.Id && up.PersonaId == existing.Id))
                {
                    db.UserPersonas.Add(new UserPersona { UserId = user.Id, PersonaId = existing.Id, IsDefault = false, Label = existing.Nickname });
                    await db.SaveChangesAsync();
                }

                if (primaryPersonaId == null) primaryPersonaId = existing.Id;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed persona from {Path}", path);
            }
        }

        // Set primary persona if available
        if (primaryPersonaId != null)
        {
            var u = await db.Users.FirstAsync(u => u.Id == user.Id);
            if (u.PrimaryPersonaId == null)
            {
                u.PrimaryPersonaId = primaryPersonaId;
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task EnsureClientProfilesAsync(CognitionDbContext db, ILogger logger)
    {
        // OpenAI default
        var openAi = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "OpenAI");
        if (openAi != null)
        {
            var model = await db.Models.AsNoTracking().FirstOrDefaultAsync(m => m.ProviderId == openAi.Id && m.Name == "gpt-4o");
            if (!await db.ClientProfiles.AnyAsync(cp => cp.Name == "Default OpenAI"))
            {
                db.ClientProfiles.Add(new Cognition.Data.Relational.Modules.LLM.ClientProfile
                {
                    Name = "Default OpenAI",
                    ProviderId = openAi.Id,
                    ModelId = model?.Id,
                    MaxTokens = 8192,
                    Temperature = 0.7,
                    TopP = 0.95,
                    PresencePenalty = 0.2,
                    FrequencyPenalty = 0.1,
                    Stream = true,
                    LoggingEnabled = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded ClientProfile: Default OpenAI");
            }
        }

        // Gemini default
        var gemini = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "Gemini");
        if (gemini != null)
        {
            var model = await db.Models.AsNoTracking().FirstOrDefaultAsync(m => m.ProviderId == gemini.Id && (m.Name == "gemini-2.5-flash" || m.Name == "gemini-2.0-flash"));
            if (!await db.ClientProfiles.AnyAsync(cp => cp.Name == "Default Gemini"))
            {
                db.ClientProfiles.Add(new Cognition.Data.Relational.Modules.LLM.ClientProfile
                {
                    Name = "Default Gemini",
                    ProviderId = gemini.Id,
                    ModelId = model?.Id,
                    MaxTokens = 8192,
                    Temperature = 0.7,
                    TopP = 0.95,
                    PresencePenalty = 0.0,
                    FrequencyPenalty = 0.0,
                    Stream = true,
                    LoggingEnabled = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded ClientProfile: Default Gemini");
            }
        }

        // Ollama default
        var ollama = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "Ollama");
        if (ollama != null)
        {
            var model = await db.Models.AsNoTracking().FirstOrDefaultAsync(m => m.ProviderId == ollama.Id && m.Name == "llama3.2:3b");
            if (!await db.ClientProfiles.AnyAsync(cp => cp.Name == "Default Ollama"))
            {
                db.ClientProfiles.Add(new Cognition.Data.Relational.Modules.LLM.ClientProfile
                {
                    Name = "Default Ollama",
                    ProviderId = ollama.Id,
                    ModelId = model?.Id,
                    MaxTokens = 8192,
                    Temperature = 0.5,
                    TopP = 0.9,
                    PresencePenalty = 0.0,
                    FrequencyPenalty = 0.0,
                    Stream = false,
                    LoggingEnabled = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded ClientProfile: Default Ollama");
            }
        }
    }
    private static async Task<User> EnsureUserAsync(CognitionDbContext db, ILogger logger, string username, string password)
    {
        var norm = username.ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == norm);
        if (user != null)
        {
            if (user.PrimaryPersonaId == null)
            {
                var persona = new Persona
                {
                    Name = user.Username,
                    Nickname = user.Username,
                    Role = "User",
                    Type = PersonaType.User,
                    OwnerUserId = user.Id,
                    IsPublic = false,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Personas.Add(persona);
                await db.SaveChangesAsync();
                user.PrimaryPersonaId = persona.Id;
                db.UserPersonas.Add(new UserPersona { UserId = user.Id, PersonaId = persona.Id, IsDefault = true, Label = persona.Nickname, CreatedAtUtc = DateTime.UtcNow });
                await db.SaveChangesAsync();
            }
            return user;
        }

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = HashPasswordPbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        user = new User
        {
            Username = username,
            NormalizedUsername = norm,
            EmailConfirmed = false,
            PasswordAlgo = "pbkdf2",
            PasswordHashVersion = 1,
            PasswordSalt = salt,
            PasswordHash = hash,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var p = new Persona
        {
            Name = username,
            Nickname = username,
            Role = "User",
            Type = PersonaType.User,
            OwnerUserId = user.Id,
            IsPublic = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Personas.Add(p);
        await db.SaveChangesAsync();
        user.PrimaryPersonaId = p.Id;
        db.UserPersonas.Add(new UserPersona { UserId = user.Id, PersonaId = p.Id, IsDefault = true, Label = p.Nickname, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default user {Username} with persona {Persona}", username, p.Id);
        return user;
    }

    private static byte[] HashPasswordPbkdf2(string password, byte[] salt, int iterations, HashAlgorithmName algo, int bytes)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, algo);
        return pbkdf2.GetBytes(bytes);
    }

    private static readonly Regex FieldRegex = new(@"^(?<k>[^:]+):\s*(?<v>.+)$", RegexOptions.Compiled);

    private static (string Name, string Nickname, string Role, string? Gender, string? Essence, string? Beliefs, string? Background, string? CommunicationStyle, string? EmotionalDrivers, string[]? SignatureTraits, string[]? NarrativeThemes, string[]? DomainExpertise) ParsePersonaText(string blob)
    {
        if (string.IsNullOrWhiteSpace(blob)) return ("", "", "", null, null, null, null, null, null, null, null, null);
        string name = ""; string nickname = ""; string role = ""; string? gender = null; string? essence = null; string? beliefs = null; string? background = null; string? comms = null; string? emotions = null;
        var sig = new List<string>(); var themes = new List<string>(); var domain = new List<string>();
        var lines = blob.Replace("\r", "").Split('\n');
        string section = "";
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            // section headers
            if (line.StartsWith("SignatureTraits")) { section = "sig"; continue; }
            if (line.StartsWith("NarrativeThemes")) { section = "themes"; continue; }
            if (line.StartsWith("DomainExpertise")) { section = "domain"; continue; }
            if (line.StartsWith("EmotionalDrivers")) { section = ""; emotions = line.Contains(":") ? line[(line.IndexOf(':')+1)..].Trim() : emotions; continue; }

            var m = FieldRegex.Match(line);
            if (m.Success)
            {
                var k = m.Groups["k"].Value.Trim();
                var v = m.Groups["v"].Value.Trim();
                switch (k.ToLowerInvariant())
                {
                    case "name": name = v; break;
                    case "nickname": nickname = v; break;
                    case "role":
                    case "role =": role = v; break;
                    case "gender": gender = v; break;
                    case "essence": essence = v; break;
                    case "beliefs": beliefs = v; break;
                    case "background": background = v; break;
                    case "communicationstyle": comms = v; break;
                }
                continue;
            }

            // list items under sections
            if (section == "sig" || section == "themes" || section == "domain")
            {
                // Strip leading markers/emoji and trailing commas
                var val = line.Trim().TrimStart('-', '•', '*').Trim().TrimEnd(',');
                if (!string.IsNullOrWhiteSpace(val))
                {
                    if (section == "sig") sig.Add(val);
                    else if (section == "themes") themes.Add(val);
                    else domain.Add(val);
                }
            }
        }
        return (name, nickname, role, gender, essence, beliefs, background, comms, emotions, sig.Count>0? sig.ToArray(): null, themes.Count>0? themes.ToArray(): null, domain.Count>0? domain.ToArray(): null);
    }
}
