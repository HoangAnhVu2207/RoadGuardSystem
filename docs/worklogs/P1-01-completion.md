# Antigravity completion log — P1-01

## Identity and scope

- **Task ID/title:** P1-01 / API Platform Foundation
- **Owner / reviewer:** Person 1 (Antigravity) / Person 2
- **Date / branch or commit:** 2026-09-17 / `anh` / baseline `f2008c25c7b5c76e0065370ad66c387f4a6022a8`
- **Trace:** TE-02, depends on P1-00
- **Status:** Done (final Person 2 cross-review completed)

### In-scope behavior
- API versioning using URL path segment `/api/v{version:apiVersion}` with default v1.
- OpenAPI / Swashbuckle configuration generating versioned v1 specification at `/swagger/v1/swagger.json`.
- ProblemDetails error envelope conforming to RFC 7807/9110 with extensions: `code` and `correlationId`.
- Centrally defined stable machine-readable error codes: `validation_error`, `unsupported_api_version`, `not_found`, `internal_error`, `method_not_allowed`, `unsupported_media_type`.
- Mapping framework 405 (Method Not Allowed) and 415 (Unsupported Media Type) to standard ProblemDetails envelopes with `method_not_allowed` and `unsupported_media_type` codes.
- Security invariant: complete suppression of stack traces, internal exception types/messages, machine paths, and secrets in error responses across all environments.
- Unhandled exceptions consistently return HTTP 500 with generic public message and `internal_error` code.
- Correlation ID middleware using header `X-Correlation-ID` with standard UUID format validation, generation fallback for missing or invalid values, response header propagation, HttpContext/Trace attachment, and structured logging scope.
- Unversioned process liveness health endpoint at `/health` returning 200 OK without database, Docker, or external service dependencies.
- Modular DI and middleware composition extension methods keeping `Program.cs` thin.
- Negative-first and positive API contract tests via `WebApplicationFactory<Program>` using a test-only probe controller registered via `ApplicationPart`.

### Explicitly out of scope
- Business controllers, endpoints, and domain workflows (surveys, defects, projects, repairs, etc.).
- Authentication flows (JWT, Identity, Google auth), password hashing, login/logout, tokens.
- Business actor authorization, project-scope access guards, or supervisor policies.
- Database context, entities, EF Core migrations, and NetTopologySuite spatial mappings (owned by P2-00/P2-01).
- Database readiness health checks (owned by P2-01).
- Architecture Decision Records (`docs/adr/001-backend-boundary.md`, `002-authentication.md`) and error-code naming policy documentation (`docs/api-errors.md`) (owned by P1-02).
- Retargeting framework or editing `global.json`.
- Modifying `BusinessObjects`, `Repositories`, persistence, or domain logic.
- Serializing or returning `RoadGuardSystem.cRepositories.Commons.ApiResult.Exception`.

---

## Preconditions and decisions

- **Actor and project-scope rule:** Not applicable. P1-01 establishes cross-cutting API transport and platform infrastructure; there is no business actor or project-scope authorization involved.
- **State before / allowed state after:**
  - Before: Baseline `f2008c2` with minimal `Program.cs` and startup smoke test; commit `d833c48` with initial P1-01 foundation.
  - After: Fully configured API pipeline with versioning, OpenAPI v1, correlation ID middleware, uniform ProblemDetails (covering 400, 404, 405, 415, 500), and health endpoint.
- **Data/version/immutability rules:**
  - Business data immutability is not applicable (no entities or DB in P1-01).
  - API versioning uses route segment `/api/v{version:apiVersion}` with default `1.0`.
  - Error responses strictly use RFC 7807/9110 `ProblemDetails` with `application/problem+json` content type.
- **Audit event and stable error codes:**
  - Business audit events are not applicable (no domain state transitions).
  - Stable error codes: `validation_error`, `unsupported_api_version`, `not_found`, `internal_error`, `method_not_allowed`, `unsupported_media_type`.
