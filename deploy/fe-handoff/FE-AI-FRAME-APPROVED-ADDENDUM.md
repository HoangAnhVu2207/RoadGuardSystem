# Task34 approved AI frame read addendum

TARGET_CONFIRMED: Owner approved reuse of GET /api/v1/files/{fileId} and GET /api/v1/files/{fileId}/content for completed worker-generated AI frames. Apply the existing AI result/project authority at read time; issue no new signed URL.

CURRENT_VERIFIED: Completed provenance binds file/checksum, detection, result, run, job, attempt and manifest. No synthetic UploadSession is created. Supervisor and current active project PM can read. Assigned Operator, unrelated PM, inactive identity and ended project membership are denied. Content checks current authority again after opening storage; denied streams are disposed before response.

## Executed regression

The existing Real_synthetic_upload_manifest_worker_provenance_replay_and_revocation test first reproduced metadata HTTP404 (historical pre-patch RED). Final same-case runs passed against disposable owned SQL with both the memory store and a disposable real MinIO instance. These are one unique test case, not two. It asserts metadata/content, PNG checksum, Supervisor, wrong-project/Operator denial, identity revocation during object open and membership revocation.

A separate sequential batch passed ten unique tests covering web cookie/CSRF transport, old-attempt callback fencing/replay, five-minute private URL expiry/revocation and existing upload scope/If-Match. Final result for this addendum: 11 unique .NET PASS, 0 FAIL/SKIP/ABORT. Original RED retained separately. This does not promote any API to FULL_CONTRACT.

Raw commands, TRX, UTC duration, resource monitoring, extracted distribution checks and final source hashes are indexed in the external Task34 ai-frame-approved evidence directory and C2C addendum. The final release receipt records actual V5 build/start/HTTP assertions; this document alone is not a runtime acceptance claim.

## Limits

Real AI issuer/protocol and G02-G06 public discovery contracts remain gated. A1/B2/C1 missing parameters remain owner decisions. No new route, DTO, schema, role or signed-link contract is introduced. No real provider or external FE E2E is claimed. Knowledge runner stays DENY_ALL.

## Current cache and version correction

CURRENT_VERIFIED: Follow-up regression reproduced missing Cache-Control on both bearer metadata/content responses. Both existing read actions now emit `Cache-Control: private, no-store`; read-time authority and stream recheck remain in place. Metadata ETag is an opaque quoted file version (AI frames use their SHA-256 provenance checksum), not an upload-session rowversion. No new signed URL or conditional-GET/304 behavior is promised. A matching client ETag does not bypass current authorization.

The focused SQL + real MinIO lifecycle Fact passed after the cache patch. The generated Swagger regression verifies these headers and the opaque ETag description. Historical RED runs remain separate from final results. Final V6 command/count/release evidence is recorded externally in closure-continuation.
