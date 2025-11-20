Human-Gated Tool Foundry (HGTF) — Final Spec (v1.0)
0) Executive summary

Goal: A Cognition-native, human-gated system where agents request capabilities, LLM drafts specs & scaffold only, humans approve, and deterministic pipelines build, sign, test, and publish tool versions that are disabled by default and enabled per ScopePath using HMAC-SHA256 prefix hashing.
Non-goals: Auto-execution, global auto-enablement, unconstrained File/Net access.

1) Ubiquitous Language

Capability – semantic function string (e.g., nlp.summarize).

Tool – compiled & signed implementation of a capability.

ToolVersion – immutable SemVer’d build of a Tool (artifact+signature+SBOM).

Contract – Request/Response DTO schema set and error taxonomy.

Recipe – deterministic build config (TFM/RID/SDK flags).

SecuritySpec / PolicyEnvelope – CPU/mem limits, File/Net allowed? allowed host APIs.

Registry – canonical metadata & enablements for resolution.

ScopePath – canonical path (factory-only) identifying caller context.

Enablement – whitelist entries (scope prefixes) for a ToolVersion.

Backlog – Planner/Jobs lifecycle tracking (ToolRequest, ToolDraft).

Foundry – isolated jobs for draft/build/sign/test/publish.

2) Architecture & bounded contexts

Authoring BC (Cognition.Tools.Authoring)

Aggregates: ToolDefinition, ToolVersion

Tables: ToolDefinitions, ToolVersions, ToolDrafts, Policies

Foundry BC (Cognition.Tools.Foundry)

Services: IToolSpecGenerator, IToolSourceSynthesizer, IFoundryService, IQaService

Jobs: ToolFoundryJob, ToolBuildJob

Registry BC (Cognition.Tools.Registry)

Services: IToolRegistry

Tables: ToolVersionEnablements

Runtime BC (Cognition.Tools.Runtime)

Services: IToolLoader, PolicyEnforcer, SignatureVerifier, CompatibilityChecker, ArtifactCache

Planners (Cognition.Planners.Tooling)

ToolWeaver : PlannerBase<ToolWeaverParams> + templates

Console (Cognition.Console)

Review & enablement UIs

3) Planner — ToolWeaver (phases, gates, rules)
3.1 Steps (stops after draft)
#	Step Id	TemplateId	Kind	In	Out
1	capability-vision	tool.vision.v1	LLM	ToolIdea	CapabilityBrief
2	contract-architect	tool.contracts.v1	LLM	CapabilityBrief	ToolContractSet
3	recipe-designer	tool.recipe.v1	LLM	ToolContractSet	BuildRecipe
4	security-review	tool.security.v1	LLM	BuildRecipe	SecuritySpec
5	source-synthesis	tool.scaffold.v1	LLM	Contracts + Security + Recipe	SourceBundleId
—	HUMAN GATE A	—	—	—	Planner completes: ToolDraft (AwaitingApproval)
6	build-and-sign	(service)	Determ.	SourceBundleId + Recipe	ArtifactId, SignatureId, SbomId, ContentHash
7	test-runner	(service)	Determ.	Artifact + Contracts + Security	QaReport
8	publish-disabled	tool.publish.v1	Determ.	QaReport + Artifact	ToolVersionId (Enabled=false)
9	canary-rollout*	tool.rollout.v1	Spec+Svc	ToolVersionId	RolloutReport

*Canary is feature-flagged off for v1.0 (canary.enabled=false).

Planner enforcement: budgets (token+iteration), correlation IDs, template existence gate at boot, ScopePath factory usage only.

4) Foundry jobs (Hangfire)
4.1 ToolFoundryJob (LLM draft only)

Input: ToolRequestBacklog.BacklogItemId

Actions: call IToolSpecGenerator.GenerateAsync → contracts/recipe/security; IToolSourceSynthesizer.SynthesizeAsync → scaffold tar/zip; write ToolDrafts with Status=AwaitingApproval and ReviewUrl.

No build/sign/test here.

4.2 ToolBuildJob (deterministic)

Precondition: ToolDrafts.Status=Approved

Build in pinned SDK image with deterministic flags → ArtifactId, ContentHash

Code-sign + timestamp → SignatureId

Generate SBOM → SbomId

Run IQaService.RunAsync (unit/contract/negative security/budget/compat/snapshots) → QaReport.Passed

Publish to Registry: Status=PublishedDisabled, Enabled=false

