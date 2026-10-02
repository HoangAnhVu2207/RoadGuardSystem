# Common baseline handoff plan — draft, no Git transition executed

Checkpoint 2026-10-01: local branch `anh`, HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; `git status --short --branch` shows a dirty tree. Remote freshness was not fetched or claimed. HEAD alone does **not** identify the content of uncommitted or untracked RF-04..09 artifacts. No commit, push, merge, branch switch, reset or clean was run. The `huy` branch was not inspected or modified. This document is a proposed reviewable procedure, not an assertion that two branches match.

## Ownership classification for review

| Bucket | Current paths/evidence | Handoff handling |
|---|---|---|
| Pre-existing before this post-RF-09 turn | `git status` initially showed retired `.agents/references/`, `.agents/rules/roadguard.md`, old RoadGuard skills deleted; `AGENTS.md`, CI, RF-05/06 fixture/test files, RF-07 project mapping, RF-08 role parser/callers, survey `.Dataset.cs` and P2 survey tests, V2 planning changes, `docs/product`, `docs/backend`, `docs/decisions`, `contracts`, `planning/refactor` untracked. RF-09 report lists its exact own paths. | Attribute each RF-04..09 slice using its report; obtain human ownership for the dirty survey/V2 work and any path without an RF report. Do not bundle unknown changes into a refactor commit. |
| This turn's refactor planning | `03-master-plan.md`, `03-two-developer-plan.md`, `04-delivery-slices.md`, `09-change-gates.md`, `09-cg17-package.md`, `.agents/modules/README.md`, `planning/refactor/README.md`, `10-refactor-slices.md`, `10-refactor-checklist.md`, `11-development-plan.md`, this handoff file and reports/checkpoints. | Review as one documentation-only planning package after link checks. Historical A/B rows remain marked as historical. |
| This turn's characterization | `tests/RoadGuardSystem.ApiTests/Files/UploadApiTests.cs` already had RF-06 edits before this turn (pre-edit SHA-256 `00E52F846C273F83C97958A1035F837AD4CE78EDDFF8DEFB6A7C701E3861C2C5`); this turn adds only `UploadCreate_CurrentIntBoundaryOverflowsValidationWithoutWriting`. | Review the method hunk against RF-06 prior diff and report. It is an observation test for current 500 behavior, not a new acceptance target. |
| Attributed prior Anh survey work, not this turn's edit | `planning/V2/Execution/ANH-02-project-survey.md` checkpoint 2026-09-30 13:24 names `SurveyV2PersistenceService.cs`, new `.Dataset.cs` and `P2V2SurveyScopeConcurrencyTests.cs` with 59/59 historical SQL. This turn's C00 reread source and ran 6/6 fresh focused SQL; source was not edited. | Keep this as a distinct ANH-02 checkpoint; reconcile any other V2 planning/coordination paths individually before a commit. It is not a new refactor code diff. |
| Unknown ownership | Any existing dirty path not covered by an RF-04..09 or ANH-02 checkpoint, external Web/Android/AI repositories/deployments, and any further dirty paths that appear before checkpoint. | Ask the owner to attribute exact paths. Keep them out of a scoped checkpoint until reviewed; external consumers remain UNKNOWN, not assigned to Anh or Huy. |

## Reproducibility fingerprint, local bytes

Hashes below were read from this dirty checkout on 2026-10-01, not from HEAD. A later edit invalidates that row. Full file inventory and per-file SHA-256 for **every** accepted tracked/untracked handoff file must be regenerated at checkpoint time; this selected table is a review anchor, not a complete bundle manifest. Values never contain secrets or connection strings.

