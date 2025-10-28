# HGTF §8 — Runtime Loader & Policy (v1.0)

## 0) Executive summary

**Goal:** Execute published tools **safely** under strict policy envelopes with **no raw host privileges**, enforced by **process isolation**, **resource controls**, and **brokered capabilities**.

Lane strategy

* **Lane A (v1.0):** **Out-of-Process Sandbox (OOPS)** — each tool run executes in a short-lived sandboxed worker process, with CPU/memory/IO/network control (Windows: Job Objects + AppContainer; Linux: cgroups v2 + namespaces + seccomp (optional)). All host access is **brokered** via RPC and policy-checked.
* **Lane B (v1.1+):** **WASI lane** — tools compiled to WASI, executed via Wasmtime/Wasmer with capability-based imports. This becomes the default once a handful of core capabilities are ported.

**Non-goals (v1.0):** In-process ALC “sandboxing” (not secure), unbounded network/file IO, dynamic codegen inside the host.

---

## 1) Threat model

Trust boundaries

* **Host:** Cognition Jobs/API runtime (trusted).
* **Sandbox Worker:** Untrusted tool code (trusted **artifact** integrity, not runtime behavior).
* **Broker:** Minimal privileged mediator enforcing policy; terminates workers that violate ceilings.

Primary threats & controls

| Threat                                         | Control                                                                                       |
| ---------------------------------------------- | --------------------------------------------------------------------------------------------- |
| Arbitrary code execution against host          | Out-of-process isolation, no in-proc ALC                                                      |
| Resource exhaustion (CPU/mem/file descriptors) | Job Objects / cgroups limits, fd quotas, timeouts                                             |
| Unauthorized egress (network)                  | AppContainer no-network / netns with no interfaces, iptables/nftables drop; broker denies net |
| File system exfiltration                       | Per-invocation scratch dir + read-only artifact mount; no host FS                             |
| Privilege escalation                           | Non-priv user, seccomp basic profile (Linux), AppContainer low IL (Windows)                   |
| Covert channel via logs                        | Size caps, rate limiting, truncation with indicators                                          |

---

## 2) High-level architecture

```plaintext
Caller (Planner/Client)
   │
   ▼
Registry.Resolve → (ToolResolution, PolicyEnvelope)
   │
   ▼
Runtime Orchestrator
   ├─ Verify signature + hash
   ├─ Prepare sandbox
   │    ├─ Linux: cgroups v2, pid/mount/net namespaces, chroot-like root via pivot_root/bind
   │    └─ Windows: Job Object, AppContainer, process token, low IL
   ├─ Start Sandbox Worker (short-lived)
   ├─ Brokered RPC (Unix domain socket / Named Pipe)
   └─ Enforce ceilings (wall/CPU/mem/fd/output)
       └─ Kill on breach, emit telemetry
```

**RPC protocol:** **JSON-RPC 2.0** over **UDS/Named Pipes** (no TCP). Strict schema, bounded payload sizes, request/response correlation IDs.

---

## 3) Core abstractions

```csharp
public sealed record PolicyEnvelope(
  TimeSpan CpuTimeLimit, long MemoryLimitMb,
  bool AllowNetworkEgress, bool AllowFileIO,
  string[] AllowedHostApis);

public interface IToolLoader
{
  Task<LoadedTool> LoadAsync(ToolResolution res, PolicyEnvelope policy, CancellationToken ct);
}

public interface IToolInvoker
{
  Task<ToolInvocationResult> InvokeAsync(
    ToolInvocationRequest request,
    PolicyEnvelope policy,
    CancellationToken ct);
}

public sealed record LoadedTool(
  Guid ToolId, string Version, string ArtifactId, string EntryPoint);

public sealed record ToolInvocationRequest(
  string Method, JsonElement Input, string CorrelationId);

public sealed record ToolInvocationResult(
  int StatusCode, JsonElement Output, string[] Warnings, TimeSpan Duration);
```

**Important:** **Loader returns a process descriptor** (endpoint handle) bound to the policy; invocations go through the sandbox worker via the Broker.

---

## 4) Sandbox “lanes”

### 4.1 Lane A — OOPS (Out-of-Process Sandbox, v1.0)

Linux (preferred on servers)

* **Namespaces:** `pid`, `mount`, `uts`, `ipc`, `user`, `net` (dedicated netns with **no interfaces** by default).
* **cgroups v2:** CPU quota + mem.max + pids.max; IO throttling optional.
* **seccomp (optional v1.0):** “baseline” profile (deny dangerous syscalls—`ptrace`, `keyctl`, `mount`, `reboot`, `kexec`, raw sockets).
* **FS:** Bind-mount **read-only artifact** under `/tool`, per-invocation **tmpfs scratch** under `/scratch`. No host FS visibility.
* **User:** Unprivileged uid/gid remapped via user ns.

