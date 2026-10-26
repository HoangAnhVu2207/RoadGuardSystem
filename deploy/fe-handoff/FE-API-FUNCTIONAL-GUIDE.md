# FE API functional guide

Authority: OWNER-FE-UI-AUTONOMY-01. FE chooses screens/navigation. This guide describes all 260 existing canonical operations by current contract/source; it does not approve new business transitions or certify unexecuted behavior.

Use [business flows](FE-BUSINESS-FLOW-GUIDE.md), [test instructions](FE-API-TEST-GUIDE.md), [auth](FE-AUTH-AND-CLIENT-GUIDE.md) and the exact [OpenAPI](openapi.json). Current bounded proof and missing predicates are separate in [preview acceptance](review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json). Historical frozen status remains unchanged.

For writes, first load the authorized resource and use its declared version/ETag and exact request schema. Never use an ID as proof of access. A business precondition not established by the referenced contract/source remains unresolved; do not infer it from a route name. GETs read authorized data; do not treat their availability as permission to perform a later command.

## API-001 — POST `/api/v1/admin/users/{userId}/password-reset`

Reset password admin users resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: AdminPasswordResetRequestDto.
- Parameters: userId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=expectedTargetRowVersion; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AdminPasswordResetResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: identity_user_not_found.; 409 Error codes: auth_concurrency_conflict, duplicate_request, identity_user_inactive.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-001`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AdminUsersController.cs`.

## API-002 — POST `/api/v1/auth/android/login`

Android login auth resources. Permissions: Anonymous credentials / current session.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Anonymous credentials / current session.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: LoginRequestDto.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=email,password; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AuthTokenResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_invalid_credentials.; 409 Error codes: auth_concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-002`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AuthController.cs`.

## API-003 — POST `/api/v1/auth/android/refresh`

Android refresh auth resources. Permissions: Anonymous credentials / current session.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Anonymous credentials / current session.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RefreshRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=refreshToken; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AuthTokenResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: refresh_token_invalid, refresh_token_expired, auth_session_revoked, auth_unauthorized.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-003`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AuthController.cs`.

## API-004 — POST `/api/v1/auth/change-password`

Change password auth resources. Permissions: Current active user.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current active user.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ChangePasswordRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=currentPassword,newPassword; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 204: no response body.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_invalid_credentials, auth_session_revoked, auth_unauthorized, refresh_token_expired, refresh_token_invalid.; 409 Error codes: auth_concurrency_conflict, idempotency_key_reused.; 403 Error codes: auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-004`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AuthController.cs`.

## API-005 — POST `/api/v1/auth/forced-password-change`

Forced password change auth resources. Permissions: Anonymous credentials / current session.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Anonymous credentials / current session.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ForcedPasswordChangeRequestDto.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=confirmPassword,currentPassword,newPassword,username; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 204: no response body.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_invalid_credentials, auth_session_revoked, auth_unauthorized, refresh_token_expired, refresh_token_invalid.; 409 Error codes: auth_concurrency_conflict, idempotency_key_reused.; 403 Error codes: auth_password_change_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-005`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AuthController.cs`.

## API-006 — POST `/api/v1/auth/login`

Login auth resources. Permissions: Anonymous credentials / current session.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Anonymous credentials / current session.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: LoginRequestDto.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=email,password; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AuthTokenResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_invalid_credentials.; 409 Error codes: auth_concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-006`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AuthController.cs`.

## API-007 — POST `/api/v1/auth/logout`

Logout auth resources. Permissions: Current active user.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Current active user.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 204: no response body.
- Supported errors: 401 Error codes: auth_invalid_credentials, auth_session_revoked, auth_unauthorized, refresh_token_expired, refresh_token_invalid.; 400 Error codes: validation_error.; 403 Error codes: auth_password_change_required.; 409 Error codes: auth_concurrency_conflict, idempotency_key_reused.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-007`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AuthController.cs`.

## API-008 — POST `/api/v1/auth/password-recovery-requests`

Request password recovery auth resources. Permissions: Anonymous credentials / current session.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Anonymous credentials / current session.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: PasswordRecoveryRequestDto.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=email; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: no response body.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_invalid_credentials, auth_session_revoked, auth_unauthorized, refresh_token_expired, refresh_token_invalid.; 403 Error codes: auth_password_change_required.; 409 Error codes: auth_concurrency_conflict, idempotency_key_reused.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-008`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AuthController.cs`.

## API-009 — POST `/api/v1/auth/refresh`

Refresh auth resources. Permissions: Anonymous credentials / current session.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Anonymous credentials / current session.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RefreshRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=refreshToken; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AuthTokenResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: refresh_token_invalid, refresh_token_expired, auth_session_revoked, auth_unauthorized.; 409 Conflict; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-009`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AuthController.cs`.

## API-010 — POST `/api/v1/auth/reporter-registrations`

Register reporter registrations resources. Permissions: Anonymous Reporter applicant.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Anonymous Reporter applicant.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RegisterReporterRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=displayName,email,password,reporterType; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: RegistrationIntentResponseDto.
- Supported errors: 400 Error codes: validation_error.; 409 Error codes: identity_email_conflict, duplicate_request.; 503 Error codes: internal_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-010`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterRegistrationsController.cs`.

## API-011 — POST `/api/v1/auth/reporter-registrations/resend`

Resend reporter registrations resources. Permissions: Anonymous Reporter applicant.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Anonymous Reporter applicant.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ResendReporterOtpRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: RegistrationIntentResponseDto.
- Supported errors: 400 Error codes: validation_error.; 409 Error codes: identity_email_conflict, duplicate_request.; 404 Error codes: not_found.; 429 Error codes: validation_error.; 503 Error codes: internal_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-011`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterRegistrationsController.cs`.

## API-012 — POST `/api/v1/auth/reporter-registrations/verify`

Verify reporter registrations resources. Permissions: Anonymous Reporter applicant.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Anonymous Reporter applicant.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: VerifyReporterOtpRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=otp; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AuthTokenResponseDto.
- Supported errors: 400 Error codes: validation_error.; 409 Error codes: identity_email_conflict, duplicate_request.; 404 Error codes: not_found.; 429 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-012`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterRegistrationsController.cs`.

## API-013 — GET `/api/v1/auth/web/csrf`

Get the CSRF token for this browser session.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Controller/action and service authority; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-013`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/WebAuthController.cs`.

## API-014 — POST `/api/v1/auth/web/login`

