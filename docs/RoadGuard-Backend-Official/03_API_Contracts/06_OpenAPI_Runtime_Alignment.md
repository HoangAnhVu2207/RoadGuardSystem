# 06 OpenAPI Runtime Alignment

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Snapshot `openapi.backend.yaml` comes from fresh GET `/swagger/v1/swagger.json` using existing CustomWebApplicationFactory Development, database deliberately disconnected, storage endpoint disabled and hosted workers removed. API Explorer restricts production controller assembly; test-only paths and the unreferenced ProbePayloadDto schema are excluded. No Backend wire contract is corrected or canonical V2 replaced.

Observed:47 controllers,241 operations,223 paths,357 production-reachable component schemas, OpenAPI3.0.1. Raw host response had358 schemas including one test probe. Every production method/path matches the independent API Explorer list. `/health` is separately documented infrastructure.

All241 operationId fields are absent in the runtime snapshot; OpenAPI3.0.1 permits absence. Duplicate non-null IDs=0. Do not silently manufacture stable client identifiers or declare operation IDs present. Optional future consumer-generator IDs require compatibility review, not a Backend fix in this task.

Request/response schema $refs, security schemes, nullable/required and ETag/If-Match/Idempotency-Key declarations are preserved. OpenAPI validity does not prove that manual Swagger declares every dynamic error/required guard or every anonymous response projection. Controller/result mapper evidence in catalog/error pages remains necessary. Nullability/schema assertions are structural observations, not every invalid-input request executed.

Historical V2 proposal uses different contract shape and frozen FE lock; its route count does not establish runtime equivalence. Prior lock mismatch remains known; no V2 or generated FE files are edited. Deployed runtime Swagger/UAT NOT_RUN.
