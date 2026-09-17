# ADR 002: Authentication Architecture, Session Management, and Token Lifecycle

## Status

Accepted (2026-09-17)

- **Date:** 2026-09-17
- **Author / Owner:** Person 1 (Antigravity)
- **Reviewer:** Person 2
- **Approved by:** Product Owner (Decision P1-02)
- **Trace:** Architecture / Task P1-02 (Foundation post P1-00 and P1-01)
- **Downstream Context:** Architectural foundation for use cases CN01 (Login / Logout), CN02 (Profile), CN03 (Assigned Project / Work Scope), CN10 (Password Reset), QT01 (Account Suspension), QT02 (Role and Project Access Management), and User Stories US-01/US-17. (Note: P1-02 establishes architectural policy and contracts only; production implementation is assigned to P1-10, P2-10, P1-11, P1-12, P2-11, and P1-64).

---

## Context

The RoadGuard System requires a robust, secure, and auditable authentication and session management mechanism. Users access the platform via diverse client form factors:
1. Field personnel (`Drone Operator`, `Repair Crew`) using the Android Mobile Application (`RoadGuard Mobile`), frequently operating in variable network conditions.
2. Project Managers (`PM`) and System Supervisors (`Supervisor`) using the Web Dashboard (`RoadGuard Dashboard`).

Product specifications in `RoadGuard_Data_Dictionary_v1.md` (Section 3.1) and `RoadGuard_Domain_Model_v1.md` specify database entities for `Session`, `RefreshToken`, `PasswordResetLog`, and `AccountStatusChangeLog`. Furthermore, safety-critical road inspection workflows demand strict multi-tenant project isolation and instantaneous session revocation upon account suspension (QT01), password reset (CN10), logout (CN01), or security compromise.

---

## Decision

### 1. Core Authentication Architecture

1. **ASP.NET Core Identity & Standard Password Hashing:**
   - User account lifecycle and credential verification will be built on ASP.NET Core Identity abstractions.
   - Passwords must be hashed using the Microsoft standard `PasswordHasher<TUser>` (implementing PBKDF2 with HMAC-SHA256/SHA512 and adaptive iteration counts matching framework defaults).
   - Custom, proprietary, or home-grown cryptographic algorithms are strictly prohibited.

2. **JWT Bearer + Rotating Opaque Refresh-Token Model:**
   - The API uses a token-based authentication model:
     - **Access Token:** Short-lived JSON Web Token (JWT) transmitted via the `Authorization: Bearer <token>` HTTP header.
     - **Refresh Token:** High-entropy opaque credential presented exclusively to the token refresh endpoint to obtain a new token pair.
   - *(Note: This architecture implements standard token authentication, not a full OAuth 2.0 authorization server protocol).*

---

### 2. Token Lifecycle and Session Management

1. **Configurable Short-Lived Access Token:**
   - Access tokens must be short-lived.
   - The token lifetime must be configurable via the ASP.NET Core Options pattern (e.g., `JwtOptions:AccessTokenLifetimeMinutes`) and must **never** be hard-coded in domain entities, services, or documentation.
   - The access token payload contains standard RFC 7519 claims:
     - `sub` (Subject): Primary User ID (`Guid`).
     - `jti` (JWT ID): Unique token instance UUID.
     - `exp`, `iat`, `nbf`: Standard temporal claims.
     - **`sid` (Session ID):** Standard session identifier matching the database `Session.id`. Every authenticated access token must carry this claim.
     - **`role`:** Machine-readable role claim using Data Dictionary codes: `SUPERVISOR`, `PM`, `DRONE_OPERATOR`, `REPAIR_CREW`. Display labels may remain "Supervisor", "PM", "Drone Operator", and "Repair Crew" in user interfaces, but serialized role claims must strictly use these four stable uppercase codes. The claim is a snapshot, never the authorization source of truth; current server-side `User.role_code` is authoritative. A strongly typed `UserRole` enum or role lookup is future work owned by task **P2-10** (not currently defined in `BusinessObjects`).