Sign in to a browser session. Supply the CSRF token from the bootstrap endpoint.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Controller/action and service authority; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: LoginRequestDto.
- Parameters: none declared.
- Request headers: X-CSRF-TOKEN (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=email,password; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 403 Error codes: csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-014`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/WebAuthController.cs`.

## API-015 — POST `/api/v1/auth/web/logout`

Sign out of the current browser session. Supply its CSRF token and a new Idempotency-Key.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Controller/action and service authority; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: none declared.
- Request headers: Idempotency-Key (required); X-CSRF-TOKEN (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 204: no response body.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 409 Error codes: idempotency_key_reused.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-015`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/WebAuthController.cs`.

## API-016 — POST `/api/v1/auth/web/renew`

Renew the browser session using its renewal cookie and CSRF token. No body or Authorization header.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Controller/action and service authority; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: none declared.
- Request headers: X-CSRF-TOKEN (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 403 Error codes: csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-016`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/WebAuthController.cs`.

## API-017 — GET `/api/v1/auth/web/session`

Read the current browser session. Authentication: RoadGuard web session cookie.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Controller/action and service authority; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 401 Error codes: auth_unauthorized, auth_session_revoked.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-017`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/WebAuthController.cs`.

## API-018 — GET `/api/v1/cases`

List cases resources. Permissions: Supervisor / PM; unassigned readable only by Supervisor.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor / PM; unassigned readable only by Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [query] {"type": "string", "format": "uuid"}; status [query] {"type": "string"}; pageSize [query] {"type": "integer", "format": "int32", "default": 20}; cursor [query] {"type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: CasePageDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: reference_scope_conflict.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-018`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CasesController.cs`.

## API-019 — GET `/api/v1/cases/{caseId}`

Read cases resources. Permissions: Supervisor / PM; unassigned readable only by Supervisor.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor / PM; unassigned readable only by Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: caseId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: InternalCaseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: reference_scope_conflict.; 412 Error codes: concurrency_conflict.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-019`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CasesController.cs`.

## API-020 — POST `/api/v1/cases/{caseId}/conclusions`

Conclude cases resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CaseConclusionDto.
- Parameters: caseId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: InternalCaseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, reference_scope_conflict, invalid_state_transition, source_not_ready.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-020`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CasesController.cs`.

## API-021 — POST `/api/v1/cases/{caseId}/publications`

Publish cases resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CasePublicationDto.
- Parameters: caseId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: CasePublicationResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, reference_scope_conflict, invalid_state_transition, source_not_ready.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-021`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CasesController.cs`.

## API-022 — POST `/api/v1/cases/{caseId}/report-links`

Link cases resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CaseLinkDto.
- Parameters: caseId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: InternalCaseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, reference_scope_conflict, invalid_state_transition.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-022`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CasesController.cs`.

## API-023 — POST `/api/v1/cases/{caseId}/report-splits`

Split cases resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CaseSplitDto.
- Parameters: caseId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: InternalCaseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, reference_scope_conflict, invalid_state_transition.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-023`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CasesController.cs`.

## API-024 — POST `/api/v1/cases/{caseId}/triage`

Triage a case using the selected verification method. Requires Supervisor for an UNASSIGNED case or Project Manager where permitted. Use the current case ETag in If-Match and a new Idempotency-Key.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: Controller/action and service authority; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CaseTriageDto.
- Parameters: caseId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: InternalCaseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, reference_scope_conflict, invalid_state_transition, source_not_ready, candidate_stale.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-024`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CasesController.cs`.

## API-025 — GET `/api/v1/datasets/{datasetId}/coverage`

Get dataset coverage survey v2 resources. Permissions: PM / Supervisor / assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: datasetId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: DatasetCoverageResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 422 Error codes: survey_validation_failed.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-025`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-026 — GET `/api/v1/datasets/{id}`

Read dataset survey v2 resources. Permissions: PM / Supervisor / assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: id [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: DatasetDetailView.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-026`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-027 — POST `/api/v1/datasets/{id}/assessments`

Create survey assessments resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateAssessmentDto.
- Parameters: id [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: AssessmentView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: duplicate_request.; 412 Error codes: concurrency_conflict.; 422 Error codes: assessment_method_not_approved, assessment_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-027`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyAssessmentsController.cs`.

## API-028 — GET `/api/v1/datasets/{id}/assessments/{assessmentId}`

Read survey assessments resources. Permissions: PM / Supervisor / assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: id [path] {"type": "string", "format": "uuid"}; assessmentId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AssessmentView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-028`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyAssessmentsController.cs`.

## API-029 — GET `/api/v1/files/{fileId}`

Get file metadata uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator. AI frame reads require current AI-result project authority (PM or Supervisor); no signed URL is issued.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.; current resource authority applies AI frames: current active project PM or Supervisor under existing AI-result authority; Operator is denied. Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: fileId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: FileMetadataResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: file_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-029`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UploadsController.cs`.

## API-030 — GET `/api/v1/files/{fileId}/content`

Download uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator. AI frame reads require current AI-result project authority (PM or Supervisor); no signed URL is issued.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.; current resource authority applies AI frames: current active project PM or Supervisor under existing AI-result authority; Operator is denied. Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: fileId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: string.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: file_not_found.; 503 Error codes: upload_storage_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-030`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UploadsController.cs`.

## API-031 — POST `/api/v1/internal/processing-jobs/{jobId}/results`

Submit a processing result. Requires a dedicated RS256 AI service bearer token (issuer roadguard-backend, audience roadguard-ai-callback, TTL 300 seconds), bound to the current jobId and attemptId.

- Classification: BACKEND_INTERNAL; functional group W05.
- When: Dedicated AI-service callback only. FE polls authorized job/result resources; do not call this endpoint with human credentials.
- Actor/scope: Controller/action and service authority; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ReceiveAiResultRequestDto.
- Parameters: jobId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=checksumSha256,detections,manifestHash,mode; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProcessingJobResponseDto.
- Supported errors: 428 Error codes: validation_error.; 404 Error codes: not_found.; 409 Error codes: duplicate_request, validation_error.; 422 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.; 401 Authentication challenge: missing, expired or invalid credential/session.; 403 Authorization policy or current authority denies this request.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-031`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs`.

## API-032 — POST `/api/v1/invitations`

Create invitations resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateInvitationRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=email,projectIds,role; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: InvitationResponseDto.
- Supported errors: 400 Error codes: validation_error.; 404 Error codes: not_found.; 409 Error codes: identity_email_conflict, duplicate_request.; 403 Error codes: access_forbidden.; 503 Error codes: internal_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.; 401 Authentication challenge: missing, expired or invalid credential/session.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-032`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/InvitationsController.cs`.

## API-033 — POST `/api/v1/invitations/accept`

Accept invitations resources. Permissions: Anonymous invitation recipient.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Anonymous invitation recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: AcceptInvitationRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=displayName,invitationToken,password; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AuthTokenResponseDto.
- Supported errors: 400 Error codes: validation_error.; 404 Error codes: not_found.; 409 Error codes: identity_email_conflict, duplicate_request.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-033`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/InvitationsController.cs`.

## API-034 — POST `/api/v1/labels/{labelId}/review`

Review training labels resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W04.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ReviewTrainingLabelDto.
- Parameters: labelId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: TrainingLabelViewDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 400 Error codes: validation_error.; 409 Error codes: candidate_stale, source_not_ready, idempotency_key_reused, invalid_state_transition.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-034`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/TrainingLabelsController.cs`.

## API-035 — GET `/api/v1/me`

Get me resources. Permissions: Current active user (self).

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Current active user (self).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ActorResponseDto.
- Supported errors: 401 Request rejected.; 404 Error codes: identity_user_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-035`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/MeController.cs`.

## API-036 — PATCH `/api/v1/me`

Update me resources. Permissions: Current active user (self).

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current active user (self).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: UpdateMeRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=displayName; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ActorResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: identity_user_not_found.; 409 Error codes: duplicate_request.; 412 Error codes: auth_concurrency_conflict.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-036`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/MeController.cs`.

## API-037 — GET `/api/v1/me/inspection-tasks`

List inspection tasks resources. Permissions: Crew; current own assignments.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: Crew; current own assignments.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: cursor [query] {"type": "string"}; limit [query] {"type": "integer", "format": "int32", "default": 50}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: InspectionTaskPageResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-037`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/InspectionTasksController.cs`.

## API-038 — GET `/api/v1/me/survey-tasks`

List my tasks survey v2 resources. Permissions: Assigned Operator.

- Classification: FE_DIRECT; functional group STANDALONE/W05.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: cursor [query] {"type": "string"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "format": "int32"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyTaskPageV2ResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-038`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-039 — GET `/api/v1/notifications`

List notifications resources. Permissions: Current recipient; project/duty scope applies.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: Current recipient; project/duty scope applies.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: cursor [query] {"type": "string"}; limit [query] {"type": "integer", "format": "int32"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: NotificationPageDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-039`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/NotificationsController.cs`.

## API-040 — GET `/api/v1/notifications/{notificationId}`

Get notifications resources. Permissions: Current recipient; project/duty scope applies.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: Current recipient; project/duty scope applies.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: notificationId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: NotificationDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 404 Error codes: notification_not_found.; 400 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-040`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/NotificationsController.cs`.

## API-041 — POST `/api/v1/notifications/{notificationId}/read`

Mark read notifications resources. Permissions: Current recipient; project/duty scope applies.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: Current recipient; project/duty scope applies.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: notificationId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: NotificationDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 404 Error codes: notification_not_found.; 409 Error codes: duplicate_request.; 412 Error codes: notification_concurrency_conflict.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-041`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/NotificationsController.cs`.

## API-042 — GET `/api/v1/notifications/{notificationId}/scope`

Scope h6 notification operations resources. Permissions: Current recipient; project/duty scope applies.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: Current recipient; project/duty scope applies.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: notificationId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: H6NotificationScopeDto.
- Supported errors: 401 Error codes: unauthorized.; 404 Error codes: notification_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-042`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/H6NotificationOperationsController.cs`.

## API-043 — POST `/api/v1/offline/uploads`

Create offline uploads resources. Permissions: PM / Crew; valid offline artifact/source.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Crew; valid offline artifact/source.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineUploadCreateRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: UploadSessionResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: upload_session_not_found.; 409 Error codes: duplicate_request.; 415 Error codes: unsupported_media_type.; 422 Error codes: upload_validation_failed.; 428 Error codes: validation_error.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-043`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UploadsController.cs`.

## API-044 — POST `/api/v1/processing-jobs`

Create processing job processing v2 resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateProcessingJobRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=configVersion,mode,preprocessingVersion; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: ProcessingJobResponseDto.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: duplicate_request, validation_error.; 422 Error codes: validation_error.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-044`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs`.

## API-045 — GET `/api/v1/processing-jobs/{jobId}`

Get processing job processing v2 resources. Permissions: PM / Supervisor / assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: jobId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProcessingJobResponseDto.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-045`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs`.

## API-046 — POST `/api/v1/processing-jobs/{jobId}/retry`

Retry processing job processing v2 resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RetryProcessingJobRequestDto.
- Parameters: jobId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProcessingJobResponseDto.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: duplicate_request, validation_error.; 412 Error codes: concurrency_conflict.; 422 Error codes: validation_error.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-046`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs`.

## API-047 — GET `/api/v1/profile`

Get profile resources. Permissions: Current active user (self).

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Current active user (self).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProfileResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 400 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-047`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProfileController.cs`.

## API-048 — PUT `/api/v1/profile`

Update profile resources. Permissions: Current active user (self).

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current active user (self).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ProfileUpdateRequestDto.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=displayName,expectedRowVersion; additionalProperties={}. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProfileResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 409 Error codes: auth_concurrency_conflict, duplicate_request, identity_email_conflict.; 403 Error codes: csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-048`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProfileController.cs`.

## API-049 — GET `/api/v1/projects`

List projects currently readable by the caller; page using the returned opaque cursor

- Classification: FE_DIRECT; functional group W02.
- When: Follow W02 only when its entry prerequisites and current authority hold.
- Actor/scope: Current authenticated actor with ProjectListService resource filtering Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: search [query] {"type": "string"}; status [query] {"type": "string"}; cursor [query] {"type": "string"}; limit [query] {"type": "integer", "format": "int32", "default": 20}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProjectListResponseDto.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-049`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectsController.cs`.

## API-050 — POST `/api/v1/projects`

Create projects resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateProjectRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=name; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: CreateProjectResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_primary_pm_not_found, project_handover_file_not_found.; 409 Error codes: project_code_conflict, duplicate_request.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-050`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectsController.cs`.

## API-051 — PUT `/api/v1/projects/{projectId}`

Update projects resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: UpdateProjectRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=expectedRowVersion,name; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: UpdateProjectResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found.; 409 Error codes: concurrency_conflict, duplicate_request.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-051`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectsController.cs`.

## API-052 — GET `/api/v1/projects/{projectId}/ai-candidates/{detectionId}`

Candidate ai mock runs resources. Permissions: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; detectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AiCandidateFactsV1.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: source_not_ready, stale_geometry.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-052`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AiMockRunsController.cs`.

## API-053 — POST `/api/v1/projects/{projectId}/ai-mock-runs`

Create ai mock runs resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateAiMockRunRequest.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: AiMockRunView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 428 Error codes: validation_error.; 422 Error codes: ai_contract_invalid, mock_fixture_source_mismatch.; 409 Error codes: duplicate_request, source_not_ready, candidate_stale.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-053`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AiMockRunsController.cs`.

## API-054 — GET `/api/v1/projects/{projectId}/ai-mock-runs/{runId}`

Get ai mock runs resources. Permissions: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; runId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AiMockRunView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-054`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AiMockRunsController.cs`.

## API-055 — GET `/api/v1/projects/{projectId}/ai-mock-runs/{runId}/result`

Result ai mock runs resources. Permissions: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; runId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: AiResultV1.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: result_not_ready, source_not_ready.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-055`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/AiMockRunsController.cs`.

## API-056 — GET `/api/v1/projects/{projectId}/baseline-selections`

Current survey assessments resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W05.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; segmentSetId [query] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-056`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyAssessmentsController.cs`.

## API-057 — POST `/api/v1/projects/{projectId}/baseline-selections`

Select survey assessments resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W05.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateBaselineDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: BaselineView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: baseline_selection_conflict, duplicate_request.; 422 Error codes: baseline_not_eligible.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-057`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyAssessmentsController.cs`.

## API-058 — GET `/api/v1/projects/{projectId}/baseline-selections/{id}`

History survey assessments resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W05.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; id [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: BaselineView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-058`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyAssessmentsController.cs`.

## API-059 — POST `/api/v1/projects/{projectId}/candidate-decisions`

Decide candidates resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CandidateDecisionRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: CandidateDecisionResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: candidate_stale, idempotency_key_reused, source_not_ready, invalid_state_transition, concurrency_conflict, genuine_recurrence_source_required, verified_project_evidence_required.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-059`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CandidatesController.cs`.

## API-060 — GET `/api/v1/projects/{projectId}/candidate-decisions/{decisionId}`

Read candidates resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; decisionId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: CandidateDecisionResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-060`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CandidatesController.cs`.

## API-061 — GET `/api/v1/projects/{projectId}/candidates`

Match candidates resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; sourceKind [query] {"enum": ["REPORT", "AI_DETECTION", "FIELD_OBSERVATION"], "type": "string", "description": "Source kind; actual producer readiness and exact sourceId required."}; sourceId [query] {"type": "string", "format": "uuid"}; expand [query] {"type": "boolean", "default": false}; pageSize [query] {"type": "integer", "format": "int32", "default": 50}; cursor [query] {"type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: CandidateMatchPageDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: candidate_stale, source_not_ready.; 412 Error codes: concurrency_conflict.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-061`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/CandidatesController.cs`.

## API-062 — GET `/api/v1/projects/{projectId}/clocks`

Clocks h6 notification operations resources. Permissions: Current project/digest recipient; current duty scope.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current project/digest recipient; current duty scope.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; cursor [query] {"type": "string"}; limit [query] {"type": "integer", "format": "int32"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: H6ClockPage.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: project_access_denied.; 409 Error codes: cursor_stale.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-062`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/H6NotificationOperationsController.cs`.

## API-063 — POST `/api/v1/projects/{projectId}/clocks/{clockId}/appointments`

Appoint clock duty resources. Permissions: PM review duty: PM; Supervisor escalation/final duty: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM review duty: PM; Supervisor escalation/final duty: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: BusinessDutyAppointmentInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; clockId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, clock_decision_authority_required, assignee_not_authorized. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: clock_extension_prohibited, clock_source_stale, concurrency_conflict, idempotency_key_reused, use_receiving_request_or_task_assignment, duty_assignment_superseded.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-063`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ClockDutyController.cs`.

## API-064 — POST `/api/v1/projects/{projectId}/clocks/{clockId}/extensions`

Extend clock duty resources. Permissions: Supervisor: final/escalation; PM: supplement/sync; other kinds denied.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor: final/escalation; PM: supplement/sync; other kinds denied.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ClockExtensionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; clockId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, clock_decision_authority_required. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: clock_extension_prohibited, clock_source_stale, concurrency_conflict, idempotency_key_reused.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-064`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ClockDutyController.cs`.

## API-065 — GET `/api/v1/projects/{projectId}/crs-profiles`

Profiles geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: array of CrsProfileView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found.; 412 Error codes: concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-065`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-066 — POST `/api/v1/projects/{projectId}/crs-profiles`

Create profile geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CrsProfileInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: CrsProfileView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found.; 409 Error codes: duplicate_request, project_closed, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-066`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-067 — GET `/api/v1/projects/{projectId}/crs-profiles/{profileId}`

Profile geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; profileId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: CrsProfileView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: crs_profile_not_found, project_not_found.; 412 Error codes: concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-067`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-068 — GET `/api/v1/projects/{projectId}/defect-statistics`

Read defect statistics resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; routeVersionId [query] {"type": "string", "format": "uuid"}; segmentSetId [query] {"type": "string", "format": "uuid"}; segmentIds [query] {"type": "array", "items": {"type": "string", "format": "uuid"}}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: DefectStatisticsRead.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-068`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DefectStatisticsController.cs`.

## API-069 — POST `/api/v1/projects/{projectId}/defect-statistics`

Confirm defect statistics resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: DefectStatisticsInputDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: DefectStatisticsRead; 200: DefectStatisticsRead.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, scope_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 428 Error codes: precondition_required.; 409 Error codes: statistics_source_stale, scope_stale, segment_source_mismatch, quantity_source_mismatch, shared_identity_scope_conflict, quantity_shared_identity_conflict, synthetic_source_cannot_activate, concurrency_conflict, statistics_supersession_conflict, quantity_allocation_conflict, quantity_source_ambiguous, idempotency_key_reused.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-069`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DefectStatisticsController.cs`.

## API-070 — GET `/api/v1/projects/{projectId}/defects`

List defects resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; status [query] {"type": "string"}; type [query] {"type": "string"}; segmentId [query] {"type": "string", "format": "uuid"}; pageSize [query] {"type": "integer", "format": "int32", "default": 50}; cursor [query] {"type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: DefectPageDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 400 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-070`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DefectsController.cs`.

## API-071 — GET `/api/v1/projects/{projectId}/defects/{defectId}`

Read defects resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; defectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: DefectViewDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-071`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DefectsController.cs`.

## API-072 — POST `/api/v1/projects/{projectId}/defects/{defectId}/assessments`

Assess defects resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: DefectAssessmentRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; defectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: DefectViewDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 400 Error codes: validation_error.; 409 Error codes: source_not_ready, candidate_stale, invalid_state_transition, idempotency_key_reused.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-072`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DefectsController.cs`.

## API-073 — POST `/api/v1/projects/{projectId}/defects/{defectId}/verification-decisions`

Verify defects resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: DefectVerificationRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; defectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: DefectViewDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 400 Error codes: validation_error.; 409 Error codes: source_not_ready, candidate_stale, invalid_state_transition, idempotency_key_reused.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-073`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DefectsController.cs`.

## API-074 — POST `/api/v1/projects/{projectId}/exports`

Create exports resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateExportRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: ExportJobViewDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: csrf_failed, forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_conflict.; 422 Error codes: export_invalid, export_source_unavailable, no_eligible_labels, producer_invalid.; 428 Error codes: precondition_required.; 503 Error codes: producer_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-074`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ExportsController.cs`.

## API-075 — GET `/api/v1/projects/{projectId}/exports/{exportId}`

Get exports resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; exportId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ExportJobViewDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-075`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ExportsController.cs`.

## API-076 — GET `/api/v1/projects/{projectId}/exports/{exportId}/content`

Content exports resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; exportId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: string.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: forbidden.; 404 Error codes: not_found.; 409 Error codes: export_not_ready.; 410 Error codes: export_expired.; 503 Error codes: export_storage_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-076`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ExportsController.cs`.

## API-077 — GET `/api/v1/projects/{projectId}/exports/{exportId}/manifest`

Manifest exports resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; exportId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ExportManifestV1Dto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-077`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ExportsController.cs`.

## API-078 — GET `/api/v1/projects/{projectId}/field-inspection-tasks`

List field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; afterId [query] {"type": "string", "format": "uuid"}; limit [query] {"type": "integer", "format": "int32", "default": 50}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-078`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-079 — POST `/api/v1/projects/{projectId}/field-inspection-tasks`

Create field inspection workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldTaskCreateInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: FieldTaskViewFact; 200: FieldTaskViewFact.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, assignee_not_authorized, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, geometry_version_mismatch, idempotency_key_reused, project_not_active, source_not_ready.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-079`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-080 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}`

Get field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: FieldTaskViewFact.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-080`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-081 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/accept`

Accept field inspection workflow resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldTaskActionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: FieldTaskViewFact; 200: FieldTaskViewFact.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-081`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-082 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/assign`

Assign field inspection workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldTaskActionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: FieldTaskViewFact; 200: FieldTaskViewFact.
- Supported errors: 400 Error codes: handover_facts_required, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, assignee_not_authorized, auth_password_change_required, csrf_failed, handover_history_scope_invalid. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, project_not_active, repair_binding_required.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-082`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-083 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/cancel`

Cancel field inspection workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldTaskActionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: FieldTaskViewFact; 200: FieldTaskViewFact.
- Supported errors: 400 Error codes: handover_facts_required, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, handover_history_scope_invalid. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, project_not_active, repair_binding_required.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-083`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-084 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence-reuse`

Reuse field inspection workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldEvidenceReuseInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, evidence_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, evidence_version_mismatch, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-084`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-085 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}`

Evidence field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}; fileId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: FieldTaskEvidenceFile.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, evidence_access_forbidden.; 404 Error codes: not_found.; 409 Error codes: evidence_version_mismatch.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-085`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-086 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}/content`

Download evidence field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}; fileId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: string.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, evidence_access_forbidden.; 404 Error codes: not_found.; 409 Error codes: evidence_version_mismatch, source_not_ready.; 500 Error codes: internal_error.; 503 Error codes: storage_unavailable.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-086`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-087 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/geometry`

Geometry field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 409 Error codes: source_not_ready.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-087`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-088 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/history`

History field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-088`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-089 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/location-impact-decisions`

Impact field inspection workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldLocationImpactActionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: handover_facts_required, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, assignee_not_authorized, auth_password_change_required, csrf_failed, handover_history_scope_invalid. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, geometry_version_mismatch, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, project_not_active, repair_binding_required, source_not_ready.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-089`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-090 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reassign`

Reassign field inspection workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldTaskActionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: FieldTaskViewFact; 200: FieldTaskViewFact.
- Supported errors: 400 Error codes: handover_facts_required, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, assignee_not_authorized, auth_password_change_required, csrf_failed, handover_history_scope_invalid. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, project_not_active, repair_binding_required.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-090`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-091 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reject`

Reject field inspection workflow resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldTaskActionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: FieldTaskViewFact; 200: FieldTaskViewFact.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-091`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-092 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reviews`

Review field inspection workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldReviewInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: FieldReviewViewFact; 200: FieldReviewViewFact.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, current_review_assignee_required, evidence_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: evidence_not_found, not_found.; 409 Error codes: concurrency_conflict, current_assignment_missing, evidence_version_mismatch, field_result_insufficient, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, project_not_active, repair_binding_required, submission_lineage_conflict.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-092`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-093 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start`

Start field inspection workflow resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldStartInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, fast_track_clock_origin_locked, fast_track_original_start_locked, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, origin_content_conflict, project_not_active, repair_authorization_scope_conflict, repair_binding_not_current, repair_package_source_missing.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-093`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-094 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start-origin`

Origin field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-094`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-095 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions`

Submit field inspection workflow resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: FieldSubmissionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: FieldSubmissionViewFact; 200: FieldSubmissionViewFact.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, evidence_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: evidence_not_found, not_found.; 409 Error codes: concurrency_conflict, evidence_version_mismatch, first_start_required, idempotency_key_reused, invalid_state_transition, legacy_task_read_only, origin_content_conflict, project_not_active, submission_lineage_conflict.; 415 Error codes: unsupported_media_type.; 428 Error codes: precondition_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-095`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-096 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId}`

Submission field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

- Classification: FE_DIRECT; functional group W06.
- When: Follow W06 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}; submissionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: FieldSubmissionViewFact.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-096`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs`.

## API-097 — GET `/api/v1/projects/{projectId}/geometry-location-impacts`

List impacts pavement workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; previousRouteVersionId [query] {"type": "string", "format": "uuid"}; newRouteVersionId [query] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: array of GeometryImpactView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 412 Error codes: content_hash_mismatch.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-097`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-098 — POST `/api/v1/projects/{projectId}/geometry-location-impacts`

Create impact pavement workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryImpactInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: GeometryImpactView; 200: GeometryImpactView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_conflict, impact_road_section_mismatch, project_not_active, resource_conflict.; 412 Error codes: content_hash_mismatch.; 422 Error codes: impact_versions_invalid, pavement_layout_invalid, route_version_required.; 428 Error codes: idempotency_key_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-098`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-099 — GET `/api/v1/projects/{projectId}/geometry-location-impacts/{impactId}`

Get impact pavement workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; impactId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryImpactView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 412 Error codes: content_hash_mismatch.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-099`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-100 — POST `/api/v1/projects/{projectId}/geometry-location-impacts/{impactId}/decisions`

Decide impact pavement workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryImpactDecisionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; impactId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: GeometryImpactDecisionView; 200: GeometryImpactDecisionView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_conflict, project_not_active, resource_conflict, task_not_in_impact.; 412 Error codes: content_hash_mismatch.; 422 Error codes: impact_action_invalid, pavement_layout_invalid.; 428 Error codes: idempotency_key_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-100`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-101 — POST `/api/v1/projects/{projectId}/geometry-map-publications`

Publish pavement workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryMapPublishInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: GeometryMapManifest; 200: GeometryMapManifest.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: geometry_version_mismatch, idempotency_conflict, native_route_metadata_missing, official_crs_acceptance_pending, project_not_active, resource_conflict, segment_set_not_published.; 412 Error codes: content_hash_mismatch.; 422 Error codes: crs_profile_invalid, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, pavement_layout_invalid, publication_mode_invalid, route_version_required, unsupported_crs.; 428 Error codes: content_hash_required, idempotency_key_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-101`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-102 — GET `/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}`

Manifest pavement workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; publicationId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryMapManifest.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 412 Error codes: content_hash_mismatch.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-102`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-103 — GET `/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}/layers/{layer}`

Read geometry features for the selected layer. Supply expectedContentHash in the query or a quoted If-Match hash. If both are supplied, they must agree.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Controller/action and service authority; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; publicationId [path] {"type": "string", "format": "uuid"}; layer [path] {"type": "string"}; expectedContentHash [query] {"pattern": "^[0-9a-fA-F]{64}$", "type": "string", "description": "Unquoted 64-character hexadecimal hash, alternative to If-Match. If both are supplied, they must agree."}; limit [query] {"maximum": 500, "minimum": 1, "type": "integer", "format": "int32", "default": 100}; bbox [query] {"maxItems": 4, "minItems": 4, "type": "array", "items": {"type": "number", "format": "double"}, "description": "Four finite native-coordinate bounds minX,minY,maxX,maxY; ordered extent required."}; cursor [query] {"maxLength": 4096, "type": "string", "description": "Opaque page cursor bound to publication,layer and filter hash; use next cursor exactly."}; routeVersionId [query] {"type": "string", "format": "uuid"}; segmentSetId [query] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryMapPage.
- Supported errors: 400 Error codes: geometry_cursor_invalid, geometry_page_invalid, validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: geometry_layer_not_found, not_found.; 409 Error codes: geometry_layer_unavailable, geometry_version_mismatch.; 412 Error codes: content_hash_mismatch.; 422 Error codes: geometry_snapshot_invalid.; 428 Error codes: content_hash_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-103`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-104 — GET `/api/v1/projects/{projectId}/geometry-package`

Package geometry workflow resources. Permissions: PM / Supervisor / assigned Operator.

- Classification: FE_DIRECT; functional group W02.
- When: Follow W02 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; routeVersionId [query] {"type": "string", "format": "uuid"}; segmentSetId [query] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryPackageView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found, road_section_version_not_found, segment_set_not_found.; 412 Error codes: concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-104`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-105 — GET `/api/v1/projects/{projectId}/labels`

List training labels resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W04.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; pageSize [query] {"type": "integer", "format": "int32", "default": 50}; cursor [query] {"type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: TrainingLabelPageDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 400 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-105`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/TrainingLabelsController.cs`.

## API-106 — POST `/api/v1/projects/{projectId}/labels`

Create training labels resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W04.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateTrainingLabelDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: TrainingLabelViewDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 400 Error codes: validation_error.; 409 Error codes: candidate_stale, source_not_ready, idempotency_key_reused.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-106`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/TrainingLabelsController.cs`.

## API-107 — GET `/api/v1/projects/{projectId}/labels/{labelId}`

Read training labels resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W04.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; labelId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: TrainingLabelViewDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-107`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/TrainingLabelsController.cs`.

## API-108 — POST `/api/v1/projects/{projectId}/labels/{labelId}/revisions`

Revise training labels resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W04.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ReviseTrainingLabelDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; labelId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: TrainingLabelViewDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 400 Error codes: validation_error.; 409 Error codes: candidate_stale, source_not_ready, idempotency_key_reused.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-108`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/TrainingLabelsController.cs`.

## API-109 — GET `/api/v1/projects/{projectId}/lifecycle`

Read project lifecycle resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProjectLifecycleViewDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-109`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectLifecycleController.cs`.

## API-110 — POST `/api/v1/projects/{projectId}/lifecycle/{command}`

Execute project lifecycle resources. Permissions: PM: declarations/recurrences; Supervisor: confirmations/closures/transfers.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM: declarations/recurrences; Supervisor: confirmations/closures/transfers.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: LD06LifecycleInputDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; command [path] {"type": "string"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: ProjectLifecycleViewDto; 200: ProjectLifecycleViewDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, obligation_responsibility_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, construction_declaration_required, construction_evidence_stale, source_unavailable, construction_evidence_required, mandatory_obligations_unresolved, defect_already_closed, obligation_inventory_incomplete, transfer_scope_conflict, transfer_source_stale, verified_project_evidence_required, idempotency_key_reused, candidate_stale, source_not_ready, invalid_state_transition, genuine_recurrence_source_required.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-110`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectLifecycleController.cs`.

## API-111 — POST `/api/v1/projects/{projectId}/lifecycle/renewed-handling-scope`

Renew project lifecycle resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RenewedHandlingScopeInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: ProjectLifecycleViewDto; 200: ProjectLifecycleViewDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, operational_closure_source_unavailable, idempotency_key_reused.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-111`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectLifecycleController.cs`.

## API-112 — GET `/api/v1/projects/{projectId}/members`

List current project members; includeHistory is Supervisor-only

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current Supervisor or authorized project PM; StaffReadService.MembersAsync and repository access check Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; search [query] {"type": "string"}; role [query] {"type": "string"}; status [query] {"type": "string"}; includeHistory [query] {"type": "boolean", "default": false}; limit [query] {"type": "integer", "format": "int32", "default": 20}; cursor [query] {"type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProjectMemberResponseDtoStaffPageResponseDto.
- Supported errors: 
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-112`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/StaffDirectoryController.cs`.

## API-113 — POST `/api/v1/projects/{projectId}/offline/artifacts`

Register artifact offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Crew; original and recipient authority also checked.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineCaptureArtifactInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, offline_admission_proof_unavailable, offline_key_revoked. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, origin_content_conflict, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-113`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-114 — GET `/api/v1/projects/{projectId}/offline/artifacts/{artifactId}`

Get artifact offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / Crew; resource-owner restrictions.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; artifactId [path] {"type": "string", "format": "uuid"}.
- Request headers: If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-114`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-115 — GET `/api/v1/projects/{projectId}/offline/batches/{batchId}`

Get batch offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / Crew; resource-owner restrictions.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; batchId [path] {"type": "string", "format": "uuid"}.
- Request headers: If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OfflineBatchResult.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, handover_grant_expired, handover_grant_revoked, offline_admission_proof_unavailable, offline_key_revoked.; 404 Error codes: not_found.; 409 Error codes: offline_admission_proof_unavailable, origin_content_conflict.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-115`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-116 — POST `/api/v1/projects/{projectId}/offline/devices`

Register device offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Crew; original and recipient authority also checked.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineDeviceRegisterInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 409 Error codes: device_revision_limit, idempotency_key_reused, offline_key_revoked, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-116`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-117 — GET `/api/v1/projects/{projectId}/offline/devices/{registrationId}`

Get device offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / Crew; resource-owner restrictions.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; registrationId [path] {"type": "string", "format": "uuid"}.
- Request headers: If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-117`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-118 — POST `/api/v1/projects/{projectId}/offline/devices/{registrationId}/revoke`

Revoke device offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Crew; original and recipient authority also checked.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineDeviceRevokeInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; registrationId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, offline_key_revoked, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-118`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-119 — POST `/api/v1/projects/{projectId}/offline/grants`

Issue grant offline workflow resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineHandoverGrantInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, offline_key_revoked. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, offline_admission_proof_unavailable, origin_content_conflict, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-119`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-120 — GET `/api/v1/projects/{projectId}/offline/grants/{grantId}`

Get grant offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / Crew; resource-owner restrictions.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; grantId [path] {"type": "string", "format": "uuid"}.
- Request headers: If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-120`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-121 — POST `/api/v1/projects/{projectId}/offline/grants/{grantId}/revoke`

Revoke grant offline workflow resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineGrantRevokeInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; grantId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: handover_grant_revoked, idempotency_key_reused, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-121`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-122 — POST `/api/v1/projects/{projectId}/offline/import`

Import package offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Crew; original and recipient authority also checked.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflinePackageImportInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OfflineBatchResult.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, evidence_access_forbidden, handover_grant_expired, handover_grant_revoked, offline_admission_proof_unavailable, offline_key_revoked, repair_assignment_not_current, repair_binding_not_current, signed_evidence_admission_required, signed_origin_admission_required. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: evidence_not_found, not_found.; 409 Error codes: offline_admission_proof_unavailable, origin_content_conflict, repair_adapter_not_ready, stale_snapshot.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-122`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-123 — GET `/api/v1/projects/{projectId}/offline/origins/{originId}`

Get origin offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / Crew; resource-owner restrictions.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; originId [path] {"type": "string", "format": "uuid"}.
- Request headers: If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OfflineItemResult.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, handover_grant_expired, handover_grant_revoked, offline_admission_proof_unavailable, offline_key_revoked.; 404 Error codes: not_found.; 409 Error codes: offline_admission_proof_unavailable, origin_content_conflict.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-123`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-124 — GET `/api/v1/projects/{projectId}/offline/packages/{packageId}`

Get package offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / Crew; resource-owner restrictions.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}.
- Request headers: If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-124`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-125 — POST `/api/v1/projects/{projectId}/offline/packages/export`

Export package offline workflow resources. Permissions: Source Crew.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: Source Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflinePackageExportInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, offline_key_revoked. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, origin_content_conflict, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-125`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-126 — POST `/api/v1/projects/{projectId}/offline/packages/prepare`

Prepare package offline workflow resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflinePackageExportInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, offline_key_revoked. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, origin_content_conflict, project_not_active.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-126`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-127 — POST `/api/v1/projects/{projectId}/offline/snapshots`

Capture snapshot offline workflow resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineSnapshotInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: object; 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: idempotency_key_reused, offline_location_source_unavailable, project_not_active, repair_adapter_not_ready, repair_snapshot_source_unavailable, source_not_ready.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-127`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-128 — GET `/api/v1/projects/{projectId}/offline/snapshots/{snapshotId}`

Get snapshot offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / Crew; resource-owner restrictions.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; snapshotId [path] {"type": "string", "format": "uuid"}.
- Request headers: If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required.; 404 Error codes: not_found.; 500 Error codes: internal_error.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-128`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-129 — POST `/api/v1/projects/{projectId}/offline/starts/reconcile`

Reconcile start offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Crew; original and recipient authority also checked.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineStartReconcileInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OfflineBatchResult.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, evidence_access_forbidden, handover_grant_expired, handover_grant_revoked, offline_admission_proof_unavailable, offline_key_revoked, repair_assignment_not_current, repair_binding_not_current, signed_evidence_admission_required, signed_origin_admission_required. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: evidence_not_found, not_found.; 409 Error codes: offline_admission_proof_unavailable, origin_content_conflict, repair_adapter_not_ready, stale_snapshot.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-129`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-130 — POST `/api/v1/projects/{projectId}/offline/sync`

Synchronize offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

- Classification: FE_DIRECT; functional group W09.
- When: Follow W09 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Crew; original and recipient authority also checked.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: OfflineSignedBatchInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OfflineBatchResult.
- Supported errors: 400 Error codes: offline_signature_invalid, validation_error.; 401 Error codes: auth_session_revoked, auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden, auth_password_change_required, csrf_failed, evidence_access_forbidden, offline_admission_proof_unavailable, repair_assignment_not_current, repair_binding_not_current, signed_evidence_admission_required, signed_origin_admission_required. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: evidence_not_found, not_found.; 409 Error codes: origin_content_conflict, repair_adapter_not_ready, stale_snapshot.; 415 Error codes: unsupported_media_type.; 428 Error codes: idempotency_key_required.; 500 Error codes: internal_error.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-130`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`.

## API-131 — GET `/api/v1/projects/{projectId}/pavement-layouts/{layoutId}`

Get layout pavement workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; layoutId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: PavementLayoutView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 412 Error codes: content_hash_mismatch.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-131`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-132 — POST `/api/v1/projects/{projectId}/pavement-layouts/as-built`

Create as built pavement workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: AsBuiltLayoutInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: PavementLayoutView; 200: PavementLayoutView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed, source_file_purpose_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: geometry_version_mismatch, idempotency_conflict, native_route_metadata_missing, planned_source_required, project_not_active, resource_conflict, segment_set_not_published, source_file_not_ready, source_file_provenance_unresolved.; 412 Error codes: content_hash_mismatch.; 422 Error codes: pavement_layout_invalid, route_version_required.; 428 Error codes: content_hash_required, idempotency_key_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-132`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-133 — GET `/api/v1/projects/{projectId}/primary-project-manager`

Read the current primary PM, or a null primary when none exists

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current Supervisor or authorized project PM; StaffReadService.PrimaryAsync and repository access check Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: PrimaryProjectManagerResponseDto.
- Supported errors: 
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-133`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/StaffDirectoryController.cs`.

## API-134 — PUT `/api/v1/projects/{projectId}/primary-project-manager`

Reassign primary project manager projects resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ReassignPrimaryProjectManagerRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=effectiveFrom,expectedCurrentMembershipRowVersion,reason; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ReassignPrimaryProjectManagerResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_primary_pm_not_found.; 409 Error codes: concurrency_conflict, duplicate_request, project_closed.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-134`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectsController.cs`.

## API-135 — GET `/api/v1/projects/{projectId}/receiving-requests`

List business duty resources. Permissions: Current responsible actor / PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current responsible actor / PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; scopeId [query] {"type": "string", "format": "uuid"}; after [query] {"type": "string", "format": "uuid"}; limit [query] {"type": "integer", "format": "int32", "default": 25}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: object.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-135`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/BusinessDutyController.cs`.

## API-136 — GET `/api/v1/projects/{projectId}/receiving-requests/{requestId}`

Read business duty resources. Permissions: Current responsible actor / PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current responsible actor / PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; requestId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: BusinessDutyView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-136`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/BusinessDutyController.cs`.

## API-137 — POST `/api/v1/projects/{projectId}/receiving-requests/{requestId}/ack`

Ack business duty resources. Permissions: Responsible actor (ack).

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Responsible actor (ack).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: BusinessRequestAckInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; requestId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: BusinessDutyView; 200: BusinessDutyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, current_assignee_required. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: request_source_stale, concurrency_conflict, idempotency_key_reused.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-137`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/BusinessDutyController.cs`.

## API-138 — POST `/api/v1/projects/{projectId}/receiving-requests/{requestId}/appointment`

Appoint business duty resources. Permissions: PM for CrewSupplement; Supervisor otherwise.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM for CrewSupplement; Supervisor otherwise.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: BusinessDutyAppointmentInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; requestId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: BusinessDutyView; 200: BusinessDutyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, assignee_not_authorized. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: use_task_reassignment, duty_assignment_superseded, request_source_stale, concurrency_conflict, idempotency_key_reused.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-138`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/BusinessDutyController.cs`.

## API-139 — POST `/api/v1/projects/{projectId}/repair-packages`

Create repair packages resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairPackageCreateInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairPackageView; 200: RepairPackageView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, obligation_responsibility_transferred. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: active_scope_conflict, concurrency_conflict, geometry_version_mismatch, idempotency_key_reused, physical_road_source_mismatch, project_not_active, repair_anchor_not_ready.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-139`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPackagesController.cs`.

## API-140 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items`

Propose repair packages resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairItemProposeInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairItemView; 200: RepairItemView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, obligation_responsibility_transferred, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, explicit_successor_required, idempotency_key_reused, project_not_active, repair_anchor_not_ready.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-140`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPackagesController.cs`.

## API-141 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}`

Item repairs resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RepairItemView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-141`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairsController.cs`.

## API-142 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/approve`

Approve repair packages resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairDecisionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairItemView; 200: RepairItemView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, current_review_assignee_required, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, project_not_active, repair_anchor_not_ready.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-142`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPackagesController.cs`.

## API-143 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/assign`

Assign repair packages resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairItemAssignInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairTaskBindingView; 200: RepairTaskBindingView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, assignee_not_authorized, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, execution_regrant_policy_pending, geometry_version_mismatch, idempotency_key_reused, invalid_state_transition, project_not_active, published_policy_source_required, repair_anchor_not_ready, repair_scope_frame_mismatch, repair_source_conflict, source_not_ready.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-143`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPackagesController.cs`.

## API-144 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/cancellations`

Cancel repair lifecycle resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairCancellationInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairLifecycleView; 200: RepairLifecycleView.
- Supported errors: 400 Error codes: handover_facts_required, validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, handover_start_source_conflict, idempotency_key_reused, project_not_active, repair_anchor_not_ready, repair_binding_source_conflict, repair_binding_source_required, repair_item_not_current, submitted_or_finished_history_requires_intake_review.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-144`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairLifecycleController.cs`.

## API-145 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/corrections`

Correct repairs resources. Permissions: Normal: Supervisor; FT: PM (current final-approver role).

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Normal: Supervisor; FT: PM (current final-approver role).; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairCorrectionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairCorrectionView; 200: RepairCorrectionView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, correction_evidence_access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, correction_evidence_source_not_ready, decision_head_conflict, decision_time_source_conflict, idempotency_key_reused, repair_scope_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-145`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairsController.cs`.

## API-146 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/eligibility`

Get repair eligibility resources. Permissions: Current scoped PM / Supervisor / assigned Crew; repository authority.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Current scoped PM / Supervisor / assigned Crew; repository authority.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RepairEligibilityReadView.
- Supported errors: 400 Error codes: validation_error.; 401 Request rejected.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: repair_binding_not_current, repair_scope_source_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-146`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairEligibilityController.cs`.

## API-147 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/final-confirmation`

Confirm final repair packages resources. Permissions: Normal: Supervisor; FT: PM.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Normal: Supervisor; FT: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairDecisionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairItemView; 200: RepairItemView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, current_review_assignee_required, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: accepted_review_source_conflict, concurrency_conflict, final_confirmation_source_conflict, idempotency_key_reused, invalid_state_transition, project_not_active, repair_anchor_not_ready, supervisor_final_clock_source_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-147`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPackagesController.cs`.

## API-148 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/history`

History repairs resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RepairHistoryView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-148`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairsController.cs`.

## API-149 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/normal-successors`

Continue normally repair lifecycle resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairNormalContinuationInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairLifecycleView; 200: RepairLifecycleView.
- Supported errors: 400 Error codes: handover_facts_required, validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: cancellation_source_required, concurrency_conflict, correction_intake_source_conflict, handover_source_required, handover_start_source_conflict, idempotency_key_reused, normal_continuation_source_conflict, project_not_active, repair_anchor_not_ready, repair_binding_source_conflict, repair_binding_source_required, repair_item_not_current, submitted_or_finished_history_requires_intake_review.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-149`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairLifecycleController.cs`.

## API-150 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/review-requests`

Request review repairs resources. Permissions: PM / assigned Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairReviewRequestInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairReviewRequestView; 200: RepairReviewRequestView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, decision_head_conflict, decision_time_source_conflict, idempotency_key_reused.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-150`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairsController.cs`.

## API-151 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures`

Create repair safety resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairSafetyCreateInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairSafetyView; 200: RepairSafetyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, safety_scope_not_authorized, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, project_not_active, repair_assignment_not_current, repair_binding_not_current, safety_measure_already_exists, safety_obligation_source_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-151`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairSafetyController.cs`.

## API-152 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}`

Read repair safety resources. Permissions: PM / Supervisor / responsible Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / responsible Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; measureId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RepairSafetyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, safety_responsibility_not_current.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-152`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairSafetyController.cs`.

## API-153 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/checks`

Check repair safety resources. Permissions: Responsible Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Responsible Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairSafetyCheckInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; measureId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairSafetyView; 200: RepairSafetyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, evidence_access_forbidden, safety_evidence_scope_forbidden, safety_responsibility_not_current, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: evidence_not_found, not_found.; 409 Error codes: concurrency_conflict, evidence_pending, evidence_version_mismatch, idempotency_key_reused, invalid_state_transition, project_not_active, safety_assignment_source_conflict, safety_evidence_source_conflict, safety_first_check_clock_missing, safety_responsibility_source_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-153`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairSafetyController.cs`.

## API-154 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/installation`

Install repair safety resources. Permissions: Responsible Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Responsible Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairSafetyInstallInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; measureId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairSafetyView; 200: RepairSafetyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, safety_responsibility_not_current, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, project_not_active, safety_already_installed, safety_assignment_source_conflict, safety_responsibility_source_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-154`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairSafetyController.cs`.

## API-155 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/warnings/{warningId}/acknowledgement`

Acknowledge repair safety resources. Permissions: Responsible Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Responsible Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairSafetyAcknowledgementInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; measureId [path] {"type": "string", "format": "uuid"}; warningId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairSafetyView; 200: RepairSafetyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, safety_responsibility_not_current, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_state_transition, project_not_active, safety_assignment_source_conflict, safety_responsibility_source_conflict, safety_warning_clock_conflict, safety_warning_not_current.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-155`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairSafetyController.cs`.

## API-156 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/assessment`

Assess repair execution resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairMeasurementAssessmentInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairAssessmentView; 200: RepairAssessmentView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, evidence_access_forbidden, repair_assignment_not_current, repair_binding_not_current, signed_origin_admission_required, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: evidence_not_found, not_found.; 409 Error codes: concurrency_conflict, evidence_version_mismatch, first_start_source_conflict, idempotency_key_reused, invalid_state_transition, origin_content_conflict, project_not_active, repair_anchor_not_ready, repair_capture_source_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-156`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairExecutionController.cs`.

## API-157 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts`

Submit attempt repair execution resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairAttemptSubmitInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairAttemptIntakeView; 200: RepairAttemptIntakeView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, repair_assignment_not_current, repair_binding_not_current, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, execution_finish_source_conflict, execution_start_source_conflict, formal_intake_source_conflict, idempotency_key_reused, invalid_state_transition, pm_review_clock_source_conflict, project_not_active, repair_anchor_not_ready.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-157`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairExecutionController.cs`.

## API-158 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts/reviews`

Review attempt repair execution resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairAttemptReviewInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairAttemptReviewView; 200: RepairAttemptReviewView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, current_review_assignee_required, evidence_access_forbidden, repair_binding_not_current, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: evidence_not_found, not_found.; 409 Error codes: concurrency_conflict, evidence_pending, evidence_version_mismatch, formal_intake_source_conflict, idempotency_key_reused, invalid_state_transition, project_not_active, repair_anchor_not_ready, repair_review_after_evidence_required, repair_review_evidence_source_conflict, repair_review_source_insufficient, submission_lineage_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-158`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairExecutionController.cs`.

## API-159 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts/supplements`

Supplement attempt repair execution resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairAttemptSupplementInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairAttemptIntakeView; 200: RepairAttemptIntakeView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, repair_assignment_not_current, repair_binding_not_current, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, formal_intake_source_conflict, idempotency_key_reused, invalid_state_transition, physical_claim_source_conflict, project_not_active, repair_anchor_not_ready, submission_lineage_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-159`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairExecutionController.cs`.

## API-160 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/execution-finish`

Finish repair execution resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairExecutionFinishInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairExecutionFinishView; 200: RepairExecutionFinishView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, repair_assignment_not_current, repair_binding_not_current, signed_origin_admission_required, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, execution_start_required, execution_start_source_conflict, execution_time_source_conflict, idempotency_key_reused, origin_content_conflict, project_not_active, repair_anchor_not_ready, sync_clock_origin_locked.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-160`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairExecutionController.cs`.

## API-161 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/execution-start`

Start repair execution resources. Permissions: Assigned Crew.

- Classification: FE_DIRECT; functional group W07.
- When: Follow W07 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Crew.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairExecutionStartInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}; taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairExecutionStartView; 200: RepairExecutionStartView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, repair_assignment_not_current, repair_binding_not_current, signed_origin_admission_required, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: assessment_required, assessment_source_conflict, concurrency_conflict, fast_track_coverage_mapping_unknown, fast_track_prerequisites_not_met, field_first_start_required, idempotency_key_reused, invalid_state_transition, normal_plan_not_approved, origin_content_conflict, project_not_active, repair_anchor_not_ready, test_only_source_not_executable.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-161`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairExecutionController.cs`.