Windows

* **Job Objects:** CPU rate control, working set, process count, kill-on-job-close; I/O rate (optional via QOS).
* **Token:** Low-IL restricted token (no admin, no elevation).
* **AppContainer:** No network capabilities by default; optional loopback if explicitly allowed.
* **FS:** Per-invocation temp folder; artifact copied to RO location; ACL deny write except `/scratch`.

Process model

* **One process per invocation** (simple, predictable limits) OR **pool with hard kill between invocations** (latency optimization; keep off for v1.0).
* **Lifecycle:** spawn → run → exit → collect metrics → cleanup.

### 4.2 Lane B — WASI (v1.1+)

* Execute WASI artifacts via Wasmtime/Wasmer .NET.
* Capabilities: import only allowed host APIs; no ambient FS/Net by default.
* Maps 1:1 to our **brokered API** model, but with stronger guarantees from the runtime.

---

## 5) Brokered host APIs (capability model)

**Principle:** tool cannot call OS APIs directly; it calls **logical capabilities** that the Broker implements and guards via policy.

Example capability interfaces (RPC level):

* `kv.get/kv.set` — scoped to ephemeral scratch only (or namespace keyspace).
* `http.fetch` — **disabled by default**; when enabled, allowlist schemas/domains and response size, timeouts, redirects.
* `fs.readText/fs.writeText` — **disabled by default**; when enabled, restricted to `/scratch`; path normalization + quota.
* `images.resize` — pure compute; no side-effects.

Policy enforcement

* If `AllowedHostApis` doesn’t include a capability → 403 (capability not enabled).
* Each capability has **sub-policy** (e.g., `maxBytes`, `maxRequests`, `domains[]`).

---

## 6) Resource enforcement (hard & soft ceilings)

Hard limits (OS-enforced)

* CPU (cgroups cpu.max / Job Object CPU rate)
* Memory (cgroups memory.max / Job working set)
* Process/thread count (pids.max / Job limit)
* File descriptors / handle count (Linux ulimit / Windows job+handle quotas)
* Network: netns with no interfaces / AppContainer no capabilities

Soft limits (Broker-enforced)

* Wall-clock timeout (kill on exceed)
* Output size (truncate with warning)
* RPC request count/size
* Log rate (token bucket)

Kill policy

* Any OS limit breach → SIGKILL/TerminateJobObject
* Any policy breach → broker sends **TerminationReason**; always tear down worker

---

## 7) Protocol & wire format

Transport

* Linux: **Unix Domain Socket** at a private path under `/run/cognition/<invocationId>.sock`
* Windows: **Named Pipe** `\\.\pipe\cognition\<invocationId>`

Protocol

* **JSON-RPC 2.0**, UTF-8, max message size (configurable, default 2MB)
* **Schema:** all requests include `correlationId`, `toolId`, `version`, `policyId` (for audit)
* **Handshake**

  1. Broker → Worker: `Init { artifactPath, entryPoint, policyDigest }`
  2. Worker → Broker: `Ready { toolMeta }`
  3. Broker → Worker: `Invoke { method, input }`
  4. Worker → Broker: `Result { status, output, warnings }`
  5. Broker → Worker: `Shutdown`

Integrity

* Broker verifies **signature** + **content hash** on artifact before launch
* Broker computes **policyDigest** (HMAC of policy fields) and logs it

---

## 8) Artifact layout & entrypoint

Artifact (zip/tar)

```plaintext
/manifest.json  // { entryPoint: "Tool.Entry", contracts: {...}, policyClass: "safe-default" }
/bin/Tool.dll   // signed
/lib/...        // deps
```

Entrypoint contract (Tool SDK)

```csharp
public interface IToolEntry
{
  Task<ToolInvocationResult> InvokeAsync(ToolInvocationRequest request, ICapabilityBroker broker, CancellationToken ct);
}
```

Worker responsibility

* Load `Tool.dll` in its own AppDomain/ALC (inside the sandbox process)
* No reflection allowed beyond entrypoint assembly
* JIT allowed; NGEN/AOT optional; no dynamic assembly save

---

## 9) Policy model (config → enforcement)

