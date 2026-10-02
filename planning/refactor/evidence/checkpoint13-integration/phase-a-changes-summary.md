# Phase A Documentation Changes Summary

**Date:** 2026-10-01  
**Branch:** `anh`  
**Commit base:** `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`

## Changed Files (8 total)

### 1. planning/refactor/10-refactor-checklist.md
**Finding 1:** Survey C02 status normalized
- **Change:** Line 24, C02 status from "Partial" to "Done locally for C02 assertion and provenance correction; Partial tổng do checkpoint 08 corrections lacked before-snapshots (documented limitation)"
- **Reason:** Clarify C02 local completion vs historical provenance limitation

### 2. planning/refactor/10-refactor-slices.md
**Finding 1 & 7:** Survey C02 status + raw evidence names
- **Change:** Line 16, added "C02 Partial tổng: checkpoint 08 corrections applied without before-snapshots; historical limitation documented; checkpoint 10 final evidence complete"
- **Change:** Line 28, emphasized "correction-02 focused HTTP/owned-SQL 5/5" and added raw evidence names (checkpoint 11 failed, correction-01 passed with gap, correction-02 final)
- **Audit A11-01:** Line 37, RF-11-C01 status updated from "START" to "DONE locally for non-destructive audit, RF-11 parent Partial pending G01"
- **Reason:** Status clarity, evidence traceability, audit finding remediation

### 3. planning/refactor/10-survey-coexistence-baseline.md
**Finding 2:** Survey replay clarification
- **Change:** Line 42, changed "stored *old* ETag" to "stored *create-time* projection (original ETag before later mutation)"
- **Change:** Finding RF10-03-C01-F05 description expanded: "V2 create replay returns the stored create projection (with original version/operator), not current row state after reassignment. SQL keeps durable reassignment to operator B."
- **Reason:** Precise replay semantics - stored projection vs current SQL state

### 4. planning/refactor/10-inspection-measurement-characterization-baseline.md
**Findings 3, 4, 5, 6:** Checkpoint separation, scope, NOT_VERIFIED clarity
- **Change:** Lines 111, added scope clarification: "Immutability verified for success GET only (RepairCrew with valid membership). Denied GET (PM/Supervisor) and filtered GET (crew without membership) NOT verified; no snapshots captured for audit/outbox or other entities when GET returns 403 or empty list."
- **Change:** Lines 157-160, separated checkpoint 11 correction-01 as having own verification evidence separate from checkpoint 12
- **Change:** Lines 183-194, updated NOT_VERIFIED with detailed clarifications: "Database trigger enforcement at runtime (HasTrigger is EF configuration reference only)" and "SRID 4326 constraint enforcement at runtime (configuration SOURCE_INSPECTED, not tested with invalid SRID data)"
- **Reason:** Precise scope boundaries, evidence provenance, runtime vs configuration distinction

### 5. planning/refactor/10-survey-request-task-assignment-characterization.md
**Finding 2:** Replay behavior clarification
- **Change:** Line 30, changed to "This is observed stored create-time replay behavior; SQL keeps durable reassignment. No claim that replay response reflects current state after later mutations."
- **Reason:** Consistent replay semantics across survey baseline and characterization

### 6. planning/refactor/reports/RF-10-03-C02.md
**Finding 2:** Replay SQL state
- **Change:** Line 20, added "V2 create replay returns the stored create projection (`operatorId` A, `NEW_ASSIGNED`, original version) while the durable task remains reassigned to operator B; SQL keeps reassignment."
- **Reason:** Explicit distinction between replay response and durable SQL state

### 7. planning/refactor/reports/RF-10-07-C01.md
**Findings 3, 5, 6:** Historical evidence, provenance coverage, NOT_VERIFIED
- **Change:** Lines 49, emphasized provenance limitations with bold: **no production-source fingerprint**, **build end timestamp approximate**, **assembly hash recorded but not claiming reproducible build**
- **Change:** Lines 58-61, separated checkpoint 11 correction-01 as having own verification evidence separate from checkpoint 12
- **Change:** Lines 70-71, clarified trigger/SRID: "Database trigger enforcement at runtime (HasTrigger is EF configuration reference only; actual trigger behavior not tested with invalid mutation attempts)" and "SRID 4326 constraint enforcement at runtime (configuration SOURCE_INSPECTED, not tested with invalid SRID data)"
- **Reason:** Honest provenance limitations, evidence separation, runtime enforcement precision

### 8. planning/refactor/reports/RF-10-07-R01.md
**Finding 8:** RETAIN gate removal
- **Change:** Removed hard gate "chỉ reassess khi 3+ patterns"; replaced with flexible criteria: "Reassess when duplicate equivalent patterns emerge or concrete extraction benefit identified"
- **Reason:** Avoid arbitrary numeric threshold; use duplicate equivalent/benefits or concrete risks instead

## Unchanged File (1 total)

### planning/refactor/reports/RF-10-06-C01.md
- **Status:** No changes required
- **Reason:** Finding 6 documentation corrections were already applied in earlier checkpoint work

## Summary Statistics

- **Total files scanned:** 9
- **Files changed:** 8
- **Files unchanged:** 1
- **Findings remediated:** 8 (plus 1 audit finding)
- **Lines affected:** Approximately 20-30 lines across 8 files

## Hash Verification

Before/after SHA-256 hashes captured in:
- `checkpoint13-integration/docs-before.sha256`
- `checkpoint13-integration/docs-after.sha256`
- `checkpoint13-integration/phase-a-hash-changes.txt`

All changes are documentation-only. No production code, schema, contracts, migrations, CI, or shared database modified.