## API-162 — POST `/api/v1/projects/{projectId}/repair-policies/drafts`

Create repair policies resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W08.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairPolicyDefinitionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairPolicyView; 200: RepairPolicyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 409 Error codes: concurrency_conflict, defect_type_not_found, idempotency_key_reused, project_not_active.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-162`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs`.

## API-163 — GET `/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}`

Draft repair policies resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W08.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RepairPolicyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-163`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs`.

## API-164 — PUT `/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}`

Update repair policies resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W08.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairPolicyDefinitionInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairPolicyView; 200: RepairPolicyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, defect_type_not_found, idempotency_key_reused, invalid_policy_transition, project_not_active.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-164`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs`.

## API-165 — POST `/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}/publish`

Publish repair policies resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W08.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairPolicyReasonInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairPolicyView; 200: RepairPolicyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_policy_transition, project_not_active.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-165`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs`.

## API-166 — GET `/api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}`

Revision repair policies resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W08.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; revisionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RepairPolicyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-166`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs`.

## API-167 — POST `/api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}/revoke`

Revoke repair policies resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W08.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RepairPolicyReasonInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; revisionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RepairPolicyView; 200: RepairPolicyView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, stored_receipt_access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: concurrency_conflict, idempotency_key_reused, invalid_policy_transition, project_not_active.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-167`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs`.