Complete backlog; emit tool.published.disabled

4.3 ToolEnableJob (admin)

Writes all cumulative prefix hashes for the target scope (see §7) and invalidates cache.

Queues & throttles: tool-foundry with max concurrency N; budgets; rate limits; CT propagation.

5) Data model (EF Core)
5.1 Tables

ToolDefinitions

Id GUID PK

Name NVARCHAR(200) UNIQUE

CapabilityTags JSONB

ToolFamily NVARCHAR(64) (optional; v1 stores null)

DefaultPolicyId GUID FK Policies

CreatedUtc DATETIMEOFFSET

ToolVersions

Id GUID PK

ToolId GUID FK ToolDefinitions

SemVer NVARCHAR(32), UNIQUE (ToolId, SemVer)

Status NVARCHAR(32) — { Draft, AwaitingApproval, ApprovedDraft, Built, PublishedDisabled, Deprecated }

RuntimeTarget NVARCHAR(32) (e.g., net9.0)

SourceBundleId NVARCHAR(256)

RecipeJson JSONB

SecurityJson JSONB

ContractsJson JSONB

ArtifactId NVARCHAR(256)

SignatureId NVARCHAR(256)

SbomId NVARCHAR(256)

ContentHash CHAR(64)

CreatedUtc, PublishedUtc

ToolVersionEnablements

Id GUID PK

ToolVersionId GUID FK ToolVersions

ScopePrefixHash CHAR(64) (HMAC-SHA256)

Depth INT (0=first segment, …)

EnabledBy NVARCHAR(100)

EnabledAt DATETIMEOFFSET

UNIQUE (ToolVersionId, ScopePrefixHash)

ToolDrafts

BacklogItemId GUID PK FK BacklogItems(Id)

ToolId GUID FK ToolDefinitions

SemVer NVARCHAR(32)

SpecJson JSONB

SourceBundleId NVARCHAR(256)

RecipeJson JSONB

SecurityJson JSONB

ApprovedBy NVARCHAR(100) NULL

ReviewUrl NVARCHAR(256)

Status NVARCHAR(32) — { AwaitingApproval, Rejected, Approved }

CreatedUtc, UpdatedUtc

Policies

Id GUID PK

Name NVARCHAR(100)

CpuMs INT

MemMb INT

AllowNet BIT

AllowFile BIT

AllowedApis JSONB

5.2 Constraints / indexes

ToolVersions: UNIQUE (ToolId, SemVer)

ToolVersionEnablements: UNIQUE (ToolVersionId, ScopePrefixHash), INDEX (ToolVersionId)

ToolDrafts: INDEX (Status, ToolId), FK to BacklogItems

6) Scope hashing & cache
6.1 HMAC-SHA256 prefix hashing (no PII in DB)

ScopePrefixHash = HEX(HMAC_SHA256(scopePrefixCanonical, scopeHashKey))

Store all cumulative prefixes when enabling:

Example scope: agent:123/persona:writer/tools:experimental

Store hashes for:

agent:123 (Depth=0)

agent:123/persona:writer (Depth=1)

agent:123/persona:writer/tools:experimental (Depth=2)

6.2 Hashing service
public interface IScopeHashing
{
    // Returns ordered prefixes with depth and HMAC hex
    IReadOnlyList<(string Prefix, int Depth, string Hex)> ComputePrefixHashes(ScopePath scope);
    // Hashes arbitrary canonical path (single)
    string Hash(string canonical);
}


Key management: scopeHashKey held in KMS/KeyVault. Plan for dual-key rotation in v1.1 (accept K_old, write K_new).

6.3 ScopePrefixCache

In-memory per-ToolVersion cache of enabled prefix hashes (HashSet<string>), invalidated on enable/disable.

Refresh pattern: read-through on miss; TTL 5–15 minutes; explicit invalidation from write path.

7) Registry & resolution
7.1 Service contract
public sealed record ToolResolutionRequest(string Capability, string RuntimeTarget = "net9.0", string? MinVersion = null);

public sealed record ToolResolution(Guid ToolId, string Name, string Version, string ArtifactId, string ArtifactHash, string SignatureId, string RuntimeTarget, string[] Contracts, string PolicyClass);

