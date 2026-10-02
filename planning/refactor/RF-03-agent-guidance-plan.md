# RF-03 — proposed agent-guidance system

> Preliminary proposal retained for traceability. `03-agent-design.md` supersedes its `agent-guidance/` layout with the owner-requested `AGENTS.md` and `.agents/` proposal. Neither set is activated by RF-03.

## Identity

- Branch: `anh`
- Surveyed HEAD: `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`
- Working tree: dirty; existing guidance is survey input only for this program.

## Proposed layout (requires owner acceptance)

```text
agent-guidance/
  README.md                 # authority, precedence, evidence labels
  operating-model.md        # scope, approvals, preservation and escalation
  source-routing.md         # where to find requirements/contracts/runtime proof
  architecture.md           # layer and dependency rules
  verification.md           # evidence ladder and reporting format
  templates/
    assessment.md
    refactor-work-package.md
    decision.md
  modules/
    api.md
    identity.md
    projects.md
    survey.md
    files.md
    processing.md
    inspection.md
```

The new set is inactive until the owner accepts it. It must never silently coexist as a second active authority with the old files.

## Operating rules to encode

- Preserve uncommitted work; record branch, HEAD and status in every report.
- Read source, tests, runtime configuration and data shape before inferring behavior.
- Label current facts, confirmed targets, proposals and unknowns separately.
- Require explicit approval for business decisions, public-contract changes, schema/data loss, external side effects and retirement of old guidance.
- Use reversible small slices; reserve shared hotspots; do not broaden a module task into a system rewrite.
- Record exact file/symbol/test/command evidence for every important claim.
- Keep production, migration, contract, CI and documentation-retirement scopes distinct.
- Do not expose secrets, passwords, private connection strings or personal data.
- Final reports state changed files, checks run, evidence not run, risks and next decision.

## Transition protocol

1. Owner reviews and accepts this proposal.
2. Publish the new guidance files and a one-page precedence statement.
3. Add a machine-readable manifest for active guidance and document owners.
4. For each old guidance source, classify `active`, `historical`, `superseded` or `retirement-pending`.
5. During transition, task reports must name which guidance set is active.
6. Delete/archive old guidance only through a separately approved documentation-retirement task.

## Anti-patterns

Do not infer missing decisions from IDs such as `32A`; do not treat V2/Done/test-pass labels as proof; do not let a target document override runtime evidence without recording the gap; do not use a broad refactor to hide an unapproved contract or schema change.