## API-168 — GET `/api/v1/projects/{projectId}/reports/items`

Items reporting resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; metric [query] {"enum": ["reportsReceived", "casesByStatus", "defectsByStatus", "surveyTasksByStatus", "legacyUnclassified", "baselineCoverageByBand", "verifiedSourceBytes", "repairItemsByStatus", "repairAcceptanceRate", "validationMetrics", "projectDistinctDefects", "matchedDistinctDefects", "explicitSharedParts", "segmentRelatedDefects", "sourcedAllocatedQuantity", "sourcedUnallocatedQuantity"], "type": "string"}; from [query] {"type": "string", "format": "date-time"}; to [query] {"type": "string", "format": "date-time"}; routeVersionId [query] {"type": "string", "format": "uuid"}; segmentSetId [query] {"type": "string", "format": "uuid"}; segmentIds [query] {"type": "array", "items": {"type": "string", "format": "uuid"}}; cursor [query] {"type": "string"}; pageSize [query] {"maximum": 200, "minimum": 1, "type": "integer", "format": "int32", "default": 50}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ReportingItemsPageDto.
- Supported errors: 400 Error codes: cursor_invalid, validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 422 Error codes: producer_invalid, validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-168`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReportingController.cs`.

## API-169 — GET `/api/v1/projects/{projectId}/reports/summary`

