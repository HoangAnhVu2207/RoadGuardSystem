> CURRENT_VERIFIED iteration 8 final (2026-10-23): exact Owner-approved B2 implemented and isolated verification executed. Genuine SUBMITTED and PM-final Supervisor delivery now have Backend SQL producer-to-inbox evidence: global authority, reassignment, recipient-specific revocation, retry/dedup, concurrency and rollback. No Supervisor approval transition or Normal clock reuse. RC7 build/cold-start, collections/AI frames, B2 HTTP, browser and restart persistence PASS within recorded bounds. OpenAPI documentation errata corrected against exact260-operation runtime; original failures retained. See B2-IMPLEMENTATION-ACCEPTANCE.md for evidence/limits. Independent implementation review PENDING; FE_INTEGRATION_PREVIEW, FULL_CONTRACT=0. Historical unsupported/pending/RC5/RC6-in-progress statements below are superseded only for current B2. No Git publication.

> Current checkpoint 2026-10-23: OWNER_B2_EXACT_CONTRACT_APPROVED. Exact B2 assignment GET/PUT/DELETE, global Supervisor authority, ETag/idempotency, versioned snapshots, revocation and both genuine events are TARGET_CONFIRMED. Implementation/isolated verification in progress. Huy/Codex is sole writer for this slice including shared schema/DI. Historical pending wording below is superseded. No Supervisor approval transition; no release PASS or Git publication.

# B2 exact notification assignment and final information — revised proposal

Task: RG-BE-FE-FINAL-HANDOFF-CLOSEOUT. **TARGET_CONFIRMED / IMPLEMENTATION_IN_PROGRESS.**

Owner decision 2026-10-23: OWNER_B2_EXACT_CONTRACT_APPROVED approves this entire exact design, including GET/PUT/DELETE, global Supervisor authority, ETag/idempotency, versioned assignment snapshot, revocation, SUBMITTED and PM-final information. No Supervisor approval transition. Approval does not grant release PASS or Git publication.

Historical proposal and pending-decision wording below is retained as design history and is superseded by this current approval.
Base: huy-review, dd94aa697c411bb11d305973580055216f65ba73 plus preserved dirty sources.
Latest B2_OWNER_DECISION_REVIEW recommends keeping both Supervisor-positive and post-final information mandatory; OWNER_NEW_DECISION remains PENDING. This replaces the earlier proposal's project-membership and pre-SUBMITTED-only assumptions. It neither waives release scope nor authorizes implementation.

## Source and authority reconciliation

| Source | Observed meaning / exact gap |
|---|---|
| planning/development/HUY-FINAL-INTEGRATION.md:654, R03 | TARGET_CONFIRMED: PM checks/confirms FastTrack; Supervisor receives information. No Supervisor approval transition. |
| RoadGuardSystem.Repositories/Implementations/Messaging/BusinessDutyRepository.cs:24–32 | CurrentAuthority validates current actor/role and project existence; Supervisor does not inherently require ProjectMember. PM requires current membership. |
| RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Scope.cs:15–23 | Supervisor project visibility is global, but inbox is still restricted to RecipientUserId. Global scope is not broadcast authority. |
| RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Delivery.cs:122–156 | Snapshot Supervisor checks an existing clock appointment, then common recipient check requires ProjectMember. This conflicts with global Supervisor semantics for a future exact notification assignment. No genuine FastTrack producer currently supplies that assignment. |
| RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Retry.cs:35–43 | Previously pinned recipient is never replaced during retry. Preserve this rule; no retrospective reassignment of an event. |
| RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Producing.cs:226–239 | Supervisor final clock and PM_REVIEWED handoff are Normal only. Do not reuse them for FastTrack. |
| Same file:33–88 | ConfirmFinalAsync persists final RepairDecision/resolution/audit. It does not currently produce a genuine FastTrack final-information outbox event. This is the proposed second event origin, not a new transition. |

Recommendation: a narrow B2 assignment-backed Supervisor authority branch uses the existing current-actor/global-Supervisor rule plus exact project/item/assignment proof. It does not remove the common membership check for unrelated recipients/events. Retain user active, role active, password/session policies, project/resource existence, assignment/current version and revocation checks. PM membership and Crew source assignment rules remain unchanged. No role-wide selection.

## Exact resource and lifecycle proposed for Owner approval

