# Alpha Security & Sandbox Closeout (Deferred)

Status: **Deferred**  
Reason: We’re as far as we can go without a real sandbox host, persistence, or paid alerting/paging. Current repo state:

- Policy + enforcement is wired (dispatcher deny/allow, audit, webhook/log alerts, enqueue-on-deny).
- OOP worker exists but is a stub (launches a trivial process; no real tool execution or resource limits).
- Approval queue is in-memory; admin API and console page exist but are not wired into nav or persisted.
- Abuse/rate-limit + correlation tests pass; webhook alert publisher has test coverage.

Deferred scope:
- Real sandbox host that executes tools with resource limits/isolation.
- Durable queue/persistence and full console integration.
- Production-grade alerting/paging and tightened rate-limit assertions.
