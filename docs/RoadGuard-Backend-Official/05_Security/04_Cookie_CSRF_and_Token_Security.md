# Cookie, CSRF và token security

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Cookie transport

Session __Host-RoadGuardSession: HttpOnly/Secure Always/SameSite Lax/Path=/, SlidingExpiration=false, ticket12h. Renewal __Host-RoadGuardRenewal: HttpOnly/Secure/Lax/Path=/, MaxAge365d, IDataProtector purpose RoadGuard.WebRenewal.v1. Browser MaxAge không server lifecycle expiry. CSRF cookie __Host-RoadGuardCsrf Secure/HttpOnly/Lax/Path=/; header X-CSRF-TOKEN.

Selector ưu tiên Authorization header. Cookie chỉ exact endpoint WebCookieEligible hoặc legacy whitelist, không broad projects prefix. New staff users/members/primary GET, Projects.List và account detail có marker nguồn. Legacy inbox chỉ GET list/get và POST GUID/read; inspection-tasks GET. Không suy cookie cho mọi verb/version; root catalog giữ exhaustive operation metadata.

## Main flow, CSRF và alternative

GET web/csrf anonymous có thể authenticate existing cookie để bind requestToken, no-store. Web login/renew/logout tự validate antiforgery; renew từ chối Authorization. Unsafe cookie business call ngoài GET/HEAD/OPTIONS qua middleware CSRF. Bearer không tự bị cookie CSRF. Eligible authenticated bearer+valid cookie khác actor→400 validation_error; same actor vẫn business checks.

WebCookieActivityFilter sau successful eligible result touch authoritative state, có thể replace result401 khi revoked; skip OPTIONS/failures/web-auth/change-password riêng. Minimal MustChangePassword exception không full session quyền. Auth responses no-store, export content private,no-store.

## Persistence, lifecycle và concurrency

DB giữ refresh hash; protected renewal không plaintext JS response. Android rotation/idempotent recovery không token reuse vô hạn. Current user/role/session/transport checked và family revoked theo repository; logout replay exception hẹp. Jwt options validate startup, docs chỉ KEY names. DataProtection cookie/renewal keys khác JWT signing keys. Server new persistent session không ordinary30m/12h/30d cap; finite ticket renewal vẫn cần actual browser credential.

## Gaps

UNKNOWN shared durable DataProtection keyring, TLS termination, real cross-origin cookie/proxy/restart/replica/rotation. SameSite Lax không thay CSRF. PROPOSED browser TLS/login/renew/mixed actor/revocation/keyring-restart acceptance; AddCookie không chứng minh CORS/proxy. Tests NOT_RUN; no external deployment certification.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [WebCookieConfiguration.cs](../../../RoadGuardSystem.API/Authentication/WebCookieConfiguration.cs) | `WebCookieConfiguration` :10 | CURRENT_VERIFIED source |
| [WebAuthController.cs](../../../RoadGuardSystem.API/Controllers/WebAuthController.cs) | `WebAuthController` :23 | CURRENT_VERIFIED source |
| [WebCookieRequestMiddleware.cs](../../../RoadGuardSystem.API/Middlewares/WebCookieRequestMiddleware.cs) | `WebCookieRequestMiddleware` :11 | CURRENT_VERIFIED source |
| [WebCookieActivityFilter.cs](../../../RoadGuardSystem.API/Authentication/WebCookieActivityFilter.cs) | `WebCookieActivityFilter` :11 | CURRENT_VERIFIED source |
| [ServiceCollectionExtensions.cs](../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs) | `ServiceCollectionExtensions` :39 | CURRENT_VERIFIED source |
| [StaffDirectoryController.cs](../../../RoadGuardSystem.API/Controllers/StaffDirectoryController.cs) | `StaffDirectoryController` :19 | CURRENT_VERIFIED source |
| [ProjectsController.cs](../../../RoadGuardSystem.API/Controllers/ProjectsController.cs) | `ProjectsController` :19 | CURRENT_VERIFIED source |
| [UsersController.cs](../../../RoadGuardSystem.API/Controllers/UsersController.cs) | `UsersController` :18 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [HuyFinalCookieHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Authentication/HuyFinalCookieHttpTests.cs)
- [AuthenticationSessionFlowTests.cs](../../../tests/RoadGuardSystem.ApiTests/Authentication/AuthenticationSessionFlowTests.cs)
- [StaffMembershipReadHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Projects/StaffMembershipReadHttpTests.cs)

## Symbol checkpoints

- [WebCookieConfiguration.cs](../../../RoadGuardSystem.API/Authentication/WebCookieConfiguration.cs) :17 — `SelectScheme` (`CURRENT_VERIFIED` source).
- [WebCookieRequestMiddleware.cs](../../../RoadGuardSystem.API/Middlewares/WebCookieRequestMiddleware.cs) :13 — `InvokeAsync` (`CURRENT_VERIFIED` source).
- [WebAuthController.cs](../../../RoadGuardSystem.API/Controllers/WebAuthController.cs) :63 — `Renew(` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.

## Central fresh execution update

| Class | PASS | FAIL | Limit |
|---|---|---|---|
| StaffMembershipReadHttpTests | 17 | 0 | Selected actual cases only; no all-route/E2E certification |

Các test khác không trong executed-tests.json vẫn NOT_RUN.
