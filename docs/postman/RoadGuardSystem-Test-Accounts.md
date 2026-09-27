# RoadGuardSystem Postman test accounts

## Important

When the API is started in Development with `RoadGuardDatabase:InitializeOnStartup=true` and `RoadGuardDatabase:SeedDevelopmentUsers=true`, the `DbInitializer` provisions the four deterministic accounts below. They are never seeded in Production.

The API test fixtures use this password convention when creating disposable users:

| Role | Login email convention | Password | Use |
|---|---|---|---|
| Supervisor | `supervisor.postman@example.test` | `Supervisor1!` | Admin password reset and project-management authorization |
| Project manager | `pm.postman@example.test` | `Current1!` | Project and profile success paths |
| Drone operator | `operator.postman@example.test` | `Current1!` | Login, refresh, profile and forbidden-path checks |
| Repair crew | `crew.postman@example.test` | `Current1!` | Role/authorization checks |

These are Development-only test-fixture credentials. Do not use them in production.

## Collection variables to set

- `baseUrl`: `http://localhost:5112` for the HTTP launch profile, or `https://localhost:7088` for HTTPS.
- `authEmail`: email of the active test user.
- `authPassword`: password of that user.
- `replacementPassword`: a strong disposable password, only if running the destructive change-password request.
- `primaryProjectManagerUserId`, `targetUserId`, and row-version variables: IDs/row versions from the database or a preceding successful request.

Run `Login - valid account` first. Its test script stores `accessToken`, `refreshToken`, `targetUserId`, and `profileRowVersion` in collection variables for later requests.

The collection deliberately does not contain SQL credentials, JWTs, refresh tokens, or a fixed user password.
