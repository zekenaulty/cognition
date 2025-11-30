# Testing & CI Gap Close (Deferred)

Status: **Deferred**

Reason: We reached a reasonable stopping point without enforcing mutation gates or wiring remaining UI/nav. Current state:
- Hotspot tests landed (dispatcher sandbox enforce path, sandbox alerts/webhook, rate-limit/abuse e2e).
- Stryker config exists (targeted thresholds), and a basic CI workflow runs build/test (mutation step optional/commented).
- Admin API + console page for sandbox queue exist; queue remains in-memory and nav wiring is pending.

Deferred scope:
- Enforce mutation/coverage gates in CI.
- Wire console nav to the sandbox queue page and consider persisting queue state.
- Broader `unit_testing_expansion` checklist items and HGTF coverage once implemented.
