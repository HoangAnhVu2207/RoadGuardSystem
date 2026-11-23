# Crew current supplement review reason

Status: ASSIGNED by owner request 2026-11-23. Writer Huy/Codex, branch codex/crew-supplement-reason, delivery huy-review. Base 973e318f9022cb7c9e96cae34029a7c63678e38d; isolated worktree D:/RG-web-pm-contracts initially clean, main checkout preserved.

TARGET_CONFIRMED: API-080 adds nullable repairContext.attempt.review.reason for the exact effective SUPPLEMENT review only. Existing review ID, decision, project/assignment/binding/source guards and lifecycle remain. Other decisions return null; legacy absent/blank reason null. No new endpoint, request, migration or PM history access.

CURRENT_VERIFIED: CrewRepairContextAsync already fences currentReviewId against attempt/intake/submission/hash. Add projection there, persistence fact and both DTO conversions. Existing RepairAttemptReviews.Reason is stored; no schema change. Sole writer reserves these files, targeted tests, runtime OpenAPI and Mobile handoff; no other writer spawned.

Acceptance: actual HTTP review→fresh Crew read→FIELD supplement/API-159→clear review→new review, SQL exact row matching; ACCEPT/null compatibility; regression assignment/project/binding. Build/analyzer, format, contract tests, diff checks. Two self-review passes check authority/source and producer/consumer contract. No external/app/FE DB verification. Owner authorizes normal huy-review push then develop fast-forward; fetch and ancestry guard before each, no force/deploy/FE migration.


## Owner scope update — CI base 973e318

ASSIGNED 2026-11-23: include all five API failures from run 45116026588 before commit/push, preserve reason slice. Artifact api-results 14792702600 downloaded and api.trx inspected. CURRENT_VERIFIED: migration assertion line276 expected WebPm migration last although full migrate includes FIELD/safety; boundary line225 and lifecycle line259 exact run fields omit originCorrelationId/failure; route validation line90 (two theory cases) omits safe AI diagnostic fields. No production regression established by these failures. Keep exact-wire assertions, add values/nullability/provenance checks, keep full schema/model/data preservation, route/feature/auth/hash/lifecycle/nonmutating reads. Only optional configured MinIO smoke is skipped, matching existing whitelist. CI test-results gate was skipped after dotnet test failed, not independently passing. Keep CI gates unchanged. Run all affected classes + reason, attempt full API with repo SQL/MinIO fixture if local resources allow; report limits truthfully. No unrelated rerun of accepted unit/SQL groups.


Final CURRENT_VERIFIED: full API 1695 passed/0 failed/1 allowed MinIO smoke skip (1696 identities, 16m44s); exact CI TRX gate passed. Focused whole affected classes 15/0/0, earlier reason/Swagger 38/0/0, SQL bootstrap/intake 6/0/0 (overlap not summed). API build analyzer on; whole-repo format, all source-contract scripts/self-tests and staged diff checks passed. Two self-review passes completed, no external review. Local Windows uses installed Arial and DOTNET_PROCESSOR_COUNT=2 versus Ubuntu DejaVu path; private SQL/MinIO fixture only. No migration/deployment/shared data changes. Both remotes last observed at base; refetch/ancestry checks required immediately before push.