- **Idempotency/concurrency behavior:** Not applicable. There are no mutable domain aggregates, state transitions, or entity persistence.
- **Assumptions, ADRs, or specification conflicts:**
  - Package dependencies: Added `Asp.Versioning.Mvc` (8.1.0) and `Asp.Versioning.Mvc.ApiExplorer` (8.1.0) pinned to exact versions compatible with net8.0.
  - Probe controller: Declared strictly within `tests/RoadGuardSystem.ApiTests` and registered via `AddApplicationPart` in `CustomWebApplicationFactory`, avoiding fake production controllers.
  - Information disclosure: No stack traces or exception details are serialized in ProblemDetails regardless of environment.
  - Review Finding 1 resolution: Framework 405 and 415 errors now explicitly carry `method_not_allowed` and `unsupported_media_type` machine-readable codes in their ProblemDetails extensions.
  - Review Finding 2 resolution (confirmed numeric routing policy): `ApiVersioningValidationMiddleware` replaces broad substring/StartsWith route matching with an anchored, culture-invariant numeric-version matcher `^/api/v\d+(\.\d+)?(/.*)?$`. Only numeric version segments enter version validation. The captured numeric version is extracted independently of whether an optional trailing slash exists (normalized logically inside the middleware without redirection or changing the public contract). Unsupported numeric versions (e.g. `/api/v99.0`, `/api/v99.0/`, `/api/v99.0/probe/ok`) return HTTP 400 `unsupported_api_version`. Non-numeric malformed prefixes (e.g. `/api/vabc/probe/ok`, `/api/v/probe/ok`, `/api/v1beta`, `/api/v1.`) and unrelated paths (`/api/videos`, `/api/videos/123`) do not match the numeric version namespace, bypass the middleware, and return HTTP 404 `not_found`.

---

## Files changed

| Change | File | Purpose |
|---|---|---|
| Modified | `docs/worklogs/P1-01-completion.md` | Completion log for task P1-01 updated with confirmed numeric routing policy, evidence, and commands |
| Modified | `RoadGuardSystem.API/Middlewares/ApiVersioningValidationMiddleware.cs` | Uses anchored regex `^/api/v\d+(\.\d+)?(/.*)?$` to validate only numeric version segments; extracts version independently of trailing slash |
| Modified | `tests/RoadGuardSystem.ApiTests/Platform/ProblemDetailsNegativeTests.cs` | Added negative contract tests for numeric unsupported versions (/api/v99.0, /api/v99.0/, /api/v99.0/probe/ok), non-numeric 404s (/api/vabc/..., /api/v/...), boundary non-numeric routes (/api/v1beta, /api/v1.), and unrelated paths (/api/videos, /api/videos/123) |
| Modified | `tests/RoadGuardSystem.ApiTests/Platform/ApiPlatformPositiveTests.cs` | Strengthened positive version routing test to assert HTTP 200 OK for `GET /api/v1/probe/ok` |
| Modified | `RoadGuardSystem.API/Constants/ApiErrorCodes.cs` | Added `MethodNotAllowed` and `UnsupportedMediaType` stable constants |
| Modified | `RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs` | Mapped HTTP 405 and 415 in `CustomizeProblemDetails` to stable codes |
| Added (d833c48) | `RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj` | Added `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` (8.1.0) |
| Added (d833c48) | `RoadGuardSystem.API/Program.cs` | Wired pipeline: CorrelationId, ProblemDetails, versioning, health, OpenAPI |
| Added (d833c48) | `RoadGuardSystem.API/Middlewares/CorrelationIdMiddleware.cs` | X-Correlation-ID middleware (UUID parsing, response echo, logging scope) |
| Added (d833c48) | `RoadGuardSystem.API/Extensions/ConfigureSwaggerOptions.cs` | Versioned OpenAPI generation for Swagger UI and `/swagger/v1/swagger.json` |
| Added (d833c48) | `tests/RoadGuardSystem.ApiTests/Infrastructure/CustomWebApplicationFactory.cs` | Test factory registering test assembly ApplicationPart |
| Added (d833c48) | `tests/RoadGuardSystem.ApiTests/Controllers/ProbeController.cs` | Test-only versioned probe controller for contract assertions |

---

## Database, API, config, and operations impact

- **Migration added and recovery/downgrade note:** None (no database changes in P1-01).
- **API/OpenAPI compatibility impact:** Introduces versioned route template `/api/v{version:apiVersion}`, OpenAPI v1 endpoint `/swagger/v1/swagger.json`, uniform `application/problem+json` envelope across 400, 404, 405, 415, 500, and `X-Correlation-ID` header.
- **Configuration/secret/environment impact:** None. Zero secrets added; no changes to configuration schema.
- **Seed/data migration impact:** None.
- **Worker/storage/queue impact:** None.

