# Open decisions — refactor program

- Branch: `anh`; surveyed HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; working tree dirty.

1. **Authority adoption:** approve the proposed `docs/project`, `docs/backend`, `planning/refactor`, and `agent-guidance` authority model? Until accepted, new guidance remains a proposal.
2. **Legacy retirement:** when may old `AGENTS.md`, `.agents/`, `.agent/`, old ADR/docs and V2 planning be marked superseded or removed? No deletion is included here.
3. **Canonical contract owner:** who owns approval of public API, stable errors, actor/scope and state transitions?
4. **Schema authority:** which source is authoritative when code, migrations, ERD and target docs disagree? Require explicit decision per conflict.
5. **Module order:** accept RF-01 order, or prioritize identity/survey/processing differently?
6. **Compatibility policy:** which route/status/error/state changes are allowed during refactor, and what versioning/deprecation window is required?
7. **Data migration:** is any backfill or schema change authorized in a later package? If yes, identify datasets, rollback and isolated environment.
8. **Runtime evidence:** which SQL Server, storage and external-provider environments may be used for verification, and who owns credentials/fixtures?
9. **Ownership:** assign owners and one-writer reservations for DbContext, migrations/snapshot, OpenAPI/Postman, seed, shared middleware and current dirty survey files.
10. **Agent guidance activation:** after acceptance, should the new guidance be committed at repository root, and which old files become historical first?

No item is silently resolved by this survey.
