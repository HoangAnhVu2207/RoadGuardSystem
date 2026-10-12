# Workflow authentication và accounts

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## EXPECTED_BUSINESS_BEHAVIOR

TARGET_CONFIRMED: persistent login mới trong HUY-FINAL-INTEGRATION supersede PR37 ordinary idle/max login bounds. JWT/ticket hữu hạn vẫn renew/refresh; account suspended/revoked không tiếp tục, legacy expired không resurrect. Web cookie/server session, Android access/refresh. OTP10 phút/5 attempts; resend≥60s,≤3/15phút. Token hết hạn không xóa offline; reconnect kiểm current rights.

## Actor, preconditions, API và DTO

Credentials đúng, user Active/role Active. Web GET /api/v1/auth/web/csrf → POST login+CSRF → GET session → POST renew khi ticket hết → POST logout+CSRF+Idempotency-Key. Android POST /auth/android/login → /auth/android/refresh. Compatibility /auth/login,/auth/refresh còn tồn tại. LoginRequestDto/RefreshRequestDto/AuthTokenResponseDto/AuthActorResponseDto là bearer wire. Web body user(id/displayName/role/version), mustChangePassword, issuedAt, absoluteExpiresAt, idleExpiresAt, renewable, ticketLifetimeSeconds; không bearer/refresh plaintext.

Forced-password-change dùng old credential+OperationId; authenticated change-password có Idempotency-Key. Password recovery requests trả202 generic; Supervisor account update/reset qua UsersController. Reporter registration/invitation onboarding riêng, không tự đổi Reporter thành staff. Endpoint catalog giữ exact route/DTO/status từng onboarding operation. Onboarding source gọi IIdentityMessageSender trực tiếp sau persistence thành công; DI dùng GmailIdentityMessageSender. Không phải email outbox dispatcher, không có real delivery acceptance ở task này.

## Main flow/state/persistence

AuthService credential verifier → Active account/role → UserSession PersistentRenewable(ExpiresAt=null,IssuedRole,Transport) + random refresh lưu hash → transactional issuance kiểm user version → token response. Web persistent ticket12h, protected renewal cookie MaxAge365d. Server new session không ordinary30m idle/12h absolute cap. Renew giữ same session/credential, kiểm owner/transport/lifecycle/role/account; Android refresh rotation khác Web renew.

State invalid credentials→401; MustChangePassword chỉ minimal paths; active→explicit logout/security/account/role revocation→revoked. LegacyBounded kiểm legacy bounds; expired không tự persistent. LastActivity telemetry không là removed idle timeout. Profile/account rowversion khác auth operation receipts.

## Alternative / concurrency

Refresh optional operation key bị giới hạn printable/length; ACK-loss retry dùng protected receipt/current checks. Replayed revoked refresh thiếu operation key có thể revoke family. Password changes/reset/status/role/logout theo repository locks và revoke boundary, không cấp PM quyền quản trị tài khoản. 400 invalid/mixed actor;401 invalid session/token/credential;403 password-change-required/CSRF;409 conflict/idempotency.

## Reporter OTP và invitation: vertical flow hiện hành

| Operation v1 | DTO / actor | Flow / durable effect |
|---|---|---|
| POST /auth/reporter-registrations | RegisterReporterRequestDto, anonymous, Idempotency-Key | Normalize email/password policy/reporter type → Pending user/password hash + ReporterRegistrationIntent(hash/generation/expiry/resend) + receipt → direct OTP sender;202 RegistrationIntentResponseDto(PENDING_VERIFICATION,resendAfterSeconds) |
| POST /auth/reporter-registrations/verify | VerifyReporterOtpRequestDto, anonymous,key | Intent exists/unconsumed/unexpired/attempts under max → compare OTP hash → Active Reporter/session+refresh and receipt →200 AuthTokenResponseDto |
| POST /auth/reporter-registrations/resend | ResendReporterOtpRequestDto, anonymous,key | Cooldown/rate window/current intent → new OTP generation/hash, reset attempts/expiry/resend time+receipt → direct email;202 |
| POST /invitations | CreateInvitationRequestDto, current Supervisor,key | Normalize email/role/project IDs → StaffInvitation/token hash/project pins/receipt → direct invitation sender;201 InvitationResponseDto+ETag |
| POST /invitations/accept | AcceptInvitationRequestDto, anonymous possession token,key | Unrevoked/unexpired invitation/current pinned sources → activate invited staff/membership/session+refresh/consume invitation/receipt →200 AuthTokenResponseDto |

OTP defaults có source10 phút/5 attempts/60s/3resends15phút. Repo wrong OTP tăng FailedAttempts với concurrency recovery; expired/consumed/rate-limited không activate. Resend rate dùng durable idempotency records trong window. InvitationLifetimeHours default72 là implementation option, không tự thành chính sách hạn mức mới ngoài scope. Email Conflict409,duplicate request409,invalid400,not-found404,too-many429,delivery unavailable503. Persistence có thể đã thành công trước sender503; same-key retry không mặc định gửi lại email, client cần follow resend/available recovery theo source. SMTP external không transactional SQL; không diễn giải503 là user/intent chắc chắn chưa tạo.

## Gaps

UNKNOWN: real OTP mail, browser TLS/proxy/cookie restart, Android secure persistence/ACK-loss trên device. PROPOSED: isolated client acceptance expired ticket renew và revoked retry denial, không tái áp PR37 legacy cap lên new sessions. Browser mất actual renewal credential vẫn có thể cần login. Tests NOT_RUN task này.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [AuthService.cs](../../../RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs) | `AuthService` :13 | CURRENT_VERIFIED source |
| [WebAuthController.cs](../../../RoadGuardSystem.API/Controllers/WebAuthController.cs) | `WebAuthController` :23 | CURRENT_VERIFIED source |
| [AuthController.cs](../../../RoadGuardSystem.API/Controllers/AuthController.cs) | `AuthController` :19 | CURRENT_VERIFIED source |
| [UsersController.cs](../../../RoadGuardSystem.API/Controllers/UsersController.cs) | `UsersController` :18 | CURRENT_VERIFIED source |
| [IdentityRepository.WebSessions.cs](../../../RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.WebSessions.cs) | `IdentityRepository` :7 | CURRENT_VERIFIED source |
| [IdentityRepository.RefreshTokens.cs](../../../RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.RefreshTokens.cs) | `IdentityRepository` :9 | CURRENT_VERIFIED source |
| [IdentityRepository.PasswordChanges.cs](../../../RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.PasswordChanges.cs) | `IdentityRepository` :7 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [HuyFinalCookieHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Authentication/HuyFinalCookieHttpTests.cs)
- [AuthenticationSessionFlowTests.cs](../../../tests/RoadGuardSystem.ApiTests/Authentication/AuthenticationSessionFlowTests.cs)

## Symbol checkpoints

- [AuthService.cs](../../../RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs) :49 — `LoginAsync` (`CURRENT_VERIFIED` source).
- [IdentityOnboardingService.cs](../../../RoadGuardSystem.Services/Implementations/Authentication/IdentityOnboardingService.cs) :50 — `RegisterReporterAsync` (`CURRENT_VERIFIED` source).
- [IdentityOnboardingRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Identity/IdentityOnboardingRepository.cs) :182 — `VerifyReporterOtpAsync` (`CURRENT_VERIFIED` source).
- [GmailIdentityMessageSender.cs](../../../RoadGuardSystem.Services/Implementations/Authentication/GmailIdentityMessageSender.cs) :18 — `SendReporterOtpAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