Summary reporting resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; from [query] {"type": "string", "format": "date-time"}; to [query] {"type": "string", "format": "date-time"}; routeVersionId [query] {"type": "string", "format": "uuid"}; segmentSetId [query] {"type": "string", "format": "uuid"}; segmentIds [query] {"type": "array", "items": {"type": "string", "format": "uuid"}}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProjectSummaryV1.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 422 Error codes: producer_invalid, validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-169`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReportingController.cs`.

## API-170 — GET `/api/v1/projects/{projectId}/reports/timeline`

Timeline reporting resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; aggregateType [query] {"enum": ["Project", "SurveyRequest", "SurveyDataVersion", "DatasetAssessment", "BaselineSelection"], "type": "string"}; aggregateId [query] {"type": "string", "format": "uuid"}; from [query] {"type": "string", "format": "date-time"}; to [query] {"type": "string", "format": "date-time"}; cursor [query] {"type": "string"}; pageSize [query] {"maximum": 200, "minimum": 1, "type": "integer", "format": "int32", "default": 50}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ReportingTimelinePageDto.
- Supported errors: 400 Error codes: cursor_invalid, validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 422 Error codes: producer_invalid, validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-170`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReportingController.cs`.

## API-171 — POST `/api/v1/projects/{projectId}/retention/evaluations`

