# V2 Alignment Final Review

## Review metadata

- Owner/branch: `anh` / `anh`
- Scope: final review of the alignment diff against D01-D28, 32A-44A, Appendix E/F; no backend implementation.
- Status: `DONE` for documentation review; API delivery remains `NEEDS_REPO_CHECK` per task files.
- Runtime: canonical and generated V2 contracts remain `PROPOSED_DELTA` / `NOT_ENABLED`.

## Findings and corrections

- Partial publication is a D10 design delta: `PublishCase` requires `defectIds[]`, permits optional `reportIds[]`, and keeps Report-Defect authorization/public projection as a runtime gate. P1-033's embedded snapshot was corrected.
- `FAST_TRACK_EVALUATE` is a fourth `SyncOperation` union member in the canonical draft. It uses `measurementSessionId`, `localEvaluationId`, task version and `EvaluationRequest`; replay must recheck task/policy snapshot and current authority. P1-052, P2-029, the standalone proposed schema and offline sync note were corrected to match while retaining `PROPOSED_DELTA`/`NOT_ENABLED`.
- Added positive and negative fixtures for both proposed behaviors. Generated artifacts remain derived from canonical YAML; no generated client claim implies backend support.
- Updated current validation/count references to the current SHA, 154 schemas, 239 links and 10 positive/6 negative fixtures. Historical reports retain historical numbers and hashes.
- Mermaid/Graphviz renderers were not available in PATH. Textual ERD review confirms cardinality, nullable fields and proposed constraints are marked as logical/target design; `TARGET_DOCUMENTED` is not presented as current DB schema.

## V2-P1-001..005 delivery scope

| Task | Actual source evidence | Classification | Required delta before delivery | Minimum evidence next slice |
|---|---|---|---|---|
| P1-001 login | `AuthController.Login` -> `IAuthService.LoginAsync`/`AuthService`; authentication repositories; `V2AuthenticationFlowTests` and session tests | `REUSE_CANDIDATE` | Compare DTO fields, ProblemDetails codes, role mapping and `/api/v1` base against canonical `TokenPair`; confirm auth policy and token/session side effects | API build; focused auth API tests; real login smoke inspecting status/body/headers and persisted session |
| P1-002 refreshTokens | `AuthController.Refresh` -> `IAuthService.RefreshAsync`/`AuthService`; refresh-token repository and concurrency tests | `PARTIAL` inventory hit (method token differs from operationId) | Verify operation mapping, rotation/replay/expiry contract, DTO/error parity and idempotency/concurrency behavior | API build; focused refresh/session tests including expired, unknown and concurrent replay; smoke with durable refresh rotation |
| P1-003 logout | `AuthController.Logout` -> overloaded `IAuthService.LogoutAsync`/`AuthService`; session/idempotency repositories; auth flow tests | `REUSE_CANDIDATE` | Verify required `Idempotency-Key`, actor/session claim binding, 204/error parity and replay semantics | API build; focused logout tests; authenticated smoke twice with same key plus session revocation check |
| P1-004 requestPasswordRecovery | `AuthController.RequestPasswordRecovery` -> `IAuthService.RequestPasswordRecoveryAsync`; `IIdentityRepository.CreatePasswordRecoveryRequestAsync`; persistence/config/migration and flow tests | `PARTIAL` inventory hit (repository token is `PasswordRecoveryRequest`) | Verify neutral 202 behavior, DTO validation, request persistence and no user enumeration; no Reporter registration expansion | API build; focused recovery tests for known/unknown/invalid email; smoke plus durable request readback |
| P1-005 changePassword | `AuthController.ChangePassword` -> `IAuthService.ChangePasswordAsync`/`AuthService`; password/session/idempotency repositories; auth flow tests | `REUSE_CANDIDATE` | Verify current-user binding, password policy, session revocation/token response semantics and idempotency parity | API build; focused password-change tests; authenticated smoke with old/new login and session effects |

Out of scope for this next slice: Reporter registration, web-cookie implementation, migration creation/application, package upgrades, data changes, and runtime implementation of proposed decision deltas.

## Completion history

### 2026-09-29 - DONE

- Changed: corrected task snapshots, proposed sync schema comments/shape, offline sync wording, current counts/hashes and fixtures; added this review handoff.
- Checks: alignment guard, contract hash/build/structural validation, contract guard, manifest/hash, diff check and production/migration safety rerun after edits.
- Unverified: backend build/test/smoke, SQL/live schema, Mermaid rendering, TypeScript compiler, provider/device/browser/performance/UAT.
- Side effects: documentation/generated fixtures only; no backend, migration, database, package, commit or push.