2. **Session Persistence and Logical State:**
   - Each successful login creates a `Session` record linked to the user account, capturing issuance time (`issued_at`), optional versioned device metadata (`device_metadata_json`), expiration time (`expires_at`), and optional revocation time (`revoked_at`).
   - `device_metadata_json` is nullable SQL Server `nvarchar(max)` with `CHECK (device_metadata_json IS NULL OR ISJSON(device_metadata_json) = 1)` and mandatory application-level schema validation. Schema version 1 is a JSON object containing required `schema_version = 1` plus optional string fields `device_id`, `platform`, and `app_version`; unknown properties and non-object JSON are rejected.
   - Device metadata is untrusted, write-once session context. It must not contain passwords, access tokens, refresh tokens, or other secrets; must not be used as an authorization signal; must not be returned by business/public APIs; and must not be written to application logs.
   - **Schema Note:** The Product Owner approved the nullable `device_metadata_json` extension on 2026-09-17, and the Data Dictionary, ERD, Domain Model, and delivery plans were updated together. No separate `status` column is approved for `Session`; its state (`ACTIVE`, `REVOKED`, `EXPIRED`) remains logically computed/derived:
     - `ACTIVE`: `revoked_at IS NULL AND expires_at > UtcNow`
     - `REVOKED`: `revoked_at IS NOT NULL`
     - `EXPIRED`: `revoked_at IS NULL AND expires_at <= UtcNow`
   - Any future database schema additions or column adjustments are strictly reserved for Person 2 under task **P2-10**.

3. **Per-Request Authoritative Session and Account Validation (Instant Revocation):**
   - Every incoming authenticated API request must be intercepted by authentication/authorization middleware to verify that:
     1. The session identified by the token's `sid` claim is currently active and not revoked (`revoked_at IS NULL AND expires_at > UtcNow`).
     2. The associated user account is active and has not been suspended (`status != SUSPENDED`).
     3. The token role claim exactly matches the current authoritative `User.role_code` loaded from the server-side store.
   - A role claim mismatch means the credential is stale. The backend must fail closed, revoke the affected session/token family, and terminate with HTTP 401 Unauthorized using `auth_session_revoked`; it must never continue by trusting the stale claim.
   - If session or account validation fails, the request must immediately terminate with HTTP 401 Unauthorized and standard error code `auth_session_revoked` or `auth_unauthorized`.

4. **Instant Revocation Triggers:**
   - The following operations must immediately block subsequent API requests:
     - **User Logout (CN01):** Sets `revoked_at = UtcNow` for the current session.
     - **Password Reset / Change (CN10):** Sets `revoked_at = UtcNow` across **all active sessions** belonging to the user and records an append-only entry in `PasswordResetLog`.
     - **Account Suspension (QT01):** Sets `revoked_at = UtcNow` across all active sessions and refresh tokens for the user and records an append-only entry in `AccountStatusChangeLog`.
     - **Global Role Change (QT02):** Emits `UserRoleChanged`, records an append-only audit event, and atomically revokes all active sessions and refresh tokens for the user so the new role is obtained only through a fresh login.
     - **Replay Attack Detection:** Immediately revokes the entire session and token family (detailed below).

5. **Strict Caching and Authoritative Verification Policy:**
   - A positive (ACTIVE) session cache may **only** be introduced if backed by a security-stamp/session-version mechanism or an invalidation protocol provably guaranteed never to return a stale `ACTIVE` state after revocation.
   - **Baseline Mandate:** Until such a mechanism is formally approved, implemented, and verified with integration tests, Task **P1-10** must query the authoritative Session and User persistence store on every authenticated request.
   - Under any cache communication failure, timeout, or ambiguity, the system must **fail closed** (deny the request and return HTTP 401).

---

### 3. Refresh Token Rotation and Replay Detection

1. **Entropy and Cryptographic Hash Persistence:**
   - Refresh tokens must be generated with high cryptographic entropy (e.g., 256 bits of cryptographically secure random bytes generated via `RandomNumberGenerator`, Base64URL-encoded).
   - In accordance with `RoadGuard_Data_Dictionary_v1.md` (Section 3.1), the database **only stores a cryptographic hash** (`token_hash`) of the refresh token. (Storing the cryptographic hash is an explicit ADR architectural decision fulfilling the data specification).
   - Plaintext refresh tokens are **never** stored in the database, logged, or serialized outside the initial issuance response.

2. **Strict Refresh Token Rotation:**
   - Every invocation of the token refresh endpoint rotates the credential pair.
   - Upon successful verification, the presenting refresh token is invalidated (`revoked_at = UtcNow`), and an entirely new Refresh Token (with a newly computed hash) is issued alongside a new short-lived Access Token.

3. **Replay Detection (Token Family Defense):**
   - Each `Session` aggregate serves as the boundary for a token family.
   - If an incoming request presents a refresh token whose hash matches a record that has **already been revoked or consumed**, the authentication engine flags a **Replay Attack**.
   - Upon replay detection, the system must:
     1. Immediately revoke the entire parent `Session` (`revoked_at = UtcNow`).
     2. Invalidate all active refresh tokens associated with that session.
     3. Emit a high-priority security audit log event (`auth_token_replay_detected`).
     4. Plaintext tokens must **never** be written to the security log.

