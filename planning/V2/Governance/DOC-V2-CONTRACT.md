# DOC-V2-CONTRACT

## Task metadata

- Owner/branch: `anh` / `anh`
- deliveryStatus: `DONE`
- contractStatus: `PROPOSED_DELTA`
- implementationStatus: `PARTIAL`
- verificationStatus: `DOCS_PASS`
- sourceCheckpoint: decision register `V2-ALIGN-2026-09-28`; canonical OpenAPI SHA-256 recorded below

## Source evidence

| Source | Heading / ID | Evidence used | Revision/checkpoint |
|---|---|---|---|
| Decision register | D02/D03/D05/D09/D10/D12/D13/D15/D20/D21/D25/32A/33A/34A/36A-43A | Approved behavior and explicit technical gates | `V2-ALIGN-2026-09-28` |
| Canonical OpenAPI | `05_Technical/openapi.yaml`; `PublishCase`, `SyncOperation`, `AiResult` | Wire design delta | SHA `65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab` |
| AI service OpenAPI | `AI_Integration/openapi.yaml`; D12/D13/33A | Separate service proposal | `ai-contract-2.0-draft` |
| Frontend contract tools | check/build/validate/lock/fixtures | Generated parity and structural checks | Working tree |
| V2 task drafts | P1-033/P2-029/P2-033 | Existing whole-case/sync/AI contract deltas | Base + current docs |

## Contract changes

- Canonical BE contract version is `0.2.0-draft-alignment`, with `x-contract-status: PROPOSED_DELTA` and `x-runtime-status: NOT_ENABLED`.
- `PublishCase` now drafts `defectIds[]` for D10 partial publication; report projection/privacy remains an implementation gate.
- `SyncOperation` now explicitly drafts `FAST_TRACK_EVALUATE` with measurement/local-evaluation IDs and keeps legacy three-kind compatibility until rollout.
- AI result/fixture semantics retain valid normalized bbox, immutable raw artifact/checksum, mode/provenance and late-attempt fencing. AI service remains a separate spec and is not counted in 133 BE operations.
- Generated baseline, JSON Schema, TypeScript, catalog and lock were regenerated from canonical YAML; no Postman runtime request was added for unenabled service proposals.

## Delivery status

`DONE`

## Completion history

### 2026-09-28 - DONE

- Scope/result: updated canonical OpenAPI and separate AI crosswalk/design status; regenerated frontend contract artifacts and contract lock.
- AC: 133 operations preserved; generated schema count is 154; 4 sync variants are explicit; partial publication selector is drafted; AI service is separate/not enabled.
- Verification: canonical YAML parse PASS; `check_contracts.py` PASS (`65a92d0e...32ab`); `build_contracts.py` PASS (`schemas=154`, `operations=133`); `validate_package.py` `STRUCTURAL_CHECKS_PASS` (`239` links, 8 positive/4 negative fixtures); `test_contract_guard.py` 7/7 PASS including explicit drafted FAST_TRACK_EVALUATE; manifest 73 files/0 mismatches; `git diff --check` PASS.
- Unverified: full OpenAPI/JSON Schema standards validation, TypeScript compile, Mermaid, backend/runtime/API/SQL/AI provider/device/browser/performance/UAT; all remain NOT_RUN.
- Side effects: docs/contracts/generated artifacts only; no package, migration, database, seed, Postman runtime or external call; uncommitted.