Evaluate retention resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateRetentionEvaluationRequest.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: RetentionEvaluationView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: idempotency_conflict.; 422 Error codes: validation_error.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-171`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RetentionController.cs`.

## API-172 — GET `/api/v1/projects/{projectId}/retention/evaluations/{id}`

Evaluation retention resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; id [path] {"type": "string", "format": "uuid"}; cursor [query] {"type": "string", "format": "uuid"}; pageSize [query] {"maximum": 200, "minimum": 1, "type": "integer", "format": "int32", "default": 50}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RetentionEvaluationView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-172`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RetentionController.cs`.

## API-173 — GET `/api/v1/projects/{projectId}/retention/files/{fileId}`

File retention resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; fileId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RetentionFileView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-173`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RetentionController.cs`.

## API-174 — PUT `/api/v1/projects/{projectId}/retention/files/{fileId}/basis`

Basis retention resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ConfirmRetentionBasisRequest.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; fileId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RetentionBasisView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: idempotency_conflict, retention_inventory_incomplete, retention_inventory_stale.; 412 Error codes: concurrency_conflict.; 422 Error codes: basis_invalid, validation_error.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-174`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RetentionController.cs`.

## API-175 — GET `/api/v1/projects/{projectId}/road-coverage`

Read road coverage resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W08.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RoadCoverageReadFact.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 400 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-175`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RoadCoverageController.cs`.

## API-176 — POST `/api/v1/projects/{projectId}/road-coverage`

Confirm road coverage resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W08.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RoadCoverageInputDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RoadCoverageReadFact; 200: RoadCoverageReadFact.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, scope_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 400 Error codes: validation_error.; 409 Error codes: scope_stale, coverage_source_stale, synthetic_source_cannot_activate, concurrency_conflict, handover_time_mismatch, warranty_scope_time_mismatch, coverage_supersession_conflict, idempotency_key_reused.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-176`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RoadCoverageController.cs`.

## API-177 — POST `/api/v1/projects/{projectId}/road-geometry-drafts`

Create draft geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryDraftInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: GeometryDraftView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_section_not_found.; 409 Error codes: duplicate_request, project_closed, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, crs_profile_scope_mismatch, crs_profile_srid_mismatch, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, unsupported_crs.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-177`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-178 — GET `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}`

Draft geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryDraftView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found, road_geometry_draft_not_found, road_section_not_found.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_scope_mismatch, crs_profile_srid_mismatch.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-178`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-179 — PUT `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}`

Edit draft geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryDraftInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryDraftView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_geometry_draft_not_found, road_section_not_found.; 409 Error codes: duplicate_request, project_closed, road_geometry_invalid_state, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, crs_profile_scope_mismatch, crs_profile_srid_mismatch, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, unsupported_crs.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-179`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-180 — POST `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/confirm`

Confirm geometry workflow resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryConfirmInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RoadGeometryVersionView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_geometry_draft_not_found, road_section_not_found.; 409 Error codes: duplicate_request, project_closed, road_geometry_invalid_state, road_section_code_conflict, road_section_concurrency_conflict, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: branch_junction_mismatch, branch_parent_or_junction_invalid, crs_profile_invalid, crs_profile_scope_mismatch, crs_profile_srid_mismatch, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, route_system_scope_mismatch, route_topology_cycle, route_topology_invalid, segment_validation_failed, unsupported_crs.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-180`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-181 — POST `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/preview`

Preview geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryPreview.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_geometry_draft_not_found, road_section_not_found.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, crs_profile_scope_mismatch, crs_profile_srid_mismatch, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, unsupported_crs.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-181`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-182 — GET `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/readiness`

Readiness geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryDraftReadiness.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found, road_geometry_draft_not_found.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_scope_mismatch, crs_profile_srid_mismatch.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-182`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-183 — POST `/api/v1/projects/{projectId}/road-sections`

Create project road sections resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateRoadSectionRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=changeReason,code,coordinates; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RoadSectionVersionResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found.; 409 Error codes: duplicate_request, project_closed, road_section_code_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-183`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectRoadSectionsController.cs`.

## API-184 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts`

Create draft geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryDraftInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: GeometryDraftView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_section_not_found.; 409 Error codes: duplicate_request, project_closed, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, crs_profile_scope_mismatch, crs_profile_srid_mismatch, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, unsupported_crs.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-184`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-185 — GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}`

Draft geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryDraftView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found, road_geometry_draft_not_found, road_section_not_found.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_scope_mismatch, crs_profile_srid_mismatch.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-185`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-186 — PUT `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}`

Edit draft geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryDraftInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryDraftView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_geometry_draft_not_found, road_section_not_found.; 409 Error codes: duplicate_request, project_closed, road_geometry_invalid_state, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, crs_profile_scope_mismatch, crs_profile_srid_mismatch, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, unsupported_crs.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-186`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-187 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}/confirm`

Confirm geometry workflow resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: GeometryConfirmInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RoadGeometryVersionView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_geometry_draft_not_found, road_section_not_found.; 409 Error codes: duplicate_request, project_closed, road_geometry_invalid_state, road_section_code_conflict, road_section_concurrency_conflict, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: branch_junction_mismatch, branch_parent_or_junction_invalid, crs_profile_invalid, crs_profile_scope_mismatch, crs_profile_srid_mismatch, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, route_system_scope_mismatch, route_topology_cycle, route_topology_invalid, segment_validation_failed, unsupported_crs.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-187`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-188 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}/preview`

Preview geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; draftId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: GeometryPreview.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_geometry_draft_not_found, road_section_not_found.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, crs_profile_scope_mismatch, crs_profile_srid_mismatch, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, unsupported_crs.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-188`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-189 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans`

Create plan survey planning resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateSurveyPlanRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=outputRequirements; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: CreateSurveyPlanResponseDto.
- Supported errors: 400 Bad Request; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found.; 409 Error codes: duplicate_request, survey_invalid_state_transition.; 422 Error codes: survey_validation_failed.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-189`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyPlanningController.cs`.

## API-190 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-requests`

Create request survey planning resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W05.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateSurveyRequestRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=outputRequirements; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: CreateSurveyRequestResponseDto.
- Supported errors: 400 Bad Request; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Not Found; 409 Error codes: duplicate_request, survey_invalid_state_transition.; 422 Error codes: survey_validation_failed.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-190`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyPlanningController.cs`.

## API-191 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions`

Create version project road sections resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateRoadSectionVersionRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=changeReason,coordinates; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RoadSectionVersionResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Not Found; 409 Error codes: duplicate_request, project_closed, road_section_concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-191`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectRoadSectionsController.cs`.

## API-192 — GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/geometry`

Geometry geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W02.
- When: Follow W02 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}; routeVersionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RoadGeometryVersionView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found, road_section_not_found, road_section_version_not_found.; 412 Error codes: concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-192`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-193 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets`

Create set geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: SegmentDefinition.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}; routeVersionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: SegmentSetView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_section_not_found, road_section_version_not_found.; 409 Error codes: duplicate_request, project_closed, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, geometry_metadata_incomplete, native_alignment_invalid, segment_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-193`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-194 — GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}`

Set geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W02.
- When: Follow W02 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}; routeVersionId [path] {"type": "string", "format": "uuid"}; setId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SegmentSetView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found, road_section_not_found, road_section_version_not_found, segment_set_not_found.; 412 Error codes: concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-194`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-195 — PUT `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}`

Edit set geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: SegmentDefinition.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}; routeVersionId [path] {"type": "string", "format": "uuid"}; setId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SegmentSetView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_section_not_found, road_section_version_not_found, segment_set_not_found.; 409 Error codes: duplicate_request, project_closed, road_geometry_invalid_state, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, geometry_metadata_incomplete, native_alignment_invalid, segment_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-195`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-196 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}/publish`

Publish geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: SegmentPublishInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}; routeVersionId [path] {"type": "string", "format": "uuid"}; setId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SegmentSetView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_section_not_found, road_section_version_not_found, segment_set_not_found.; 409 Error codes: duplicate_request, project_closed, road_geometry_invalid_state, road_section_concurrency_conflict, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: geometry_metadata_incomplete, segment_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-196`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-197 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/preview`

Preview set geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: SegmentDefinition.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}; routeVersionId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SegmentPreview.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found, road_section_not_found, road_section_version_not_found.; 412 Error codes: concurrency_conflict.; 422 Error codes: crs_profile_invalid, geometry_metadata_incomplete, native_alignment_invalid, segment_validation_failed.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-197`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-198 — GET `/api/v1/projects/{projectId}/route-systems`

Systems geometry workflow resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (conditional/optional; see contract); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: array of RouteSystemView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found.; 412 Error codes: concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-198`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-199 — POST `/api/v1/projects/{projectId}/route-systems`

Create system geometry workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: RouteSystemInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RouteSystemView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: project_not_found.; 409 Error codes: duplicate_request, project_closed, segment_publication_conflict.; 412 Error codes: concurrency_conflict.; 422 Error codes: route_system_invalid.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-199`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## API-200 — POST `/api/v1/projects/{projectId}/route-versions/{routeVersionId}/segment-sets/{segmentSetId}/pavement-plans`

Create plan pavement workflow resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: PavementPlanCreateInput.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; routeVersionId [path] {"type": "string", "format": "uuid"}; segmentSetId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: PavementLayoutView; 200: PavementLayoutView.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: unauthorized.; 403 Error codes: access_forbidden, csrf_failed. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 409 Error codes: geometry_version_mismatch, idempotency_conflict, native_route_metadata_missing, project_not_active, resource_conflict, segment_set_not_published.; 412 Error codes: content_hash_mismatch.; 422 Error codes: crs_profile_invalid, geometry_metadata_incomplete, geometry_validation_failed, native_alignment_invalid, pavement_layout_invalid, route_version_required, unsupported_crs.; 428 Error codes: idempotency_key_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-200`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs`.

## API-201 — POST `/api/v1/projects/{projectId}/survey-plans`

