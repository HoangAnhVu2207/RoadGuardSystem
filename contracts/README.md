# Contract transition index (RF-04 draft, inactive)

This directory is a **draft comparison surface**. It does not supersede controller/DTO source, `docs/diagram/V2/05_Technical/openapi.yaml`, FE snapshot/lock, Postman or `API.http`. No active contract, generator or lock was changed. Branch `anh`, surveyed local HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`.

| Surface | Current/source evidence | Proposed target and gate |
|---|---|---|
| HTTP | 57 controller actions + `/health`; method/path/DTO evidence in [RF-01 inventory](../planning/refactor/01-endpoint-inventory.md), live wire not verified. | 133 parsed unique draft operations in historical V2 OpenAPI. [Bidirectional crosswalk](../planning/refactor/04-operation-crosswalk.md) assigns each a disposition. Approve per operation with external consumer compatibility before any canonical switch. |
| Errors/auth | Lowercase runtime codes and bearer web source; `/profile`/`/me` overlap. | PR-36A/37 target transport; CG01-03 and Q-RF02-02 block new active wire. Capture isolated HTTP responses first. |
| Events/AI/offline | Outbox/processing callback and upload retry source exists; dispatcher/provider/Android wire unverified. | [event contract questions](events/README.md) are PROPOSED; provider/consumer sign-off and fixture tests required. |
| Files/data | `int` stored size, PR-38 8 GiB video target. | CG17 contract/schema migration package after RF-09; no silent reinterpretation of active DTO or SQL column. |

The [pilot draft](http/work-package.proposed.yaml) gives a source-grounded example for a read-only implemented route. It is deliberately separate from the proposed 133-operation V2 document, which has **no** `work-package` path. Its response and nested road/warranty schemas reference the current DTO records and Service mapping; schema references, properties and required lists are statically checked. Required means non-nullable C# source intent, not HTTP proof. Actual serialization, middleware error wire, access role values, decimal wire precision and consumer expectations await isolated RF-06/07 capture. This example is not codegen input or deployment authority.

Rule for future adoption: approve a versioned canonical `contracts/http/openapi.yaml` with each operation's status, actor/scope, request/response/error examples and consumer registry; generate FE types/snapshot and Postman skeletons from it; compare hashes and do not auto-relock. Hand-authored assertions/fixtures remain reviewed. RF-05 may plan validators; activation requires user acceptance.
