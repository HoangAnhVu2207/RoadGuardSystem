# C02 — Remaining risks

Baseline `07222494417969070e8c5d56dcc78f243f795afb`. Scope is the two proven deadlock causes, not an MVP release.

| Area | Status | Evidence / limitation |
|---|---|---|
| ND01 Users ↔ ObligationResponsibilities | Local cause regression CLOSED; exact-SHA CI pending | Exact pre-fix SQL 1205 and bounded deterministic two-session red/green; command authority and source provenance retained |
| H3 Roles ↔ DeadlineClocks | Local cause regression CLOSED; exact-SHA CI pending | Exact graph and clock repository replay returns 200 without another extension; unchanged FIELD HTTP tests separately required |
| H6 claim/delivery | Local regression PASS; exact-SHA CI pending | C01 lease ownership code unchanged; worker source locks and atomic receipt/outbox fence unchanged |
| Roles ↔ FieldInspectionAssignments | Shared boundary addressed; separate graph regression NOT_RUN | FIELD source passes through changed recipient authority; no blanket claim that every assignment schedule is covered |
| Other C01 graphs | OPEN / not proven fixed by C02 | ObserveClocks broad Outbox predicate ↔ clocks; three-party Outbox/role cycles; AI worker contention. These are outside the two deterministic cycles; monitor SQL deadlock telemetry |
| Production workload | NOT_VERIFIED_DEPLOYMENT | Test SQL 2019 CU18 and real repositories/HTTP are not production load evidence. No real Web/Expo/FastAPI or external provider E2E |

Shared S authority remains capable of blocking revocation until transaction completion (intentional). Multi-row authority writes combined with business locks may form other cycles. No unbounded retry or catch-and-ignore was introduced. Under production load, monitor SQL 1205, notification iteration failures, lease completion and delivery latency; preserve correlation IDs and deadlock Extended Events. Rollback is the six-file source change; no data/schema downgrade is required.

The 15 MVP tracking entries remain open, with no changes in this task. Native local encryption, real providers and deployment evidence are not accepted by SQL or CI PASS. See [root cause](01_ROOT_CAUSE_AND_LOCK_ORDER.md), [checks](02_FIX_AND_REGRESSION_EVIDENCE.md) and [closure recommendation](04_PHASE5_CLOSURE_RECOMMENDATION.md).

Future consumers must use the read-only opt-in only when they do not mutate authority rows. S-to-X conversion by a new consumer could introduce another deadlock; the flag is not permission to bypass current-authority predicates. No production load benchmark or separate RCSI-enabled deployment matrix was executed.