```csharp
public sealed record PolicyEnvelope(
  TimeSpan CpuTimeLimit,
  long MemoryLimitMb,
  bool AllowNetworkEgress = false,
  bool AllowFileIO = false,
  string[] AllowedHostApis = default!);
```

Derived controls

* If `AllowNetworkEgress=false` → no netns interfaces (Linux) / AppContainer without networking (Windows); `http.fetch` capability disabled regardless of allowed list.
* If `AllowFileIO=false` → `/scratch` not mounted; `fs.*` capabilities disabled.

**Default (`safe-default`)**

* `CpuTimeLimit=5000ms`, `MemoryLimitMb=128`, `AllowNetworkEgress=false`, `AllowFileIO=false`, `AllowedHostApis=[]`

---

## 10) Orchestration Flow (v1.0, end-to-end)

1. **Resolve** tool via Registry (scope hash membership).
2. **Verify** signature + content hash of artifact.
3. **Prepare sandbox**

   * Linux: create cgroup subtree + namespaces; mount RO artifact; create tmpfs `/scratch` if enabled; spawn non-priv worker.
   * Windows: create Job, restricted token, AppContainer, RO artifact folder, scratch if enabled; spawn worker.
4. **Handshake** with worker; send `Init`.
5. **Invoke** entrypoint; broker enforces soft limits and capabilities.
6. **Collect** result and metrics; emit `tool.invocation` telemetry.
7. **Tear down** worker and clean sandbox.

---

## 11) Testing strategy (must-pass suites)

Negative security

* Attempt `File.Write` with `AllowFileIO=false` → must fail.
* Attempt network DNS/TCP with `AllowNetworkEgress=false` → must fail (no netns iface / AppContainer block).
* Attempt to access host path outside `/scratch` → must fail (mount/root guard).

Resource exhaustion

* Tight loop CPU burn → killed by CPU quota or wall timeout.
* Memory balloon → killed by cgroup/jobj memory cap.
* Thread flood → pids.max / job process cap enforced.

Protocol robustness

* Oversized payloads → 413 with truncation.
* Malformed JSON → 400 with termination.
* Infinite stream/log spam → broker truncation & rate-limit; hard kill if exceeded.

Reproducibility

* Same artifact under same policy produces identical outputs (where deterministic).

Chaos

* Kill worker mid-run; broker reports failure and cleans up.
* Drop socket/pipe; worker reports error and exits.

---

## 12) Telemetry

Events (all include `correlationId`, `scope`, `toolId`, `version`, `policyDigest`):

* `tool.sandbox.spawned` `{ pid, lane, os, cpuQuota, memMax }`
* `tool.sandbox.violation` `{ type, detail, hard|soft }`
* `tool.invocation` `{ durationMs, cpuMs?, memPeakMb, bytesIn, bytesOut, outcome }`
* `tool.sandbox.terminated` `{ reason }`

Dashboards

* p95/p99 duration per tool/version
* Error rate & violation rate
* Average/peak mem; CPU throttling %
* Queue wait time (invocation latency)

---

## 13) CI/CD gates (runtime)

* **Unit & Integration tests** for broker + workers across Linux/Windows.
* **e2e** scenario: resolve → sandbox → invoke → teardown.
* **Seccomp/AppContainer** checks (feature probes at boot; fail closed if policy demands but platform missing).
* **Linters:** forbid `HttpClient`/`File` in tool code (Roslyn analyzer in Tool SDK).
* **Repro builds**: artifact signature & hash checked in CI.
* **Vuln scan**: worker runtime + Tool SDK dependencies.

---

## 14) Implementation plan (8–10 working days)

Day 1–2

* Define runtime interfaces & Tool SDK entrypoint
* Implement **Broker** abstraction and RPC transport (UDS/Named Pipe)
* Artifact verifier (signature + content hash)

Day 3–4

* Linux sandbox runner: namespaces + cgroups; FS mounts; spawn
* Windows sandbox runner: Job Object + AppContainer + token; spawn

Day 5

* Capability broker & policy enforcement (fs/http/kv stubs; deny by default)
* Policy → OS mapping (quotas, limits)

Day 6

* Negative security & resource tests; wall/CPU/mem/timeouts
* Telemetry events

Day 7

* Integration with Registry/Loader pipeline
* End-to-end path (resolve → run → telemetry)

Day 8–9

* Hardening: truncation, rate limits, log quotas, teardown on breach
* CI gates and cross-platform test matrices

Day 10

* Docs & runbooks; failure scenarios; on-call cheat-sheet

---

## 15) Runbooks (ops)

If a tool OOMs / CPU throttles constantly