**Authority amendment being requested:** Owner checkpoint B2_SUPERVISOR_ASSIGNMENT_AUTHORITY previously accepted only a unique source-backed DeadlineClock appointment. Approval of this proposal must explicitly supersede that clock-only restriction for these two FastTrack notification events only, using the notification assignment generation instead. Until approval, the existing clock-only fallback remains in force. Normal approval clocks, appointments, deadlines and notifications retain their existing semantics.

Current project PM alone manages one notification-only assignment for the exact `(projectId, packageId, itemId)`. Parent package/item must agree and item must be current FAST_TRACK. Recipient must be one current active Supervisor with active role and valid existing global authority in the exact resource context; **ProjectMember is not required**. Assignment grants no repair action, approval, final authority, deadline or clock.

Maintain one durable resource head per item, including after revocation, and append-only assignment generations/audit. Head has rowversion; a filtered unique active generation plus serializable item/head locking prevents two active recipients. Replacement creates a new assignmentId/version, retires the old generation and audits both; same Supervisor replacement still creates a new generation. Never rewrite old event snapshots.

Create/replace is allowed before SUBMITTED and after SUBMITTED/PM ACCEPT **until committed PM final**, provided item is current and not superseded/cancelled. Revoke is allowed after final too, to suppress pending delivery/new assignment-backed reads. Create/replace after final returns409; it cannot replay final information to a new recipient. Concurrent assignment write and event commit serialize on the same item/head: the snapshot references exactly the assignment generation committed before that event, or null. Reassignment never emits a business review/final event.

| Timing / action | SUBMITTED event | PM-final informational event |
|---|---|---|
| Supervisor A assigned before SUBMITTED | Immutable snapshot A/generation1 | Independently snapshot current valid assignment at final |
| A replaced by B between SUBMITTED and final | Retain A/generation1; no B retroactive delivery. Suppress undelivered A because generation retired; record unresolved | Snapshot B/generation2 if currently valid |
| A revoked before final | Keep original snapshot/audit; no substitute or role lookup | Snapshot null/unresolved if no replacement |
| Missing at SUBMITTED, assigned B before final | SUBMITTED remains null/unresolved on retry | Final snapshots B; absence at first event does not prevent final delivery |
| Final delivered, assignment later revoked | Do not erase delivery/audit history or retract facts already seen | Deny new assignment-backed inbox detail/list/read access and suppress pending retry; retain private originals |
| Assignment changed concurrently with final | Serializable ordering determines one pinned generation or null; never mixed IDs/version | Same exact rule; rollback leaves neither partial final event nor delivery |

For the Supervisor recipient only, inbox filtering for these two new B2 assignment-backed event types must apply recipient ownership and current exact assignment generation plus actor/global-Supervisor/resource authority **before pagination**. PM deliveries and inbox records from SUBMITTED remain governed by their own current actor/role/project authority and immutable PM snapshot; they do not require a Supervisor assignment. Revoking/replacing/missing Supervisor never suppresses PM inbox list/detail/scope/mark-read or delivery/retry. Do not change visibility of unrelated historical notification types. This proposed revocation behavior needs explicit approval alongside the lifecycle; delivery-only revocation would be a different contract.

## Two distinct immutable event contracts (internal proposals)

1. EVENT_1 uses existing `repair.work.submitted.v1`, anchored to the committed genuine SUBMITTED RepairItemLifecycleEvent ID, formal intake link/submission and attempt. Snapshot all authorized PM IDs and one valid assignment generation, atomically with lifecycle/outbox. Preserve current PM delivery/unresolved fallback.
2. EVENT_2 reuses the registered `repair.work.fast_track_confirmed.v1` / FastTrackConfirmedInformation (NotificationEventEnvelope.cs:47; NotificationRegisteredTypes.cs:9), adding its missing genuine producer, anchored to the **actual committed RepairDecision ID** from successful FastTrack ConfirmFinalAsync. Payload pins project/package/item, final decision/review/intake/attempt, server final timestamp and current assignment generation/Supervisor (or null). Recipient is the one pinned Supervisor only; PM already performs final and receives no newly invented final notification. Insert outbox/audit within the existing final transaction. It is informational, has no ACK/approval/deadline semantics, and does not reuse PM_REVIEWED or Normal clocks.

