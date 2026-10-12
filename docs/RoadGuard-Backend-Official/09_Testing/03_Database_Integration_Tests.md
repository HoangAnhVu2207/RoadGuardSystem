# 03 Database Integration Tests

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Database test sources include isolated fixture model/snapshot/catalog, identity sessions, reporter/case/receipt, repair policy/execution/lifecycle/safety, coverage/statistics/retention, outbox and signed offline persistence. Complete test declaration inventory is provided beside the API test matrix; all methods are source-indexed, not blanket PASS.

BE-DOC-02 does not run new SQL integration tests or apply migrations. Fresh EF DesignTimeModel is captured from disconnected host:196entitytypes/191tables/1940unique mapped columns/662FK. This is EF_LOGICAL_MODEL only. No test success is inferred from migration source.

Historical audit schema test applied current two migrations to fixture-owned SQL and compared EF/snapshot/catalog. Its5cases and overall84 SQL cases were historical execution;578schema input hashes are revalidated unchanged. Physical191tables/1940columns/662FK/693indexes/219keys/229checks/189triggers describe that historical fixture, not live deployment. Raw historical trigger definitions and current source hashes are retained in provenance/constraint metadata.

Future SQL verification must use disposable database, preserve actual source/policy/assignment/account/current recipient facts, prove transaction rollback/dedup/concurrency and fail-closed immutable history. Production row policy, external bytes, restore duration, live connection permissions and actual trigger effects require explicit runtime acceptance beyond a model match.
