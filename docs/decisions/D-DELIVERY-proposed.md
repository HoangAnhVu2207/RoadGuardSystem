# D-DELIVERY: Refactor task and guidance adoption

- **Status:** PROPOSED, RF-04 draft; historical ADR 006 is not the operating workflow for this program.
- **Sources:** explicit owner override for RF-00..04, RF-03 two-developer supplement and report template.
- **Proposal:** each implementation slice has one writer across Controller/Service/Repository/tests; shared migrations, root guidance, CI and canonical contract have one writer at a time. Record START/INTEGRATE/RELEASE gates and self-review. A coordination note proposes an owner; the two developers decide assignment. Draft guidance has no effect until exact replacement set and validator/CI switch are accepted.
- **Gate:** user acceptance of RF-05 guide/tooling diff. This document does not activate `AGENTS.md` or `.agents/` and does not permit old-doc deletion.