---

## Negative-first evidence

List each negative/edge case before positive cases. If a standard case is irrelevant, state why.

| Test | Layer | Expected failure/code | RED Result | GREEN Result |
|---|---|---|---|---|
| Malformed JSON body | API contract | 400 Bad Request, `validation_error`, no stack trace, has correlationId | RED (500 without ProblemDetails) | PASS (400, `validation_error`, no leak) |
| Invalid model payload | API contract | 400 Bad Request, `validation_error`, validation errors dictionary | RED (500 without model validation config) | PASS (400, `validation_error`, errors dict) |
| Unhandled exception | API contract | 500 Internal Server Error, `internal_error`, generic message, no leak | RED (unhandled exception unmasked) | PASS (500, `internal_error`, zero secret/stack leak) |
| Non-existent route (404) | API contract | 404 Not Found, `not_found`, has correlationId | RED (empty 404 without ProblemDetails) | PASS (404, `not_found`, correlationId) |
| Unsupported numeric API version with subpath (`/api/v99.0/probe/ok`) | API contract | 400 Bad Request, `unsupported_api_version`, matching correlationId, zero leak | RED (returned 500 / 404 without version middleware) | PASS (400, `unsupported_api_version`, problem+json, zero leak) |
| Unsupported numeric API version without trailing slash (`/api/v99.0`) | API contract | 400 Bad Request, `unsupported_api_version`, matching correlationId, zero leak | RED (returned 404 not_found) | PASS (400, `unsupported_api_version`, problem+json, zero leak) |
| Unsupported numeric API version with trailing slash (`/api/v99.0/`) | API contract | 400 Bad Request, `unsupported_api_version`, matching correlationId, zero leak | RED (captured empty version segment) | PASS (400, `unsupported_api_version`, problem+json, zero leak) |
| Non-numeric version prefix (`/api/vabc/probe/ok`) | API contract | 404 Not Found, `not_found`, matching correlationId, zero leak | RED (returned 400 unsupported_api_version) | PASS (404, `not_found`, matching correlationId, zero leak) |
| Empty version prefix (`/api/v/probe/ok`) | API contract | 404 Not Found, `not_found`, matching correlationId, zero leak | RED (returned 400 unsupported_api_version) | PASS (404, `not_found`, matching correlationId, zero leak) |
| Boundary non-numeric routes (`/api/v1beta`, `/api/v1.`, etc.) | API contract | 404 Not Found, `not_found`, matching correlationId, zero leak | RED (returned 400 unsupported_api_version) | PASS (404, `not_found`, matching correlationId, zero leak) |
| Unrelated paths (`/api/videos`, `/api/videos/123`) | API contract | 404 Not Found, `not_found`, matching correlationId, zero leak | RED (`/api/videos/123` returned 400) | PASS (404, `not_found`, matching correlationId, zero leak) |
| Invalid / multi-value correlation header | API contract | Ignored; server issues a new valid UUID | RED (X-Correlation-ID missing) | PASS (new valid UUID generated, not echoed raw) |
| POST JSON to GET-only endpoint (405) | API contract | 405 Method Not Allowed, `method_not_allowed`, correlationId | RED (code extension missing in ProblemDetails) | PASS (405, `method_not_allowed`, matching correlationId) |
| POST text/plain to JSON-only endpoint (415) | API contract | 415 Unsupported Media Type, `unsupported_media_type`, correlationId | RED (code extension missing in ProblemDetails) | PASS (415, `unsupported_media_type`, matching correlationId) |
| Unauthorized / wrong project | Service/API | N/A — No business authorization in P1-01 (owned by P1-12) | N/A | N/A |
| Invalid transition / prerequisite | Domain/service | N/A — No domain workflows in P1-01 | N/A | N/A |
| Duplicate retry / idempotency | Integration/worker | N/A — No state-mutating commands in P1-01 | N/A | N/A |
| Stale concurrency | Integration/API | N/A — No mutable aggregates in P1-01 | N/A | N/A |
| DB / storage / queue timeout | Component | N/A — No DB or storage in P1-01 | N/A | N/A |
| Integrity / checksum / immutable history | Integration | N/A — No file storage or audit store in P1-01 | N/A | N/A |

