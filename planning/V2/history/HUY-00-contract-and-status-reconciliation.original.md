# HUY-00 - Contract and operation-status reconciliation

- Owner/branch: Huy, Person 2, `huy` after the shared-base gate in [index](README.md).
- Base evidence: checkout `25834bdc36d42dc4a33dea525622b6cacdb491ea` plus current uncommitted design/planning files; refresh on `huy` before work.
- sourceCheckpoint: base `25834bdc36d42dc4a33dea525622b6cacdb491ea`; current canonical OpenAPI SHA-256 `ada7f48f522c0c0dbdace21f483224a00fc4c76264a9a61d12f3e2211ecce665`; lock mismatch; refresh after branch integration.
- deliveryStatus: `TODO`; contractStatus: `PROPOSED_DELTA`; implementationStatus: `NEEDS_REPO_CHECK`; verificationStatus: `NOT_RUN`.
- Goal: make the first API assignments trustworthy without silently approving a proposed wire change or rewriting historical Done evidence.

## Source evidence to use

- [AGENTS](../../../AGENTS.md), [lifecycle](../TASK_LIFECYCLE.md), [ADR 006](../../../docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md), [decision register](../V2-3_DECISION_REGISTER.md) status vocabulary and affected D12/D13/D15/D20.
- [API specification](../../../docs/design/05_Technical/03_API_Specification.md) sections 3.1, 3.7 and REVIEW-01; [canonical OpenAPI](../../../docs/design/05_Technical/openapi.yaml); [contract lock](../../../docs/design/09_Frontend/contracts/contract.lock.json); [manifest](../task_manifest.json).
- Operation cards [019](../Person_2/V2-P2-019_submitDataset.md), [020](../Person_2/V2-P2-020_getDatasetCoverage.md), [030](../Person_2/V2-P2-030_createProcessingJob.md), [031](../Person_2/V2-P2-031_getProcessingJob.md), [032](../Person_2/V2-P2-032_retryProcessingJob.md), [033](../Person_2/V2-P2-033_receiveAiResult.md), [034](../Person_2/V2-P2-034_createValidationRun.md), [035](../Person_2/V2-P2-035_getValidationResult.md); their completion history is evidence, not a status to overwrite.
- Current API/Service source: [SurveyV2Controller](../../../RoadGuardSystem.API/Controllers/SurveyV2Controller.cs), [ProcessingV2Controller](../../../RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs), [SurveyV2Service](../../../RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs), [ProcessingV2Service](../../../RoadGuardSystem.Services/Implementations/Processing/ProcessingV2Service.cs).

## Work and boundaries

1. Record the actual card-header versus manifest `deliveryStatus` differences for 019, 020 and 030-035, with the most recent completion-history evidence and current source/test symbol. Distinguish `status` legacy trace from current `deliveryStatus`.
2. Determine which exact canonical OpenAPI change produced `ada7f48f...` while the lock records `dd991f20...`; report route/schema/compatibility differences and the owner decision required. Do not update the lock from a hash alone.
3. Propose status/index corrections only when lifecycle gates have evidence; preserve all prior Done entries. Send any persistence impact to Anh through [handoffs](../../CROSS_OWNER_HANDOFFS.md).

## Acceptance and verification

- Deliver a compact evidence table in this task's `Completion history`: eight status rows, current/proposed contract hash, affected operation IDs, recommended owner decision, and specific next task. `DONE` means the audit is complete, not that the contract is frozen or eight API operations are complete.
- Run `python docs/design/ci/check_alignment.py`, `python docs/design/ci/check_links.py`, and `python docs/design/ci/test_contract_guard.py`; record the guard's actual result. Docs/source-audit work does not require .NET runtime tests.
- Out of scope: API implementation, OpenAPI/lock modification without an approved decision, migration/schema/data changes, commit, and push. No package or external side effect.

## Completion history

- None. Start only after the owner-approved scope card under `AGENTS.md`.


