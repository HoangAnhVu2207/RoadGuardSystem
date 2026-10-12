# Deployment configuration

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Phạm vi chứng cứ

CURRENT_VERIFIED Program/DI/options source, không deployed acceptance. /health liveness không DB/object/AI readiness. Swagger Development-only; source-derived OpenAPI nhãn riêng nếu chưa API Explorer. Không secret values/connection strings/real recipient/PII.

| KEY group | Vai trò | Acceptance cần |
|---|---|---|
| RoadGuardDatabase | persistence/initialize/seed flags | isolated target/model-migration parity; no development seed production |
| Jwt:Issuer/Audience/ActiveKeyId/SigningKeys | trust/rotation | startup validation/current session revoke |
| Jwt:*Lifetime* | finite access/legacy options | không ép persistent session theo legacy cap |
| MinioStorage:Endpoint/BucketName/AccessKey/SecretKey/UseSsl | object client | TLS/scoped ACL/presign/checksum/recovery |
| FileStorage | local root/limits | durable mount/permissions/path |
| UploadSession:* | multipart/lifetime/recovery | registered worker/state/idempotency |
| Anh02:WorkersEnabled/MockEnabled/Export | exports/retention/mock | gated environment/lease/retry/queue |
| SessionDeviceMetadata | metadata bounds | no secret/PII leakage |

Optional Development.local không production secret injection. InitializeOnStartup Development+flag, không authorization apply migrations shared/team DB. SeedDevelopmentUsers Development+flag. H6/Validation always hosted registered; upload verification Development+endpoint; recovery explicit flag; mock additional Development/Testing/Test. Registration không actual host liveness.

## Auth/TLS/replica

Secure host cookies yêu cầu HTTPS. Proxy/forwarded headers/origins/CORS/DataProtection persistent shared keyring phải thiết kế/kiểm deployment; AddCookie không chứng minh configured. Protected renewal/retry phụ thuộc key continuity; không disable validation hoặc plaintext key sharing để restart chạy. Signing keys và DataProtection keyring khác responsibility.

## Authentication configuration bổ sung

KEY names PasswordChangeFingerprint:Key; IdentityOnboarding:Secret/OtpLifetimeMinutes/OtpMaxAttempts/OtpResendCooldownSeconds/OtpMaxResendsPerWindow/OtpResendWindowMinutes/InvitationLifetimeHours/FrontendBaseUrl/BrandName/SenderDisplayName/SmtpHost/SmtpPort/SmtpUsername/SmtpPassword/LogoPath. Không đưa giá trị credentials.

## Recovery/operational gaps

UNKNOWN host/replicas/live SQL/buckets/ACL/mail/provider/keyring/alerts. PROPOSED isolated startup/readiness/current-session-CSRF smoke/lease recovery/consistent restore. RPO≤15min/RTO≤4h/p95≤2s50users TARGET_CONFIRMED, không benchmark/drill là NOT_VERIFIED. FIELD Purpose generated-default sentinel warning là runtime configuration limitation, chưa verified business failure. Docs không config edit/deploy/live calls.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [Program.cs](../../../RoadGuardSystem.API/Program.cs) | `Program` :100 | CURRENT_VERIFIED source |
| [ServiceCollectionExtensions.cs](../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs) | `ServiceCollectionExtensions` :39 | CURRENT_VERIFIED source |
| [RoadGuardPersistenceExtensions.cs](../../../RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs) | `RoadGuardPersistenceExtensions` :32 | CURRENT_VERIFIED source |
| [JwtOptions.cs](../../../RoadGuardSystem.Services/Options/JwtOptions.cs) | `JwtOptions` :5 | CURRENT_VERIFIED source |
| [FileStorageOptions.cs](../../../RoadGuardSystem.Repositories/Options/FileStorageOptions.cs) | `FileStorageOptions` :3 | CURRENT_VERIFIED source |
| [MinioStorageOptions.cs](../../../RoadGuardSystem.Repositories/Options/MinioStorageOptions.cs) | `MinioStorageOptions` :3 | CURRENT_VERIFIED source |
| [UploadSessionOptions.cs](../../../RoadGuardSystem.Repositories/Options/UploadSessionOptions.cs) | `UploadSessionOptions` :3 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Symbol checkpoints

- [Program.cs](../../../RoadGuardSystem.API/Program.cs) :49 — `InitializeOnStartup` (`CURRENT_VERIFIED` source).
- [AuthenticationServiceCollectionExtensions.cs](../../../RoadGuardSystem.Services/Extensions/AuthenticationServiceCollectionExtensions.cs) :18 — `AddRoadGuardAuthenticationApplication` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