---

## Positive evidence

| Test | Layer | Expected state/output | RED Result | GREEN Result |
|---|---|---|---|---|
| WebApplicationFactory startup | API contract | Factory builds and client created without exception | PASS | PASS |
| Process liveness health check | API contract | GET /health returns 200 OK | RED (404, endpoint unmapped) | PASS (200 OK, Healthy) |
| OpenAPI v1 document | API contract | GET /swagger/v1/swagger.json returns 200 OK with v1 schema | RED (404, v1 doc unconfigured) | PASS (200 OK, openapi: 3.0.1, v1 routes) |
| Missing correlation header generates UUID and routes to v1 | API contract | GET /api/v1/probe/ok returns 200 OK and response contains valid UUID in X-Correlation-ID | RED (X-Correlation-ID missing, 200 unasserted) | PASS (200 OK, valid UUID attached) |
| Valid client correlation header preserved | API contract | Same client UUID echoed in header and body | RED (X-Correlation-ID missing) | PASS (echoed client UUID) |
| Correlation header and body match | API contract | Header X-Correlation-ID equals ProblemDetails correlationId | RED (header and correlationId missing) | PASS (strictly identical UUID) |

---

## Commands run

| Command | Exit code | Result/coverage | Timestamp |
|---|---:|---|---|
| `git status` | 0 | Branch anh, clean working tree | 2026-09-17T01:40:17+07:00 |
| `git rev-parse HEAD` | 0 | f2008c25c7b5c76e0065370ad66c387f4a6022a8 | 2026-09-17T01:40:50+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-01"` | 1 | RED run: 1 passed, 11 failed (contract failures observed) | 2026-09-17T01:54:48+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-01"` | 0 | GREEN run: 12 passed, 0 failed, 0 skipped | 2026-09-17T02:06:44+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-01"` | 1 | Review finding RED run: 2 failed (405/415 code missing), 12 passed | 2026-09-17T03:47:59+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-01"` | 0 | Review finding GREEN run: 14 passed (9 negative + 5 positive) | 2026-09-17T03:48:16+07:00 |
| `dotnet restore RoadGuardSystem.slnx` | 0 | All projects up-to-date for restore | 2026-09-17T03:49:00+07:00 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 0 errors, 0 warnings | 2026-09-17T03:49:15+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-01" --no-build` | 0 | 14 passed, 0 failed, 0 skipped | 2026-09-17T03:49:30+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --no-build` | 0 | 16 passed (14 P1-01 + 2 P1-00), 0 failed | 2026-09-17T03:49:45+07:00 |
| `dotnet test RoadGuardSystem.slnx --no-build` | 0 | 92 passed (33 Unit, 16 Api, 43 Integration), 0 failed | 2026-09-17T03:50:15+07:00 |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Clean formatting across solution | 2026-09-17T03:50:30+07:00 |
| `git diff --check` | 0 | No whitespace errors | 2026-09-17T03:50:40+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-01"` | 1 | Numeric routing policy RED run: 6 failed (bypassed non-numeric routes returned 400, /api/v99.0 returned 404), 18 passed | 2026-09-17T12:34:00+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-01"` | 0 | Numeric routing policy GREEN run: 24 passed (19 negative + 5 positive), 0 failed | 2026-09-17T12:34:28+07:00 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 0 errors, 0 warnings | 2026-09-17T12:35:00+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests --no-build` | 0 | 26 passed (24 P1-01 + 2 P1-00), 0 failed | 2026-09-17T12:35:10+07:00 |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Clean formatting across solution | 2026-09-17T12:35:20+07:00 |
| `git diff --check` | 0 | No whitespace errors | 2026-09-17T12:35:30+07:00 |

---

## Review handoff

