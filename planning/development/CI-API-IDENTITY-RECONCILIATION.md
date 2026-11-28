# CI API identity reconciliation — 2026-11-28

Owner-assigned follow-up to FIELD-REAL-EXPORT-TIMELINE-FORMAL-GUARDS. Sole writer Codex/Huy. Worktree `D:/RG-field-evidence-gps`, branch `codex/field-real-export-timeline-guards`; base/HEAD before correction and both remotes: `628b24d07b18f100a33573be6b9687a6c28b2be4`. Main checkout remains untouched with its 16 WIP paths. Scope: CI manifest and this evidence; no production/FE contract, test C#, workflow, verifier, skip whitelist, runtime/image or database changes.

## CURRENT_VERIFIED incident

Both completed runs were verified against the exact base SHA:

- [develop run 46161863520](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/46161863520), [aggregate job 141033816778](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/46161863520/job/141033816778).
- [huy-review run 46161847298](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/46161847298), [aggregate job 141032511259](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/46161847298/job/141032511259).

The failing step is `Reconcile API identities and require successful lanes`, not an HTTP test assertion. Both logs throw at `tests/CI/Verify-TestLanes.ps1:45`:

```text
api-lane-1 has missing or unexpected test identities:
```

Develop first reports `HandoffExpiredCredentialHttpTests.ExpiredJwtAndRevokedSessionDenyExactProtectedOperation` with `operationId: "ADD-13"`; huy-review first reports `AssignmentCredentialBoundaryHttpTests.ExactAssignmentOperationChecksCredentialLifetimeRevocationAndCsrf` with `operation: "ADD-15"`. Different first rows reflect result order; both identities exist in lane 1 and have the same manifest encoding defect. All nine lane TRX inputs used below come from the completed develop run, never from mixed runs or a local substitute.

## Root causes and minimal correction

1. **304 existing identities were corrupted in the preceding delivery.** Relative to the UTF-8 manifest at `d800e35`, the strings match three successive erroneous UTF-8-to-Windows-1252 decoding passes. Their `···` truncation markers became mojibake during local JSON read/write. This was introduced by Codex tooling in the prior task, not by production FIELD/export/timeline changes or a Linux test failure. Restore each name from the valid baseline only after proving it exactly matches the original Linux TRX identity in the same lane. Read/write the corrected manifest explicitly as UTF-8.
2. **One legitimate new case was absent from lane 2 expectedNames.** `Milestone2AnonymousBoundaryHttpTests.ProtectedOperations` enumerates secured operations from the handoff OpenAPI. Adding the formal eligibility GET generated a new case automatically:

```text
Milestone2AnonymousBoundaryHttpTests.AdvertisedProtectedOperationRejectsAnonymousAndInvalidBearer
operationKey: 1B103F2401FEAAFD
method: GET
contractRoute: /api/v1/projects/{projectId}/repair-packages/eligibility
```

The key is independently recomputed as the first 16 uppercase SHA-256 hex characters of `GET <contractRoute>`. The original TRX records this exact case as Passed, with its TestMethod definition assigned to the existing lane-2 class. Add its exact observed display identity. Do not remove a case or infer a shortened name.

The two short `reporter`/`crew` theory aliases introduced in the preceding task already match CI and remain unchanged. No C# display identity is changed in this correction, so the original completed-run TRX remains representative for reconciliation after the manifest-only fix.

| Lane | Restored UTF-8 identities | Added expected case | CI Passed | Optional skip |
|---|---:|---:|---:|---:|
| 1 | 16 | 0 | 208 | 0 |
| 2 | 267 | 1 | 467 | 0 |
| 3 | 0 | 0 | 179 | 0 |
| 4 | 13 | 0 | 242 | 1 |
| 5 | 8 | 0 | 144 | 0 |
| 6 | 0 | 0 | 184 | 0 |
| 7 | 0 | 0 | 207 | 0 |
| 8 | 0 | 0 | 94 | 0 |
| 9 | 0 | 0 | 79 | 0 |

Total: restore 304 names, add 1 case, retain all class assignments and all pre-existing cases. Final total 1,805 distinct identities: 1,804 Passed and 1 NotExecuted. The only skip is `RoadGuardSystem.ApiTests.Files.UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`, allowed by the existing exact whitelist when ROADGUARD_MINIO_SMOKE is not 1.

## Focused verification

- Downloaded all nine `api-results-lane-*` artifacts from run 46161863520 outside the repository; preserved original TRX. Captured SHA-256 before/after; all nine hashes unchanged.
- Ran the workflow's exact command against those inputs:

```powershell
pwsh -NoProfile -File tests/CI/Verify-ApiLanes.ps1 -ResultsPath <original-artifact-directory> -MatrixResult success
```

Before correction: exit 1, reproduced the same lane-1 exception and ADD-13 identity. After correction: exit 0, all nine lanes reconciled, 1,805 unique identities / 1,804 passed / 1 permitted skip. This is local verification of original completed-run CI artifacts, not a new execution of 1,805 tests or a claim that a future CI run passed.

- Compared every actual TRX TestMethod class/method with its lane assignment; exact sets match after correction, no duplicate or removed identity.
- `Verify-LaneDiscovery.ps1 -ManifestPath tests/CI/api-lanes.json -AssemblyPath tests/RoadGuardSystem.ApiTests/bin/Release/net8.0/RoadGuardSystem.ApiTests.dll`: PASS, 246 classes and 0 separately assigned methods. Existing Release binary corresponds to unchanged test C#/source; no rebuild necessary for manifest/docs-only correction.
- UTF-8 JSON parse and `git diff --check`: PASS. No verifier/workflow edits, no C# formatting/build, no new test execution, no full API/SQL/unit suites. All strict nine-file, identity, duplicate, counter, ResultSummary and skip checks remain intact.

Self-review 1: verified exact SHA/logs, original artifact hashes, root-cause mapping against baseline and actual lane TestMethod definitions; no fuzzy matching introduced into the verifier. Self-review 2: reviewed all changed names/counts, dynamic OpenAPI case, assignments and staged scope; preserved original tests/assertions and the skip whitelist. External review not performed.

Delivery: ordinary push to huy-review followed by safe develop fast-forward after fresh fetch. No new CI polling/watch/rerun. FE needs no additional API integration. Prior production handoff remains valid. No deployment, migration, image build, seed/reset or shared DB action.
