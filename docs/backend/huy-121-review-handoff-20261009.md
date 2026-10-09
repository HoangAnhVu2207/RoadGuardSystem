# HUY 121 API review handoff — 2026-10-09

Base: `322ba98e67af3c5074266786ce01579f01f60d8a` on `huy-review`. The final commit SHA and hosted CI status are reported in the review chat.

## Scope and result

- Upload verification now advances through a stable `(ExpiresAt, Id)` cursor, so retryable failures at the front of the queue do not starve later sessions. The 22-session SQL regression checks a success after 21 failures and recovery of the first session.
- The incremental SWG-151 migration admits a VERIFIED reporter defect only for a repair-bound POST_REPAIR task in NORMAL or CONDITIONAL_FT mode. Other H3 source and geometry checks remain in place. The existing H4 pin trigger rejects a wrong-defect repair item with SQL 51321; no additional H4 guard was added.
- The isolated schema inventory records the second migration and observed H3 definition. Existing isolated HTTP tests remain in the base commit; they are not new files in this package.

## Verification and provenance

- Current scoped SQL TRX: `tests/RoadGuardSystem.IntegrationTests/TestResults/resume-focused.trx`, 22 passed; wrong-defect TRX `huy121-wrong-defect.trx`, 1 passed. These test result files are local runtime artifacts, not committed evidence.
- Existing isolated production HTTP TRX: `tests/RoadGuardSystem.ApiTests/TestResults/huy121-existing-e2e.trx`, 2 passed: SWG-166/167 correction path and SWG-194 authority path. This is test-host evidence and is excluded from the live API count.
- Earlier pre-final-diff checks recorded full SQL integration 666/666, focused SQL 18/18, repair API 11/11 and build. The focused 22/22 and 2/2 runs cover the later test additions. Final format and diff checks are reported with the commit handoff.
- The external fictional fixture and live HTTP evidence pack remains at `D:\DO_AN\swagger-huy-real`; it is not committed. Its sanitized continuation ledger is `evidence/huy121-continuation-20261009.json`, and the gap matrix is `evidence/huy121-gap-matrix-20261009.json`. Fixture generation is reproducible from `fixtures/huy121/build_assets.py` and `fixtures/huy121/facts.json` in that external pack. No credentials, tokens, live URLs, database dump, or runtime log is included here.

The live ledger is 116/121 positive: 113 agent-observed and 3 owner-reported. Newly positive IDs are SWG-029, 141, 142, 143, 144, 145, 150, 152, 159, 160, 161, 162, 163, 166, and 167. Remaining live gaps are SWG-017, 055, 082, 130, and 194.

## Self-review

Pass 1: checked trigger admission against task purpose, mode, binding, reporter source, geometry, and existing H4 cross-defect pin; checked queue ordering and retry progress. Pass 2: checked producer/service/repository signatures, migration Down restoration, inventory hashes, staged file scope, and evidence separation. External review and exact-SHA hosted CI remain pending at commit time.
