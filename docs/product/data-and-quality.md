# Data meanings, NFR and acceptance evidence (RF-04 draft)

**Not active.** Business meaning is proposed to live here; physical tables and transactions are in [backend persistence](../backend/persistence-and-operations.md). Source: [RF-02 module matrix](../../planning/refactor/02-requirements-traceability.md), [decision register](../../planning/refactor/02-decision-register.md), HISTORICAL `docs/diagram/V2/03_Data/01_Data_Dictionary.md` and `02_ERD_V2.md`.

| Term | Meaning / distinction | Status and missing proof |
|---|---|---|
| Project membership | Effective role and project scope used at action time, including reconnect; a cached role is insufficient. | PROPOSED interpretation of R03/PR-37, verify effective dates in source and SQL. |
| Road section/version vs segment set | Road input/version is not itself a published segment; a segment keeps version/geometry identity for evidence. | CURRENT_VERIFIED entities and road APIs; GIS publish flow PROPOSED. |
| Report, defect, case, notification | Reporter source, assessed physical issue, grouped workflow and delivery message are different facts; preserving report provenance is required before grouping. | HISTORICAL FR-11..14/BR-29..30; new case model UNKNOWN. |
| Source video/SRT vs dataset vs AI result | Source bytes and pairing/provenance precede dataset snapshot; AI result is derived candidate and cannot replace source or PM decision. | PR-34A/38/44; current parser/provider evidence incomplete CG10/11. |
| Measurement, temporary safety, repair acceptance | Measurement is observation, temporary action reduces hazard, repair execution and acceptance/closure are separate authority states. | PR-32A/35A; numeric Fast Track policy UNKNOWN. |
| Business audit vs technical log | Business history follows project dossier; technical log is operational telemetry. | PR-41A confirmed; per-record retention basis UNKNOWN. |

## Business data definitions and integrity questions

These are meanings and required relationships, not a physical ERD or permission to add a column. The source-level fields and migration evidence must be checked at each module checkpoint.

| Concept | Identity, provenance and relationship | Required distinction / unresolved data rule | Source / task |
|---|---|---|---|
| Project and primary PM | Project identity; one effective primary PM plus assignment history. | Membership validity at action time; handover/warranty basis can differ by road section. | BR-01, FR-04 historical; RF-10-02 |
| Road version and segment | Road/branch source geometry, original CRS, metric CRS, station origin, width profile; segment references the version. | Suggested 100 m is configurable pilot advice; published geometry and remainder decision need PM record. | BR-34..37 historical, PR-39A confirmed; RF-10-02 |
| Warranty obligation | Project/section/contract scope, handover date, start/end and supporting document. | Multiple obligations and unknown end must retain evidence; effective retention basis remains unresolved. | BR-45 historical, PR-41A confirmed; RF-10-02/09-A |
| Survey plan and task | Band, road/version/scope, assignee, time window, actor and version. | Old and V2 records may represent the same work; migration identity/consumer mapping unknown. | FR-26, US-04/05 historical, CG04; RF-10-03 |
| Source file and dataset | Immutable uploaded video/SRT, checksum, size, owner, project/scope; dataset references verified source set and submitter. | File admission and 8 GiB video target confirmed; `int` size current limitation, pairing and dataset immutability need proof. | PR-38 confirmed, CG10/17; RF-10-04/03 |
| Coverage assessment and baseline | Position, quality and coverage each keep result, method/version and reviewed dataset; baseline points to selected evidence. | UNKNOWN differs from PASS/FAIL; no invented confidence threshold or baseline when method absent. | FR-28/30, BR-40/41 historical, CG06; RF-10-03 |
| Report, candidate, defect, case | Every Reporter/AI/field observation retains distinct source, owner and evidence; PM decision links to a physical defect and optional dossier. | Nearby GPS or repeated reports do not merge automatically; public Reporter projection cannot expose other identities. | PR-33A/34A confirmed, BR-29/30 historical; RF-10-06 |
| Measurement and policy | Measured values, units, image, actor, task mode and policy version evaluated. | Measurement-only batch never grants repair; numeric eligibility/configuration remains missing. | PR-32A confirmed, BR-08..12 historical; RF-10-07 |
| Repair item and attempt | Per-item proposal/approval, assigned Crew, attempt/evidence and acceptance record. | Rejected proposal leaves defect open; failed attempt and recurrence after closure are distinct histories. | BR-21..28 historical; RF-10-07 |
| Temporary safety action | Separate PM authorization, scope, Crew, evidence and Supervisor notification. | Completion does not close defect or replace final acceptance. | PR-35A confirmed, BR-46 historical; RF-10-07/09-A |
| Offline operation and receipt | Original actor/device, project, command identity, payload fingerprint, task/policy version, durable receipt. | Handover encryption/key custody and conflict record need Android contract; pending evidence cannot be deleted on failed sync. | PR-42A/43A confirmed, BR-15/16 historical; RF-10-08 |
| Processing attempt and AI candidate | Job, active attempt, immutable source/model/config, provider receipt and returned candidate. | Late/duplicate result must not replace active result; provider protocol and training label authority separate. | PR-34A/44 confirmed, CG11; RF-10-05/06 |
| Business event and notification | Aggregate/version and actor audit are durable business facts; notification is a delivery projection with recipient/read state. | Outbox transaction and consumer receipt/lease must be verified; notification is not a dossier or ticket. | BR-47 historical, R17; RF-10-09-A |

Source content with conditions and acceptance has been transferred by ID to [FR/BR](historical-fr-br.md), [US](historical-us.md), [PF](historical-pf.md) and [NFR](historical-nfr.md). Their old CHỐT/KẾ THỪA labels remain provenance, while each unconfirmed detail has a module gate in the source crosswalk. The data owner must answer the listed rules before accepting a schema or data migration.

## Quality targets and proof

PR-40 confirms **targets**, not measured results: 50 concurrent users, metadata-server p95 <= 2 seconds, RPO <= 15 minutes and RTO <= 4 hours. RF-10-09-B owns the benchmark design with RF-10-09-A backup/restore coordination; RF-11 checks release evidence. They must define workload, sampling interval, percentile calculation, benchmark environment and restore drill. Current result: **UNKNOWN**. Do not report PASS from a build or unit test.

PR-38 file/dataset limits are pilot admission rules. Current source rejects files over `int.MaxValue`; no 8 GiB upload proof exists. Test accepted/rejected byte boundaries, resumable restart, checksum and storage/SQL consistency in an isolated environment after RF-09/10-04 approval. PR-41A retention durations are project policy; `WAITING_RETENTION_BASIS` blocks destructive cleanup when warranty end or obligations are unknown. A dry-run count, hold case, backup behavior and restore rehearsal are required before deletion.

Frontend/Android/AI acceptance is external under PR-44: BE publishes approved wire fixtures and consumer compatibility evidence; an in-repo Postman request is not confirmation from a deployed consumer. For offline PR-42A/43A, test geometry/task availability, original actor provenance, duplicate replay, stale permission and unsynced-evidence preservation with the Android owner. AI integration needs provider receipt/attempt replay and immutable source/model provenance before being called verified.
