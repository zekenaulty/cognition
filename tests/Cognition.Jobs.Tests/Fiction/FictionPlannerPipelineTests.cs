using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Clients.LLM;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Tools.Planning.Fiction;
using Cognition.Clients.Tools.Fiction.Authoring;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Jobs;
using Cognition.Testing.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Cognition.Jobs.Tests.Fiction;

public class FictionPlannerPipelineTests
{
    [Fact]
    public async Task Pipeline_CompletesBacklogItems()
    {
        var agentService = new PipelineAgentService(new[]
        {
            Responses.Vision,
            Responses.Iterative,
            Responses.ChapterBlueprint,
            Responses.Scroll,
            Responses.Scene
        });

        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new PipelineTestDbContext(options);
        var graph = await SeedPlanGraphAsync(db);

        var telemetry = new NullPlannerTelemetry();
        var transcriptStore = new NullPlannerTranscriptStore();
        var templateRepo = new InMemoryTemplateRepository(new Dictionary<string, string>
        {
            ["planner.fiction.vision"] = Templates.Vision,
            ["planner.fiction.iterative"] = Templates.Iterative,
            ["planner.fiction.chapterArchitect"] = Templates.ChapterArchitect,
            ["planner.fiction.scrollRefiner"] = Templates.ScrollRefiner,
            ["planner.fiction.sceneWeaver"] = Templates.Scene
        });
        var critiqueOptions = Options.Create(new PlannerCritiqueOptions());
        var scopePathBuilder = ScopePathBuilderTestHelper.CreateBuilder();

        var services = new ServiceCollection()
            .AddSingleton<IAgentService>(agentService)
            .BuildServiceProvider();

        var lifecycle = Substitute.For<ICharacterLifecycleService>();
        lifecycle.ProcessAsync(Arg.Any<CharacterLifecycleRequest>(), Arg.Any<CancellationToken>())
            .Returns(CharacterLifecycleResult.Empty);
        var authorRegistry = Substitute.For<IAuthorPersonaRegistry>();
        var authorContext = new AuthorPersonaContext(
            Guid.NewGuid(),
            "Author Persona",
            "Author should write in clipped, tense prose.",
            new[] { "Remember the ally's oath." },
            new[] { "Lore: Whisperglass protocol remains secret." });
        authorRegistry.GetForPlanAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthorPersonaContext?>(authorContext));

