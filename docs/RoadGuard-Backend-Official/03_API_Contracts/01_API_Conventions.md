# 01 API Conventions

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

HTTP versioned controllers use `/api/v1/...` via Asp.Versioning attributes. `/health` is a separate unversioned process-liveness endpoint mapped by Program.cs; Swagger/UI are Development-only infrastructure surfaces, outside production controller operation total. Minimal endpoints are not hidden by the count.

JSON naming, enum handling and ProblemDetails behavior follow API platform registration and each DTO/mapper. Use actual schema property names and enum strings, not historical V2 aliases. Query pagination is route-specific (`limit` versus `pageSize`, cursor contents/signed scope differ); there is no universal cursor contract. Null primary-PM is200 with null for a valid project; invalid/inaccessible project is a separate result.

Authentication defaults to bearer or method/class eligible cookie selector. Explicit web endpoints choose web-cookie scheme; AI callback uses its named authentication policy. Anonymous registration/login/csrf still validate input/OTP/antiforgery as applicable. Authorization header precedence and current account/session checks are retained. Never infer all endpoints permit cookie from a GET example.

GET requests have no body unless an actual schema/action says otherwise. Commands bind path, body and headers exactly: some use strong If-Match ETags/Idempotency-Key; project updates use ExpectedRowVersion/OperationId in DTO. ETag quote/base64 semantics differ by aggregate; never substitute project version for membership/task/item version. Catalog contains request schema/response/header observations for every operation.

Validation/auth failures can occur in middleware before the action; action result maps add business codes. A documented Swagger status is not an exhaustive execution proof. Content-Type application/problem+json is used where controller/platform creates ProblemDetails; legacy mapping differences remain visible rather than normalized by docs.

Source evidence: RoadGuardSystem.API/Program.cs, Extensions/ServiceCollectionExtensions.cs, Extensions/ConfigureSwaggerOptions.cs, Extensions/ManualContractOperationFilter.cs, Extensions/ManualContractSchemaFilter.cs and Constants/ApiErrorCodes.cs; exact action paths/lines are in the catalog.
