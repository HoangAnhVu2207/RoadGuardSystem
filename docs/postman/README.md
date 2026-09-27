# RoadGuard Postman workflow

## Import and setup

1. Import `RoadGuardSystem-V2.postman_collection.json` and `RoadGuard.local.postman_environment.json`.
2. Select `RoadGuard.local` and set `baseUrl`, `authEmail`, and `authPassword` using a Development-only seeded account. Keep secret values in Postman local/current values; do not commit them. Set disposable `replacementPassword` only when using the destructive request.
3. Start the API with Development `DbInitializer` enabled when a real database smoke is allowed. The initializer applies migrations and seeds the accounts documented in `RoadGuardSystem-Test-Accounts.md`.

## Running requests

- `Health` and the unauthorized profile request are independent checks.
- In `Authentication`, run a valid login before refresh, profile, project, or admin requests. Login scripts save access and refresh tokens in collection variables.
- Requests that create, update, change password, logout, or reset a password are state-changing. Use disposable accounts/data and do not run destructive requests against shared seed accounts.
- Project/admin examples require IDs and row versions from a permitted setup response or the collection variables documented in the account guide. A missing prerequisite is `BLOCKED`, not a passing assertion.
- Send runs one request. Runner follows folder order and is the appropriate mode for ordered scenarios; it does not prove SQL transaction or rollback behavior.

## Maintenance rule

When an API changes method, route, request/response, auth, headers, stable errors, or direct setup dependency, update the matching request in the same task. Preserve request names/IDs and unrelated scripts. Validate the collection as Postman Collection v2.1 and report runtime checks separately from static validation.