        var visionTool = new VisionPlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepo,
            critiqueOptions,
            scopePathBuilder);

        var iterativeTool = new IterativePlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepo,
            critiqueOptions,
            scopePathBuilder);

        var chapterTool = new ChapterArchitectPlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepo,
            critiqueOptions,
            scopePathBuilder);

        var scrollTool = new ScrollRefinerPlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepo,
            critiqueOptions,
            scopePathBuilder);

        var sceneTool = new SceneWeaverPlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepo,
            critiqueOptions,
            scopePathBuilder);

        var runners = new IFictionPhaseRunner[]
        {
            new VisionPlannerRunner(db, agentService, services, lifecycle, visionTool, NullLogger<VisionPlannerRunner>.Instance, scopePathBuilder),
            new IterativePlannerRunner(db, agentService, services, iterativeTool, NullLogger<IterativePlannerRunner>.Instance, scopePathBuilder),
            new ChapterArchitectRunner(db, agentService, services, chapterTool, NullLogger<ChapterArchitectRunner>.Instance, scopePathBuilder),
            new ScrollRefinerRunner(db, agentService, services, lifecycle, authorRegistry, scrollTool, NullLogger<ScrollRefinerRunner>.Instance, scopePathBuilder),
            new SceneWeaverRunner(db, agentService, services, lifecycle, authorRegistry, sceneTool, NullLogger<SceneWeaverRunner>.Instance, scopePathBuilder)
        };

        var jobs = new FictionWeaverJobs(
            db,
            runners,
            Substitute.For<Rebus.Bus.IBus>(),
            Substitute.For<IPlanProgressNotifier>(),
            new WorkflowEventLogger(db, enabled: false),
            Substitute.For<IFictionBacklogScheduler>(),
            scopePathBuilder,
            NullLogger<FictionWeaverJobs>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var conversationPlanId = Guid.NewGuid();
        var baseMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["conversationPlanId"] = conversationPlanId.ToString()
        };

        await jobs.RunVisionPlannerAsync(
            graph.PlanId,
            graph.AgentId,
            graph.ConversationId,
            providerId,
            modelId,
            metadata: baseMetadata,
            cancellationToken: CancellationToken.None);

        await jobs.RunIterativePlannerAsync(
            graph.PlanId,
            graph.AgentId,
            graph.ConversationId,
            iterationIndex: 1,
            providerId,
            modelId,
            metadata: baseMetadata,
            cancellationToken: CancellationToken.None);

        Dictionary<string, string> BuildPhaseMetadata(string backlogId)
        {
            return new Dictionary<string, string>(baseMetadata, StringComparer.OrdinalIgnoreCase)
            {
                ["backlogItemId"] = backlogId,
                ["taskId"] = Guid.NewGuid().ToString()
            };
        }

        var chapterMetadata = BuildPhaseMetadata("outline-core-conflicts");
        await jobs.RunChapterArchitectAsync(
            graph.PlanId,
            graph.AgentId,
            graph.ConversationId,
            graph.BlueprintId,
            providerId,
            modelId,
            metadata: chapterMetadata,
            cancellationToken: CancellationToken.None);

        var scrollMetadata = BuildPhaseMetadata("refine-scroll");
        await jobs.RunScrollRefinerAsync(
            graph.PlanId,
            graph.AgentId,
            graph.ConversationId,
            graph.ScrollId,
            providerId,
            modelId,
            metadata: scrollMetadata,
            cancellationToken: CancellationToken.None);

        var sceneMetadata = BuildPhaseMetadata("draft-scene");
        var sceneResult = await jobs.RunSceneWeaverAsync(
            graph.PlanId,
            graph.AgentId,
            graph.ConversationId,
            graph.SceneId,
            providerId,
            modelId,
            metadata: sceneMetadata,
            cancellationToken: CancellationToken.None);

        sceneResult.Transcripts.Should().NotBeNull();
        sceneResult.Transcripts.Should().HaveCountGreaterThan(0);

        agentService.TotalCalls.Should().Be(5);
        agentService.RemainingResponses.Should().Be(0);
        await authorRegistry.Received(2).AppendMemoryAsync(
            Arg.Any<Guid>(),
            Arg.Any<AuthorPersonaMemoryEntry>(),
            Arg.Any<CancellationToken>());

        var backlog = await db.FictionPlanBacklogItems
            .Where(x => x.FictionPlanId == graph.PlanId)
            .ToListAsync();

        backlog.Should().HaveCount(3);
        backlog.Single(x => x.BacklogId == "outline-core-conflicts").Status.Should().Be(FictionPlanBacklogStatus.Complete);
        backlog.Single(x => x.BacklogId == "refine-scroll").Status.Should().Be(FictionPlanBacklogStatus.Complete);
        backlog.Single(x => x.BacklogId == "draft-scene").Status.Should().Be(FictionPlanBacklogStatus.Complete);

        var plan = await db.FictionPlans.SingleAsync(x => x.Id == graph.PlanId);
        plan.Status.Should().Be(FictionPlanStatus.InProgress);

        var checkpoints = await db.FictionPlanCheckpoints
            .Where(x => x.FictionPlanId == graph.PlanId)
            .ToListAsync();
        checkpoints.Should().HaveCount(5);
        checkpoints.Should().OnlyContain(cp => cp.Status == FictionPlanCheckpointStatus.Complete);
        checkpoints.Should().OnlyContain(cp => cp.CompletedCount == cp.TargetCount);

        var transcripts = await db.FictionPlanTranscripts
            .Where(x => x.FictionPlanId == graph.PlanId)
            .ToListAsync();
        transcripts.Should().NotBeEmpty();
        transcripts.Count(t => t.Metadata is { Count: > 0 } && t.Metadata.ContainsKey("backlogItemId"))
            .Should().BeGreaterOrEqualTo(3);
        transcripts
            .Where(t => t.Metadata?.ContainsKey("backlogItemId") == true)
            .Select(t => t.Metadata!["backlogItemId"]?.ToString())
            .Should().Contain(new[] { "outline-core-conflicts", "refine-scroll", "draft-scene" });
        transcripts.Should().Contain(t => t.Phase.StartsWith(FictionPhase.SceneWeaver.ToString(), StringComparison.OrdinalIgnoreCase), "SceneWeaver should persist a transcript entry.");
        var sceneTranscript = transcripts.First(t => t.Phase.StartsWith(FictionPhase.SceneWeaver.ToString(), StringComparison.OrdinalIgnoreCase));
        sceneTranscript.Metadata.Should().NotBeNull();
        sceneTranscript.Metadata!.Should().ContainKey("plannerOutcome").WhoseValue.Should().Be("Success");
        sceneTranscript.Metadata!.Should().ContainKey("sceneSlug").WhoseValue.Should().Be("scene-1");
        sceneTranscript.FictionChapterSceneId.Should().Be(graph.SceneId);
    }

    [Fact]
    public async Task ScrollRefinerRunner_blocks_when_lore_requirement_missing()
    {
        var agentService = new PipelineAgentService(Array.Empty<string>());

        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new PipelineTestDbContext(options);
        var graph = await SeedPlanGraphAsync(db);

        db.FictionLoreRequirements.Add(new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = graph.PlanId,
            RequirementSlug = "whisperglass-protocol",
            Title = "Whisperglass Protocol",
            Description = "Needs canon before scroll drafting.",
            Status = FictionLoreRequirementStatus.Planned,
            ChapterScrollId = graph.ScrollId
        });
        await db.SaveChangesAsync();

        var telemetry = new NullPlannerTelemetry();
        var transcriptStore = new NullPlannerTranscriptStore();
        var templateRepo = new InMemoryTemplateRepository(new Dictionary<string, string>
        {
            ["planner.fiction.scrollRefiner"] = Templates.ScrollRefiner
        });
        var critiqueOptions = Options.Create(new PlannerCritiqueOptions());
        var scopePathBuilder = ScopePathBuilderTestHelper.CreateBuilder();
        var services = new ServiceCollection()
            .AddSingleton<IAgentService>(agentService)
            .BuildServiceProvider();

        var lifecycle = Substitute.For<ICharacterLifecycleService>();
        lifecycle.ProcessAsync(Arg.Any<CharacterLifecycleRequest>(), Arg.Any<CancellationToken>())
            .Returns(CharacterLifecycleResult.Empty);
        var authorRegistry = Substitute.For<IAuthorPersonaRegistry>();

        var scrollTool = new ScrollRefinerPlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepo,
            critiqueOptions,
            scopePathBuilder);

        var runners = new IFictionPhaseRunner[]
        {
            new ScrollRefinerRunner(db, agentService, services, lifecycle, authorRegistry, scrollTool, NullLogger<ScrollRefinerRunner>.Instance, scopePathBuilder)
        };

        var jobs = new FictionWeaverJobs(
            db,
            runners,
            Substitute.For<Rebus.Bus.IBus>(),
            Substitute.For<IPlanProgressNotifier>(),
            new WorkflowEventLogger(db, enabled: false),
            Substitute.For<IFictionBacklogScheduler>(),
            scopePathBuilder,
            NullLogger<FictionWeaverJobs>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["conversationPlanId"] = Guid.NewGuid().ToString(),
            ["taskId"] = Guid.NewGuid().ToString(),
            ["backlogItemId"] = "refine-scroll"
        };

        var result = await jobs.RunScrollRefinerAsync(
            graph.PlanId,
            graph.AgentId,
            graph.ConversationId,
            graph.ScrollId,
            providerId,
            modelId,
            metadata: metadata,
            cancellationToken: CancellationToken.None);

        result.Status.Should().Be(FictionPhaseStatus.Blocked);
        result.Data.Should().ContainKey("blockedLoreRequirements");
        agentService.TotalCalls.Should().Be(0);
        await authorRegistry.DidNotReceiveWithAnyArgs().AppendMemoryAsync(default, default!, default);
    }

    [Fact]
    public async Task ScrollRefinerRunner_blocks_on_validation_failure()
    {
        var agentService = new PipelineAgentService(new[]
        {
            Responses.Vision,
            Responses.Iterative,
            Responses.ChapterBlueprint,
            "not valid json"
        });

        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new PipelineTestDbContext(options);
        var graph = await SeedPlanGraphAsync(db);

        var telemetry = new NullPlannerTelemetry();
        var transcriptStore = new NullPlannerTranscriptStore();
        var templateRepo = new InMemoryTemplateRepository(new Dictionary<string, string>
        {
            ["planner.fiction.vision"] = Templates.Vision,
            ["planner.fiction.iterative"] = Templates.Iterative,
            ["planner.fiction.chapterArchitect"] = Templates.ChapterArchitect,
            ["planner.fiction.scrollRefiner"] = Templates.ScrollRefiner
        });
        var critiqueOptions = Options.Create(new PlannerCritiqueOptions());
        var scopePathBuilder = ScopePathBuilderTestHelper.CreateBuilder();

        var services = new ServiceCollection()
            .AddSingleton<IAgentService>(agentService)
            .BuildServiceProvider();

        var lifecycle = Substitute.For<ICharacterLifecycleService>();
        lifecycle.ProcessAsync(Arg.Any<CharacterLifecycleRequest>(), Arg.Any<CancellationToken>())
            .Returns(CharacterLifecycleResult.Empty);
        var authorRegistry = Substitute.For<IAuthorPersonaRegistry>();
        authorRegistry.GetForPlanAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthorPersonaContext?>(null));

        var runners = new IFictionPhaseRunner[]
        {
            new VisionPlannerRunner(db, agentService, services, lifecycle, new VisionPlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<VisionPlannerRunner>.Instance, scopePathBuilder),
            new IterativePlannerRunner(db, agentService, services, new IterativePlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<IterativePlannerRunner>.Instance, scopePathBuilder),
            new ChapterArchitectRunner(db, agentService, services, new ChapterArchitectPlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<ChapterArchitectRunner>.Instance, scopePathBuilder),
            new ScrollRefinerRunner(db, agentService, services, lifecycle, authorRegistry, new ScrollRefinerPlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<ScrollRefinerRunner>.Instance, scopePathBuilder)
        };

        var jobs = new FictionWeaverJobs(
            db,
            runners,
            Substitute.For<Rebus.Bus.IBus>(),
            Substitute.For<IPlanProgressNotifier>(),
            new WorkflowEventLogger(db, enabled: false),
            Substitute.For<IFictionBacklogScheduler>(),
            scopePathBuilder,
            NullLogger<FictionWeaverJobs>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var conversationPlanId = Guid.NewGuid();
        var baseMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["conversationPlanId"] = conversationPlanId.ToString()
        };

        await jobs.RunVisionPlannerAsync(graph.PlanId, graph.AgentId, graph.ConversationId, providerId, modelId, metadata: baseMetadata, cancellationToken: CancellationToken.None);
        await jobs.RunIterativePlannerAsync(graph.PlanId, graph.AgentId, graph.ConversationId, iterationIndex: 1, providerId, modelId, metadata: baseMetadata, cancellationToken: CancellationToken.None);

        var chapterMetadata = new Dictionary<string, string>(baseMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["backlogItemId"] = "outline-core-conflicts",
            ["taskId"] = Guid.NewGuid().ToString()
        };
        await jobs.RunChapterArchitectAsync(graph.PlanId, graph.AgentId, graph.ConversationId, graph.BlueprintId, providerId, modelId, metadata: chapterMetadata, cancellationToken: CancellationToken.None);

        var scrollMetadata = new Dictionary<string, string>(baseMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["backlogItemId"] = "refine-scroll",
            ["taskId"] = Guid.NewGuid().ToString()
        };

        var result = await jobs.RunScrollRefinerAsync(graph.PlanId, graph.AgentId, graph.ConversationId, graph.ScrollId, providerId, modelId, metadata: scrollMetadata, cancellationToken: CancellationToken.None);
        result.Status.Should().Be(FictionPhaseStatus.Blocked);
    }

    [Fact]
    public async Task SceneWeaverRunner_blocks_on_validation_failure()
    {
        var agentService = new PipelineAgentService(new[]
        {
            Responses.Vision,
            Responses.Iterative,
            Responses.ChapterBlueprint,
            Responses.Scroll,
            "This response omits all salient scene details."
        });

        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new PipelineTestDbContext(options);
        var graph = await SeedPlanGraphAsync(db);

        var telemetry = new NullPlannerTelemetry();
        var transcriptStore = new NullPlannerTranscriptStore();
        var templateRepo = new InMemoryTemplateRepository(new Dictionary<string, string>
        {
            ["planner.fiction.vision"] = Templates.Vision,
            ["planner.fiction.iterative"] = Templates.Iterative,
            ["planner.fiction.chapterArchitect"] = Templates.ChapterArchitect,
            ["planner.fiction.scrollRefiner"] = Templates.ScrollRefiner,
            ["planner.fiction.sceneWeaver"] = Templates.Scene
        });
        var critiqueOptions = Options.Create(new PlannerCritiqueOptions());
        var scopePathBuilder = ScopePathBuilderTestHelper.CreateBuilder();

        var services = new ServiceCollection()
            .AddSingleton<IAgentService>(agentService)
            .BuildServiceProvider();

        var lifecycle = Substitute.For<ICharacterLifecycleService>();
        lifecycle.ProcessAsync(Arg.Any<CharacterLifecycleRequest>(), Arg.Any<CancellationToken>())
            .Returns(CharacterLifecycleResult.Empty);
        var authorRegistry = Substitute.For<IAuthorPersonaRegistry>();
        authorRegistry.GetForPlanAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthorPersonaContext?>(null));

        var runners = new IFictionPhaseRunner[]
        {
            new VisionPlannerRunner(db, agentService, services, lifecycle, new VisionPlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<VisionPlannerRunner>.Instance, scopePathBuilder),
            new IterativePlannerRunner(db, agentService, services, new IterativePlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<IterativePlannerRunner>.Instance, scopePathBuilder),
            new ChapterArchitectRunner(db, agentService, services, new ChapterArchitectPlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<ChapterArchitectRunner>.Instance, scopePathBuilder),
            new ScrollRefinerRunner(db, agentService, services, lifecycle, authorRegistry, new ScrollRefinerPlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<ScrollRefinerRunner>.Instance, scopePathBuilder),
            new SceneWeaverRunner(db, agentService, services, lifecycle, authorRegistry, new SceneWeaverPlannerTool(agentService, NullLoggerFactory.Instance, telemetry, transcriptStore, templateRepo, critiqueOptions, scopePathBuilder), NullLogger<SceneWeaverRunner>.Instance, scopePathBuilder)
        };

        var jobs = new FictionWeaverJobs(
            db,
            runners,
            Substitute.For<Rebus.Bus.IBus>(),
            Substitute.For<IPlanProgressNotifier>(),
            new WorkflowEventLogger(db, enabled: false),
            Substitute.For<IFictionBacklogScheduler>(),
            scopePathBuilder,
            NullLogger<FictionWeaverJobs>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var conversationPlanId = Guid.NewGuid();
        var baseMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["conversationPlanId"] = conversationPlanId.ToString()
        };

        await jobs.RunVisionPlannerAsync(graph.PlanId, graph.AgentId, graph.ConversationId, providerId, modelId, metadata: baseMetadata, cancellationToken: CancellationToken.None);
        await jobs.RunIterativePlannerAsync(graph.PlanId, graph.AgentId, graph.ConversationId, iterationIndex: 1, providerId, modelId, metadata: baseMetadata, cancellationToken: CancellationToken.None);

        var chapterMetadata = new Dictionary<string, string>(baseMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["backlogItemId"] = "outline-core-conflicts",
            ["taskId"] = Guid.NewGuid().ToString()
        };
        await jobs.RunChapterArchitectAsync(graph.PlanId, graph.AgentId, graph.ConversationId, graph.BlueprintId, providerId, modelId, metadata: chapterMetadata, cancellationToken: CancellationToken.None);

        var scrollMetadata = new Dictionary<string, string>(baseMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["backlogItemId"] = "refine-scroll",
            ["taskId"] = Guid.NewGuid().ToString()
        };
        await jobs.RunScrollRefinerAsync(graph.PlanId, graph.AgentId, graph.ConversationId, graph.ScrollId, providerId, modelId, metadata: scrollMetadata, cancellationToken: CancellationToken.None);

        var sceneMetadata = new Dictionary<string, string>(baseMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["backlogItemId"] = "draft-scene",
            ["taskId"] = Guid.NewGuid().ToString()
        };

        var result = await jobs.RunSceneWeaverAsync(graph.PlanId, graph.AgentId, graph.ConversationId, graph.SceneId, providerId, modelId, metadata: sceneMetadata, cancellationToken: CancellationToken.None);
        result.Status.Should().Be(FictionPhaseStatus.Blocked);
    }

    private static async Task<PlanGraph> SeedPlanGraphAsync(CognitionDbContext db)
    {
        var projectId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var personaId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var blueprintId = Guid.NewGuid();
        var scrollId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var sceneId = Guid.NewGuid();

        var project = new FictionProject
        {
            Id = projectId,
            Title = "Pipeline Project",
            Logline = "A test project."
        };

        var plan = new FictionPlan
        {
            Id = planId,
            FictionProjectId = projectId,
            FictionProject = project,
            Name = "Pipeline Plan",
            Description = "Plan description for pipeline test.",
            PrimaryBranchSlug = "main",
            Status = FictionPlanStatus.Draft
        };
        project.FictionPlans.Add(plan);

        var persona = new Persona
        {
            Id = personaId,
            Name = "Planner Persona",
            Nickname = "Planner",
            Role = "Planner",
            OwnedBy = OwnedBy.System,
            Type = PersonaType.Assistant,
            IsPublic = true
        };

        var agent = new Agent
        {
            Id = agentId,
            PersonaId = personaId,
            Persona = persona,
            Version = Guid.NewGuid()
        };

        var conversation = new Conversation
        {
            Id = conversationId,
            AgentId = agentId,
            Agent = agent,
            Title = "Pipeline Conversation"
        };

        var blueprint = new FictionChapterBlueprint
        {
            Id = blueprintId,
            FictionPlanId = planId,
            FictionPlan = plan,
            ChapterIndex = 1,
            ChapterSlug = "chapter-1",
            Title = "Chapter One",
            Synopsis = "Existing synopsis for the chapter.",
            Structure = new Dictionary<string, object?>()
        };
        plan.ChapterBlueprints.Add(blueprint);

        var scroll = new FictionChapterScroll
        {
            Id = scrollId,
            FictionChapterBlueprintId = blueprintId,
            FictionChapterBlueprint = blueprint,
            VersionIndex = 1,
            ScrollSlug = "scroll-1",
            Title = "Scroll One",
            Synopsis = "Current scroll synopsis.",
            Metadata = new Dictionary<string, object?>()
        };
        blueprint.Scrolls.Add(scroll);

        var section = new FictionChapterSection
        {
            Id = sectionId,
            FictionChapterScrollId = scrollId,
            FictionChapterScroll = scroll,
            SectionIndex = 1,
            SectionSlug = "section-1",
            Title = "Opening Section",
            Description = "Existing section description."
        };
        scroll.Sections.Add(section);

        var scene = new FictionChapterScene
        {
            Id = sceneId,
            FictionChapterSectionId = sectionId,
            FictionChapterSection = section,
            SceneIndex = 1,
            SceneSlug = "scene-1",
            Title = "Opening Scene",
            Description = "Existing scene description."
        };
        section.Scenes.Add(scene);

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        db.Conversations.Add(conversation);
        db.FictionChapterBlueprints.Add(blueprint);
        db.FictionChapterScrolls.Add(scroll);
        db.FictionChapterSections.Add(section);
        db.FictionChapterScenes.Add(scene);
        await db.SaveChangesAsync();

        return new PlanGraph(planId, conversationId, agentId, blueprintId, scrollId, sceneId);
    }

    private sealed record PlanGraph(Guid PlanId, Guid ConversationId, Guid AgentId, Guid BlueprintId, Guid ScrollId, Guid SceneId);

    private sealed class PipelineTestDbContext : CognitionDbContext
    {
        public PipelineTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var allowed = new HashSet<Type>
            {
                typeof(Persona),
                typeof(Agent),
                typeof(AgentToolBinding),
                typeof(Conversation),
                typeof(ConversationParticipant),
                typeof(ConversationMessage),
                typeof(ConversationPlan),
                typeof(ConversationTask),
                typeof(FictionProject),
                typeof(FictionPlan),
                typeof(FictionPlanPass),
                typeof(FictionPlanBacklogItem),
                typeof(FictionPlanCheckpoint),
                typeof(FictionPlanTranscript),
                typeof(FictionChapterBlueprint),
                typeof(FictionChapterScroll),
                typeof(FictionChapterSection),
                typeof(FictionChapterScene),
                typeof(FictionLoreRequirement)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }

            modelBuilder.Entity<Agent>().Ignore(a => a.State);
            modelBuilder.Entity<AgentToolBinding>().Ignore(b => b.Config);
            modelBuilder.Entity<Conversation>().Ignore(c => c.Metadata);
            modelBuilder.Entity<FictionPlanPass>().Ignore(p => p.Metadata);
            modelBuilder.Entity<FictionChapterBlueprint>().Ignore(b => b.Structure);
            modelBuilder.Entity<FictionChapterScroll>().Ignore(s => s.Metadata);
            modelBuilder.Entity<FictionChapterSection>().Ignore(s => s.Metadata);
            modelBuilder.Entity<FictionChapterScene>().Ignore(s => s.Metadata);
            modelBuilder.Entity<FictionPlanCheckpoint>().Ignore(c => c.Progress);
            modelBuilder.Entity<FictionPlanTranscript>().Ignore(t => t.Metadata);
        }
    }

    private sealed class PipelineAgentService : IAgentService
    {
        private readonly Queue<string> _responses;

        public PipelineAgentService(IEnumerable<string> responses)
        {
            _responses = new Queue<string>(responses);
        }

        public int TotalCalls { get; private set; }
        public int RemainingResponses => _responses.Count;

        public Task<string> AskAsync(Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<string> AskWithToolsAsync(Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<string> AskWithPlanAsync(Guid conversationId, Guid agentId, Guid providerId, Guid? modelId, string input, int minSteps, int maxSteps, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<(string Reply, Guid MessageId)> ChatAsync(Guid conversationId, Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No scripted responses remain for ChatAsync.");
            }

            TotalCalls++;
            return Task.FromResult((_responses.Dequeue(), Guid.NewGuid()));
        }
    }

    private sealed class NullPlannerTelemetry : IPlannerTelemetry
    {
        public Task PlanCompletedAsync(PlannerTelemetryContext context, PlannerResult result, CancellationToken ct) => Task.CompletedTask;
        public Task PlanFailedAsync(PlannerTelemetryContext context, Exception exception, CancellationToken ct) => Task.CompletedTask;
        public Task PlanStartedAsync(PlannerTelemetryContext context, CancellationToken ct) => Task.CompletedTask;
        public Task PlanThrottledAsync(PlannerTelemetryContext context, PlannerQuotaDecision decision, CancellationToken ct) => Task.CompletedTask;
        public Task PlanRejectedAsync(PlannerTelemetryContext context, PlannerQuotaDecision decision, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class InMemoryTemplateRepository : IPlannerTemplateRepository
    {
        private readonly IReadOnlyDictionary<string, string> _templates;

        public InMemoryTemplateRepository(IReadOnlyDictionary<string, string> templates)
        {
            _templates = templates;
        }

        public Task<string?> GetTemplateAsync(string templateId, CancellationToken ct)
        {
            _templates.TryGetValue(templateId, out var template);
            return Task.FromResult<string?>(template);
        }
    }

    private static class Templates
    {
        public const string Vision =
            "Project {{projectTitle}}\\nDescription: {{description}}\\nLogline: {{logline}}\\nRespond with the JSON payload defined in the system requirements (authorSummary, bookGoals, coreCast, supportingCast, loreNeeds, planningBacklog, openQuestions, worldSeeds).";

        public const string Iterative =
            "Plan {{planName}} iteration {{iterationIndex}} on branch {{branch}}. Summaries: {{existingPasses}}. Respond with the required JSON arrays.";

        public const string ChapterArchitect =
            "You are the chapter architect for {{planName}} on branch {{branch}}. Description: {{description}}. Passes: {{passesSummary}}. Existing blueprint: {{existingBlueprintSummary}}. Respond with the required JSON.";

        public const string ScrollRefiner =
            "You are refining the chapter scroll for {{planName}} on branch {{branch}}. Blueprint: {{blueprintSynopsis}}. Structure: {{blueprintStructure}}. Existing scroll: {{scrollSummary}}. Respond with the required JSON.";

        public const string Scene =
            "You are writing the full narrative scene \"{{sceneTitle}}\" (slug {{sceneSlug}}) for branch \"{{branch}}\" in project \"{{planName}}\".\\n\\nScene description:\\n{{sceneDescription}}\\n\\nSection context:\\n{{sectionSummary}}\\n\\nScroll synopsis:\\n{{scrollSynopsis}}\\n\\nBlueprint structure (JSON):\\n{{blueprintStructure}}\\n\\nScene metadata (JSON):\\n{{sceneMetadata}}\\n\\nWrite the complete scene in rich Markdown. Include dialogue, action, and interiority. Target 900-1300 words. Return Markdown only.";
    }

    private static class Responses
    {
        public const string Vision =
            """
            {
              "authorSummary": "An energetic author voice with cinematic pacing.",
              "bookGoals": ["Deliver a tense mystery arc."],
              "coreCast": [
                {
                  "name": "Detective Mara Iles",
                  "role": "protagonist",
                  "track": true,
                  "importance": "high",
                  "summary": "Instinctive investigator who masks anxiety with sardonic wit.",
                  "continuityHooks": ["Owes intel favors to the Black Market Council."]
                }
              ],
              "supportingCast": [
                {
                  "name": "Analyst Brek",
                  "role": "support",
                  "track": true,
                  "importance": "medium",
                  "summary": "Former rival analyst now acting as reluctant partner.",
                  "notes": "Keep their code phrase 'whisperglass'."
                }
              ],
              "loreNeeds": [
                {
                  "title": "Whisperglass Protocol",
                  "requirementSlug": "whisperglass-protocol",
                  "status": "planned",
                  "description": "Spell out how the surveillance dampeners function and fail.",
                  "requiredFor": ["chapter-blueprint", "chapter-scroll"],
                  "notes": "Must tie into city lore.",
                  "track": true
                }
              ],
              "planningBacklog": [
                { "id": "outline-core-conflicts", "description": "Draft chapter conflict blueprint", "status": "pending", "inputs": ["vision-plan"], "outputs": ["chapter-blueprint"] },
                { "id": "refine-scroll", "description": "Refine chapter scroll", "status": "pending", "inputs": ["chapter-blueprint"], "outputs": ["chapter-scroll"] },
                { "id": "draft-scene", "description": "Draft opening scene", "status": "pending", "inputs": ["chapter-scroll"], "outputs": ["scene-draft"] }
              ],
              "openQuestions": ["What secret drives the rival?"],
              "worldSeeds": ["Underground market operating beneath the city."]
            }
            """;

        public const string Iterative =
            """
            {
              "storyAdjustments": ["Emphasize the mentor's warnings."],
              "characterPriorities": ["Showcase the protagonist's hesitation."],
              "locationNotes": ["Highlight the abandoned station as claustrophobic."],
              "systemsConsiderations": ["Ensure comms blackout remains consistent."],
              "risks": ["Scene may over-index on exposition."]
            }
            """;

        public const string ChapterBlueprint =
            """
            {
              "title": "Chapter One",
              "synopsis": "The heroes gather intel and discover the rival's hidden agenda.",
              "structure": [
                {
                  "slug": "beat-1",
                  "summary": "The team infiltrates the rally and spots the rival moving suspiciously.",
                  "goal": "Secure footage without being seen.",
                  "obstacle": "Crowded halls and limited visibility.",
                  "turn": "The rival activates a jammer.",
                  "fallout": "Team must adapt to silent signals.",
                  "carryForward": ["refine-scroll", "draft-scene"]
                }
              ]
            }
            """;

        public const string Scroll =
            """
            {
              "scrollSlug": "scroll-1",
              "title": "Scroll Revision One",
              "synopsis": "Detailed beat outline capturing tension as the team navigates the rally.",
              "sections": [
                {
                  "sectionSlug": "section-1",
                  "title": "Infiltration Begins",
                  "summary": "The team slips into the rally, blending with the crowd while maintaining comms discipline.",
                  "transitions": ["refine the scene to show rising stakes."],
                  "scenes": [
                    {
                      "sceneSlug": "scene-1",
                      "title": "Silent Arrival",
                      "goal": "Reach the balcony vantage point.",
                      "conflict": "Security sweeps threaten to expose them.",
                      "turn": "Jammer disables their equipment.",
                      "fallout": "They must rely on hand signals.",
                      "carryForward": ["draft-scene"]
                    }
                  ]
                }
              ]
            }
            """;

        public const string Scene =
            "In the Opening Scene of Pipeline Plan, the team glides through the rally described in the Opening Section as scroll-1 surveillance intensifies. Existing scene description beats surface while the jammer hums overhead, forcing them to whisper Pipeline Project code phrases.";
    }
}
