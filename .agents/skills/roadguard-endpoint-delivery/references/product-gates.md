# Read only the heading matching the task

## Authentication and public reporting

Use authoritative active user/session/membership checks from existing services; do not trust stale role claims alone. Separate login credential failure, token expiry and revocation only through a approved contract delta; current legacy error names remain in use until migration. Refresh only for the agreed expiry code, not every 401. Hash refresh/OTP secrets and avoid logging credentials. Reporter registration is Gmail OTP with pending account and one-time verification before token issuance; Google OAuth is outside that use case. Reporter coordinates are per-photo source/accuracy facts, not upload-device GPS. Public projections must not reveal internal case notes or another reporter's data.

## Workflow / offline / repair

Keep IncidentCase, Defect, Survey and Repair states separate. Defect VERIFIED is pre-repair confirmation; case VERIFIED denotes passed retest. Fast Track follows task/policy eligibility → Crew repair → PM review/close → notification to Supervisor; do not insert Supervisor pre-repair approval. Approval Track requires its own approved item/assignment. Preserve attempt and evidence history; repair method is a general summary, not finance/construction-detail scope.

Verify current decision status in V2 docs. REVIEW-01 recorded Q02/Q03 policy activation/evaluation, Q04 offline reassignment conflict and Q17 suspended-account data recovery as OPEN. BR-10 weekly grouping was proposed; BR-09 core remains distinct. Do not hard-code unanswered decisions. Fail closed only for the unresolved branch; proceed with independently approved work.

Offline receipt must survive restart/retry, identify client operation independently of batch and ACK only committed work. Revalidate current authority and assigned snapshot; never silently last-write-wins, discard pending data or accept revoked permissions. Token expiry is not a task TTL. Snapshot/evidence contract gaps block claims of full offline readiness. The draft sync contract has INSPECTION_SUBMIT, REPAIR_START, REPAIR_SUBMIT; FAST_TRACK_EVALUATE was a NOT_ENABLED proposal. Confirm current contract before adding kinds.

## Spatial / survey / files

Keep route/version and published segment sets immutable. Separate aircraft GPS, route station, camera footprint and defect position. Preserve configured SRID and units; longitude/latitude cannot be used as meters. Assess coverage by dataset, segment and SURFACE/LEFT_EDGE/RIGHT_EDGE band. Upload complete does not imply verified file/dataset; server integrity/QC governs downstream eligibility. Enforce scope/ownership on attachment and download, not merely on upload creation.

## Async processing / research / retention

Use immutable versioned manifests, durable job/outbox and attempt-correlated callbacks. AI results are candidate detections; no detections does not automatically mean PM no-defect approval. Deterministic/imported research data is valid when labeled; do not build or claim real AI inference/training without scope. Preserve ground truth pairing, exclusions, provenance and metrics without rewriting operational measurements. Recheck legal hold at deletion execution; absent approved retention rules must not become guessed TTLs.