* Confirm policy envelope; tighten mem/CPU or reject enablement.
* Inspect `tool.sandbox.violation` frequency; consider quarantine (auto disable at N violations/hour).

If a tool attempts network

* Ensure `AllowNetworkEgress=false`; netns/AppContainer verified at boot.
* If policy allows network, verify domains allowlist.

If worker leaks handles/threads

* Ensure per-invocation process model (no pooling); kill between invocations.

---

## 16) Open questions (decide before Day 3)

1. **Pooling?** v1.0 ships **no pooling** for simplicity and safety.
2. **Seccomp level?** Start with baseline denylist; move to strict profiles later.
3. **HTTP capability domains?** Central config or per-tool allowlists?
4. **KV capability backing?** In-memory scoped store vs. temp files in `/scratch`.

---

## 17) Reference snippets (PR-ready cores)

### 17.1 Linux cgroups v2 setup (pseudo-C#)

```csharp
// Create cgroup
var cgBase = "/sys/fs/cgroup";
var cg = Path.Combine(cgBase, $"cognition/{invocationId}");
Directory.CreateDirectory(cg);
File.WriteAllText(Path.Combine(cg, "pids.max"), "64");
File.WriteAllText(Path.Combine(cg, "memory.max"), $"{policy.MemoryLimitMb * 1024 * 1024}");
File.WriteAllText(Path.Combine(cg, "cpu.max"), $"{Math.Max(1, (int)policy.CpuTimeLimit.TotalMilliseconds) } 100000"); // quota/period
// After spawn, write PID to cgroup.procs
```

### 17.2 Windows Job Object limits (C#)

```csharp
using var job = new JobObject();
job.SetLimits(new JobLimits {
  KillOnJobClose = true,
  ProcessMemoryLimitBytes = policy.MemoryLimitMb * 1024L * 1024L,
  CpuRatePercent = 50, // derived from policy
  ActiveProcessLimit = 1
});
// Spawn worker with restricted token + AppContainer; AssignProcess(job, proc);
```

### 17.3 Capability broker (HTTP) — deny-by-default

```csharp
public sealed class HttpCapability : ICapability
{
  private readonly HttpClient _client = new(new SocketsHttpHandler {
    AllowAutoRedirect = false, MaxResponseHeadersLength = 64
  }) { Timeout = TimeSpan.FromSeconds(5) };

  public async Task<HttpResult> FetchAsync(HttpRequest req, PolicyEnvelope policy, CancellationToken ct)
  {
    if (!policy.AllowNetworkEgress) return HttpResult.Forbidden("network disabled");
    if (!Allowed(req.Url, policy)) return HttpResult.Forbidden("domain not allowed");

    using var msg = new HttpRequestMessage(HttpMethod.Get, req.Url);
    using var res = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, ct);
    if (res.Content.Headers.ContentLength is > 1_000_000) return HttpResult.TooLarge();

    using var stream = await res.Content.ReadAsStreamAsync(ct);
    using var ms = new MemoryStream(capacity: 1_000_000);
    await stream.CopyToAsync(ms, ct);
    if (ms.Length > 1_000_000) return HttpResult.TooLarge();

    return HttpResult.Ok(Encoding.UTF8.GetString(ms.ToArray()));
  }
}
```

---

## 18) Acceptance criteria (runtime)

* [ ] **Out-of-process** execution on Linux & Windows with OS-level hard limits.
* [ ] **No network** available when `AllowNetworkEgress=false` (verified by test).
* [ ] **No file IO** beyond `/scratch` when `AllowFileIO=false`.
* [ ] CPU, memory, pids limits enforced; violations kill worker; telemetry emitted.
* [ ] Brokered APIs only; capability allowlist honored.
* [ ] End-to-end: resolve → verify → sandbox → invoke → teardown with metrics.
* [ ] CI gates: negative security, resource exhaustion, protocol robustness.
* [ ] Runbooks shipped.

---

## 19) Roadmap (v1.1+)

* **WASI lane** default for new tools; .NET → WASI tool SDK.
* **Dual-key scope hash rotation** (already designed in §6; implement).
* **Per-capability quotas** (e.g., `http.fetch` per-invocation limits).
* **Signed IPC** (mutual attestation between broker/worker).
* **Pool with zygote model** (only after proven safe).

---

This is the full, production-grade **Runtime Loader & Policy** spec. If you want, I can deliver **two PR-ready modules next**—for example:

1. **Linux & Windows sandbox runners (spawn, limit, teardown) + Broker transport**, and
2. **Negative security/resource test pack** (cross-platform),