public interface IToolRegistry
{
    Task<ToolResolution?> ResolveAsync(ToolResolutionRequest req, ScopeToken scope, CancellationToken ct);
    Task<Guid> PublishAsync(Guid toolId, string semver, string artifactId, string signatureId, string contentHash, string compatJson, CancellationToken ct);
    Task EnableForScopeAsync(Guid toolVersionId, ScopePath scope, string enabledBy, CancellationToken ct);
    Task DisableForScopeAsync(Guid toolVersionId, ScopePath scope, string disabledBy, CancellationToken ct);
}

7.2 Enable/disable (store/remove all prefixes)
public async Task EnableForScopeAsync(Guid toolVersionId, ScopePath scope, string enabledBy, CancellationToken ct)
{
    var prefixes = _hashing.ComputePrefixHashes(scope);
    var rows = prefixes.Select(p => new ToolVersionEnablement {
        ToolVersionId = toolVersionId, ScopePrefixHash = p.Hex, Depth = p.Depth,
        EnabledBy = enabledBy, EnabledAt = _clock.UtcNow
    });
    await _db.ToolVersionEnablements.AddRangeAsync(rows, ct);
    await _db.SaveChangesAsync(ct);
    _cache.Invalidate(toolVersionId);
}

public async Task DisableForScopeAsync(Guid toolVersionId, ScopePath scope, string disabledBy, CancellationToken ct)
{
    var prefixes = _hashing.ComputePrefixHashes(scope).Select(p => p.Hex).ToList();
    var del = await _db.ToolVersionEnablements
        .Where(e => e.ToolVersionId == toolVersionId && prefixes.Contains(e.ScopePrefixHash))
        .ToListAsync(ct);
    _db.ToolVersionEnablements.RemoveRange(del);
    await _db.SaveChangesAsync(ct);
    _cache.Invalidate(toolVersionId);
}

7.3 Resolve (membership test on prefix hashes)
public async Task<ToolResolution?> ResolveAsync(ToolResolutionRequest req, ScopeToken scope, CancellationToken ct)
{
    var scopePath = _scopePathBuilder.Build(scope);
    var callerHashes = _hashing.ComputePrefixHashes(scopePath).Select(p => p.Hex).ToHashSet();

    var toolDefs = await _db.ToolDefinitions
        .Where(d => d.CapabilityTags.Contains(req.Capability)) // JSONB contains or join table
        .Select(d => d.Id)
        .ToListAsync(ct);

    if (toolDefs.Count == 0) return null;

    var candidates = await _db.ToolVersions
        .Where(v => toolDefs.Contains(v.ToolId)
                    && v.Status == "PublishedDisabled" /* stored but disabled */
                    || v.Status == "PublishedEnabled" /* if you later add */
                    || v.Status == "Built" /* exclude */)
        .Where(v => v.Status == "PublishedDisabled" || v.Status == "Deprecated" == false) // exclude deprecated
        .ToListAsync(ct);

    foreach (var v in candidates.OrderByDescending(v => SemVer.Parse(v.SemVer)))
    {
        var enabledSet = await _cache.GetOrLoadAsync(v.Id, async () =>
        {
            var rows = await _db.ToolVersionEnablements
                .Where(e => e.ToolVersionId == v.Id)
                .Select(e => e.ScopePrefixHash)
                .ToListAsync(ct);
            return rows.ToHashSet();
        });

        if (enabledSet.Overlaps(callerHashes))
        {
            // build resolution
            return new ToolResolution(
                v.ToolId, /* name */ (await _db.ToolDefinitions.FindAsync(v.ToolId))!.Name,
                v.SemVer, v.ArtifactId!, v.ContentHash!, v.SignatureId!, v.RuntimeTarget,
                /* contracts */ ExtractContractNames(v.ContractsJson), PolicyClassFrom(v.SecurityJson));
        }
    }
    return null;
}


Complexity: O(1) per candidate using set overlap; practically sub-10ms with cache.

8) Runtime loader & policy

Verify signature + content hash before load.

Validate compatibility (TFM/RID).

Load in collectible AssemblyLoadContext (future: WASI lane).

Inject brokered host APIs only; File/Net denied unless allowed by policy.

Enforce CPU wall-time and memory ceiling (watchdog + kill).

Emit tool.invocation telemetry with scope, duration, mem, outcome.

9) Public APIs (Problem+JSON on errors)
User

POST /tools/requests

{ capability, rationale, requestedByAgentId } → { backlogItemId }

GET /tools/requests/{id} → status (owner or admin)

GET /tools/resolve?cap=nlp.summarize&rt=net9.0 (requires X-Scope-Token) → ToolResolution or 404