4. **Transaction and Concurrency Safety:**
   - The token refresh operation must be executed within an isolated database transaction using optimistic or pessimistic concurrency controls.
   - Two concurrent refresh requests presenting the same refresh token must never both succeed; one must succeed and the other must fail as a concurrency conflict or replay attempt.

---

### 4. Server-Side Project Membership Authorization

1. **Multi-Tenancy and Civil Infrastructure Scope:**
   - RoadGuard aggregates (Surveys, Road Sections, Defects, Repair Items, Warranties) are strictly bound to specific civil infrastructure `Project` entities (governed by use case **CN03**).
   - Authorization cannot be determined solely by static role claims (e.g., `role: DRONE_OPERATOR` or `role: PM`).

2. **Authoritative Server-Side Membership Guard:**
   - The global Supervisor exemption applies only after the current server-side `User.role_code` has been verified as `SUPERVISOR`; a JWT claim alone never grants the exemption.
   - For all other actors, every query and command targeting project-scoped resources must derive the owning project from the server-side resource and verify an active, currently effective `ProjectMember` record for that user and project before loading or mutating protected data.
   - `ProjectMember.role_code` is authoritative inside the project. For the MVP it must equal the current `User.role_code` and satisfy the endpoint's required role; mismatched role records are invalid and must not grant access.
   - Client-provided claims, request headers, or token claims specifying project affiliation are untrusted hints; the backend must execute an authoritative check against the project membership store.
   - Ending, expiring, or changing a membership has immediate effect because membership is checked on every request; project identifiers and membership roles must not be cached in JWTs as authorization authority.
   - If membership is missing, ended, expired, or belongs to another project, the API returns HTTP 403 Forbidden with `project_access_denied`. If membership exists but its role is not allowed for the operation, the API returns HTTP 403 with `access_forbidden`.

---

### 5. Layer Placement of Responsibilities

To maintain Clean Architecture boundaries and avoid conflating concerns:

| Layer | Project | Responsibilities | Assigned Task |
|---|---|---|---|
| **Domain** | `BusinessObjects` | Domain user and role foundation (`ApplicationUser`, `ApplicationRole`). Future role/status enums and domain types are owned by P2-10. | **P1-00** (foundation) / **P2-10** (future enums) |
| **Persistence** | `Repositories` | EF Core Identity persistence (`IdentityDbContext`, `UserStore`, `RoleStore`), table mappings (`sessions`, `refresh_tokens`, `password_reset_logs`, `account_status_change_logs`), `device_metadata_json` schema/`ISJSON` constraint, entities, and database migrations/seeding (P2-10); authoritative active/effective project-membership read model (P2-11). | **P2-10**, **P2-11** |
| **Application** | `Services` | Authentication orchestration, credential verification, token generation, refresh rotation, replay detection, session invalidation (P1-10); profile updates, Admin password-reset and forced session-revocation flows (P1-11); server-side project membership authorization services and policies (P1-12); Admin global-role changes, `UserRoleChanged` audit, and atomic credential revocation (P1-64). | **P1-10**, **P1-11**, **P1-12**, **P1-64** |
| **Presentation** | `API` | JWT Bearer authentication handler configuration, token extraction, correlation middleware, and authentication controllers (`AuthController`, login/logout/refresh endpoints). | **P1-10** |

---

### 6. Security, Secret Handling, and Logging Policy

1. **Transport Security:**
   - HTTPS / TLS 1.2+ is mandatory across all environments. Unencrypted HTTP requests must be rejected.

2. **JWT Signing Credentials:**
   - JWT signing credentials, database credentials, and external service keys must be supplied through ASP.NET Core configuration, environment variables, or secure secret vaults.
   - Hard-coding secrets in source code, configuration defaults, or git repositories is strictly forbidden.

3. **Log Sanitization and Sensitive Data Suppression:**
   - Application and security audit logs are strictly append-only.
   - The following sensitive data elements must **NEVER** be recorded in application logs, audit logs, or error responses:
     - Plaintext passwords or password reset tokens.
     - Plaintext refresh tokens or JWT access tokens.
     - HTTP `Authorization` header values.
     - JWT signing credentials or database connection strings.

---

### 7. Deferred and Out-of-Scope Capabilities

