> CURRENT_VERIFIED iteration 8 final (2026-10-23): exact Owner-approved B2 implemented and isolated verification executed. Genuine SUBMITTED and PM-final Supervisor delivery now have Backend SQL producer-to-inbox evidence: global authority, reassignment, recipient-specific revocation, retry/dedup, concurrency and rollback. No Supervisor approval transition or Normal clock reuse. RC7 build/cold-start, collections/AI frames, B2 HTTP, browser and restart persistence PASS within recorded bounds. OpenAPI documentation errata corrected against exact260-operation runtime; original failures retained. See B2-IMPLEMENTATION-ACCEPTANCE.md for evidence/limits. Independent implementation review PENDING; FE_INTEGRATION_PREVIEW, FULL_CONTRACT=0. Historical unsupported/pending/RC5/RC6-in-progress statements below are superseded only for current B2. No Git publication.

> CURRENT_VERIFIED iteration 8 final (2026-10-23): exact Owner-approved B2 implemented and isolated verification executed. Genuine SUBMITTED and PM-final Supervisor delivery now have Backend SQL producer-to-inbox evidence, including global authority, reassignment, recipient-specific revocation, retry/dedup, concurrency and rollback. No Supervisor approval transition or Normal clock reuse. RC7 build/cold-start, 11 collections/AI frames, B2 HTTP, OTP and restart persistence PASS within recorded bounds. OpenAPI documentation errata (unused test schema and 16 encoding-corrupted descriptions) corrected against exact 260-operation runtime; initial failures retained. See B2-IMPLEMENTATION-ACCEPTANCE.md and iteration8 handoff for precise results and limits. Independent implementation review PENDING; FE_INTEGRATION_PREVIEW, FULL_CONTRACT=0. Historical unsupported/pending/RC5/RC6-in-progress statements below do not describe this current B2 checkpoint. No Git publication.

# B2 Supervisor source limitation — independent review addendum

## Current post-RC5 decision overlay

The latest B2_OWNER_DECISION_REVIEW is a reviewer recommendation, not a new Owner approval. Both genuine Supervisor-positive and PM-final information remain required by that recommendation and unresolved for release. The revised exact design is [B2-EXACT-ASSIGNMENT-PROPOSAL.md](B2-EXACT-ASSIGNMENT-PROPOSAL.md), PROPOSED_NOT_APPROVED. It supersedes the historical options/recommendation below, including any assumption that a Supervisor requires ProjectMember. Current source supports global Supervisor authority but the delivery path still imposes membership. Reassignment between SUBMITTED and final and two independent event snapshots are explicitly proposed; no route, schema or production implementation has been changed. RC5 is retained immutable. Historical statements below describe their earlier checkpoint only.

Task: RG-BE-FE-FINAL-HANDOFF-CLOSEOUT. Branch: huy-review. HEAD: dd94aa697c411bb11d305973580055216f65ba73 (dirty working tree).

Release remains FE_INTEGRATION_PREVIEW. Independent REVIEW_PASS is NOT_GRANTED. This documentation addendum does not alter source, policy, existing test evidence or RC3.

## CURRENT_VERIFIED — source boundary

| Source | Observed behavior |
|---|---|
| `RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Producing.cs:405–411` | Genuine formal intake persists SUBMITTED and snapshots FastTrack recipients into the notification event. |
| Same file `:666–677` | Inside the serializable producer transaction, snapshots all eligible current project PMs. |
| Same file `:679–683` | Supervisor requires exactly one unfinished clock in the exact project and RepairItem, appointed to Supervisor, of kind SupervisorInitialApproval or SupervisorFinalConfirmation. Missing or ambiguous source produces null Supervisor/Clock IDs. |
| Same file `:506–510` | SupervisorInitialApproval is created only for Normal mode. |
| Same file `:230–233` | SupervisorFinalConfirmation is created only for Normal mode. |
| `RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs:17,27–28,38–50` | TargetId and AppointedActorId/AppointedRole identify the duty. Appointment supports existing review duty roles; it does not create a FastTrack assignment source. |
| `RoadGuardSystem.API/Controllers/ClockDutyController.cs:16,22–24` | Existing POST /api/v1/projects/{projectId}/clocks/{clockId}/appointments appoints an existing clock; it cannot supply a missing FastTrack duty. |
| `RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Delivery.cs:36–44,124–134` | Delivery consumes pinned recipients and revalidates responsibility and the stored Supervisor clock/appointment; lost assignment is denied. |

The missing behavior is an authoritative FastTrack Supervisor assignment producer and its business/contract definition. The lookup exists, but genuine FastTrack SUBMITTED does not create either qualifying Supervisor approval duty. Project membership, Supervisor role and Crew assigner are not assignment evidence. Creating a Normal approval clock would change workflow semantics and is prohibited.

## TARGET_CONFIRMED — existing Owner decision

Owner checkpoint B2_SUPERVISOR_ASSIGNMENT_AUTHORITY requires PM delivery to continue, null SupervisorId/ClockId and unresolved recording when no unique source-backed exact-item duty exists. It prohibits fabricated appointments, role-based recipient inference, Normal duties and new Supervisor approval transitions in FastTrack.

That decision establishes safe fallback behavior. It is not an explicit release acceptance waiver for the actual Supervisor-positive branch. Reviewer therefore retains B2 acceptance as open.

## Evidence classification

- Genuine FastTrack PM-positive and missing-Supervisor fallback: previous executed results are preserved in outputs 1300–1306; not rerun for connector recovery. Their independent review remains pending.
- Actual genuine FastTrack Supervisor-positive delivery: **NOT_SUPPORTED_BY_CURRENT_SOURCE**, not PASS. Synthetic appointment tests cannot establish this producer path.
- This addendum: source inspection only; no new runtime PASS, test run or FULL_CONTRACT claim.

## PROPOSED — minimal resolution options

| Option | Exact decision needed | Impact / recommendation |
|---|---|---|
| 1. Explicit release scope deferral | Owner explicitly excludes actual FastTrack Supervisor-positive delivery from this release acceptance, accepting PM delivery plus unresolved assignment fallback. Reviewer assesses remaining gates. | Recommended smallest change; no workflow/API/data mutation. Existing fallback approval alone is insufficient to infer this waiver. |
| 2. Notification-only assignment source | Owner defines assignment actor, exact RepairItem/project binding, creation/update/revocation lifecycle, uniqueness, event snapshot timing, route/DTO/errors and compatibility. | New contract/design gate, then implementation and genuine producer-to-delivery acceptance. No Supervisor approval state or Normal duty. |
| 3. Reuse a genuine source | Demonstrate an existing authoritative FastTrack assignment producer, source data and contract for the exact item. | No such producer established by current source inspection. Do not fabricate production/fixture evidence to claim support. |

No option is self-approved. Existing A1/B2/C1 policy decisions remain valid. Git publication, contract acceptance and fresh-clone acceptance remain independently gated.

## Preservation and self-review

Outputs 1300–1306 and previous failures remain immutable. Recovery hashes are stored outside the repository in REVIEW-RECOVERY-PRESERVATION-BEFORE.json and verified after this additive documentation change. RC3 remains immutable; this addendum is outside its payload.

Self-review 1: checked exact producer conditionals, recipient snapshot and delivery revalidation; distinguished unavailable source from a query defect and from an executed positive test.

Self-review 2: checked scope and authority; no business source, test, route, DTO, state, database, Docker resource or Git history changes; no release waiver inferred from Owner fallback decision.