Admin

GET /admin/tools/review/{backlogItemId}

POST /admin/tools/requests/{id}/approve { notes? } → enqueues build

POST /admin/tools/requests/{id}/reject { reason }

POST /admin/tools/versions/{id}/enable { scopeCanonical } (server re-canonicalizes via factory)

POST /admin/tools/versions/{id}/disable { scopeCanonical }

Auth intent rule: Every action must be [Authorize(...)] or [AllowAnonymous] (limited to login/health/webhooks).

10) Console (admin UI)

Review page

Capability + rationale

Contracts diff viewer (JSON schema)

Source viewer (read-only)

Recipe & SecuritySpec cards

SBOM table (deps/license)

Buttons: Approve, Reject (with note)

Version page

Status: Published (Disabled)

Scope builder (agent/persona/tools …) → server canonicalizes & hashes

Current enablements (by depth) + disable buttons

Telemetry linkouts

11) Deterministic build, signing, SBOM, QA

Build

Pinned SDK container (e.g., mcr.microsoft.com/dotnet/sdk:9.0-<pin>)

Deterministic flags, locked restore

Normalize timestamps/path map

Sign

Strong-name + code signature w/ RFC3161 timestamp

Verify pre-load

SBOM

CycloneDX/SPDX; store SbomId

License deny list: ["GPL", "AGPL", "CC-BY-NC"] (configurable)

QA

Unit + contract conformance (schema)

Negative security (File/Net denied unless policy allows)

Budget ceilings (CPU/mem/time)

Compatibility (TFM/RID)

Golden snapshots for key outputs

12) Telemetry & SLOs

Emit structured events (with correlation IDs):

tool.request.created

tool.draft.ready

tool.draft.approved / tool.draft.rejected

tool.build.succeeded / tool.build.failed

tool.qa.passed / tool.qa.failed

tool.published.disabled

tool.enabled.scope / tool.disabled.scope

tool.invocation (duration, mem, outcome)

Dashboards: p95/p99 per tool, error rate, throttle rate, cache hit rate, queue depth.
Auto-rollback (when canary enabled): breach error >1% or p95 > target+20%.

13) CI/CD gates (must pass)

Auth intent reflection test – every controller action explicitly [Authorize] or [AllowAnonymous] (allowlist: login/health/webhooks).

Mutation testing (Stryker.NET) – ≥70% on hotspots: Planner orchestration, ScopePath canonicalization, Registry resolution, Budgets.

Coverage – ≥80% lines/branches on the same hotspots.

Repro builds – identical artifact hashes across two clean CI runs.

Vulnerability scan – dotnet list package --vulnerable clean for tool projects (including transitive).

Rate-limit e2e (k6) – 200 VUs/30s yields stable 429s; no CPU/mem runaway.

OpenSearch schema guard – refuse boot on mapping/dimension/pipeline mismatch.

Nullable + warnings as errors – enabled for all new projects.

14) Acceptance criteria (definition of done)

 ToolWeaver halts after source-synthesis, writes ToolDrafts (AwaitingApproval).

 Admin approval triggers ToolBuildJob; publish disabled.

 EnableForScopeAsync stores all prefix hashes (Depth included); disable removes them.

 ResolveAsync uses hash membership test, works for inherited scopes.

 Loader enforces policy; negative File/Net tests pass as denied.

 CI gates pass: Auth intent, Mutation ≥70% (hotspots), Repro build, Vuln clean, Rate-limit, Schema guard.

 Telemetry emitted for entire lifecycle; dashboards live.

 All ScopePath strings originate from ScopePathFactory (audit by analyzer or code review).

 All API errors are Problem+JSON.

15) Delivery plan (10 working days)
Day	Deliverables
1–2	EF migrations (ToolDrafts, ToolDefinitions, ToolVersions, ToolVersionEnablements, Policies); implement IScopeHashing (HMAC)
3–4	ToolFoundryJob (LLM draft), Review UI, Planner template existence gate in PlannerHealthService
5–6	ToolBuildJob (build/sign/SBOM/QA), PublishDisabled, vulnerability scan
7	EnableForScopeAsync (all prefixes) + ScopePrefixCache + admin enable/disable APIs
8	Loader: signature/hash, policy enforcement, negative tests
9	CI gates: Auth intent test; Stryker config (hotspots); repro build; k6 rate-limit; OpenSearch schema guard
10	E2E test: request → approval → build → publish disabled → enable → resolve → invoke; dashboards polish
16) Open config (v1.0 defaults)

