# Real Gmail Server Replay Plan

> For agentic workers: use superpowers:executing-plans after the release target and server maintenance window are approved.

**Goal:** Replay the locally accepted RoadGuard BE + FT95 AI package on Ubuntu with real Gmail delivery while preserving the local Mailpit evidence and avoiding mock AI.

**Frozen behavior:** Keep the local-tested BE/API seed, FT95 CPU model, two MP4/SRT pairs, project names `Duong Kenh Dong` and `Duong so 2`, API idempotency and provenance rules. The live run is additive and never resets existing projects, AI results, accounts or storage.

**Mail behavior:** Local Docker keeps `Sandbox.Enabled=true` and Mailpit. Ubuntu keeps `Sandbox.Enabled=false` and uses `smtp.gmail.com:587` with a Gmail App Password. The sender address and App Password are entered directly into the server configuration editor and never enter Git, command arguments, logs, receipts or chat.

## Gates

1. Commit the current handoff changes and run CI on that exact SHA. The resulting full SHA becomes `TARGET_SHA`.
2. Run read-only Ubuntu preflight against the current active release, schema, AI worker, model checksum, disk/RAM and maintenance lock.
3. Create and verify a fresh SQL/storage/config backup before any release or data write.
4. Configure Gmail in both `/etc/roadguard/application.json` and `/etc/roadguard/maintenance.json`, preserving TLS, CORS, bind IP, Funnel and AI flags. Verify only with a bounded test message.
5. Deploy `TARGET_SHA` with the reviewed updater, then verify BE health, AI health and one real BE-to-AI upload using the approved Kênh Đông and Đường số 2 assets.
6. Run the additive API seed only after the deployment smoke passes. Produce a sanitized receipt with every workflow marked `PASS`, `FAIL`, `NOT_RUN` or `UNKNOWN`.

## Account decision

The local manifest contains 14 logical actors. The last server readback showed 7 existing accounts. The server additive run must stop before account mutation unless the owner provides a private mapping for all 14 actors or explicitly accepts a 7-actor live replay. New accounts require an owner-approved creation path and private passwords. No password is copied from chat.

## Known limits carried forward

- FastTrack completion remains `NOT_RUN` until a valid handover, warranty coverage and road-scope mapping exist for the selected live defect.
- Gmail delivery is external and has not been verified by the local Mailpit run.
- FE browser, Android and iOS execution remain outside the BE-to-AI deployment gate.

## Recovery

If activation, health, SMTP or BE-to-AI smoke fails, stop the release run, preserve the backup and revert to the previously active release using the reviewed recovery table. Do not restore SQL, run a down migration or delete data as part of this replay.
