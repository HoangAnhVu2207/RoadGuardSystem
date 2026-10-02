# Source and reused runtime provenance

2026-10-02 docs/read-only assessment. Branch/HEAD verified anh / 2efc8a5775f834c7f0fe37cc0ce703011649e1f1. Current source means dirty bytes, not HEAD alone. Owner acceptance of bounded C01 assertions is TARGET_CONFIRMED from the pasted request; old COMPLETE reports are not approval. Current hash/search/archive checks are CURRENT_VERIFIED; TRX/build history is HISTORICAL reused runtime.

| Run / artifact | Exact mapping | Limit |
|---|---|---|
| BOX 1 correction-04 | RF-10-08-C01-correction-04-final.trx: 10 executed/passed. Current/after/archive source SHA-256 a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f | Before hash 6f0bebf1bb2ad34abe8efce2b511556fdd4e901b118bc5e055fb57f2023aceb1 DIFFERENT. Before-build-to-test linkage/source-to-assembly proof NOT_VERIFIED; assembly hash missing |
| BOX 2 correction-03 | RF-10-09-C01-correction-03-final.trx: 5 executed/passed. Current/source SHA-256 f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e | Reused in C04/finalization because test source unchanged; no new5/5 or fresh combined15/15. Matching test bytes alone do not establish production/fixture/build identity |
| Correction-04 build | correction-04-build.log, historical180 warnings/0 errors | Not built this turn, no historical binary reconstructed |
| Correction-04 archive | planning/refactor/evidence/checkpoint13-integration/RF-10-checkpoint-13-correction-04-handoff.tar.gz; SHA-256 974a483e4af551c40de0037136ca0004c4b0c81cd01779c7c7b1e23322b0217f | Actual archive hashes and extracted test/production byte comparison generated in checks; not new runtime |
| Finalization archive | same directory RF-10-checkpoint-13-finalization-handoff.tar.gz; SHA-256 19555b02ceba04c744fdd15aad9fb3de92cc829701a61ad4de8357a3be7fce8a | Actual31 files/27 entries; generator, inventory, summary absent from old manifest. Manifest itself excluded. Old package not modified |
| Survey checkpoint08 | RF-10-03-C02 report and failed/pass TRXs | Before first edit bytes not captured; do not fabricate them from checkpoint07/09 |
| Inspection correction01 | RF-10-07-C01 report and separate checkpoint11 correction01 artifacts | Source-to-binary linkage NOT_VERIFIED due metadata conflict; not the survey snapshot gap |
| Inspection correction02 | Report/baseline and checkpoint12 correction02 evidence | Hashes cover only test + two fixtures; assembly captured; no production fingerprint; build end approximate |

Generated checks/runtime-archive-source.json compares archive test/fixture/production payload bytes with current mapped paths and names every mismatch. Old TRX is never attributed to changed current bytes. Matching entries establish byte identity only; no assembly linkage is inferred. Failed/missing historical evidence listed in checks/historical-evidence-index.json/reference docs stays open; no build/test/DB performed here.

New package before/ hashes were captured in this turn, not claimed as missing old snapshots. payload/baseline.json lists all twelve docs and their before hashes, branch/HEAD/full dirty status and 1000 read-only file hashes. Current full Phase A files, content diffs and two assessment reports are supplied, not replaced by a narrative summary. Source payload paths are reviewer snapshots only, not code edits.