Policy class safe-default: CpuMs=5000, MemMb=128, AllowNet=false, AllowFile=false, AllowedApis=[]

License deny list: ["GPL", "AGPL", "CC-BY-NC"]

Artifact size limit: 10 MB; SourceBundle retention: 30 days (rejected), 90 days (published)

Canary: canary.enabled=false (plan for v1.1)

Scope hash key: ScopeHashKey=v1 (KMS secret); rotation plan v1.1 (dual-key)

17) Reference snippets (ready to drop)
17.1 Scope hashing (HMAC; prefixes)
public sealed class HmacScopeHashing : IScopeHashing
{
    private readonly byte[] _key; // inject from KMS/KeyVault
    public HmacScopeHashing(ISecretProvider secrets) => _key = secrets.GetBytes("ScopeHashKey");

    public IReadOnlyList<(string Prefix, int Depth, string Hex)> ComputePrefixHashes(ScopePath scope)
    {
        var canon = scope.CanonicalPath; // e.g., "agent:123/persona:writer/tools:experimental"
        var segs = canon.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var list = new List<(string,int,string)>(segs.Length);
        for (int i=0; i<segs.Length; i++)
        {
            var prefix = string.Join('/', segs.Take(i+1));
            var bytes = System.Text.Encoding.UTF8.GetBytes(prefix);
            using var hmac = new System.Security.Cryptography.HMACSHA256(_key);
            var hash = hmac.ComputeHash(bytes);
            var hex = Convert.ToHexString(hash).ToLowerInvariant();
            list.Add((prefix, i, hex));
        }
        return list;
    }

    public string Hash(string canonical)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(_key);
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

17.2 Planner template gate (boot)
public sealed class PlannerHealthService
{
    public void AssertTemplatesExist(PlannerBase planner, ITemplateRepo templates)
    {
        var missing = planner.Steps.Select(s => s.TemplateId)
            .Where(id => id is not null)
            .Where(id => !templates.Exists(id!))
            .Distinct().ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Missing templates: {string.Join(", ", missing)}");
    }
}

17.3 Auth intent reflection test (allowlist anon)
[Fact]
public void Actions_Must_Be_Authorized_Or_Explicitly_Anonymous()
{
    var allowAnonRoutes = new HashSet<string> { "AuthController.Login", "HealthController.Get", "WebhooksController.Receive" };
    var asm = typeof(Program).Assembly;
    var offenders = new List<string>();

    foreach (var c in asm.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
    {
        var classAuth = c.GetCustomAttribute<AuthorizeAttribute>() != null;
        foreach (var m in c.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!m.GetCustomAttributes().Any(a => a is HttpMethodAttribute)) continue;
            var hasAuth = m.GetCustomAttribute<AuthorizeAttribute>() != null || classAuth;
            var hasAnon = m.GetCustomAttribute<AllowAnonymousAttribute>() != null;
            var key = $"{c.Name}.{m.Name}";
            if (!hasAuth && !hasAnon) offenders.Add(key);
            if (hasAnon && !allowAnonRoutes.Contains(key)) offenders.Add($"UnexpectedAnon:{key}");
        }
    }
    offenders.Should().BeEmpty();
}

17.4 Stryker (hotspots)
{
  "$schema": "https://stryker-mutator.io/stryker.schema.json",
  "reporters": ["html", "progress"],
  "mutate": [
    "**/ScopePath*.cs",
    "**/*Planner*.cs",
    "**/*Registry*.cs",
    "**/*Budget*.cs"
  ],
  "ignoreMethods": ["ToString", "GetHashCode"],
  "thresholds": { "high": 80, "low": 60, "break": 70 }
}

18) Risk register & mitigations

Scope bleed / spoofing → HMAC prefix hashes + factory-only ScopePath + membership test.

Unsafe code execution → LLM only drafts; human gate; build/test/sign is deterministic; sandboxed loader.

Supply-chain → SBOM + pinned SDK + license deny + vuln scan.

Queue stampede → budgets, rate limits, worker concurrency caps.

Observability gaps → correlation IDs and comprehensive events + SLO dashboards.

Template drift → boot-time template existence gate; golden snapshots.

19) Final call

P0s: none (closed).

P1: ensure all prefixes are stored/removed on enable/disable (included above).

Ship: after AC & CI gates pass.