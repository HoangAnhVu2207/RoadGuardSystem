# RoadGuard Postman workflow

## Development fixtures

`DbInitializer` runs only in Development when both `RoadGuardDatabase:InitializeOnStartup=true` and a valid local SQL connection are configured. With `SeedDevelopmentUsers=true`, it creates two separate groups:

- Fixture accounts and scenario data: four `*.postman@example.test` accounts plus one deterministic project, memberships, handover, road section/version, segments, survey plan/request/assignment, defect, and repair-crew inspection task. These support authorization and business API tests.
- Manual email recipients: never seeded. Reporter registration, OTP verification/resend, invitation acceptance, and recovery must go through the real API and message sender.

Useful fixture IDs are already present in the committed Postman environment:

| Variable | Fixture |
|---|---|
| `projectId` | `RG-POSTMAN-001` |
| `primaryProjectManagerUserId` | Seeded Project Manager |
| `roadSectionId` | `RG-POSTMAN-RS-001` |
| `roadSectionVersionId` | Current version 1 |
| `surveyPlanId` | Seeded periodic plan |

The seed is idempotent and never deletes or resets data. It does not create Reporter registration intents, mark email confirmed, create invitations, accept invitations, or send email.

## Import and setup

1. Import `RoadGuardSystem-V2.postman_collection.json` and `RoadGuard.local.postman_environment.json`.
2. Select `RoadGuard.local` and set `baseUrl`, `authEmail`, and `authPassword` using a Development-only seeded account. Keep secret values in Postman local/current values; do not commit them. Set disposable `replacementPassword` only when using the destructive request.
3. Start the API with Development `DbInitializer` enabled when a real database smoke is allowed. The initializer applies migrations and seeds the accounts documented in `RoadGuardSystem-Test-Accounts.md`.
4. Copy `RoadGuardSystem.API/appsettings.Development.local.example.json` to the Git-ignored `appsettings.Development.local.json`, then fill the local database, Base64 onboarding secret, frontend URL, SMTP sender and SMTP app password. Alternatively use environment variables such as `IdentityOnboarding__SmtpUsername`, `IdentityOnboarding__SmtpPassword`, `IdentityOnboarding__Secret`, and `IdentityOnboarding__FrontendBaseUrl`. Never paste those values into chat or commit them.
5. The provider is the existing SMTP sender (`GmailIdentityMessageSender`, Gmail SMTP by default). The committed configuration deliberately has no sender address or credential. A missing username, password, recipient, or invitation frontend URL makes delivery fail without exposing the secret.

## Private recipient environment

The committed environment contains placeholders only. A machine-local `RoadGuard.local.private.postman_environment.json` may hold the real recipient addresses and is ignored by Git. Keep passwords, OTPs, invitation tokens and access tokens in Postman local/current values only.

Purpose-specific variables:

- `reporterRegistrationEmail` and `reporterRegistrationBackupEmail`: first-time Reporter registration across Gmail and non-Gmail providers.
- `otpResendEmail`: a separate pending registration used only for cooldown, resend and old-code invalidation.
- `invitationEmail`, `invitationBackupEmail`, and `invitationRetryEmail`: first invitation, non-Gmail invitation, and retry/replay scenarios.
- `recoveryEmail`: password recovery or spare mailbox.
- `manualEmailScenario`: safety gate. Allowed values are `REPORTER_REGISTRATION`, `OTP_RESEND`, `INVITATION`, or `RECOVERY`. Empty means every request capable of sending real email is skipped.

Do not run real-email requests with Newman or the entire collection. Set one `manualEmailScenario`, send only the matching request manually, then clear it.

## Check state before a manual scenario

Check only non-secret state in the local test database before using an address. Do not query OTP/token hashes:

```sql
SELECT Email, Status, EmailConfirmed
FROM Users
WHERE NormalizedEmail = UPPER(@email);

SELECT Email, AcceptedAt, RevokedAt, ExpiresAt
FROM StaffInvitations
WHERE NormalizedEmail = UPPER(@email)
ORDER BY CreatedAt DESC;
```

- No `Users` row is the precondition for first-time registration or invitation acceptance.
- A pending Reporter account belongs to its existing intent; use the resend scenario instead of creating another first-time registration.
- An ACTIVE/confirmed account must use login or recovery, not first-time registration.
- An accepted invitation must use the existing-account scenario. An active unaccepted invitation may be retried with its original operation/key as the contract permits.
- If the selected address was consumed in an earlier run, switch the relevant Postman variable to its documented backup. Never delete/reset the account, rewrite its email, force `EmailConfirmed`, or bypass validation.

## Manual Reporter flow

1. Confirm `reporterRegistrationEmail` has no account, set a new `reporterRegistrationKey`, fill `reporterPassword`, and set `manualEmailScenario=REPORTER_REGISTRATION`.
2. Send `POST /auth/reporter-registrations - success`. A successful response stores `registrationIntentId`; the database account remains pending/unconfirmed.
3. Read the OTP from the mailbox and enter it in the local `reporterOtp` value. Do not log or commit it.
4. Send verify, then login through the API. Only successful verification activates/confirms the account.
5. Clear `manualEmailScenario` and the OTP value.

For resend, use the dedicated setup request with `otpResendEmail` and `manualEmailScenario=OTP_RESEND`. It stores `otpResendIntentId`. Respect `resendAfterSeconds`; retrying the same operation keeps key and payload, while a new resend operation uses a new key. Verify that the old OTP is rejected and the newest OTP succeeds. Resend is never part of unattended Runner execution.

## Manual invitation flow

1. Confirm the recipient has no account or accepted invitation. Login as the seeded Supervisor and store its token in `supervisorAccessToken`.
2. Set `invitationEmail`, a fresh `invitationCreateKey`, and `manualEmailScenario=INVITATION`; send create invitation once.
3. Open the received link and place only the token value in local `invitationToken`; set local `invitationPassword`.
4. Send accept. For an exact retry, retain the same key and payload. A new invitation operation uses a new key and, when the original address was already consumed, `invitationBackupEmail` or `invitationRetryEmail` according to the scenario.
5. Clear the scenario and secret values. The initializer never pre-accepts this invitation.

## Verification labels

- Code/static validation: build, JSON/YAML parsing, placeholder and safety-gate checks.
- Automated tests: use `CapturingIdentityMessageSender`; they do not contact real mailboxes.
- Manual delivery: separately record whether SMTP send, mailbox receipt, OTP verify/login, or invitation accept actually ran. Static validation or automated fake-sender tests are not evidence of real delivery.

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
