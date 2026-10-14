# FIELD measurement workflow

FIELD is a formal inspection and provenance workflow, not a repair approval. PM creates/assigns through API-079/082; Crew operates an assigned task through API-080/081/091/093/095/096. Supervisor and PM read scoped evidence with API-085/086/088/094.

## Flow

1. Load task detail, geometry, history and start-origin. Confirm current Crew assignment, Defect/source chain, route/version pins and task version.
2. Accept or reject with API-081/091. Rejection preserves history; it does not reassign itself.
3. Start with API-093. The original/first-start origin is server-owned and must not be reset by a supplement or clock change.
4. Measure the physical defect in the field. Record dimensions and method as the request DTO requires. AI bbox/confidence/frame/timestamp is evidence context only and cannot populate physical dimensions.
5. Capture BEFORE/AFTER/MEASUREMENT evidence using API-233/236/235 and poll API-234 to `VERIFIED`. Link only verified, task-scoped files to API-095/156/157 as permitted.
6. Submit typed assessment/formal evidence. PM reviews via API-092/158 and may request supplement; Crew app uses API-159 with a new exact K and current version.
7. Finish execution only through API-160 after current eligibility and evidence guards pass. A submission or measurement does not itself approve or complete a repair.

## Failure boundaries

Inactive membership, wrong assignment, stale task/item version, unverified file, wrong checksum, missing Defect/source, revoked policy or missing road coverage must fail closed. Preserve original actor, task, source file IDs, snapshot/version and receipt. No last-write-wins and no client-side state transition.

`CURRENT_IMPLEMENTED_BE`: controllers/services/repositories and DTOs documented in workflows 06/07. `PROPOSED_EXPO_UI`: camera/location/file capture and form UX. `NOT_VERIFIED_EXPO_E2E`: native sensors, background capture, actual physical measurement accuracy and device build.
