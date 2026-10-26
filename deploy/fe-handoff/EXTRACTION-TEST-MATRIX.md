# Task32 actual ZIP validation map

| Check | Required evidence |
|---|---|
| Manifest/ZIP | archive SHA, every extracted payload length/SHA, no secrets/host paths/private generated state |
| Build/up | command run in extracted deploy/fe-handoff; no host SDK/Python required for Backend boot |
| Readiness | SQL/private bucket/Mailpit reachable; liveness distinct; setup exited0 separate |
| Core/scenario seed | owner server/database marker; 11 actors/2 projects; A/B roads, PNG VERIFIED, incident and PDFs; scenario journal complete |
| Identity/project | valid actor, wrong credential/project/reporter; source-correct me/profile DTOs |
| Workers | real object checksum; PDF SUCCEEDED/content/no-store; actual journal and SQL results |
| Restart/up | no new core/scenario rows; role stamps/keys/private config stable; bearer still accepted |
| Failure | SQL/MinIO/Mailpit stop→readiness503→recover; scoped only task-owned services |
| Isolation | disjoint instance owners/database/volumes, one instance's bearer not accepted by other |
| Safety | Docker responsive, active filesystem rw, no matched new I/O/JBD2 errors, inherited source/resources retained |

This is an evidence locator specification, not predeclared PASS. Final MASTER-HANDOFF records each executed result, failure, NOT_RUN and limitation. No regeneration/repacking after final cold-build is permitted without invalidating that acceptance.