1. **Google OAuth / External SSO:**
   - Google Authentication (OAuth 2.0 / OpenID Connect) is **Out of Scope and Deferred** for Phase 1.
   - System specifications (`Dac_ta_UseCase_v2.md` CN01–CN03, CN10 and `User_Stories_Acceptance_Criteria_v2.md` US-01) specify internally provisioned credentials (username + password; email is currently a PROP field in the Data Dictionary and is not yet an approved login identifier).
   - The existing package references in the solution (`Google.Apis.Auth` in `RoadGuardSystem.Services` and `Microsoft.AspNetCore.Authentication.Google` in `RoadGuardSystem.API`) represent technical debt and are not activated. They are scheduled for formal audit and removal during Task **P1-10** or a designated dependency cleanup task. The separate P1-00 correction removes only the unused legacy `Microsoft.AspNetCore.Identity` 2.3.1 package that introduced a High advisory; it does not activate or remove Google authentication.

---

## Rejected Alternatives

1. **Pure Stateless JWT without Session Verification:**
   - *Rejected:* Without server-side session checks, access tokens remain valid until expiration even after user logout (CN01), account suspension (QT01), or password reset (CN10). This security vulnerability violates core domain requirements.

2. **Cookie-Based Authentication for API Endpoints:**
   - *Rejected:* Native mobile applications (Android) and cross-domain dashboard clients require standardized Bearer token authentication via HTTP headers. Cookie-based authentication introduces complex CSRF and cross-origin challenges without operational benefit for API services.

3. **Opaque Reference Tokens (Server-Side State Only):**
   - *Rejected:* Storing all token payload state in server memory/database requires heavy database lookups for standard claims on every internal service call. Short-lived JWTs combined with lightweight session validation balance claim portability with instantaneous revocation.

4. **Custom Cryptographic Password Hashing:**
   - *Rejected:* Building proprietary hashing or token generation algorithms introduces critical security risks. The system relies exclusively on battle-tested standard ASP.NET Core Identity implementations.

5. **Client-Claim-Based Project Scope Trust:**
   - *Rejected:* Trusting client-supplied project IDs or claims creates severe privilege escalation and multi-tenant data leakage risks. Server-side membership verification is mandatory.

---

## Consequences and Follow-ups

### Positive
- **Instantaneous Revocation:** Suspended accounts (QT01), global role changes (QT02), and compromised tokens are terminated immediately across all clients.
- **Defense-in-Depth:** Refresh token rotation combined with token-family replay detection protects mobile users against credential theft.
- **Auditability:** Complete, append-only history of password changes (CN10) and account suspensions (QT01).

### Follow-up Task Allocations
- **P1-00 (Person 1):** Foundation domain `ApplicationUser` and `ApplicationRole` (already established).
- **P2-10 (Person 2):** Implement EF Core Identity persistence, `IdentityDbContext`, mapping configurations for `sessions` (including nullable `device_metadata_json`, SQL Server `ISJSON` constraint, write-once behavior, and schema validation), `refresh_tokens`, `password_reset_logs`, and `account_status_change_logs`; add the migration with downgrade/recovery notes and SQL Server integration tests; seed roles (`SUPERVISOR`, `PM`, `DRONE_OPERATOR`, `REPAIR_CREW`); and evaluate future domain enums.
- **P1-10 (Person 1):** Implement application authentication service (`IAuthService`), login/logout/refresh endpoints (`AuthController`), password hashing, and clean up inactive Google package dependencies.
- **P1-11 (Person 1):** Implement user profile updates, Admin password reset, and session revocation flows.
- **P1-12 (Person 1):** Implement server-side project membership validation service and authorization policy handlers.
- **P2-11 (Person 2):** Implement the authoritative active/effective `ProjectMember.role_code` read model and integration/API security matrix.
- **P1-64 (Person 1):** Implement Admin global-role changes, append-only audit, `UserRoleChanged`, and atomic revocation of all active sessions and refresh tokens.

---

## Unresolved Decisions

1. **JWT Signing Algorithm and Key Strategy:** Choice between symmetric HMAC-SHA256 (pre-shared secret) versus asymmetric RSA/ECDSA (public/private key pair) and the key rotation/retiral mechanism is deferred to task P1-10 security design.
2. **Distributed Cache Selection:** Evaluation of distributed cache backend (Redis vs. SQL Server cache vs. in-memory) for session lookup caching will be finalized during production infrastructure deployment.
3. **Multi-Factor Authentication (MFA):** TOTP/SMS-based two-factor authentication is deferred to future project releases.
