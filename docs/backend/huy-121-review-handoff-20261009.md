# HUY 121 API review handoff — 2026-10-09

Base: `322ba98e67af3c5074266786ce01579f01f60d8a` on `huy-review`. The final commit SHA and hosted CI status are reported in the review chat.

## Scope and result

- Review correction: upload verification now snapshots ordered candidate IDs at the start of each round and processes one ID per worker tick. Removed successes cannot shift later candidates, new arrivals wait for the next finite round, and retryable failures return in that next round. The SQL regression covers 22 retryable failures, a later success, and recovery; another regression covers draining successes plus arrivals between ticks. The prior numeric offset was unsafe on a changing VERIFYING set.
- The incremental SWG-151 migration admits a VERIFIED reporter defect only for a repair-bound POST_REPAIR task in NORMAL or CONDITIONAL_FT mode. Other H3 source and geometry checks remain in place. The existing H4 pin trigger rejects a wrong-defect repair item with SQL 51321; no additional H4 guard was added.
- The isolated schema inventory records the second migration and observed H3 definition. Existing isolated HTTP tests remain in the base commit; they are not new files in this package.

## Verification and provenance

- Current scoped SQL TRX: `tests/RoadGuardSystem.IntegrationTests/TestResults/resume-focused.trx`, 22 passed; wrong-defect TRX `huy121-wrong-defect.trx`, 1 passed. These test result files are local runtime artifacts, not committed evidence.
- Existing isolated production HTTP TRX: `tests/RoadGuardSystem.ApiTests/TestResults/huy121-existing-e2e.trx`, 2 passed: SWG-166/167 correction path and SWG-194 authority path. This is test-host evidence and is excluded from the live API count.
- Earlier pre-final-diff checks recorded full SQL integration 666/666, focused SQL 18/18, repair API 11/11 and build. The focused 22/22 and 2/2 runs cover the later test additions. Final format and diff checks are reported with the commit handoff.
- SQL CI correction: the failed run at `ae2cab15` passed all four SQL lanes (127/135/215/190) but its aggregate identity gate failed on stale `tests/CI/sql-lanes.json` entries. The manifest retains exact-name, count, duplicate, missing, unexpected, and skip checks. Final expected counts are 128/135/215/190. Local final lane 1 passed 128/128; its TRX combined with the unchanged lane 2–4 artifacts passed the aggregate gate at 668 unique identities. Hosted final-SHA results remain separate from this local reconciliation.
- The external fictional fixture and live HTTP evidence pack remains at `D:\DO_AN\swagger-huy-real`; it is not committed. Its sanitized continuation ledger is `evidence/huy121-continuation-20261009.json`, and the gap matrix is `evidence/huy121-gap-matrix-20261009.json`. Fixture generation is reproducible from `fixtures/huy121/build_assets.py` and `fixtures/huy121/facts.json` in that external pack. No credentials, tokens, live URLs, database dump, or runtime log is included here.

The live ledger is 116/121 positive: 113 agent-observed and 3 owner-reported. Newly positive IDs are SWG-029, 141, 142, 143, 144, 145, 150, 152, 159, 160, 161, 162, 163, 166, and 167. Remaining live gaps are SWG-017, 055, 082, 130, and 194.

## Five remaining IDs: isolated production HTTP proof

`tests/RoadGuardSystem.ApiTests/TestResults/huy-five-formatted-final.trx` passed 4/4 focused tests on a separate real SQL catalog through the production HTTP controllers and repositories. The H3 FIELD theory has two cases; each proves SWG-017, 055 and 082. The H6 lifecycle test proves SWG-130, and the LD07 road coverage test proves SWG-194. Sanitized actor, project, source, request and resource IDs are in the external `D:\DO_AN\swagger-huy-real\evidence\huy-five-test-host-positive-20261009.json` pack. These are `TEST_HOST_E2E_POSITIVE`, so live remains 116/121 (113 agent-observed plus 3 owner-reported). Five test-host positives bring unique positive coverage to 121/121; this does not assert 121 live or real construction provenance.

| ID | HTTP | Producer and persisted verification |
|---|---:|---|
| SWG-017 | 201 | Actual FIELD review clock, production `ObserveClocksAsync` ReviewBreach, current Supervisor appointment; persisted responsible actor matches assignee. Test-only clock crosses due time. |
| SWG-055 | 201 | Supported V2 geometry draft/confirm produces impact on a V1-pinned FIELD task; PM decision persists and task keeps its original route pin. Sample 32648 coordinates are TEST_ONLY. |
| SWG-082 | 200 | Production `ObserveCalendarAsync` at controlled Monday recovery with a pending review duty; GET returns one pending duty and three recovery periods. |
| SWG-130 | 201 | Actual defect and mandatory obligation; supported transfer issue/accept, operational closure and renewed scope. History and receiving-project responsibility persist. |
| SWG-194 | 201 | TEST_ONLY isolated SQL prerequisite includes same-project handover, verified file and source pin; production HTTP Supervisor mapping persists, current rights/replay/source guards run, and TEST_ONLY provenance remains non-executable for FT. Public linked-handover producer remains unproved on localhost. |

No production source or migration changed in this five-gap continuation. The focused test fixtures and format-only test edits are the only code changes. Failed setup attempts remain in local TRX history; they were corrected by fixture ordering and clock configuration, with no guard relaxation. Self-review pass 1 checked source producers, actor/project authority, pins and persisted effects. Pass 2 checked TEST_ONLY labeling, separate database/clock boundaries, response/request evidence, changed file scope and unchanged live counts. External review remains separate from these self-reviews.

## Self-review

Pass 1: checked trigger admission against task purpose, mode, binding, reporter source, geometry, and existing H4 cross-defect pin; checked queue ordering and retry progress. Pass 2: checked producer/service/repository signatures, migration Down restoration, inventory hashes, staged file scope, and evidence separation. External review and exact-SHA hosted CI remain pending at commit time.
