> Restart resilience: local handoff bootstrap waits up to 120 seconds for recognized SQL startup/transport failures before running ownership checks, migrations and seed. Invalid credentials/permissions are not retried. Restart containers without deleting volumes; wait for `/ready` and successful scenarios. This does not grant production startup behavior or contract acceptance.

> Closeout repair demo: real API-only Case triage -> candidate match/KEEP_NEW -> package -> proposal. Project A NORMAL AwaitingApproval; project B FAST_TRACK ReadyToAssign. No approved/executed state or Supervisor assignment is fabricated. Restarting scenarios reuses saved idempotency inputs and verifies populated collections.

> Closeout current status: see [FE-CLOSEOUT-POLICY-CONTRACT.md](FE-CLOSEOUT-POLICY-CONTRACT.md). A1/B2/C1 exact policies are OWNER_CONFIRMED and implemented with the stated bounded evidence and remaining acceptance gaps. Earlier pending-policy/wire statements below are HISTORICAL. No independent release PASS or Git publication.

> V9 adoption: G02-G06 collections are owner-approved and implemented. See [FE-APPROVED-COLLECTIONS.md](FE-APPROVED-COLLECTIONS.md) for exact routes/wire, authority, evidence and limits. Older collection-pending language below is historical. Canonical241 coverage unchanged; eleven new GETs are additive. A1/B2/C1 policy is adopted; exact source compatibility gates remain open. No FE_READY or FULL_CONTRACT promotion.

# Task34 Backend FE preview

Branch: `huy-review`. Historical base `dd94aa6` does **not** contain this uncommitted candidate. Until owner-approved publication, a GitHub clone cannot reproduce the local release. Use the Task34 distribution and its RELEASE-MANIFEST. No commit, push or tag has been performed.

1. Use Docker Desktop with Linux containers, healthy writable storage and sufficient memory. Preserve existing FE instances.
2. Extract the entire RoadGuardBackend folder. In `deploy/fe-handoff`, optionally copy `.env.example` to `.env` and choose free ports/exact loopback origins before first startup.
3. Run `docker compose -p roadguard-fe up -d --build`. Wait for SQL/API healthy **and scenarios exited0**, using `docker compose -p roadguard-fe ps -a`. `/ready` does not prove demo setup completion.
4. Default URLs: API http://localhost:18081, Swagger `/swagger`, readiness `/ready`, Mailpit http://localhost:18026, private object endpoint http://localhost:19001. SQL has no host port. For browser cookies use the optional [HTTPS profile](FE-HTTPS-LOCAL.md), API https://localhost:18443, and an HTTPS FE origin.
5. Privately read generated accounts with `docker compose -p roadguard-fe exec -T api cat /handoff/demo-accounts.json`. Never paste the output into Git or evidence. Use exact login contracts in FE-SCREEN-API-MAPPING.md.
6. Follow project list Ã¢â€ â€™ work-package Ã¢â€ â€™ G01 SegmentSet list Ã¢â€ â€™ detail/segments. For other screens, use FE-SCREEN-API-MAPPING.md and the explicit discovery gates.

Restart/recreate containers without deleting volumes. Repeated setup preserves credentials, keys, journal and data. Never reset an active FE Session DB to pass a test. A second instance requires a separate Compose project and free ports before initialization. Existing configurations are not silently upgraded.

Release classification remains FE_INTEGRATION_PREVIEW while essential G02Ã¢â‚¬â€œG06/A1/B2/C1 contracts remain owner-gated. See FE-OWNER-CONTRACT-DECISIONS.md. Task34 runtime evidence is reported separately; source documentation does not establish PASS. Bounded headless browser HTTPS/cookie/CSRF assertions PASS; external Web/Expo UI E2E, OS/browser certificate trust installation, real Gmail/YOLO and deployment remain NOT_RUN. Knowledge runner: DENY_ALL.

## Docker address-pool exhaustion

If startup reports `all predefined address pools have been fully subnetted`, do
not prune inherited networks or reset Docker. Inspect `docker network ls`,
`docker network inspect <exact IDs>` and host/VPN routes. Choose two distinct,
non-overlapping CIDRs, set `RG_FE_INTERNAL_SUBNET` and `RG_FE_EDGE_SUBNET` in
your private environment, then use:

```powershell
docker compose -f compose.yaml -f compose.networks.yaml config --quiet
docker compose -f compose.yaml -f compose.networks.yaml up -d --build
```

This optional override creates only this Compose project's networks. No subnet
default is supplied: an example range from another developer is not evidence
of availability on your machine. Normal installations use compose.yaml alone.
All subsequent commands must repeat the same compose file arguments/project.

Final policy packet: [OWNER-DECISIONS-FINAL.md](OWNER-DECISIONS-FINAL.md).
Git synchronization is conditionally authorized after mandatory acceptance gates PASS; local extracted distribution acceptance is separate from a GitHub clean clone.

## Current owner adoption and release gate

A1/B2/C1 policies are owner-confirmed by A1_B2_C1_FINAL_POLICY_APPROVAL. Existing source still requires exact JWT dispatch/key profile, FastTrack transition mapping and separate anonymous-publication wire adoption; see OWNER-DECISIONS-FINAL.md. Local preview stays fail closed for external AI callbacks and does not enable an unapproved public endpoint. Conditional Git synchronization is authorized only after mandatory implementation and acceptance gates PASS.
