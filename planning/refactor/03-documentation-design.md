# RF-03: Documentation and contract system proposal

## Identity and authority

- Branch `anh`; local HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; dirty working tree described in `00-baseline.md`. This file is **PROPOSED**; no old doc, ADR, OpenAPI, Postman, CI or guide has been moved or activated.
- User-confirmed RF-00..03 scope and the 2026-09-30 reconfirmed 32-44 table are the available accepted sources. Code/test/old docs remain implementation or historical evidence, not automatic business authority. `02-adr-transition.md` names the ADR transition; deleting a file never repeals its requirement.

## Proposed layout and one-source rules

```text
docs/product/                 product scope, actors, requirements, glossary, data meanings
docs/backend/                 implementation architecture, module maps, operations, verification
contracts/http/openapi.yaml   approved/versioned public HTTP source when adopted
contracts/events/             outbox/AI/offline wire contracts and examples
docs/decisions/               decision index, accepted ADRs, proposed decisions
planning/refactor/            assessment, task checkpoints, reports and migration evidence
AGENTS.md                     short active entrypoint only after owner acceptance
.agents/                      task routing and focused guides only after owner acceptance
```

| Content | One authoritative location after acceptance | Derived/read-only views and drift check |
|---|---|---|
| Product rule, actor/state, pilot values | `docs/product/requirements/` with stable IDs and source/date; 32-44 full text carried from `02-decision-register.md` | Module docs link IDs; checker rejects duplicate normative values and broken source links. |
| Decision and alternatives | `docs/decisions/` with Accepted/Proposed/Superseded plus owner, date and supersedes link | Decision index generated from metadata; old ADR 001-006 crosswalk stays historical until retirement. No code-only label is a decision. |
| HTTP method/path/schema/error/authorization | `contracts/http/openapi.yaml` after per-operation approval; current controller wire is separately marked implementation evidence | FE snapshot/types, API `.http` skeleton and Postman request skeleton can be generated; hand-authored assertions/fixtures stay reviewed. Parse YAML operations (133 draft), compare IDs/routes and hashes; never auto-relock unapproved drift. |
| Event/AI/offline payload and retry semantics | `contracts/events/` named versioned schemas, with external consumer sign-off | Consumer fixtures generated or validated against schema; require replay/unknown-field compatibility tests. |
| Data meanings and retention basis | `docs/product/data/`; physical tables/migrations documented in `docs/backend/persistence/` | ERD/read model generated from approved schema where feasible; compare to migration snapshot and actual isolated SQL. No generated ERD overrides business meaning. |
| Source architecture/operations | `docs/backend/` tied to code symbols and deployed configuration class | CI link/source-symbol checks and periodic source review; runtime evidence recorded in task report, not copied as timeless fact. |
| Task status and evidence | `planning/refactor/tasks/` and `planning/refactor/reports/` | Status dashboard generated from task metadata; historical V2 Done remains immutable evidence. |
| Agent operating rules | `AGENTS.md` as concise root pointer, `.agents/` focused guides | Validator checks manifest/links/active version; no independent copy of business thresholds or contracts. |

`docs/backend/contracts/` contains explanatory rules and links, **not a second canonical OpenAPI**. `docs/product/` states why/what; `contracts/` specifies wire; `docs/backend/` explains current implementation and migration. Until adoption, old V2 OpenAPI is a draft comparison surface and runtime controllers are current implementation evidence. A new canonical path is not silently active merely because files are created.

## Crosswalk and controlled switch

| Existing source/dependency | Replacement/handling | Cutover check |
|---|---|---|
| `docs/RoadGuard_Project_Scope.md`, `docs/RoadGuard_Backend_Scope.md`, V2 FR/BR/US/DD/ERD | Source-tagged migration into `docs/product/` and `docs/backend/`; unresolved statements retain Proposed/Unknown | Requirement-ID crosswalk, no missing Accepted rule, reviewer sign-off. |
| `docs/adr/001..006` | `docs/decisions/` transition records; accepted business continuity carried, ADR 005 Proposed, ADR 006 historical workflow | `02-adr-transition.md` row-by-row review and replacement decision IDs. |
| `docs/diagram/V2/05_Technical/openapi.yaml`, 133 task manifest/cards | Operation-ID crosswalk to `contracts/http/openapi.yaml`; legacy remains immutable until approval | 133/133 mapping or explicit deferred/retired reason; current 57 actions mapped separately. |
| FE contract snapshot/lock and `check_contracts.py` | Regenerate only from approved canonical version after CG01-04 review | Hash/operation/schema compatibility report, FE owner receipt; current `CONTRACT_LOCK_MISMATCH` tracked as baseline. |
| Postman JSON/environment and `API.http` | Preserve request names/IDs/fixture intent, map by operation ID; generate request skeletons only | 57 current actions versus collection 52 matches, five CG16 gaps; static and runtime checks reported separately. |
| `Verify-P102Docs.ps1`, `Verify-AgentSetup.ps1`, `check_alignment.py`, `check-docs.cmd`, CI | New path/manifest checks developed beside old checks; atomically switch CI after content approval | Run old/new guards on intended states; no coverage loss, no unexplained hard-coded path remains. |
| `AGENTS.md`, `.agents/`, `docs/prompts`, old plans/worklogs | New root guide and `.agents/` staged only after acceptance; old guidance treated as survey history during this program | Explicit active-version marker and owner acceptance; no simultaneous conflicting active guidance. |

## Transition sequence

1. RF-04 drafts complete new content, source crosswalk and link/operation manifest without replacing current files. Review source authority and unresolved questions.
2. RF-05 prepares new agent guidance and updated validators/CI together. **Activation requires explicit user acceptance of the exact new set**; until then all files remain drafts and old agent files are not followed for this program.
3. After acceptance, switch one canonical path/CI invocation in a coordinated change. Validate 57 runtime actions, draft 133 operation IDs, FE lock, Postman and generated artifacts as distinct sets; do not declare all 133 implemented.
4. Keep old docs/ADR/agent material until every incoming link, requirement/decision ID and tool path has a replacement or historical pointer. Only RF-11 may remove old files from the active tree after transition checks and user authorization; Git history retains provenance.

## Verification and retirement gates

- A machine-readable manifest proposed for documents and contracts records path, owner, source revision, authority status, generated-from path and supersession target. Checker validates schema, links and no two active canonical owners for one ID.
- Compare parsed OpenAPI paths/operation IDs and generated snapshots; compare Postman method/path/request IDs and `.http`; run validators in a dry, read-only mode first. Do not run an unreviewed generator or data-modifying script.
- Retirement gate: replacement content approved; crosswalk covers each old normative claim; external consumer/maintainer notified where public contract changes; validators and CI pass; old links resolve to historical map; report records deleted paths and Git commit. Never delete applied migrations to clean history.