- **Observable demo/output:**
  - `GET /health` -> 200 OK (`Healthy`)
  - `GET /swagger/v1/swagger.json` -> 200 OK (OpenAPI specification for v1)
  - `GET /api/v1/probe/ok` -> 200 OK (Supported v1 route returns OK and attaches `X-Correlation-ID: <UUID>`)
  - Any request returns header `X-Correlation-ID: <UUID>`
  - Request with invalid JSON -> HTTP 400 `application/problem+json` with `code: "validation_error"`, matching `correlationId`, zero stack traces
  - Request with unhandled exception -> HTTP 500 `application/problem+json` with `code: "internal_error"`, matching `correlationId`, generic title/detail, zero internal leak
  - Request with unsupported numeric version `/api/v99.0/probe/ok` -> HTTP 400 `application/problem+json` with `code: "unsupported_api_version"`
  - Request with unsupported numeric version without slash `/api/v99.0` -> HTTP 400 `application/problem+json` with `code: "unsupported_api_version"`
  - Request with unsupported numeric version with trailing slash `/api/v99.0/` -> HTTP 400 `application/problem+json` with `code: "unsupported_api_version"`
  - Request with non-numeric prefix `/api/vabc/probe/ok` -> HTTP 404 `application/problem+json` with `code: "not_found"`
  - Request with empty version prefix `/api/v/probe/ok` -> HTTP 404 `application/problem+json` with `code: "not_found"`
  - Request with boundary non-numeric prefixes `/api/v1beta`, `/api/v1.` -> HTTP 404 `application/problem+json` with `code: "not_found"`
  - Request to unrelated paths `/api/videos`, `/api/videos/123` -> HTTP 404 `application/problem+json` with `code: "not_found"`
  - POST JSON to GET-only endpoint `/api/v1/probe/ok` -> HTTP 405 `application/problem+json` with `code: "method_not_allowed"`, matching `correlationId`
  - POST text/plain to JSON-only endpoint `/api/v1/probe/validate` -> HTTP 415 `application/problem+json` with `code: "unsupported_media_type"`, matching `correlationId`
- **Known gaps, skipped tests, and reason:** Business authorization, persistence, concurrency, and audit tests skipped because P1-01 is strictly HTTP platform foundation without domain entities or database.
- **Residual risks:** None.
- **Reviewer findings and resolution:**
  - Finding 1: ProblemDetails customizer only mapped codes for 400, 404, and 5xx; framework errors 405 and 415 lacked the `code` extension, compromising machine-readability of error envelopes.
  - Resolution 1: Added `method_not_allowed` and `unsupported_media_type` to `ApiErrorCodes`, updated `CustomizeProblemDetails` in `ServiceCollectionExtensions` to handle 405 and 415, added negative-first contract tests, and verified all tests pass.
  - Finding 2 (Confirmed Numeric Routing Policy): Substring-based `StartsWith("/api/v")` matched unrelated `/api/v...` routes such as `/api/videos/123`, and did not handle numeric versions without trailing slashes (`/api/v99.0`) uniformly. Non-numeric malformed routes (e.g. `/api/vabc/probe/ok`, `/api/v/probe/ok`) were previously expected to return 400, but under confirmed routing policy only numeric version segments belong to the version middleware namespace; non-numeric prefixes must bypass it and return 404 `not_found`.
  - Resolution 2: Replaced string inspection in `ApiVersioningValidationMiddleware` with an anchored, culture-invariant regex `^/api/v\d+(\.\d+)?(/.*)?$`. The numeric version is extracted independently of trailing slashes without redirects. Unsupported numeric versions (`/api/v99.0`, `/api/v99.0/`, `/api/v99.0/probe/ok`) return HTTP 400 Bad Request `unsupported_api_version`. Non-numeric routes (`/api/vabc/...`, `/api/v/...`, `/api/v1beta`, `/api/v1.`) and unrelated routes (`/api/videos`, `/api/videos/123`) bypass the middleware and return HTTP 404 Not Found `not_found`. All negative and positive contract tests updated and verified passing.
- **Exact next task/action:** P1-02 (ADRs and API error-code naming policy).

## Final Person 2 cross-review (2026-09-17)

- **Reviewer:** Person 2
- **Evidence source:** Repository owner confirmation in the Wave 0 merge-gate review on 2026-09-17.
- **Reviewed commit:** `3072852`
- **Review scope:** authorization, state transitions, immutability/versioning, idempotency, concurrency, audit, and missing tests. The reviewer confirmed that business-workflow dimensions are correctly marked not applicable for this HTTP platform task and accepted the API versioning and ProblemDetails negative-test coverage.
- **Review result:** Accepted with no open findings.
- **Final status:** `Done`