Each event has its own immutable origin ID/type. Dedup identity is `(event type, immutable origin ID, recipient ID or unresolved slot, INTERNAL channel)` with unique durable occurrence/delivery constraint. Retry retains the original snapshot and rechecks assignment generation/current authority; it never swaps recipients or creates an event from a proposal/fixture. Duplicate final command replay returns original receipt and creates no second event. Same key/different payload remains409. Missing assignment records null/unresolved; final resolution and PM delivery are not blocked. Revoked/replaced assignments remain unresolved, not rerouted. Existing bounded retry cadence/attempt audit is retained, with no new infinite retry worker or external channel.

Exact EVENT_2 H6 wire mapping proposed: `schemaVersion=1`, `eventId=OutboxMessage.Id`, `kind="FAST_TRACK_CONFIRMED"`, `projectId=RepairItem.ProjectId`, `sourceKind="RepairWork"`, `sourceId=RepairItem.Id`, `originEventId=RepairDecision.Id`, `sourceRevisionId=RepairDecision.Id`, `occurredAtUtc=the persisted server final timestamp` (identical to outbox OccurredAtUtc), `responsibleUserId=null`, `scheduledAtUtc=null`. Envelope domain kind is FastTrackConfirmedInformation; serialized H6 kind is the catalog action string, not the enum name. The adapter must prove that exact immutable decision is the successful FastTrack PM final decision for the exact item/project/intake/review/attempt. Assignment snapshot is the sole Supervisor recipient proof; ResponsibleUserId must never select/substitute a recipient. Missing snapshot assignment remains unresolved. Additive parser/adapter support is part of the proposed implementation, not a claim that today's catalog already authenticates this new producer proof. Existing legacy final events, if any, retain their old path and are never upgraded by inference.

### Stored-event compatibility and rollout proposal

Keep existing envelope SchemaVersion=1 and registered message names; do not rename the final event or change existing kind numeric values. Preserve the exact legacy FastTrackReviewSnapshot fields/validation (CapturedAt, ProjectManagerIds, SupervisorId, SupervisorClockId). Add a separate optional `notificationAssignmentSnapshot` object with `schemaVersion:1`, capturedAt, projectManagerIds, supervisorId, assignmentId and assignmentVersion. For SUBMITTED it contains the PM snapshot; for final it contains an empty PM array. Supervisor/assignment ID/version are either all present or all null. New events use only this assignment snapshot, never a fabricated SupervisorClockId. Readers reject ambiguous dual-snapshot, unsupported version, malformed or missing required proof as unresolved/invalid; never infer current assignment as a replacement.

Existing stored outbox/delivery/inbox records without this new object remain legacy: preserve pinned recipients, nulls, clock verification, old membership policy, occurrence/delivery identities and retry/receipt behavior. Do not backfill assignments, rewrite payloads or make historical nulls positive. New assignment-backed inbox revocation applies only to records explicitly linked to this versioned proof. Upgrade consumers/readers before enabling the new producer; old dispatch workers must not consume assignment-snapshot events (quiesce the dispatcher during the coordinated local upgrade). Additive nullable persistence and indexes preserve old rows. No rolling mixed-version compatibility is claimed without an explicit additional test. This is proposed deployment behavior, not permission to restart shared workers now.

## Exact proposed HTTP wire (new, not adopted)

Only one resource path:
`/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`

| Method | Preconditions / body | Success |
|---|---|---|
| GET | Current PM in exact project; no body | 200 `{assignmentId,supervisorId,status,assignedAt,revokedAt,version}`; status ACTIVE or REVOKED. 404 before resource was ever created. Head ETag; private,no-store. |
| PUT first create | `Idempotency-Key`, `If-None-Match: *`; body exactly `{supervisorId: uuid}` | 201, Location same resource, active representation and head ETag |
| PUT replace/reassign revoked head | `Idempotency-Key`, `If-Match` current head ETag; same body | 200 with new generation, active representation and new head ETag |
| DELETE revoke | `Idempotency-Key`, `If-Match` current head ETag; no body | 204, new head ETag. Retain revoked head/generation/audit; retry same receipt 204. Fresh revoke of already-revoked head 409. |

