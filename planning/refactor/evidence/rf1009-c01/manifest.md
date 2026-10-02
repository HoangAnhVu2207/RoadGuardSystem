# RF-10-09-C01 Box 2 Review Package Manifest

**Package:** RF-10-09-C01-box2-review.zip  
**Created:** 2026-10-01  
**Owner:** Anh  
**Branch:** anh  
**Commit (before):** 2efc8a5775f834c7f0fe37cc0ce703011649e1f1

## Package Contents

### 1. New Test File

| Path | Size | SHA-256 |
|------|------|---------|
| tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs | 16384 | 1f233dc690351172812d5c5d413488457c7ebcc2e0b18193e042a0d07854f06f |

**Status:** NOT_RUN (BOX 2 does not execute tests)  
**Test methods:** 5  
**Trait:** [Trait("TaskId", "RF-10-09-C01")]

### 2. Documentation Files

| Path | Size | SHA-256 |
|------|------|---------|
| planning/refactor/10-notification-outbox-characterization-baseline.md | 9126 | 0d7474d6bf7cb5cbf09d8f73aadda40b95d34babd3fca432310623287ea76526 |
| planning/refactor/reports/RF-10-09-C01.md | 22547 | 4643ef3c1fc2cebcd6ed7e3f2bd178974c8dc418f3153c9ca1fb933ce9985097 |

### 3. Evidence Directory Files

| Path | Size | SHA-256 |
|------|------|---------|
| planning/refactor/evidence/rf1009-c01/allowlist-before.txt | 1789 | 95f43de0793bf5472d65733cd09fdc1c2b9b59fb09227206169184f9c5058935 |
| planning/refactor/evidence/rf1009-c01/scope-matrix.md | 4932 | 87cbe2df172e0720150104c73a2fe03a51154601eb364ba73058c91b2d11b27b |
| planning/refactor/evidence/rf1009-c01/before-baseline-hash.txt | 385 | dc4cadf80ef522dfa4189a8853168adc9b929a2600bb30d6d72049be8c43f65c |
| planning/refactor/evidence/rf1009-c01/existing-test-hash.txt | 393 | 37bccef8dbd8eaaccfd183a9bc2e45f37bda89e71a0858cab41f4e49b6a0aa42 |
| planning/refactor/evidence/rf1009-c01/new-test-hash.txt | 393 | 1e0861d8ad77cb8486028cdb25879387f9b76578e0c26c0b12345be6bc1c238c |
| planning/refactor/evidence/rf1009-c01/BOX2-READY.md | 5847 | c98581a7c7abd507256f5d938d0982b80cbbb0af0cfad2ea19472f25ea082c12 |
| planning/refactor/evidence/rf1009-c01/manifest.md | (this file) | (self-referential) |

### 4. Source Context (read-only inspection)

The following source files were read for characterization but are NOT included in the package (read-only, no modifications):

- RoadGuardSystem.API/Controllers/NotificationsController.cs
- RoadGuardSystem.Services/Implementations/Messaging/NotificationService.cs
- RoadGuardSystem.Repositories/Implementations/Messaging/NotificationPersistenceService.cs
- RoadGuardSystem.Repositories/Implementations/Messaging/NotificationOutboxConsumer.cs
- RoadGuardSystem.Repositories/Implementations/Messaging/OutboxWorkRepository.cs
- RoadGuardSystem.BusinessObjects/Messaging/Notification.cs
- RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs
- RoadGuardSystem.Repositories/Implementations/Surveys/SurveyAssignmentPersistenceService.cs
- tests/RoadGuardSystem.IntegrationTests/Notifications/P207NotificationPersistenceTests.cs
- tests/RoadGuardSystem.ApiTests/Notifications/P2NotificationApiTests.cs
- tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs
- tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs

**Rationale:** Source files are available in the repository at commit 2efc8a5. Including them would bloat the package and duplicate version-controlled content.

## Verification Checklist

After extracting the package, verify:

- [ ] All 11 files present (10 + manifest)
- [ ] SHA-256 hashes match manifest (except manifest itself)
- [ ] No unlisted files in package
- [ ] No duplicate paths
- [ ] Test file has UTF-8 BOM and CRLF line endings (Windows convention)
- [ ] Documentation files are valid Markdown
- [ ] Evidence files reference correct commit hash

## Package Integrity

**Verification command:**

```bash
# After extracting to temp directory
cd RF-10-09-C01-box2-review/
find . -type f ! -name "manifest.md" -exec sha256sum {} \; > actual-hashes.txt
# Compare actual-hashes.txt against manifest SHA-256 column
```

**Expected result:** All hashes match manifest (0 mismatches)

## Usage Instructions

**For BOX 3 reviewer:**

1. Extract package to temporary review directory
2. Verify package integrity (checksums, no missing/extra files)
3. Read BOX2-READY.md for handoff requirements
4. Read RF-10-09-C01.md for characterization findings
5. Read scope-matrix.md for verified vs NOT_VERIFIED scope
6. Copy test file to repository: tests/RoadGuardSystem.ApiTests/Notifications/
7. Execute test: `dotnet test --filter "TaskId=RF-10-09-C01"`
8. Verify 5/5 tests pass, capture TRX to evidence directory
9. Update ledger/checklist after successful verification

**For future reference:**

- This package documents BOX 2 work at READY milestone
- NOT_RUN status: tests written but not executed
- Dispatcher absence documented as NOT_VERIFIED limitation
- No production code modifications, no shared fixture changes

## Key Findings Summary

### Verified
- HTTP inbox recipient scope (GET/List/MarkRead)
- Cursor pagination (Base64 JSON encoding)
- Mark-read idempotency and version checking
- Outbox consumer replay protection
- Producer→outbox linkage (survey assignment)
- Lease acquisition at repository boundary

### NOT_VERIFIED (Critical)
- **No dispatcher found:** No BackgroundService calling IOutboxWorkRepository.TryLeaseNextAsync
- Outbox lease/retry loop not exercised in production
- Real notification delivery timing
- DeadLetter transition via dispatcher

### Out of Scope
- Dispatcher implementation (F work)
- External consumers (Web/Android/AI)
- Reports/retention/delete (Q-RF02-08)

---

**Manifest Version:** 1.0  
**Created:** 2026-10-01  
**Total Files:** 11 (10 content + 1 manifest)  
**Total Size:** ~62 KB (approximate, excluding manifest)
