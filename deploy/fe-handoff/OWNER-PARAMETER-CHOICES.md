> HISTORICAL recommendations, superseded by owner decision A1_B2_C1_FINAL_POLICY_APPROVAL. See OWNER-DECISIONS-FINAL.md for adopted policy and remaining exact compatibility gates. Rows below are not current choices or authority.

# Task34 owner parameter decisions

Status: PROPOSED; no row below is an approval or enabled public behavior.
G02–G06 are already adopted; this table concerns only remaining A1/B2/C1 gates.
Private access TTL300 and D1 non-deletion remain unchanged.

| Decision | Specific choices | Recommendation and FE impact |
|---|---|---|
| A1 principal registration/mint | Backend-admin registered service subject with Backend minting; or independently operated issuer with explicit trust configuration | Backend registration/mint, dedicated non-human subject, separate audience from FE. Owner must supply issuer URI, audience string, service subject and authorized registering actor before implementation. Never reuse FE credentials. |
| A1 token profile | Per-attempt short-lived token; or reusable service token plus separately authenticated attempt binding | Per-attempt token: proposed TTL300s, skew30s, required sub/jobId/attemptId/jti, exact binding to current persisted attempt. Owner must adopt claim names and payload/header placement; this adds no guessed public fields today. |
| A1 keys/callback | Asymmetric signing with key ID and rotation; or existing shared-key trust under explicitly adopted custody | Prefer asymmetric signing, private key held by minting service, verifier public keys with overlap until token expiry. Bearer callback to existing route, preserving current payload/status semantics where compatible. Owner must adopt algorithm, key custodian, rotation/revocation procedure, retry/ACK rules and route/payload compatibility. Active-attempt fence alone does not authenticate an issuer. |
| B2 trigger | Every successful persisted FastTrack transition; or selected transitions only | Select named command/state transitions in a signed table before enabling producer. Recommend notify on persisted creation and review/decision transitions, excluding no-op/replayed commands; owner must enumerate exact source states, target states and events. Current normal notification tests are not B2 acceptance. |
| B2 audience | All current PMs and Supervisors; or designated responsible PM plus designated Supervisor | Prefer designated current responsible PM and Supervisor, resolved at enqueue and rechecked at dispatch. Owner must name assignment source and missing-assignee behavior (block producer vs operational escalation). Do not silently broaden to all members. |
| B2 dedup/channel | One event per committed transition/recipient/channel; or one event per item/state | Prefer transition-event ID + recipient + channel; retry reuses identity, reassignment creates a separately recorded recipient decision, revocation suppresses pending dispatch. Recommend inbox/outbox first; external delivery needs a separate channel decision. Owner must approve replay/reassignment and fallback behavior. |
| C1 approving actor | Current responsible PM; Supervisor; or two-person PM+Supervisor | Prefer Supervisor approval of an immutable sanitized version prepared by PM. Owner must choose actor, separation of duties, approve/reject/revoke states and permitted transitions. No private-original publication follows from option approval. |
| C1 public fields/media | Metadata only; or explicitly sanitized metadata plus derived media | Prefer metadata-only first: publicationId, version, approvedAt, defect category and coarse location; exclude original file IDs, storage keys, identities, precise coordinates and raw media. Owner must adopt the exact field allowlist and location precision; these candidates are not a current wire DTO. |
| C1 audience/withdrawal | Authenticated selected audience; or anonymous published sanitized representation | Prefer authenticated audience first. Withdrawal blocks new reads immediately; specify cache TTL/purge and prior capability handling explicitly. Existing private capabilities retain natural TTL300 expiry; do not promise immediate revocation of issued storage URLs. Owner must choose audience, version identity, withdrawal actor and cache/capability policy. |

Until adopted: external AI callbacks remain denied in preview; no new native B2
producer or C1 public endpoint is enabled. FE can use the documented local preview
with Development Mock AI and current private authorization. These gates prevent an
unqualified FE_INTEGRATION_READY_LOCAL claim. No owner answer is inferred from tests.
