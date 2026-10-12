# Authentication và authoritative session

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Expected và schemes

TARGET_CONFIRMED: secure Web cookie/server session, Android access/refresh; persistent login mới supersede ordinary login bounds, explicit security revocation giữ nguyên. CURRENT_VERIFIED: RoadGuardUser chọn bearer nếu có Authorization; cookie chỉ eligible request có session cookie. Dedicated AI bearer/policy client_type, không user-cookie privilege.

JWT kiểm signature/issuer/audience/lifetime; AuthoritativeSessionValidator kiểm sub/sid/issued role/current user/session/active role/transport. Token chưa hết vẫn deny khi suspended, role changed/inactive, revoked hoặc MustChangePassword ngoài minimal exceptions. Cookie OnValidatePrincipal dùng validator transport Web; malformed claims reject;401/403 thay redirect HTML.

## Credentials, main flow/state và persistence

AuthService verifier→Active account/role→transaction issuance với user version→new PersistentRenewable session(nullable ExpiresAt,IssuedRole,Transport)+refresh hash→finite access. Random refresh plaintext chỉ gửi token client; repository giữ hash. Finite JWT/Web ticket không là indefinite bearer. Web protected renew và Android rotation giữ server persistent lifecycle. LegacyBounded giữ legacy validity; không revive expired/revoked.

Web renew hash protected credential để tìm owner, kiểm transport/lifecycle/current account/role; valid existing cookie owner/sid phải khớp renewal. Authorization header ở renew bị400. Successful eligible response touch current session; LastActivity telemetry không idle cap cho persistent lifecycle. Logout committed receipt replay exception hẹp không mở lại business session revoked.

## Account operations và errors

MustChangePassword chỉ minimal me/session/csrf/change/logout paths trong scheme source. Password change/reset/status/role/logout đi repository revoke/receipt guards; không suy quyền từ biết ID. Account detail StaffReadService Supervisor-only; auth actor response không contacts. Recovery generic202 hạn chế enumeration.

401 credentials/token/session;403 permission/password-change/CSRF;400 malformed/mixed actor;409 security concurrency/idempotency theo action. Không log password/token/keys. DTO bearer và web anonymous shape khác, client phải dùng đúng transport.

## Gaps / tests

UNKNOWN: deployed issuer/audience/key rotation/keyring/clock/proxy, real browser/mail/device persistence. PROPOSED isolated expired-ticket renew/revoked-family retry/current-role change/eligible method-version/mixed actor acceptance. Files dưới đây đối chiếu theo executed-tests.json; unselected NOT_RUN, không coi historical HTTP audit là fresh certification.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [JwtBearerConfiguration.cs](../../../RoadGuardSystem.API/Authentication/JwtBearerConfiguration.cs) | `JwtBearerConfiguration` :15 | CURRENT_VERIFIED source |
| [AiServiceJwtBearerConfiguration.cs](../../../RoadGuardSystem.API/Authentication/AiServiceJwtBearerConfiguration.cs) | `AiServiceJwtBearerConfiguration` :8 | CURRENT_VERIFIED source |
| [WebCookieConfiguration.cs](../../../RoadGuardSystem.API/Authentication/WebCookieConfiguration.cs) | `WebCookieConfiguration` :10 | CURRENT_VERIFIED source |
| [AuthService.cs](../../../RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs) | `AuthService` :13 | CURRENT_VERIFIED source |
| [AuthoritativeSessionValidator.cs](../../../RoadGuardSystem.Services/Implementations/Authentication/AuthoritativeSessionValidator.cs) | `AuthoritativeSessionValidator` :15 | CURRENT_VERIFIED source |
| [IdentityRepository.WebSessions.cs](../../../RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.WebSessions.cs) | `IdentityRepository` :7 | CURRENT_VERIFIED source |
| [WebAuthController.cs](../../../RoadGuardSystem.API/Controllers/WebAuthController.cs) | `WebAuthController` :23 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [HuyFinalCookieHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Authentication/HuyFinalCookieHttpTests.cs)
- [AuthenticationSessionFlowTests.cs](../../../tests/RoadGuardSystem.ApiTests/Authentication/AuthenticationSessionFlowTests.cs)

## Symbol checkpoints

- [AuthoritativeSessionValidator.cs](../../../RoadGuardSystem.Services/Implementations/Authentication/AuthoritativeSessionValidator.cs) :24 — `ValidateAsync` (`CURRENT_VERIFIED` source).
- [IdentityRepository.WebSessions.cs](../../../RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.WebSessions.cs) :40 — `RenewWebSessionAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
