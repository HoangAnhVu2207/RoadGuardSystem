# Configuration và dependency injection

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Composition root

CURRENT_VERIFIED: Program thêm optional appsettings.Development.local.json chỉ Development; AddApiPlatformServices và AddRoadGuardSeeding. InitializeOnStartup chỉ Development+flag. Development users cần Development+SeedDevelopmentUsers. Docs không chạy initializer/seed/migration.

AddApiPlatformServices bind/validate Jwt, register RoadGuardUser selector/bearer/AI bearer/cookie/antiforgery/services. Persistence extensions giữ scoped repositories/DbContext; TimeProvider nguồn clock. Interface name không chứng minh implementation cũ còn active.

| Seam | Registration hiện tại | Hệ quả |
|---|---|---|
| IStaffReadService/Repository | StaffReadService/StaffReadRepository | New GET current authority |
| IProjectListRepository | ProjectListReadModel | Global Supervisor, worker assignments |
| INotificationRepository/operations | Cùng scoped H6ProtectedNotificationRepository | Own recipient + protected source |
| IH6NotificationSourceAdapter | FIELD/repair/deadline/weekly/safety enumerable | Mỗi source cần proof |
| Repair workflow/producer/execution/offline adapter | Cùng scoped RepairWorkflowRepository | Same core guards |
| IFileContentStore | LocalFileContentStore singleton | Local content |
| IUploadObjectStorage | MinioUploadObjectStorage singleton | Multipart verified source |
| IAnh02ArtifactStore | MinioAnh02ArtifactStore | Export/mock artifacts |

## KEY names, không secret values

Jwt: Issuer, Audience, ActiveKeyId, SigningKeys, AccessTokenLifetimeMinutes, SessionLifetimeHours, RefreshTokenLifetimeDays. Nhóm RoadGuardDatabase, FileStorage, SessionDeviceMetadata, Anh02 và Anh02:Export. MinioStorage: Endpoint/BucketName/AccessKey/SecretKey/UseSsl. UploadSession: PartSizeBytes/SessionLifetimeHours/SignedPutUrlLifetimeMinutes/RecoveryEnabled/RecoveryDeadlineSeconds/RecoveryRetrySeconds/RecoveryBatchSize. Không copy config values/connection strings/credentials/PII.

## Auth/policy/lifetime

Default RoadGuardUser selector ưu tiên Authorization bearer; cookie chỉ eligible metadata/whitelist. RoadGuardWeb cookie, AI callback dedicated bearer+client_type. X-CSRF-TOKEN antiforgery Secure host cookie. Hosted singleton tạo scope; export heartbeat scope riêng tránh concurrent DbContext.

## Authentication configuration bổ sung

KEY names: PasswordChangeFingerprint:Key; IdentityOnboarding:Secret/OtpLifetimeMinutes/OtpMaxAttempts/OtpResendCooldownSeconds/OtpMaxResendsPerWindow/OtpResendWindowMinutes/InvitationLifetimeHours/FrontendBaseUrl/BrandName/SenderDisplayName/SmtpHost/SmtpPort/SmtpUsername/SmtpPassword/LogoPath. AuthenticationServiceCollectionExtensions validate-on-start Jwt và PasswordChangeFingerprint, bind IdentityOnboarding, register GmailIdentityMessageSender. Không cung cấp giá trị secret/SMTP thật.

## Gaps

UNKNOWN: deployed secrets injection/DataProtection keyring/proxy/worker flags/storage permissions. AddCookie không chứng minh shared durable keyring. PROPOSED: isolated startup options/restart/replica renewal/revocation acceptance. FIELD Purpose generated-default sentinel warning cần giữ là configuration/runtime limitation, chưa business failure được xác minh; không sửa model trong docs.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [Program.cs](../../../RoadGuardSystem.API/Program.cs) | `Program` :100 | CURRENT_VERIFIED source |
| [ServiceCollectionExtensions.cs](../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs) | `ServiceCollectionExtensions` :39 | CURRENT_VERIFIED source |
| [RoadGuardPersistenceExtensions.cs](../../../RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs) | `RoadGuardPersistenceExtensions` :32 | CURRENT_VERIFIED source |
| [Huy01ReporterPersistenceExtensions.cs](../../../RoadGuardSystem.Repositories/Extensions/Huy01ReporterPersistenceExtensions.cs) | `Huy01ReporterPersistenceExtensions` :8 | CURRENT_VERIFIED source |
| [JwtOptions.cs](../../../RoadGuardSystem.Services/Options/JwtOptions.cs) | `JwtOptions` :5 | CURRENT_VERIFIED source |
| [MinioStorageOptions.cs](../../../RoadGuardSystem.Repositories/Options/MinioStorageOptions.cs) | `MinioStorageOptions` :3 | CURRENT_VERIFIED source |
| [UploadSessionOptions.cs](../../../RoadGuardSystem.Repositories/Options/UploadSessionOptions.cs) | `UploadSessionOptions` :3 | CURRENT_VERIFIED source |
| [WebCookieConfiguration.cs](../../../RoadGuardSystem.API/Authentication/WebCookieConfiguration.cs) | `WebCookieConfiguration` :10 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Symbol checkpoints

- [AuthenticationServiceCollectionExtensions.cs](../../../RoadGuardSystem.Services/Extensions/AuthenticationServiceCollectionExtensions.cs) :18 — `AddRoadGuardAuthenticationApplication` (`CURRENT_VERIFIED` source).
- [IdentityOnboardingOptions.cs](../../../RoadGuardSystem.Services/Options/IdentityOnboardingOptions.cs) :5 — `SectionName` (`CURRENT_VERIFIED` source).
- [PasswordChangeFingerprintOptions.cs](../../../RoadGuardSystem.Services/Options/PasswordChangeFingerprintOptions.cs) :7 — `SectionName` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
