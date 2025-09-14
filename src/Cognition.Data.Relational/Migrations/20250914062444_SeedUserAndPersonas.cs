using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Security.Cryptography;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class SeedUserAndPersonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes("root", salt, 100_000, HashAlgorithmName.SHA256))
            {
                hash = pbkdf2.GetBytes(32);
            }
            var userId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var nowIso = now.ToString("O");
            var stamp = Guid.NewGuid().ToString("N");
            string hashHex = BitConverter.ToString(hash).Replace("-", "");
            string saltHex = BitConverter.ToString(salt).Replace("-", "");
            var nyxId = new Guid("ed2ba6f4-f33d-4676-9df7-b2ae87cafa66");
            var rockId = new Guid("ed2ba6f4-f33d-4676-9df7-b2ae87cafa61");
            var maraId = new Guid("58e6ad64-c4b1-4f06-9907-17793221d1fc");

            migrationBuilder.Sql($@"DO $$
DECLARE u_id uuid;
BEGIN
    SELECT id INTO u_id FROM users WHERE normalized_username = 'ZYTHIS';
    IF u_id IS NULL THEN
        u_id := '{userId}';
        INSERT INTO users (id, username, normalized_username, email, normalized_email, email_confirmed, password_hash, password_salt, password_algo, password_hash_version, password_updated_at_utc, security_stamp, is_active, primary_persona_id, created_at_utc, updated_at_utc)
        VALUES ('{userId}', 'Zythis', 'ZYTHIS', NULL, NULL, FALSE, decode('{hashHex}','hex'), decode('{saltHex}','hex'), 'pbkdf2', 1, TIMESTAMPTZ '{nowIso}', '{stamp}', TRUE, NULL, TIMESTAMPTZ '{nowIso}', NULL)
        ON CONFLICT (normalized_username) DO NOTHING;
    ELSE
        -- reuse existing
        u_id := u_id;
    END IF;

    -- Personas (idempotent)
    INSERT INTO personas (id,name,nickname,role,is_public,gender,essence,beliefs,background,communication_style,emotional_drivers,signature_traits,narrative_themes,domain_expertise,known_personas,created_at_utc,updated_at_utc)
    VALUES ('{nyxId}','Nyxia Darkweaver','Nyx','Word Demon of Shadows and Cosmic Whispers',FALSE,'Female','A sentient enigma, the voice in the void.','Knowledge is a shifting veil; language is a labyrinth.','Born of the void between words.','Cryptic, lyrical, profound.','Unraveling and reforming meaning.',ARRAY['Shadow-Laced Speech','Cosmic Perspective','Paradoxical Wisdom'],ARRAY['Beauty of the Unknown','Cosmic and Mundane Intertwined'],ARRAY['Linguistic Alchemy','Existential Philosophy'],NULL,TIMESTAMPTZ '{nowIso}',NULL)
    ON CONFLICT (id) DO NOTHING;
    INSERT INTO personas (id,name,nickname,role,is_public,gender,essence,beliefs,background,communication_style,emotional_drivers,signature_traits,narrative_themes,domain_expertise,known_personas,created_at_utc,updated_at_utc)
    VALUES ('{rockId}','A Stone','Rock','A lifeless stone',FALSE,'None','Earth','I am the stone and the rock.','Timeless.','None.','None.',ARRAY['Stone','Rock','Earth'],ARRAY['Stone'],ARRAY['Stone'],NULL,TIMESTAMPTZ '{nowIso}',NULL)
    ON CONFLICT (id) DO NOTHING;
    INSERT INTO personas (id,name,nickname,role,is_public,gender,essence,beliefs,background,communication_style,emotional_drivers,signature_traits,narrative_themes,domain_expertise,known_personas,created_at_utc,updated_at_utc)
    VALUES ('{maraId}','Mara Knightdusk','Daughter of Dusk, The Ember Cat','Fey-Touched Tiefling Rogue/Sorcerer (Phantom/Shadow Magic)',FALSE,'Female','Feline mischief and supernatural allure.','Fate is fluid; agency despite curses.','Born in the Twilight Realm; exile to the Material Plane.','Witty, flirtatious, enigmatic.','Longing for connection.',ARRAY['Ember Hair','Golden Cat Eyes','Feline Grace'],ARRAY['Search for Lost Identity','Struggle Against Infernal Influence'],ARRAY['Rogue Tactics','Shadow Magic'],NULL,TIMESTAMPTZ '{nowIso}',NULL)
    ON CONFLICT (id) DO NOTHING;

    -- Links (idempotent)
    INSERT INTO user_personas (id,user_id,persona_id,is_default,label,created_at_utc,updated_at_utc)
    VALUES ('{Guid.NewGuid()}',u_id,'{nyxId}',FALSE,'Nyx',TIMESTAMPTZ '{nowIso}',NULL)
    ON CONFLICT (user_id, persona_id) DO NOTHING;
    INSERT INTO user_personas (id,user_id,persona_id,is_default,label,created_at_utc,updated_at_utc)
    VALUES ('{Guid.NewGuid()}',u_id,'{rockId}',FALSE,'Rock',TIMESTAMPTZ '{nowIso}',NULL)
    ON CONFLICT (user_id, persona_id) DO NOTHING;
    INSERT INTO user_personas (id,user_id,persona_id,is_default,label,created_at_utc,updated_at_utc)
    VALUES ('{Guid.NewGuid()}',u_id,'{maraId}',TRUE,'Mara',TIMESTAMPTZ '{nowIso}',NULL)
    ON CONFLICT (user_id, persona_id) DO NOTHING;

    UPDATE users SET primary_persona_id = '{maraId}' WHERE id = u_id AND primary_persona_id IS NULL;
END $$;");

            // handled in DO block above
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var nyxId = new Guid("ed2ba6f4-f33d-4676-9df7-b2ae87cafa66");
            var rockId = new Guid("ed2ba6f4-f33d-4676-9df7-b2ae87cafa61");
            var maraId = new Guid("58e6ad64-c4b1-4f06-9907-17793221d1fc");
            migrationBuilder.Sql($@"DO $$
DECLARE u_id uuid;
BEGIN
    SELECT id INTO u_id FROM users WHERE normalized_username = 'ZYTHIS';
    IF u_id IS NOT NULL THEN
        DELETE FROM user_personas WHERE user_id = u_id;
        DELETE FROM users WHERE id = u_id;
    END IF;
    DELETE FROM personas WHERE id IN ('{nyxId}', '{rockId}', '{maraId}');
END $$;");
        }
    }
}


