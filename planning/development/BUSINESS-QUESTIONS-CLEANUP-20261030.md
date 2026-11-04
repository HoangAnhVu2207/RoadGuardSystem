# BUSINESS-QUESTIONS-CLEANUP-20261030

Status: COMPLETE_LOCAL. Assigned by ChatGPT planning in the existing workspace conversation on 30/10/2026; requested by project Owner. Sole writer: Codex/Huy. Base/HEAD `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`. No commit/push/merge.

## Scope and authority

Create only this spec and `docs/audit/BUSINESS-QUESTIONS-20261030.md`. Inventory and delete proven generated caches under the user's explicit project cleanup request. No business implementation, schema, contract, dependency, test or existing documentation edits. HUY-FINAL-INTEGRATION R01–R30 and confirmed-decisions32–44 remain authority; current code establishes implementation only.

ChatGPT assigned: fresh inventory, hashes, safe generated cleanup, Vietnamese questionnaire with current answer, UNKNOWN, two proposals, impact/dependency, owner and verification; distinguish defects and external gates; two self-reviews and independent local review.

Two scoped interpretations follow the user's request and existing authority: include the explicitly verified Go build cache and Python bytecode under artifacts in cleanup (not the artifacts root/data/evidence); do not ask Q07 from the proposed plan to choose device-key loss versus encrypted handover, since both boundaries are already accepted in42A. Record native verification as an acceptance gate instead. These are not new product decisions.

Reserved: all 147 inherited dirty paths (133 modified +14 untracked), especially G02-G06-OWNER-DECISIONS.md, HUY-FINAL-INTEGRATION, official docs and history. One writer/file. Baseline hashes and full path inventory: `D:\RoadGuard-Review-Evidence\BUSINESS-QUESTIONS-CLEANUP-20261030\scan-before.json`.

## Execution and verification

Fresh whole-tree scan excludes .git and reparse paths. Resolve absolute allowlist, check no tracked content/reparse descendants; preserve data, configs, TestResults and historical extract obj. Go-cache README identifies regenerable build artifacts; no Go compiler process observed; four active Docker containers use named volumes, no project cache bind mount.

Deleted only four directories: artifacts/anh02-live/gocache, artifacts/anh02-live/__pycache__, docs/diagram/V2/09_Frontend/contracts/__pycache__, docs/HoangHaiWarrantySystem-Official/tools/__pycache__. Manifest and native PowerShell deletion script are outside repo. No Docker mutations in this task. Retained artifacts/anh02-live/objects (~8.68GiB), Go dependency sources/toolchain, MinIO binaries, snapshots and evidence: size alone does not establish junk.

Fresh checks: verify_current_docs.py against retained disconnected capture passes259 operations/1534 source hashes; this rechecks current source pins, not fresh HTTP/SQL capture. First invocation used capture parent and failed FileNotFound; corrected to capture/bin/Debug/net8.0. V2 contract checker still fails CONTRACT_LOCK_MISMATCH; retained as a finding, no relock. Focused current source rechecks include ReporterEvidence, matching, Case conclusion, identity onboarding, GPS, BusinessDuty ACK, ClockDuty, weekly recovery, lifecycle, coverage and statistics. No application behavior changed; no new unit/SQL/client tests run.

## Reviews and handoff

Self-review1: business authority vs implementation checked; ACK/substitute/weekly/coverage/lifecycle source already exists despite old spec progress rows. No request to reopen those approved policies. Privacy race is a required repair, not optional privacy policy. Q05 proposals require full cohort/denominator details before implementation.

Self-review2: only two new repo documents; deletion excludes history/data and all inherited dirty files; scope and links checked. Native/AI/production remain unverified. Independent ChatGPT local review: DONE / INDEPENDENT_PASS, iteration1; execution outputs1359/1360 read completely, source/42A classification and scoped cleanup accepted. This is local review, GitHub diff review NOT_RUN.

Final verification:147/147 inherited file hashes unchanged;4 directories/7600 generated files/957185703 logical bytes deleted;35 questionnaire local file/line links valid;6 Q +5 T items, exactly2 proposals each; source pins exit0, V2 lock exit1 retained. Native PowerShell deletion and independent validator exit0. Full Git diff --check exit0 (pre-existing line-ending warnings only). Final verification.json records results; scan-before.json preserves full dirty baseline and candidates. Owner answers to proposals remain pending after this documentation task completes.
