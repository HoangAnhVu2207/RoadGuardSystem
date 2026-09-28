# RoadGuard Postman workflow

## Import and setup

1. Import `RoadGuardSystem-V2.postman_collection.json` and `RoadGuard.local.postman_environment.json`.
2. Select `RoadGuard.local` and set `baseUrl`, `authEmail`, and `authPassword` using a Development-only seeded account. Keep secret values in Postman local/current values; do not commit them. Set disposable `replacementPassword` only when using the destructive request.
3. Start the API with Development `DbInitializer` enabled when a real database smoke is allowed. The initializer applies migrations and seeds the accounts documented in `RoadGuardSystem-Test-Accounts.md`.
4. Identity onboarding additionally requires a Base64 `IdentityOnboarding:Secret` of at least 32 bytes. Gmail delivery uses `IdentityOnboarding__SmtpPassword` from the API process environment (the Gmail App Password, never committed) and `IdentityOnboarding__FrontendBaseUrl` for invitation links. Copy OTP and invitation token from the local mailbox into the secret environment values.

## Running requests

- `Health` and the unauthorized profile request are independent checks.
- In `Authentication`, run a valid login before refresh, profile, project, or admin requests. Login scripts save access and refresh tokens in collection variables.
- For the V2 identity Runner scenario, authenticate the seeded Supervisor and store its token as the local `supervisorAccessToken`; keep a non-Supervisor token in `operatorAccessToken`. Then run Reporter onboarding, Invitations, and V2 identity/account folders in order.
- Reporter registration captures `registrationIntentId`; enter `reporterOtp` from the mailbox before verify. Invitation creation captures invitation metadata; enter `invitationToken` from the mailbox before accept. GET requests capture actor/account versions for subsequent `If-Match` requests.
- Requests that create, update, change password, logout, or reset a password are state-changing. Use disposable accounts/data and do not run destructive requests against shared seed accounts.
- Reuse the same explicit idempotency-key variable to test replay. Change the payload without changing the key to test conflict. Generate a new key before starting a fresh scenario.
- Project/admin examples require IDs and row versions from a permitted setup response or the collection variables documented in the account guide. A missing prerequisite is `BLOCKED`, not a passing assertion.
- Send runs one request. Runner follows folder order and is the appropriate mode for ordered scenarios; it does not prove SQL transaction or rollback behavior.

## Maintenance rule

When an API changes method, route, request/response, auth, headers, stable errors, or direct setup dependency, update the matching request in the same task. Preserve request names/IDs and unrelated scripts. Validate the collection as Postman Collection v2.1 and report runtime checks separately from static validation.
