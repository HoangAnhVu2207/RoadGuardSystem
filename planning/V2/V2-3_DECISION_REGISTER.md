# RoadGuard V2(3) decision and delivery register

**Review date:** 2026-09-28  
**Scope:** documentation, design contracts, generated contract artifacts and planning only.  
**Runtime status:** this register does not claim that the current backend implements the target behavior.

## Source and status vocabulary

The attached V2(3) review prompt is the source for D01-D28 and approved pilot values. Current source code, ADRs and existing tests remain evidence of runtime behavior. `APPROVED_*` means the product decision is accepted; it is not a `VERIFIED` performance, device or field result.

Each operation is tracked independently with these fields:

| Field | Meaning |
|---|---|
| `contractStatus` | `DRAFT`, `REVIEWED`, `APPROVED`, or `PROPOSED_DELTA` for the wire contract |
| `implementationStatus` | `NEEDS_REPO_CHECK`, `PARTIAL`, `IMPLEMENTED`, or `EXTERNAL` |
| `verificationStatus` | `NOT_RUN`, `DOCS_PASS`, `FOCUSED_PASS`, `BLOCKED`, or `VERIFIED` |
| `dependencyType` | `contract`, `data-fixture`, or `integration-release` |
| `workstream` | `BE`, `AI_ADAPTER`, `WEB`, `ANDROID`, `OPS`, or `POLICY` |

The four fields must not be collapsed into one status. A GET task may use an independent seed fixture even when its POST producer is not implemented; end-to-end readiness still requires the complete create/upload/verify/link/process/review path.

## Decision crosswalk

| Decision | Applied documentation outcome | Remaining gate |
|---|---|---|
| D01, D26, D28 | Sprint 1 and later phases are separated; 133 is a baseline, not a delivery ceiling; the handoff is one final guide | No implementation claim from planning docs |
| D02-D04 | Measurement batch is separate from PM Fast Track repair; Supervisor company framework and versioned method/profile precede project activation | Approved technical basis and thresholds per method remain required |
| D05-D09, D11 | Handover acknowledgement, BEFORE incident, reopen authority, curing and traffic release are separate records/states | Detailed state schemas, conflict and acceptance tests |
| D10, D12-D13 | Partial defect publication and two-stage PM-triggered AI pipeline are explicit; BE remains business authority | Public projection and AI service contract review |
| D14-D18, D20-D24 | Provisional slabs, separate position/quality/coverage, PM-controlled destination, versioned route/segment and Android offline are explicit | Measurement/calibration, provider/license and conflict fixtures |
| D19, D25, D27 | Out-of-warranty intake remains supported; email/password plus Reporter email OTP; pilot limits are configuration, not universal legal rules | Runtime compatibility plan and empirical verification |

## Backlog outside the 133 baseline

These are planning items, not invented public endpoints. They require contract review and an owner before receiving a V2 ID.

| Capability | Suggested owner/workstream | Dependency type | Current status |
|---|---|---|---|
| Company policy framework, version/review/exception | P1 / POLICY | contract | PROPOSED_DELTA |
| Reopen and authority-specific decision | P1 / BE | contract | PROPOSED_DELTA |
| Partial publication and Report-Defect public projection | P1 / BE | contract | PROPOSED_DELTA |
| Detection review, group/split/match and segment confirmation | P1 + P2 / BE | contract | PROPOSED_DELTA |
| Handover acknowledgement, conflict intake/resolve and rescue receipt | P1 + P2 / BE | integration-release | PROPOSED_DELTA |
| BEFORE-loss incident, curing and traffic release | P1 / BE | contract | PROPOSED_DELTA |
| BE service manifest/candidate/artifact/event receipts | P2 / AI_ADAPTER | contract | PROPOSED_DELTA |
| FastAPI dispatch/result/matching workers | External AI + P2 adapter | integration-release | EXTERNAL |
| Web, Android offline evaluator and release/ops pipeline | External teams; P1/P2 own BE contracts | integration-release | EXTERNAL |

Legacy gaps remain in `COVERAGE_AND_GAPS.md`; a similarly named operation is not evidence that a legacy capability is covered.

The AI handoff draft is in `docs/diagram/V2/AI_Integration/`. Its six proposed service operations are intentionally outside the 133 BE baseline until the contract is reviewed and assigned.

## Pilot configuration register

The approved values in Appendix D are recorded as `APPROVED_PILOT_CONFIG` or `APPROVED_TARGET`. Timeout/retry/heartbeat, orphan cleanup, response-cache and AI recall/evaluation targets remain proposals. Retention without a determinable warranty basis is `WAITING_RETENTION_BASIS`; no deletion date is inferred from upload time.

## Packaging and evidence

`planning/V2` is an overlay planning bundle. It points to the canonical design under `docs/diagram/V2`; it is not a self-contained ZIP containing `docs/`. Historical originals remain byte-preserved. This review adds current evidence with date, command and limitation; historical PASS/DONE rows are not rewritten.
