# FIELD / TemporarySafety / AI MOCK integration

Status: ASSIGNED by owner attachments `3b51cab0` and superseding `dbd847b2` on 2026-11-23. Writer: Huy/Codex; delivery branch huy-review; isolated implementation branch codex/field-safety-obligations. Base: `9a022bd15be444e77a1ec3b500e9a7dce90ceb6e`. Main checkout's pre-existing Case Report changes are preserved; all work is in D:/RG-web-pm-contracts.

## Goal and authority

TARGET_CONFIRMED: finish all four attachment items. FIELD task trigger accepts measurement codes 1–6 without weakening source/pin guards. Discover and append TemporarySafety to an existing NORMAL formal item. New safety created after formal A→B transfer starts under B responsibility with A provenance; no copied transfer acceptance and no automatic movement of older safety. Enable only AI candidate readback for Web; actionable safe AI MOCK diagnostics and asynchronous correlation. No real-AI, FE database edits, deployment, broad CRUD, public file access or workflow bypass.

CURRENT_VERIFIED sources at base: FieldInspectionTasks_H3Scope trigger in `20261009043010_AllowVerifiedDefectPostRepairTasks`; RepairWorkflowRepository.Producing; RepairSafetyRepository; ProjectLifecycleRepository and ObligationResponsibilities; AiMockRunsController → Anh02AiService → Anh02AiRepository; synthetic versioned AI fixtures. Current source is implementation evidence, not new business authority.

## Behavior, data and reservations

One writer owns this slice, including two additive migrations, model snapshot, sandbox catalog, Swagger and tests. No concurrent shared writer was started. Follow controller/service/repository seams. GET/POST item-scoped temporary-safety-obligations return package/item/obligation versions separately. Current responsibility determines mutation project; bound Crew remains governed by original project/assignment. SERIALIZABLE receipt transaction, package ETag, exact request fingerprint, current-authority replay, immutable audit and initial authority action kind 8. Existing later transfers remain kind 6/7. Public responsibility readback distinguishes nullable acceptanceActionId from initialResponsibilityActionId.

FIELD migration replaces a full controlled trigger definition, never edits released migrations. Down refuses 5/6 data. Safety migration extends kind check and exact-source triggers; Down refuses kind-8 history. No backfill or rewriting prior actions. Sandbox signature/migration list must match observed upgraded SQL. AI correlation is optional manifest provenance and durable allowlisted failure audit; no extra AI schema migration. Existing canonical manifests without origin retain their byte format.

## Acceptance and delivery

Isolated SQL/Testcontainers only. HTTP tests cover standard/equivalent-patched FIELD upgrades, codes 1–6 REPORTER/SURVEY, invalid 0/7 SQL, immutability and guarded downgrade; normal/initial/transferred safety, fresh Web reads, CSRF, ETag, replay/fingerprint races and formal-transfer race; candidate PM/Sup Web/Bearer, denied role/project/membership, KEEP_NEW readback and MOCK failure correlation. Regress migration, safety, responsibility and seed/catalog, plus build/analyzer, format, source contracts, route inventory/runtime Swagger.

No open owner decision remains. Unknown: the actual external Hoàng trigger patch and FE runtime/DB; only a controlled equivalent patch is tested. MOCK/source-storage fixtures and real MinIO artifacts are labeled separately in handoff. Complete two self-review passes and record actual counts/limitations in the common handoff. Owner authorizes normal push huy-review then fast-forward develop while CI may still run; no force, PR, deployment or FE DB migration.