Create plan survey v2 resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateSurveyPlanV2RequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=scope,surveyType; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: SurveyPlanV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found.; 409 Error codes: duplicate_request.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-201`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-202 — POST `/api/v1/projects/{projectId}/survey-plans/{surveyPlanId}/postpone`

Postpone plan survey planning resources. Permissions: PM.

- Classification: FE_DIRECT; functional group STANDALONE/W05.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: PostponeSurveyPlanRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; surveyPlanId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: PostponeSurveyPlanResponseDto.
- Supported errors: 400 Bad Request; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: survey_plan_not_found.; 409 Error codes: duplicate_request, survey_invalid_state_transition.; 422 Error codes: survey_validation_failed.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-202`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyPlanningController.cs`.

## API-203 — POST `/api/v1/projects/{projectId}/survey-tasks`

Create task survey v2 resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateSurveyTaskV2RequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=scope,surveyType; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: SurveyTaskV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: identity_user_not_found, project_not_found.; 409 Error codes: duplicate_request.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-203`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-204 — POST `/api/v1/projects/{projectId}/validation-runs`

Create validation run processing v2 resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateValidationRunRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=measurementType,pairs,unit; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: ProcessingJobResponseDto.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 409 Error codes: duplicate_request, validation_error.; 422 Error codes: validation_error.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-204`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs`.

## API-205 — POST `/api/v1/projects/{projectId}/warranties`

Create project warranties resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W02.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateWarrantyRequestDto.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: CreateWarrantyResponseDto.
- Supported errors: 400 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: project_not_found, warranty_handover_document_not_found, warranty_road_section_not_found, warranty_source_document_not_found.; 409 Error codes: duplicate_request, project_closed.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-205`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectWarrantiesController.cs`.

## API-206 — GET `/api/v1/projects/{projectId}/weekly-digests/{digestId}`

Weekly digest h6 notification operations resources. Permissions: Current project/digest recipient; current duty scope.

- Classification: FE_DIRECT; functional group STANDALONE/W10.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current project/digest recipient; current duty scope.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; digestId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: WeeklyDigestView.
- Supported errors: 401 Error codes: unauthorized.; 404 Error codes: notification_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-206`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/H6NotificationOperationsController.cs`.

## API-207 — GET `/api/v1/projects/{projectId}/work-package`

Get project work packages resources. Permissions: Supervisor / current project member; WorkPackageRead policy.

- Classification: FE_DIRECT; functional group W02.
- When: Follow W02 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor / current project member; WorkPackageRead policy.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProjectWorkPackageResponseDto.
- Supported errors: 401 Request rejected.; 403 Error codes: project_access_forbidden.; 404 Error codes: project_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-207`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProjectWorkPackagesController.cs`.

## API-208 — GET `/api/v1/reporter-evidence/files/{fileId}`

Get file metadata reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: fileId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: FileMetadataResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 404 Error codes: file_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-208`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs`.

## API-209 — GET `/api/v1/reporter-evidence/files/{fileId}/content`

Download reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: fileId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: string.
- Supported errors: 401 Error codes: auth_unauthorized.; 404 Error codes: file_not_found.; 409 Error codes: source_not_ready.; 503 Error codes: upload_storage_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-209`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs`.

## API-210 — GET `/api/v1/reporter-evidence/publications/{publicationId}/reports/{reportId}/evidence/{evidenceId}/content`

Download publication reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: publicationId [path] {"type": "string", "format": "uuid"}; reportId [path] {"type": "string", "format": "uuid"}; evidenceId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: string.
- Supported errors: 401 Error codes: auth_unauthorized.; 404 Error codes: file_not_found.; 409 Error codes: source_not_ready.; 503 Error codes: upload_storage_unavailable.; 403 Error codes: access_forbidden.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-210`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs`.

## API-211 — POST `/api/v1/reporter-evidence/uploads`

Create reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ReporterEvidenceCreateRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: UploadSessionResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: upload_session_not_found.; 409 Error codes: duplicate_request.; 422 Error codes: upload_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-211`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs`.

## API-212 — GET `/api/v1/reporter-evidence/uploads/{uploadId}`

Get session reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: uploadId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: UploadSessionResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 404 Error codes: upload_session_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-212`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs`.

## API-213 — POST `/api/v1/reporter-evidence/uploads/{uploadId}/complete`

Complete reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: UploadCompleteRequestDto.
- Parameters: uploadId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=checksumSha256,parts; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: UploadSessionResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 404 Error codes: upload_session_not_found.; 409 Error codes: duplicate_request.; 412 Error codes: auth_concurrency_conflict.; 422 Error codes: upload_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.; 403 Authorization policy or current authority denies this request.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-213`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs`.

## API-214 — POST `/api/v1/reporter-evidence/uploads/{uploadId}/part-urls`

Get part urls reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: UploadPartUrlsRequestDto.
- Parameters: uploadId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=partNumbers; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: UploadPartUrlsResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 404 Error codes: upload_session_not_found.; 409 Error codes: duplicate_request.; 422 Error codes: upload_validation_failed.; 428 Error codes: validation_error.; 503 Error codes: upload_storage_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.; 403 Authorization policy or current authority denies this request.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-214`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs`.

## API-215 — GET `/api/v1/reports`

