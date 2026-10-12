# Pending environment verification

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

These accepted integration/deployment checks are deferred acceptance, not removed Backend scope. No shared/production SQL migration, real data write or remote publication occurred.

| Gate | Owner | Required evidence | Current task status |
|---|---|---|---|
| FastAPI/video model | External AI + BE | Real authenticated dispatch/callback, actual video source/manifests/checksums/model/attempt, late result/retry/error/source compatibility | NOT_RUN; synthetic worker not substitute |
| Android offline/reconnect | Android + BE | Actual signed command interoperability; durable queue kill/reboot; key custody; encrypted handover/decrypt; conflicts/revoke/replay | NOT_RUN |
| Physical SQL schema | Operations/DB | Approved isolated/deployed SQL comparison to migration/model, actual permissions/triggers; no team mutation from this task | LIVE_SQL_VERIFIED NOT_RUN; historical fixture source hashes match |
| Object storage | Operations/storage + BE | Real multipart/resume/verify/failure recovery and authorized download; 8GiB video/32GiB dataset admission vs actual streaming | NOT_RUN |
| Email and Web cookies | Operations/Web | Real delivery/recovery, actual HTTPS browser, CSRF, proxy/cookie host/keyring replicas, current revoke/renew | NOT_RUN; selected TestHost cookie tests fresh |
| GIS/source facts | Owner/GIS | Official CRS/profile/operation/independent control-point tolerance, real coverage/warranty/location/statistical sources | NOT_VERIFIED; sample transforms never official accuracy |
| UAT/deployed Swagger | QA/operations | Correct deployed source identity + actual production-host OpenAPI/business flows for intended actors | NOT_RUN |
| Performance and restore | Operations/QA | 50 concurrent/p95≤2sec; consistent SQL/object/keyring restore RPO≤15min/RTO≤4h with timed proof | NOT_RUN |

Missing credentials/environments are not requested or guessed; this task can complete documentation alignment while runtime/E2E certification remains pending.
