# D-ARCH: Preserve N-layer during first refactor pass

- **Status:** PROPOSED, RF-04 draft; no architecture decision accepted here.
- **Sources:** historical ADR 001/004; CURRENT_VERIFIED project graph, 17 controllers and shared DbContext in RF-00/01; proposed RF-03 target architecture; TARGET_CONFIRMED PR-44 external Web/Android/AI ownership.
- **Context:** current Controller -> Service -> Repository/EF slices work but have competing conventions. A framework or service rewrite has no measured justification.
- **Proposal:** retain one backend process and physical DbContext for initial use-case refactor. Controllers handle transport, Services decide actor/workflow, Repositories own durable writes/queries. Assign logical data owners by module; cross-module commands name a coordinating Service and transaction boundary. BE owns external adapter/fixtures/integration, not Web/Android/AI implementation.
- **Alternatives:** split deployable services or replace layering now; deferred until measured coupling/scale evidence and migration cost are available.
- **Decision gate:** pilot RF-07 proof, module boundary review and owner acceptance. PR-44 is confirmed independently; it does not accept this architecture choice.
