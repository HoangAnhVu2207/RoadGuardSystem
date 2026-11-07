# Ubuntu server deployment

Status: ASSIGNED by owner in chat, 2026-11-07: branch `deploy`, BE + SQL Server + storage first.
Writer: Codex in this deployment worktree; no changes to other checkouts. Branch is an owner-requested exception to `huy-review` handoff.
Base: `d810cc52668a6f0a3272f8b20b095ac6fef122b2` (`origin/develop`, same tree as `origin/huy-review`). Initial dirty paths: none.

## Goal and sources

TARGET_CONFIRMED: dedicated Ubuntu server; first deployment BE, SQL Server, private object storage; FE/AI integration later.
CURRENT_VERIFIED from supplied transcript: Ubuntu 24.04, 192.168.1.11, Docker 29.1.3, Compose 2.40.3, hello-world success, 15 GiB RAM, 96 GiB free SSD.
Source: Program.cs, database options validator, secure Web cookie configuration, existing pinned SQL/MinIO images and seeding components. Existing handoff is Development/mock-only and is not reused as live deployment configuration.

No new business endpoints, schema, libraries, upgrades or synthetic business facts. Production checks remain enforced. Private CA is scoped to this LAN server; public hosting/domain/TLS issuance is a separate deployment decision. SQL Developer license is for development/test, not commercial production; this server initially hosts real services for project integration testing.

## File ownership and behavior

Reserved to this writer: `deploy/server/**`, `planning/development/SERVER-DEPLOYMENT.md`, `RoadGuardSystem.API/Extensions/ServerDeployment.cs`, three integration points in Program.cs, `tests/RoadGuardSystem.ApiTests/Deployment/**`, `.dockerignore`, `.gitattributes`, deploy checks/trigger in `.github/workflows/ci.yml`.
New opt-in ServerDeployment composition: Production only, explicit HTTPS CORS origins, persistent keys, live DB/storage health checks, existing upload verification worker. No changes to authentication policy or cookie security.
Maintenance process only: apply existing migrations using dedicated private maintenance configuration; canonical roles and one explicitly entered first Supervisor; create restricted runtime SQL login. Refuse adopting existing user data or changing an existing account. Normal startup never migrates/seeds.
Code is versioned under `/opt/roadguard/releases/<SHA>` with a bare public Git repository; only five source directories plus required build/fixture/deploy assets are extracted. Private configuration is in `/etc/roadguard`; persistent SQL/objects/files/keys/backups in `/srv/roadguard`. Only a public CA certificate is exported at `/opt/roadguard/roadguard-lan-ca.crt`. No host SQL/admin port. Updates build before downtime, stop API/storage, back up SQL/storage/secrets, apply forward migrations explicitly, restart and check health. Failures remain visible; no automatic downgrade, delete, reset or volume removal.

## Implementation and verification plan

- [x] Write API composition tests: disabled unchanged; reject non-Production, empty/wildcard/insecure origins; register exactly one verification worker and dependency health checks. Initial focused run failed on missing implementation; later focused 18 tests passed.
- [x] Add opt-in extension and guarded one-shot maintenance. API Release build: 0 errors, 224 existing warning occurrences. Focused deployment + HandoffBootstrap + HandoffPolicyGate + SandboxConfiguration regression: 50 passed, 0 failed/skipped. SQL-backed cookie/HTTP tests remain a hosted-CI/live checkpoint.
- [x] Add server Dockerfile, Compose, private setup wizard, lifecycle script and Vietnamese runbook. Ubuntu SSH scratch verification: 5 Python tests passed, including actual OpenSSL issuance/verification and overwrite refusal (root identity/chown mocked; only scratch paths used). Compose config/source contexts/SQL-port/CA-isolation checks and bash syntax passed. Scratch files removed. No real server data/config created by these checks.
- [x] Review pass 1: secure SQL/TLS, no mock/bootstrap on normal startup, failure paths and data preservation. Fixed archive entry for nonexistent .editorconfig, Compose project-directory/build-context resolution, SQL certificate supplementary group, SQL role idempotency, credential handling in probes and bounded initial account input. Review pass 2: scoped file ownership, source-only releases, private mounts, key permissions, update forward-only ancestry, inherited deployment lock and partial failure/restore limits. Added exact deployment runbook and public-CA-only export; no branch merge or shared data writes.
- [ ] Push only `deploy`, observe hosted CI. If server access exists, execute isolated deployment verification and report actual outcomes; otherwise mark live deployment NOT RUN and give precise pending steps.

No external review is claimed. Local compile/mock checks and live server checks are reported separately. No existing shared database may be migrated or seeded.

## Remaining evidence checkpoints

CURRENT_VERIFIED: source API build and configuration regressions; Python/certificate, Compose configuration and bash syntax checks on Ubuntu; existing Operations/CI validators; scoped `dotnet format --verify-no-changes --no-restore`; `git diff --check`.

UNKNOWN / NOT RUN: live container builds/start, SQL migrations on the dedicated owned DB, real runtime SQL permissions, private signed upload/download, backup restore, login/password change, FE/mobile/AI E2E. Local Linux Docker daemon is unavailable and server dev cannot use Docker/sudo unattended. Owner must run the reviewed bootstrap with interactive sudo and privately enter the initial account; no broad sudo permission or Docker group membership is requested. Hosted CI status is recorded after push, independently of live deployment.
