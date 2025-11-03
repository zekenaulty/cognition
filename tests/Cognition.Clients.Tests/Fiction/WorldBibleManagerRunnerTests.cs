using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Testing.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cognition.Clients.Tests.Fiction;

public class WorldBibleManagerRunnerTests
{
    [Fact]
    public async Task ExecuteCoreAsync_UpsertsWorldBibleEntriesAndVersions()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new WorldBibleRunnerTestDbContext(options);

        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Starlit Atlas" };
        var planId = Guid.NewGuid();
        var plan = new FictionPlan
        {
            Id = planId,
            FictionProjectId = project.Id,
            FictionProject = project,
            Name = "Atlas Draft",
            PrimaryBranchSlug = "main",
            Status = FictionPlanStatus.InProgress
        };

        var worldBibleId = Guid.NewGuid();
        var worldBible = new FictionWorldBible
        {
            Id = worldBibleId,
            FictionPlanId = planId,
            FictionPlan = plan,
            Domain = "core",
            BranchSlug = "main"
        };

        var persona = new Persona { Id = Guid.NewGuid(), Name = "Lore Weaver" };
        var agent = new Agent { Id = Guid.NewGuid(), PersonaId = persona.Id, Persona = persona };
        var conversation = new Conversation { Id = Guid.NewGuid(), AgentId = agent.Id, Agent = agent, Title = "Lore Sync" };

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        db.FictionWorldBibles.Add(worldBible);
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        var responses = new Queue<string>(new[] { BuildWorldBiblePayload("Active"), BuildWorldBiblePayload("Updated") });
        var agentService = Substitute.For<IAgentService>();
        agentService.ChatAsync(
                conversation.Id,
                agent.Id,
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult((responses.Dequeue(), Guid.NewGuid())));

        var runner = new WorldBibleManagerRunner(db, agentService, NullLogger<WorldBibleManagerRunner>.Instance, ScopePathBuilderTestHelper.CreateBuilder());

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var baseMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerId"] = providerId.ToString(),
            ["modelId"] = modelId.ToString(),
            ["worldBibleId"] = worldBibleId.ToString(),
            ["backlogItemId"] = "map-world-seeds",
            ["iterationIndex"] = "1"
        };

        var context = new FictionPhaseExecutionContext(
            planId,
            agent.Id,
            conversation.Id,
            "main",
            IterationIndex: 1,
            Metadata: baseMetadata);

        await runner.RunAsync(context, CancellationToken.None);

        var entriesAfterFirstRun = await db.FictionWorldBibleEntries
            .Where(e => e.FictionWorldBibleId == worldBibleId)
            .OrderBy(e => e.Sequence)
            .ToListAsync();

        entriesAfterFirstRun.Should().HaveCount(3);
        entriesAfterFirstRun.Should().OnlyContain(e => e.Version == 1 && e.IsActive);
        entriesAfterFirstRun.Select(e => e.ChangeType).Should().OnlyContain(t => t == FictionWorldBibleChangeType.Seed);

        var metadataSecondRun = new Dictionary<string, string>(baseMetadata)
        {
            ["iterationIndex"] = "2"
        };
        var contextSecondRun = context with { IterationIndex = 2, Metadata = metadataSecondRun };

        await runner.RunAsync(contextSecondRun, CancellationToken.None);

        var allEntries = await db.FictionWorldBibleEntries
            .Where(e => e.FictionWorldBibleId == worldBibleId)
            .OrderBy(e => e.Sequence)
            .ToListAsync();

        allEntries.Should().HaveCount(6);
        allEntries.Select(e => e.Sequence).Should().BeInAscendingOrder();

        var characterEntries = allEntries.Where(e => e.EntrySlug.StartsWith("characters:", StringComparison.OrdinalIgnoreCase)).OrderBy(e => e.Version).ToList();
        characterEntries.Should().HaveCount(2);
        characterEntries[0].IsActive.Should().BeFalse();
        characterEntries[1].IsActive.Should().BeTrue();
        characterEntries[1].Version.Should().Be(2);
        characterEntries[1].ChangeType.Should().Be(FictionWorldBibleChangeType.Update);
        characterEntries[1].DerivedFromEntryId.Should().Be(characterEntries[0].Id);
        characterEntries[1].Content.Status.Should().Be("Updated");
        characterEntries[1].Content.IterationIndex.Should().Be(2);

        var latestNotes = characterEntries[1].Content.ContinuityNotes ?? Array.Empty<string>();
        latestNotes.Should().Contain("Ensure prophecy threads remain consistent.");
    }

    private static string BuildWorldBiblePayload(string status)
    {
        var payload = new
        {
            characters = new[]
            {
                new
                {
                    name = "Arin Vale",
                    summary = "Rebellious heir bound to the glass citadel.",
                    status,
                    continuityNotes = new[]
                    {
                        "Maintain scar detail across chapters.",
                        "Ensure prophecy threads remain consistent."
                    }
                }
            },
            locations = new[]
            {
                new
                {
                    name = "Glass Citadel",
                    summary = "Shifting fortress overlooking the astral sea.",
                    status,
                    continuityNotes = new[]
                    {
                        "Map crystal growth from previous chapter."
                    }
                }
            },
            systems = new[]
            {
                new
                {
                    name = "Arcane Loom",
                    summary = "Weaves narrative threads into tangible memory crystals.",
                    status,
                    continuityNotes = new[]
                    {
                        "Reference iteration outputs for divergence tracking."
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private sealed class WorldBibleRunnerTestDbContext : CognitionDbContext
    {
        public WorldBibleRunnerTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var allowed = new HashSet<Type>
            {
                typeof(FictionProject),
                typeof(FictionPlan),
                typeof(FictionPlanPass),
                typeof(FictionWorldBible),
                typeof(FictionWorldBibleEntry),
                typeof(Persona),
                typeof(Agent),
                typeof(Conversation)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }

            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Passes);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.ChapterBlueprints);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Checkpoints);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Backlog);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Transcripts);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.StoryMetrics);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.WorldBibles);
            modelBuilder.Entity<FictionPlanPass>().Ignore(x => x.Metadata);

            modelBuilder.Entity<Conversation>().Ignore(x => x.Participants);
            modelBuilder.Entity<Conversation>().Ignore(x => x.Messages);
            modelBuilder.Entity<Conversation>().Ignore(x => x.Summaries);
            modelBuilder.Entity<Conversation>().Ignore(x => x.Metadata);

            modelBuilder.Entity<Agent>().Ignore(x => x.ToolBindings);
            modelBuilder.Entity<Agent>().Ignore(x => x.State);
            modelBuilder.Entity<Persona>().Ignore(x => x.SignatureTraits);
            modelBuilder.Entity<Persona>().Ignore(x => x.NarrativeThemes);
            modelBuilder.Entity<Persona>().Ignore(x => x.DomainExpertise);
            modelBuilder.Entity<Persona>().Ignore(x => x.KnownPersonas);

            modelBuilder.Entity<FictionWorldBibleEntry>()
                .OwnsOne(x => x.Content, owned =>
                {
                    owned.ToJson("content");
                });
        }
    }
}
