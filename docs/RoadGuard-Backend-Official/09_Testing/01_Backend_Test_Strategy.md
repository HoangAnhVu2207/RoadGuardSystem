# 01 Backend Test Strategy

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Test evidence is scoped by execution, source identity and environment. Unit tests isolate domain/policy/crypto/boundary logic; ApiTests exercise actual middleware/controllers/DI with fixture/fake/disconnected or owned SQL per class; IntegrationTests use disposable SQL fixtures. A class name or syntactic reference does not prove a negative case was exercised.

For this documentation-only package: fresh build with default analyzers; fresh92 focused auth/directory/project/Supervisor/Swagger tests; fresh safe API Explorer/OpenAPI and EF metadata capture; full source/DTO/entity/action inventories and document/schema/link/hash checks. Full unit/SQL suites and live external E2E are NOT_RUN; no source changed, so unrelated full suite repetition is unnecessary.

Historical DOC-AUDIT-01 executed381tests:380pass/1fail,0skip at f0c8762. All971 production source fingerprints match current baseline; selected old fixture/schema input hashes match. This confirms source identity and historical evidence provenance, not fresh execution of those381cases. Current fresh92 are separately counted and must not be added to historical381 as one run.

Negative validation coverage must distinguish wrong current actor/session/role/project/resource, stale version, wrong state/evidence, protected replay after revoke, same key changed payload, rollback and assignment/membership windows. Focused test assertion bodies have been inventoried; per-operation references are static candidates, with executed classes mapped by TRX.

Never target shared/team/production SQL for tests. Existing source boundary fixtures can write inventory: use external output override if selected. Current capture explicitly disables startup migration/seed, real storage and workers. Actual browser/TLS/proxy, email/MinIO8GiB, FastAPI/model, Android queues/keys and restore/SLO checks need separate acceptance.
