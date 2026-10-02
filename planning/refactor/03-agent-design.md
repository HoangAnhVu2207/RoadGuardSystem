# RF-03: Agent guidance proposal, inactive

## Identity and non-activation

- Branch `anh`; local HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; working tree dirty as in `00-baseline.md`. This design is **PROPOSED**. Existing `AGENTS.md`/`.agent/`/`.agents/` were survey evidence only under the user's refactor instruction. RF-03 does not create or activate replacement files.
- New guidance becomes active only after the user accepts its exact content and switch plan. Neither an old `AGENTS.md` pasted into the session nor an accepted business decision activates the new set for this program.

## Proposed files

| Path after approval | Responsibility |
|---|---|
| `AGENTS.md` | Short entrypoint: active guidance version, source precedence, scope/permission boundary and links. No duplicated product rule. |
| `.agents/README.md` and `manifest.json` | Index of active guides with version/owner/last review; machine-checkable paths. |
| `.agents/rules/evidence.md` | CURRENT_VERIFIED, TARGET_CONFIRMED, PROPOSED, UNKNOWN, HISTORICAL; current code is implementation evidence, not business authority. |
| `.agents/rules/scope-and-safety.md` | Worktree/dirty-file preservation, external/shared-data protection, migrations and irreversible change gates. |
| `.agents/rules/delivery.md` | Source -> contract -> implementation -> focused verification -> checkpoint; distinguish structure-only from behavior/data changes. |
| `.agents/rules/reporting.md` | Required seven-section task report below. |
| `.agents/modules/*.md` | Small source-routing maps for identity, project, survey, file, processing, inspection/repair, offline and messaging; links to `docs/product`, `docs/backend`, `contracts`, task. |

The proposed guides describe how to find authority and gather proof. They do not hard-code numeric Fast Track thresholds, duplicate contract schemas, designate a V2 route as canonical by name, or require a framework rewrite. Current RoadGuard skills remain historical until explicitly replaced; no old skill is used to execute RF-03.

## Required report form for RF-04 onward — revised 2026-09-30

Use the canonical [seven-section task report](templates/task-report.md). It replaces the earlier seven-section wording; historical RF-00..03 reports remain unchanged.

Follow [two-developer ownership and self-review](03-two-developer-plan.md). Every slice names one implementation owner, allowed files/symbols, shared-file reservations, producer/consumer contract and START/INTEGRATE/RELEASE gates. Use [coordination notes](templates/coordination-note.md) for cross-owner findings; propose a primary fixer and leave assignment to the two developers. Complete independent work while a note is unresolved.

RF-05 drafts references to these rules in the new guidance (delivery/reporting/collaboration); do not duplicate business rules or activate new guidance from this planning update. Self-review is performed in the coding session without invoking old RoadGuard skills or requiring another agent. Test doubles do not establish actual integration success.

## Adoption and tooling transaction

1. RF-04 supplies source-tagged new documentation, contract crosswalk and old-path map. RF-05 drafts the exact new `AGENTS.md`/`.agents/` set and a validator/CI diff for review.
2. User accepts the new guidance and activation scope explicitly. Record date/source and active version in `manifest.json` and task report. Until that event, old agent set is not the workflow for this program and new draft is inactive.
3. In the same activation change, update `Verify-P102Docs.ps1`, `Verify-AgentSetup.ps1`, V2 alignment/FE contract guards where applicable, `.github/workflows/ci.yml`, docs links and any hard-coded paths. Run both historical and new validators where they can still apply; mark intentional differences and do not weaken coverage to make a pass.
4. Verify no conflicting active root/skill instruction, no broken path, and no silent OpenAPI/Postman/FE lock drift. Only then start RF-06/RF-07 under the new set. Old guidance/docs are removed only at RF-11 retirement gate, with Git traceability.

## Decision boundary

- **Can draft now:** guide structure, report template, source-routing maps, validator plan and content for owner review. These are reversible planning artifacts.
- **Needs user acceptance:** activating/replacing root `AGENTS.md` and `.agents/`; changing CI or current path validators; removing old guidance. No acceptance is inferred from the 2026-09-30 product decisions.
- **Needs business/consumer decision:** instructions that would choose `/me` over `/profile`, V2 survey over old survey, new error wire, retention deletion or AI provider protocol. Guidance links these decisions rather than deciding them.
