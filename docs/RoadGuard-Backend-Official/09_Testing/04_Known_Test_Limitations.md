# 04 Known Test Limitations

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Fresh focused API test result:92total,91PASS,1FAIL,0skip. Failure ManualSwaggerContractTests.ProductionInventoryAndEvidenceExcerptsMatchCurrentSource points at stale SWG-014 BusinessDutyRepository excerpt. The test/code/embedded Swagger evidence JSON are outside docs scope and remain unchanged. Prior independent audit found11stale of2533unique excerpts across9sourcefiles; this statistic is historical source-revalidated evidence, not eleven fresh runtime failures.

Build PASS0errors/222warnings is fresh; warnings are not suppressed and analyzer remained enabled. Fresh OpenAPI structural validity and API Explorer match do not certify all241 business requests or declaration completeness. Runtime operationId is absent for all operations; this is permitted by schema and documented as consumer limitation.

All unselected tests are NOT_RUN this task; historical failures of two FE validators/281legacy local references belong to the prior audit and are not imported as failures in the new internal-link gate. V2 bytes/lock are preserved. Fresh new docs links are checked independently.

Storage-open privacy race is a source risk not reproduced; callback stale-attempt limitation is observed source gap without live exploit test. Android device, real FastAPI, physical deployed SQL, MinIO/email/browser/proxy, GIS/control data, NFR and backup restore remain pending. No dummy values or absent environment is turned into PASS.
