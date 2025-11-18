using System.Linq;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Data.Relational.Modules.Fiction;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cognition.Clients.Tests.Tools.Fiction;

public class LifecyclePayloadParserTests
{
    [Fact]
    public void ExtractCharacters_reads_core_and_supporting_cast()
    {
        const string payload = """
        {
          "coreCast": [
            {
              "name": "Captain Mira",
              "role": "protagonist",
              "track": true,
              "importance": "high",
              "summary": "Relentless captain balancing duty and loyalty.",
              "continuityHooks": ["Debt to Admiral Kerr"]
            }
          ],
          "supportingCast": [
            {
              "name": "Analyst Brek",
              "role": "support",
              "track": true,
              "importance": "medium",
              "summary": "Nervous analyst who keeps the roster honest."
            },
            {
              "name": "Skip Me",
              "role": "cameo",
              "track": false,
              "importance": "low",
              "summary": "Should not be promoted."
            }
          ]
        }
        """;

        var token = JToken.Parse(payload);
        var characters = LifecyclePayloadParser.ExtractCharacters(token);

        characters.Should().HaveCount(2);
        characters.Select(c => c.Name).Should().BeEquivalentTo("Captain Mira", "Analyst Brek");
        characters.All(c => c.Track).Should().BeTrue();
        characters.First().ContinuityHooks.Should().NotBeNull();
        characters.First().ContinuityHooks!.Should().ContainSingle("Debt to Admiral Kerr");
    }

    [Fact]
    public void ExtractLoreRequirements_reads_extended_schema()
    {
        const string payload = """
        {
          "loreNeeds": [
            {
              "title": "Fracture Gate Protocol",
              "requirementSlug": "fracture-gate-protocol",
              "status": "ready",
              "description": "Document how the gate behaves.",
              "requiredFor": ["chapter-blueprint", "chapter-scroll"],
              "notes": "Track sabotage cases",
              "track": true
            }
          ]
        }
        """;

        var token = JToken.Parse(payload);
        var lore = LifecyclePayloadParser.ExtractLoreRequirements(token);

        lore.Should().HaveCount(1);
        lore[0].Title.Should().Be("Fracture Gate Protocol");
        lore[0].RequirementSlug.Should().Be("fracture-gate-protocol");
        lore[0].Status.Should().Be(FictionLoreRequirementStatus.Ready);
        lore[0].Metadata.Should().NotBeNull();
        lore[0].Metadata!.Should().ContainKey("raw");
    }
}
