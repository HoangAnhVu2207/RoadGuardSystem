# RoadGuard FE handoff — preparation

## FE Starter continuation — approved optional response compatibility

Use this index, [business flows](FE-BUSINESS-FLOW-GUIDE.md), [API cookbook](FE-API-COOKBOOK.md), [authentication](FE-AUTH-AND-CLIENT-GUIDE.md), [test instructions](FE-API-TEST-GUIDE.md) and the [Postman collection](FE-API.postman_collection.json) as the Starter package. The canonical catalog retains all 260 operations. A documented operation is not automatically runtime accepted.

Owner decision OWNER-OPTIONAL-NULL-EXTENSION-02 preserves OpenAPI: API-019 may omit `conclusion` before a conclusion exists; API-225 may omit `accessPoint` when none was supplied. Both properties remain objects when populated. Consumers must check property presence before accessing nested values. No database, business transition or global JSON setting changes are authorized by this decision.

Candidate3 now has bounded runtime verification: scenario recovery exit0, C1/Newman18 assertions, optional-object/schema/authority10 checks and Starter auth/privacy28 checks PASS. The two current-source Case lifecycle tests PASS separately. See [current Candidate3 status](FE-STARTER-CANDIDATE3-STATUS.md) for exact boundaries and the portable Starter bundle manifest. Candidate1/2 and all failed runs remain retained. H02–H05, independent readiness, full certification and actual FE acceptance remain open.

The portable bundle includes the unchanged Candidate3 ZIP and separately hashed documentation supplements. Its top-level README uses relative paths, a new Compose project and free default ports; no developer-machine private path is needed to start Backend. Credentials are generated privately on first startup, never shipped. Use [HTTPS guidance](FE-HTTPS-LOCAL.md) and the existing credential procedure in the extracted quickstart.

Known compatibility limitation under investigation: API-030 returned `video/mp4` for a video while the retained OpenAPI declares `application/octet-stream`. This is separate from optional-null compatibility; do not treat that execution as schema accepted or modify the media contract under the null approval.

Current preview closeout (2026-10-24): [FE preview gates and candidate](review-evidence/fe-preview-closeout/FE-PREVIEW-STATUS.md). A new source-matched candidate has been built and tested in an owned runtime. H02/H05 and complete candidate journey reconciliation remain open; this is not FE_INTEGRATION_READY. The original certification task is suspended for preview closeout, with its frozen criteria and unresolved ledger preserved. The older R1/R2/R3 dependency statement below describes that certification path; preview is evaluated separately under H01–H06.

Release classification: **FE_INTEGRATION_PREVIEW**. The new integration kit is under preparation. R1 contract acceptance is open; R2 needs a new source-matched artifact, and R3/F01–F10 have not passed. Candidate1 is immutable and available in the private review artifact store; final FE readiness and acceptance remain pending.

1. **Where do I obtain and run Backend?** Final candidate selection is pending. Existing [quickstart](FE-INTEGRATION-QUICKSTART.md) and [HTTPS instructions](FE-HTTPS-LOCAL.md) describe the preview environment. Their historical runtime results do not certify the current dirty source. Use a separately owned Compose project and free ports; preserve all existing volumes.
2. **What do I import/configure?** Start with [OpenAPI](openapi.json), [environment contract](FE-ENV-CONTRACT.md) and [existing Postman collection](FE-API.postman_collection.json). Candidate1 archive/source/OpenAPI parity and the bounded maintained Newman collection are verified; missing workflow examples and final readiness remain tracked in the preview gates.
3. **Where do I find an operation?** The [functional guide](FE-API-FUNCTIONAL-GUIDE.md) and [catalog](FE-API-CATALOG.json) index all 260 method/path operations. Under OWNER-FE-UI-AUTONOMY-01, FE owns screens/navigation. Use [business flows](FE-BUSINESS-FLOW-GUIDE.md) and [workflow recipes](FE-W01-W10-RECIPES.md); historical [screen mapping](FE-SCREEN-API-MAPPING.md) is a reference. Standalone capabilities are documented without inventing workflow authority.
4. **How do I call/test it?** Follow only the applicable verified transport and operation contract. [Web HTTPS/CSRF](FE-HTTPS-LOCAL.md), [special policies](FE-CLOSEOUT-POLICY-CONTRACT.md), [B2 exact contract](B2-EXACT-ASSIGNMENT-PROPOSAL.md) and [API contract](FE-API-CONTRACT.md) are source inputs. Draft catalog entries without verified examples are not executable acceptance evidence. Mock AI proves development behavior only.
5. **How do I report an error?** Consult [existing troubleshooting](FE-ERROR-TROUBLESHOOTING.md). Report screen/feature, method/path, correlation ID, sanitized request, expected/actual response, HTTP status, environment/build and reproduction steps. Never include passwords, JWTs, cookies, private signed URLs, private media or credentials.

The draft [auth and client guide](FE-AUTH-AND-CLIENT-GUIDE.md) and [browser client example](examples/roadguard-web-client.mjs) describe explicit Web authentication and request handling. Final-candidate runtime verification remains pending.

Draft [special cases](FE-CONTRACT-SPECIAL-CASES.md), [error diagnostics](FE-API-TROUBLESHOOTING.md) and [sanitized issue template](FE-ISSUE-REPORT-TEMPLATE.md) provide the current boundaries and reporting procedure. Their F07/F08/F10 acceptance remains open.

Current acceptance and missing work are recorded in [the kit checklist](FE-INTEGRATION-ACCEPTANCE-CHECKLIST.md). Source and document hashes are in [the preparation manifest](review-evidence/fe-integration-kit/PREPARATION-MANIFEST.json). This index is not `FE_HANDOFF_DELIVERED` or `FE_HANDOFF_VERIFIED`.

Current functional integration entry points: [business flows](FE-BUSINESS-FLOW-GUIDE.md), [all API functions](FE-API-FUNCTIONAL-GUIDE.md), [test instructions](FE-API-TEST-GUIDE.md). Missing screen names do not block H02; missing mandatory contract, security or runtime evidence does.