All three actions require narrowly scoped WebCookieEligible metadata; writes additionally retain existing CSRF validation for cookie transport. Bearer remains supported. Body IDs only identify the requested Supervisor. Actor/project/item identity comes from authenticated principal/route. Head ETag is quoted base64 SQL rowversion; no item version is substituted. GET has no pagination. No list/search, public DTO or anonymous route is proposed. All responses use Cache-Control: private, no-store. UUID fields serialize as UUID strings, timestamps as UTC ISO8601, version as the unquoted base64 rowversion string matching ETag; only revokedAt is nullable. A revoked head reports its last generation, not a new assignment.

Validation precedence proposed: authentication/session → cookie CSRF when applicable → current PM authority for route project → exact parent/item resource → envelope/header/body validation → idempotency replay with **current resource/actor checks** → resource concurrency → current recipient authority and lifecycle → atomic assignment/audit/receipt. Same key and identical command returns the stored status/body/ETag before evaluating stale resource preconditions; this receipt is explicitly historical and never restores its generation or grants recipient access. Replaying after replacement/revocation does not return the new head; callers GET current state separately. A different payload with the same key returns existing idempotency conflict. New commands always check current recipient authority. Replay never generates either notification event.

Assignment command fingerprint is SHA256 of deterministic UTF-8 canonical JSON containing uppercase method, exact route resource UUIDs (project/package/item), body supervisorId or null, and normalized conditional header kind/value (If-Match rowversion or If-None-Match `*`). Normalize UUID spelling and JSON order, not ETag bytes. Receipt scope includes authenticated actor, operation/resource and key. Changed method/resource/body/conditional headers are different commands; a conflicting reuse within receipt scope is409. Original create/replace replay may return a historical receipt after revocation but cannot restore access. This is separate from Supervisor assignment-backed inbox operations: **list, detail, scope and mark-read (including a previously successful mark-read receipt replay)** must first revalidate current actor/role, recipient ownership, exact resource and active matching assignment generation. Revoked/replaced generation returns existing concealed not-found behavior on detail/scope/mark-read; list excludes it in SQL before cursor/order/limit. Never return cached notification content from an old successful receipt after denial. Keep the audit/receipt unchanged and make no new mark-read mutation on denied replay. PM inbox operations use existing current PM authority and do not depend on Supervisor generation.

| Status | Exact proposed reason/code |
|---|---|
| 401 | Existing unauthenticated/invalid or revoked-session behavior; do not invent a new auth envelope |
| 403 | Existing CSRF error; or `access_forbidden` for wrong role/no current PM scope |
| 404 | `not_found` for missing/mismatched package/item; absent/inactive/non-Supervisor recipient without disclosing identity; GET/DELETE never-created head |
| 400 | `validation_error`: invalid UUID/body/unknown or duplicate fields, malformed key/ETag, both conditional headers |
| 428 | `precondition_required`: required conditional header missing |
| 412 | `precondition_failed`: stale If-Match or initial If-None-Match against an existing head |
| 409 | `invalid_state_transition`: non-FAST_TRACK, superseded/cancelled item or create/replace after final; `notification_assignment_revoked` for fresh duplicate revoke; existing idempotency conflict for mismatched replay |

Use existing ProblemDetails/code/correlationId shape. This is an exact **new** wire proposal; it does not change old repair status conventions. Owner approval must cover routes, DTO/nullability (`revokedAt` nullable), ETag/precondition/status precedence, global Supervisor authority, additive persistence, snapshot/delivery/inbox revocation and both event lifecycles. A current resource head is needed to safely reassign after DELETE; do not treat revoked head as initial create.

## Acceptance required after exact approval

Genuine assign A → Crew execution/finish/SUBMITTED → all authorized PM inbox + A inbox. Then replace A with globally authorized B **without ProjectMember** → actual PM ACCEPT → PM final → B informational inbox and durable resolution. Prove distinct event origins/dedup, no retroactive SUBMITTED for B, suppression/revocation, post-final rejection of replacement, missing/unresolved continuity, inactive/wrong role and wrong-project item denial, cookie/CSRF/session/Bearer, current inbox filtering, concurrent assignment/event snapshots, retry/replay, rollback/audit and unchanged Normal duty behavior. No positive may rely on synthetic Normal clocks. This proposal itself is not executed acceptance.

## Decision requested

Approve the exact revised package above (including EVENT_2 and global Supervisor semantics), or amend specific rows. Reviewer recommendation A is not approval. Until Owner decides, both genuine Supervisor-positive and final-information producer remain release blockers and no B2 source/API/schema changes are authorized.
