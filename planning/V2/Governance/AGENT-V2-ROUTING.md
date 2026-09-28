# AGENT-V2-ROUTING

## Task metadata

- Owner/branch: `anh` / `anh`
- deliveryStatus: `DONE`
- verificationStatus: `DOCS_PASS`
- sourceCheckpoint: base `49c7eae`, decision register `V2-ALIGN-2026-09-28`

## Source evidence

| Source | Heading / ID | Evidence used | Revision/checkpoint |
|---|---|---|---|
| `AGENTS.md` | Skill Routing; Source Evidence And Task Lifecycle | Canonical routing/lifecycle | Working tree |
| Alignment plan | §13.1-13.3 | Required read path and 8 scenarios | 28/09/2026 |
| Decision register | Status vocabulary; D01-D28/32-44 | Current decision authority | V2-ALIGN-2026-09-28 |
| Five project skills | SKILL.md + references + metadata | Endpoint/persistence/test/Postman/review workflow | Working tree |

## Delivery status

`DONE`

## Completion history

### 2026-09-28 - DONE

- Scope/result: routed V2 work through lifecycle/task checkpoint/current decision sources; removed frozen REVIEW-01 gates from product-gates; made five skills persist source/completion evidence.
- Files: `AGENTS.md`, `.agents/rules/roadguard.md`, five skill `SKILL.md`/selected references/metadata, `docs/prompts/RoadGuard_Task_Workflow.md`.
- AC/evidence: RED baseline failed six missing behaviors; GREEN static checks passed all six. Scenario S1-S8 all true, including docs-only, no repeated approval, historical OPEN crosswalk, reuse, SQL gate, Postman invalidation, docs completion and REOPENED history.
- Verification: `quick_validate.py` with `PYTHONUTF8=1` returned `Skill is valid!` 5/5; docs validator `STRUCTURAL_CHECKS_PASS` (229 links, 153 schemas, 133 operations).
- Tool limitation: default Windows cp1252 run could not decode the two existing Vietnamese skills; UTF-8 mode validates their actual content. No skill text was changed to hide Unicode.
- Side effects: no package/migration/data/external action; no commit/push.
- Unverified: no live agent/subagent pressure execution; scenario checks are deterministic content/workflow assertions, not runtime API tests.
