# DOC-AUDIT-01 — Document index và source-of-truth map

**ACTIVE**, source baseline `f0c8762509310b2779068891062267c5f520aff2`, branch `huy-review`. Inventory covers **2976 tracked/in-scope files** (2968 baseline + 8 new; **931 Markdown**) under docs/contracts/planning. Counts include schemas, fixtures, codegen/validator assets and media, not only prose. Every file has one ACTIVE/PROPOSED/HISTORICAL row in [CSV](DOC-AUDIT-01-DOCUMENT-INDEX.csv): reason, scan limits, baseline/current hash, changed flag and missing local reference targets.

| Classification | Files | Meaning |
|---|---:|---|
| ACTIVE | 123 | Current entrypoint/accepted authority/local scope or manual asset. ACTIVE does not imply runtime passed. |
| PROPOSED | 76 | Unadopted V2/FE/AI/schema/design material; current reconciliation at top is sourced, original body remains proposal/history. |
| HISTORICAL | 2777 | Frozen checkpoint/old plan/old generated SQL/operational evidence; preserved, not current feature absence or PASS. |

## Precedence và where to answer questions

| Question | Authority/evidence | Limits |
|---|---|---|
| Expected behavior | [confirmed32–44](../product/confirmed-decisions.md), [assigned HUY-FINAL](../../planning/development/HUY-FINAL-INTEGRATION.md) latest ND01/activation; [GET Projects](../../planning/development/GET-PROJECTS.md), [Supervisor global](../../planning/development/SUPERVISOR-GLOBAL-PROJECT-AUTHORITY.md), [Staff Read](../../planning/development/STAFF-MEMBERSHIP-READ.md) | Current owner assignment supersedes conflicting historic excerpts, not all unreviewed V2 requirements. |
| What backend implements | [backend](../backend/README.md), [43-row matrix](DOC-AUDIT-01-MATRIX.md), exact Controller/DTO/service/repository source | Source observation is not accepted business authority or complete workflow certification. |
| Actual local routes/wire | [241-operation runtime inventory](DOC-AUDIT-01-API.md), actual DTO/controller; [local Huy](../../contracts/http/huy-final.local-contract.md), [local Anh](../../contracts/http/anh01.local-contract.md) | Local adopted scope only; V2 YAML/FE lock not equivalent; G06/G07 remain open. |
| Physical data/relationships | [fresh dictionary/ERD mapping](../backend/data/audited-data-model.md), RoadGuardDbContext/configs/current2migrations/snapshot | Owned SQL observed191 tables, not deployed/shared schema. Original current-* RF06 docs retained historical. |
| Actors/IDs/version/recovery | [workflows](../product/workflows.md), API inventory and matrix | No blanket CRUD/ETag/paging rule; memberships versus account/project version kept distinct. |
| Tests actually run | [report](DOC-AUDIT-01-REPORT.md), [evidence manifest](DOC-AUDIT-01-EVIDENCE.json), fresh external TRX/logs | Source baseline frozen; documentation-only commit does not change tested implementation. External E2E/hosted CI not implied. |
| Manual API usage | [Staff Postman guide](../postman/Staff-Membership-Read-Guide.md), Supervisor Projects/staff/optional primary PUT collections | Parsing/request script is not actual Postman Runner PASS. No hardcoded credentials added. |
| Risk and missing acceptance | [open gaps](DOC-AUDIT-01-OPEN-GAPS.md) | Source gap, un-reproduced risk and external gate separated. |

V2 133-operation YAML, FE generated schemas/types/lock and AI proposal are preserved without wire edits. Their original **canonical** wording only names the earlier draft bundle, not active runtime authority. Old FR/BR/UC, historical148 requirements/701 excerpts and RF checkpoints are review material, not automatic implementation assignments. ADR001–004 accepted boundaries remain references with supersession notices; ADR005 proposed, ADR006 older delivery routing historical. planning/development specs are authority within assigned scopes; chronological ledgers/summary results are historical execution evidence unless rerun.

All text assets were scanned in full for status, links and stale-claim terms; active module claims were traced through matrix/source and selected execution. Binary media/PDF/gzip only metadata/hash inventoried; no claim of full visual or decompressed payload review. External URLs and heading-anchor semantics were not network validated. Local path existence checks skip fenced/inline code and URL schemes; G08 records missing historical/proposed references rather than rewriting snapshots. This is documentation alignment, not a certificate that every document or endpoint has an E2E test.

CSV omits self-referential current digests for the report/index/manifest; final committed file digests and final SHA are in the external handoff manifest. A source hash does not confer business adoption.