| File | SHA-256 |
|---|---|
| `AGENTS.md` | `005628386D0492E10BABCFD76125A85A3CFD652D00462BD22960DF9D1C1DF476` |
| `.agents/manifest.json` | `DB1F44AE4198C32507C93A284C691CFC4B7477EDAEEB763EA52F9A7F4E305410` |
| `planning/refactor/10-refactor-slices.md` | `5D0903115BA1746C0700643A04A766BC77283C9D52093A2529CAFF699CF15DAB` |
| `planning/refactor/10-survey-coexistence-baseline.md` | `8D34946604944310DCD8EB0B553E31384D6827B2303BDA54F287329C375CC481` |
| `planning/refactor/11-development-plan.md` | `AE6A3C283B8F79A0F31DCEBEFF8B267F7EF3CE9B000F92CA1759779000C1409D` |
| `docs/backend/data/current-schema.inventory.json` | `D9A07D493B3B7865F7BB541719EC829E3780FDDE2826AE7D6805B18A97FEF751` |
| `docs/diagram/V2/05_Technical/openapi.yaml` | `ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665` |
| `RoadGuardSystem.Services/Implementations/Files/UploadService.cs` | `58CC65A52841A05A54820FA50DF7339111E5201CFE25984107DF08C962DBDFB8` |
| `RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs` | `2EE065401EA173F12BE25DBA62CC7178C24F3B45E799587E834EF56A3C431D0C` |
| `tests/RoadGuardSystem.ApiTests/Files/UploadApiTests.cs` | `7F5AB56CA150C6951A7F9A91FBE16B332298BB87E1DFE8C8982057B61C88E3FA` |
| `tests/Tooling/rf09_transition_guard.py` | `DCB62CC23716CF0407F341366D9CF31601F1FBD5DE6F6A1800697078897D7692` |
| `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs` | `5C92878F3C542290CA64215B600E2BDE131BA947744C55A1EC1D355773DA14E6` |
| `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs` | `88BCE3F290E64169127F4CD29D89A02B20387DFAA5003F959B3B114A61AEF5F9` |
| `tests/RoadGuardSystem.IntegrationTests/Surveys/P2V2SurveyScopeConcurrencyTests.cs` | `C564DB4E3C5597B80229EA536B79653686617514BCE46494CBC87DC4ACD846AD` |

## Proposed review and transfer steps — not executed

1. Freeze the `anh` checkout for the handoff review; record `git status --porcelain=v1 --untracked-files=all`, `git diff --name-status`, `git diff --cached --name-status`, base HEAD, and SHA-256 for every accepted file including untracked content. Compare RF-04..09 reports and this turn's report to the path list; resolve unknown ownership first.
2. Review this turn's code hunk and final focused/combined verification. Separate pre-existing survey/V2 changes from refactor changes. Decide which already-dirty files belong in each checkpoint; do not stage by directory wildcard or include secrets/generated local data.
3. Request a **separate** owner instruction for exact checkpoint commit paths on `anh`, commit message and desired transfer method. A commit is not authorized by this plan. Re-run affected tests if the approved file set or content changes before committing.
4. After an authorized checkpoint commit, compare full tree content and explicit manifest hashes on a new isolated `huy` checkout/branch. Agree whether to cherry-pick or merge the checkpoint only after seeing both branch histories and existing `huy` work; do not overwrite that branch. If it already has work, reconcile it separately. `origin/anh`/`origin/huy` freshness needs a deliberate remote check before any push/merge instruction.
5. Only once both branches contain the reviewed same source/docs/tooling baseline should future A/B development prompts in `11-development-plan.md` be assigned. Reserve shared migrations/snapshot, canonical contract/FE lock, Postman, CI, root agent and fixtures by turn. Validate per-branch source hashes and selected tests after transfer.

Suggested read-only review commands: `git status --short --branch`, `git rev-parse HEAD`, `git diff --name-status`, `git diff --cached --name-status`, `git ls-files --others --exclude-standard`, `Get-FileHash <reviewed-path> -Algorithm SHA256`, and `git diff --check`. Proposed mutating Git operations are deliberately omitted until the owner has reviewed the exact checkpoint paths and gives a separate instruction. No shared database is involved in handoff.
