# <Package ID>: <goal>

Status: PROPOSED / ASSIGNED (record assignment source). Writer/branch: <name, anh-review or huy-review>. Base SHA and initial dirty paths: <observed>. Scope and shared reservations: <exact files/symbols, named writer, integration order>.

## Goal and sources

- Goal, included functionality and non-goals: <bounded outcome; no silent scope removal>.
- Source references: <requirement/decision, active contract adoption, current code/caller/consumer/tests and revision; evidence labels>.

## Behavior and data

- Endpoint/request/response/errors: <method/path/version, DTO/serialization, status/body/headers, compatibility; approved source or OPEN decision; N/A for non-HTTP tasks>.
- Role/project scope and transitions: <allowed/denied actors, project boundaries, before/after states and concurrency>.
- SQL effects/transaction/idempotency when needed: <writes/readers, atomic boundary, keys/replay, rowversion/outbox, migration/recovery and isolated fixture; otherwise N/A with reason>.
- File ownership/interfaces: <allowlist, shared reservations, producer/consumer, agreed version and fixture; canonical changes through designated writer>.

## Acceptance and handoff

- Acceptance cases and focused tests: <success, denied scope, invalid transition, errors/replay/concurrency as relevant; commands and expected evidence, isolated SQL only>.
- Open decisions: <business/public compatibility/data policy question, owner and dependent checkpoint; independent work that can continue>. Do not invent routes/schema or infer approval from the package table.
- Handoff conditions: <acceptance evidence, two concise self-review passes/fixes, final diff and short PR summary for ChatGPT review>. Record executed/failed/skipped/not-run counts; distinguish BE verified, mock verified and external/deployment not verified. No repeated approval for steps already assigned by this spec; no RF report or separate coordination file required.