List reports resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: pageSize [query] {"type": "integer", "format": "int32", "default": 20}; cursor [query] {"type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OwnReportPageDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 503 Error codes: dependency_unavailable.; 400 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-215`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReportsController.cs`.

## API-216 — POST `/api/v1/reports`

Create reports resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateReporterReportRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: ReporterReportResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 503 Error codes: dependency_unavailable.; 400 Error codes: validation_error.; 409 Error codes: source_not_ready, idempotency_key_reused.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-216`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReportsController.cs`.

## API-217 — GET `/api/v1/reports/{reportId}`

Read reports resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: reportId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OwnReportDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 503 Error codes: dependency_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-217`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReportsController.cs`.

## API-218 — GET `/api/v1/reports/{reportId}/evidence/{evidenceId}/download`

Download reports resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: reportId [path] {"type": "string", "format": "uuid"}; evidenceId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: string.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 503 Error codes: dependency_unavailable.; 409 Error codes: source_not_ready.; 412 Error codes: concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-218`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReportsController.cs`.

## API-219 — POST `/api/v1/reports/{reportId}/supplements`

Supplement reports resources. Permissions: Reporter; own record / named publication recipient.

- Classification: FE_DIRECT; functional group W03.
- When: Follow W03 only when its entry prerequisites and current authority hold.
- Actor/scope: Reporter; own record / named publication recipient.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateReporterReportRequestDto.
- Parameters: reportId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OwnReportDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden. Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.; 404 Error codes: not_found.; 503 Error codes: dependency_unavailable.; 400 Error codes: validation_error.; 409 Error codes: source_not_ready, idempotency_key_reused.; 412 Error codes: concurrency_conflict.; 428 Error codes: precondition_required.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-219`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ReportsController.cs`.

## API-220 — POST `/api/v1/retention/holds`

Create hold retention resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: CreateRetentionHoldRequest.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: RetentionHoldView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: idempotency_conflict.; 422 Error codes: validation_error.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-220`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RetentionController.cs`.

## API-221 — GET `/api/v1/retention/holds/{id}`

Hold retention resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: id [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RetentionHoldView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-221`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RetentionController.cs`.

## API-222 — POST `/api/v1/retention/holds/{id}/release`

Release retention resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group W10.
- When: Follow W10 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ReleaseRetentionHoldRequest.
- Parameters: id [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RetentionHoldView.
- Supported errors: 401 Error codes: unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 409 Error codes: idempotency_conflict, invalid_transition.; 412 Error codes: concurrency_conflict.; 422 Error codes: validation_error.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-222`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/RetentionController.cs`.

## API-223 — GET `/api/v1/survey-plans/{id}`

Read plan survey v2 resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: id [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyPlanV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-223`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-224 — POST `/api/v1/survey-plans/{planId}/postpone`

Postpone survey v2 resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: PostponeSurveyPlanV2RequestDto.
- Parameters: planId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyPlanV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: survey_plan_not_found.; 409 Error codes: duplicate_request, survey_scope_incompatible.; 412 Error codes: auth_concurrency_conflict, concurrency_conflict.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-224`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-225 — GET `/api/v1/survey-tasks/{id}/work-package`

Read work package survey v2 resources. Permissions: PM / Supervisor / assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: id [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyTaskWorkPackage.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-225`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-226 — GET `/api/v1/survey-tasks/{taskId}`

Get task survey v2 resources. Permissions: PM / Supervisor / assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor / assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyTaskV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: survey_request_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-226`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-227 — POST `/api/v1/survey-tasks/{taskId}/accept`

Accept task survey v2 resources. Permissions: Assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyTaskV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: survey_request_not_found.; 409 Error codes: duplicate_request, survey_invalid_state_transition, survey_scope_incompatible.; 412 Error codes: auth_concurrency_conflict, concurrency_conflict.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-227`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-228 — POST `/api/v1/survey-tasks/{taskId}/cancel`

Cancel task survey v2 resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: SurveyTaskReasonV2RequestDto.
- Parameters: taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyTaskV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: survey_request_not_found.; 409 Error codes: duplicate_request, survey_invalid_state_transition, survey_scope_incompatible.; 412 Error codes: auth_concurrency_conflict, concurrency_conflict.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-228`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-229 — POST `/api/v1/survey-tasks/{taskId}/datasets`

Submit dataset survey v2 resources. Permissions: Assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: SubmitDatasetRequestDto.
- Parameters: taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=deviceId,recordedAt,scope,telemetryFileIds,videoFileIds; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: DatasetResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: survey_request_not_found.; 409 Error codes: duplicate_request, survey_invalid_state_transition, survey_scope_incompatible.; 412 Error codes: auth_concurrency_conflict, concurrency_conflict.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-229`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-230 — POST `/api/v1/survey-tasks/{taskId}/decline`

Decline task survey v2 resources. Permissions: Assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: SurveyTaskReasonV2RequestDto.
- Parameters: taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyTaskV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: survey_request_not_found.; 409 Error codes: duplicate_request, survey_invalid_state_transition, survey_scope_incompatible.; 412 Error codes: auth_concurrency_conflict, concurrency_conflict.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-230`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-231 — POST `/api/v1/survey-tasks/{taskId}/reassign`

Reassign task survey v2 resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: ReassignSurveyTaskV2RequestDto.
- Parameters: taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyTaskV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: identity_user_not_found, survey_request_not_found.; 409 Error codes: duplicate_request, survey_invalid_state_transition, survey_scope_incompatible.; 412 Error codes: auth_concurrency_conflict, concurrency_conflict.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-231`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-232 — POST `/api/v1/survey-tasks/{taskId}/supplements`

Request supplement survey v2 resources. Permissions: PM.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: SupplementSurveyTaskV2RequestDto.
- Parameters: taskId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason,scope; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: SurveyTaskV2ResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized, unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: identity_user_not_found, survey_request_not_found.; 409 Error codes: duplicate_request, survey_invalid_state_transition, survey_scope_incompatible.; 412 Error codes: auth_concurrency_conflict, concurrency_conflict.; 422 Error codes: survey_validation_failed.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-232`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.

## API-233 — POST `/api/v1/uploads`

Create uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: UploadCreateRequestDto.
- Parameters: none declared.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=checksumSha256,fileName,mediaType,purpose; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: UploadSessionResponseDto.
- Supported errors: 428 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 409 Error codes: duplicate_request.; 422 Error codes: upload_validation_failed.; 404 Error codes: upload_session_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-233`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UploadsController.cs`.

## API-234 — GET `/api/v1/uploads/{uploadId}`

Get session uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: uploadId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: UploadSessionResponseDto.
- Supported errors: 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: upload_session_not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-234`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UploadsController.cs`.

## API-235 — POST `/api/v1/uploads/{uploadId}/complete`

Complete uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: UploadCompleteRequestDto.
- Parameters: uploadId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=checksumSha256,parts; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 202: UploadSessionResponseDto.
- Supported errors: 428 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: upload_session_not_found.; 409 Error codes: duplicate_request.; 422 Error codes: upload_validation_failed.; 412 Error codes: auth_concurrency_conflict.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-235`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UploadsController.cs`.

## API-236 — POST `/api/v1/uploads/{uploadId}/part-urls`

Get part urls uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: UploadPartUrlsRequestDto.
- Parameters: uploadId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=partNumbers; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: UploadPartUrlsResponseDto.
- Supported errors: 428 Error codes: validation_error.; 401 Error codes: auth_unauthorized.; 403 Error codes: access_forbidden.; 404 Error codes: upload_session_not_found.; 409 Error codes: duplicate_request.; 422 Error codes: upload_validation_failed.; 503 Error codes: upload_storage_unavailable.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 400 Malformed or DTO-invalid JSON: validation_error.; 415 Unsupported request media type: unsupported_media_type.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-236`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UploadsController.cs`.

## API-237 — GET `/api/v1/users`

Search the staff directory by supported role/status filters and opaque cursor

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Current global Supervisor only; StaffReadService.UsersAsync and current repository authority Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: search [query] {"type": "string"}; role [query] {"type": "string"}; status [query] {"type": "string"}; limit [query] {"type": "integer", "format": "int32", "default": 20}; cursor [query] {"type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: StaffUserResponseDtoStaffPageResponseDto.
- Supported errors: 
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-237`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/StaffDirectoryController.cs`.

## API-238 — GET `/api/v1/users/{userId}`

Get users resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group W01.
- When: Follow W01 only when its entry prerequisites and current authority hold.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: userId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ActorResponseDto.
- Supported errors: 400 Error codes: validation_error.; 403 Error codes: access_forbidden.; 404 Error codes: identity_user_not_found.; 409 Error codes: auth_concurrency_conflict, duplicate_request.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 401 Authentication challenge: missing, expired or invalid credential/session.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-238`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UsersController.cs`.

## API-239 — PATCH `/api/v1/users/{userId}`

Update users resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: AccountUpdateRequestDto.
- Parameters: userId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason,role,status; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ActorResponseDto.
- Supported errors: 400 Error codes: validation_error.; 403 Error codes: access_forbidden.; 404 Error codes: identity_user_not_found.; 409 Error codes: auth_concurrency_conflict, duplicate_request.; 412 Error codes: auth_concurrency_conflict.; 428 Error codes: validation_error.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.; 401 Authentication challenge: missing, expired or invalid credential/session.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-239`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UsersController.cs`.

## API-240 — POST `/api/v1/users/{userId}/password-reset`

Reset password users resources. Permissions: Supervisor.

- Classification: FE_DIRECT; functional group STANDALONE/W01.
- When: Standalone Backend capability: invoke only for the documented function with its actual resource and current authority; no mandatory ordered workflow inferred.
- Actor/scope: Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: AdminResetPasswordV2RequestDto.
- Parameters: userId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=reason,temporaryPassword; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 204: no response body.
- Supported errors: 400 Error codes: validation_error.; 403 Error codes: access_forbidden.; 404 Error codes: identity_user_not_found.; 409 Error codes: auth_concurrency_conflict, duplicate_request.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 415 Unsupported request media type: unsupported_media_type.; 401 Authentication challenge: missing, expired or invalid credential/session.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-240`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/UsersController.cs`.

## API-241 — GET `/api/v1/validation-runs/{runId}`

Get validation result processing v2 resources. Permissions: PM / Supervisor.

- Classification: FE_DIRECT; functional group W05.
- When: Follow W05 only when its entry prerequisites and current authority hold.
- Actor/scope: PM / Supervisor.; current resource authority applies Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: runId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ValidationResultDto.
- Supported errors: 401 Error codes: unauthorized.; 404 Error codes: not_found.; 500 Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.; 403 Authorization policy or current authority denies this request.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#API-241`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs`.

## ADD-01 — GET `/api/v1/projects/{projectId}/ai-mock-runs`

List Development AI mock runs

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor and Development mock/detail guard Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: MockRunSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-01`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-02 — GET `/api/v1/projects/{projectId}/repair-packages`

List readable repair packages

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor and current obligation responsibility Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RepairPackageSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-02`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-03 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items`

List readable Normal/FastTrack items

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: Current repair ReadItem and readable parent obligation Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}; mode [query] {"enum": ["NORMAL", "FAST_TRACK"], "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RepairItemSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-03`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-04 — GET `/api/v1/projects/{projectId}/exports`

List export history

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: Creator only and current export/snapshot source visibility Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ExportSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-04`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-05 — GET `/api/v1/projects/{projectId}/survey-plans`

List survey plans

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor and plan detail guard Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyPlanSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-05`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-06 — GET `/api/v1/projects/{projectId}/survey-tasks`

List survey tasks

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor/Operator and task detail guard Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SurveyTaskSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-06`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-07 — GET `/api/v1/projects/{projectId}/processing-jobs`

List processing jobs

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor/Operator and job detail guard Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: ProcessingJobSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-07`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-08 — GET `/api/v1/projects/{projectId}/offline/devices`

List offline devices

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor/Crew and device-get guard Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OfflineDeviceSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-08`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-09 — GET `/api/v1/projects/{projectId}/offline/packages`

List offline packages

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor/Crew and original actor/recipient grant/Supervisor guard Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OfflinePackageSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-09`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-10 — GET `/api/v1/projects/{projectId}/offline/grants`

List offline grants

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor/Crew and recipient-role/device/Supervisor guard Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: OfflineGrantSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-10`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-11 — GET `/api/v1/projects/{projectId}/retention/evaluations`

List retention evaluations

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: PM/Supervisor and retention guard Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; limit [query] {"maximum": 100, "minimum": 1, "type": "integer", "default": 25}; cursor [query] {"maxLength": 4096, "minLength": 1, "type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: RetentionEvaluationSummaryCollectionPage.
- Supported errors: 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-11`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs`.

## ADD-12 — GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets`

Discover explicit SegmentSets for a road route version

- Classification: FE_DIRECT; functional group CROSS_WORKFLOW.
- When: Follow CROSS_WORKFLOW only when its entry prerequisites and current authority hold.
- Actor/scope: Current PM/Supervisor and exact project/road/version authority Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; roadSectionId [path] {"type": "string", "format": "uuid"}; routeVersionId [path] {"type": "string", "format": "uuid"}; pageSize [query] {"type": "integer", "format": "int32", "default": 25}; cursor [query] {"type": "string", "format": "uuid"}; status [query] {"type": "string"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: SegmentSetPageDto.
- Supported errors: 
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-12`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs`.

## ADD-13 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`

Read notification-only item Supervisor assignment

- Classification: FE_DIRECT; functional group W08.
- When: Follow W08 only when its entry prerequisites and current authority hold.
- Actor/scope: Current project PM; recipient Supervisor global without ProjectMember Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: NotificationSupervisorDto.
- Supported errors: 401 Existing invalid or revoked authentication/session behavior.; 403 access_forbidden or existing csrf_failed for cookie writes.; 404 not_found: concealed missing/mismatched resource, absent head or invalid recipient.
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-13`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/NotificationSupervisorController.cs`.

## ADD-14 — PUT `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`

Create or replace notification-only item Supervisor assignment

- Classification: FE_DIRECT; functional group W08.
- When: Follow W08 only when its entry prerequisites and current authority hold.
- Actor/scope: Current project PM; recipient Supervisor global without ProjectMember Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: object.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (conditional/optional; see contract); If-None-Match (conditional/optional; see contract). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: required=supervisorId; additionalProperties=False. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: NotificationSupervisorDto; 200: NotificationSupervisorDto.
- Supported errors: 400 validation_error: malformed/duplicate/unknown body fields, invalid key/ETag or both conditionals.; 401 Existing invalid or revoked authentication/session behavior.; 403 access_forbidden or existing csrf_failed for cookie writes.; 404 not_found: concealed missing/mismatched resource, absent head or invalid recipient.; 409 invalid_state_transition, notification_assignment_revoked, or idempotency_key_reused.; 412 precondition_failed: head version mismatch or initial create against existing head.; 428 precondition_required: missing conditional header.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-14`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/NotificationSupervisorController.cs`.

## ADD-15 — DELETE `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`

Revoke notification-only item Supervisor assignment

- Classification: FE_DIRECT; functional group W08.
- When: Follow W08 only when its entry prerequisites and current authority hold.
- Actor/scope: Current project PM; recipient-specific revocation Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; packageId [path] {"type": "string", "format": "uuid"}; itemId [path] {"type": "string", "format": "uuid"}.
- Request headers: Idempotency-Key (required); If-Match (required). Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 204: no response body.
- Supported errors: 400 validation_error: malformed/duplicate/unknown body fields, invalid key/ETag or both conditionals.; 401 Existing invalid or revoked authentication/session behavior.; 403 access_forbidden or existing csrf_failed for cookie writes.; 404 not_found: concealed missing/mismatched resource, absent head or invalid recipient.; 409 invalid_state_transition, notification_assignment_revoked, or idempotency_key_reused.; 412 precondition_failed: head version mismatch or initial create against existing head.; 428 precondition_required: missing conditional header.
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-15`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/NotificationSupervisorController.cs`.

## ADD-16 — POST `/api/v1/projects/{projectId}/defects/{defectId}/publications`

Publish immutable sanitized eligible Defect snapshot

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: Current project PM and strict C1 Defect/Case/evidence policy Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; defectId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 201: PublicPublicationDto.
- Supported errors: 
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-16`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PublicPublicationsController.cs`.

## ADD-17 — POST `/api/v1/projects/{projectId}/publications/{publicId}/withdraw`

Withdraw public representation; retain private originals

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: Current project PM and publication project/source authority Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: projectId [path] {"type": "string", "format": "uuid"}; publicId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: PublicPublicationDto.
- Supported errors: 
- State and related APIs: apply only the specific authorized command; refresh its detail/receipt before a dependent command. Exact durable effects/lifecycle are established by the referenced source and attributed assertions, never by this HTTP verb alone.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-17`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PublicPublicationsController.cs`.

## ADD-18 — GET `/api/v1/public/publications/{publicId}`

Read four sanitized public fields; missing/withdrawn404

- Classification: FE_DIRECT; functional group W04.
- When: Follow W04 only when its entry prerequisites and current authority hold.
- Actor/scope: Anonymous; PUBLISHED snapshot only Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: publicId [path] {"type": "string", "format": "uuid"}.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: PublicPublicationDto.
- Supported errors: 
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-18`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: `RoadGuardSystem.API/Controllers/PublicPublicationsController.cs`.

## ADD-19 — GET `/ready`

Check SQL, private storage and mail readiness

- Classification: OPERATIONAL; functional group RUNTIME.
- When: Check owned environment readiness; no business screen or state transition.
- Actor/scope: Anonymous operational probe Verify current role, resource ownership/project/assignment and revocation; the catalog role label is a locator, not executed proof.
- Request DTO: No JSON body declared.
- Parameters: none declared.
- Request headers: no operation-specific header parameter declared. Bearer/cookie eligibility and CSRF follow the exact auth guide; do not assume every action supports cookies.
- Validation: declared path/query constraints above; no JSON body. Nested properties, enum/range/pattern/nullability and business errors remain exactly as [catalog](FE-API-CATALOG.json)/[schema](openapi.json).
- Success DTO/status: 200: no response body.
- Supported errors: 
- State and related APIs: read current authorized representation; feed returned opaque IDs/versions into the applicable business-flow step.
- Retry:401 restore the correct session;403 stop and resolve current authority;409/412 refresh and inspect the exact problem code. Retain original idempotency identity for an intended replay where supported; never change the key blindly after a timeout.
- Workflow/example and proof: [business sequence](FE-BUSINESS-FLOW-GUIDE.md), [candidate examples](FE-API-TEST-GUIDE.md), `review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json#ADD-19`. A missing example remains NOT_VERIFIED, not an executed success.
- Source boundary: source pins in catalog.
