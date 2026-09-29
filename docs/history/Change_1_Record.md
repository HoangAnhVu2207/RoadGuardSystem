# Change 1 Record — RoadGuard target synchronization

Ngày ghi nhận: 22/09/2026  
Phạm vi: kiểm tra readiness của hai execution plan và bổ sung Reporter self-registration bằng Gmail OTP.  
Trạng thái: tài liệu/plan đã cập nhật; runtime chưa triển khai phần mới.

Phạm vi cấp dự án dựa trên phiếu khảo sát ban đầu được chuẩn hóa tại [RoadGuard_Project_Scope.md](RoadGuard_Project_Scope.md). `RoadGuard_Backend_Scope.md` là phạm vi của riêng backend, không phải toàn bộ capstone.

`RoadGuard_Project_Scope.md` cũng có bảng các điểm khách hàng dễ hiểu là In Scope, gồm drone control, giấy phép bay, thiết bị/GCP, photogrammetry accuracy, AI training, legal liability, integrations, provider quota, offline, migration dữ liệu cũ, SLA, backup/DR, security certification, user training và các yêu cầu vận hành. Đây là danh sách phòng ngừa scope creep, không phải cam kết bổ sung.

## Kết luận readiness

Hai plan đang đúng hướng kiến trúc: `Controller -> IService -> IRepository`, Person 1 giữ API/Service/DTO, Person 2 giữ entity/DbContext/migration/SQL, và đã có các bậc build/test/HTTP smoke. Tuy nhiên, **chưa thể gọi toàn bộ target hiện tại là build-ready** vì code checkout hiện chỉ có login/refresh/logout/profile và chưa có:

- role `REPORTER` trong runtime enum/seed;
- endpoint register/verify/resend;
- email sender/Gmail adapter và config/secret contract;
- bảng challenge OTP và trạng thái email đã xác minh;
- test đăng ký, chống enumeration, rate-limit, replay, concurrency và provider failure.

Plan chỉ sẵn sàng để bắt đầu hai slice mới khi hoàn thành dependency `P2-13 -> P1-13`. Các dòng `Done` của P1-10/P2-10 vẫn giữ đúng bằng chứng lịch sử, không được dùng làm bằng chứng cho Gmail OTP.

## Thay đổi sản phẩm

1. Reporter (Citizen hoặc InvestorRepresentative) được tự đăng ký bằng Gmail `gmail.com` hoặc `googlemail.com`.
2. Registration tạo User `PENDING`, role cố định `REPORTER`, `email_confirmed = false`, không tạo ProjectMember và không cấp token.
3. Backend gửi OTP qua adapter Gmail. OTP chỉ lưu hash/HMAC, expiry, attempt count, cooldown, consume time và provider correlation ID.
4. OTP hợp lệ được consume một lần trong transaction; User chuyển `ACTIVE`, email được xác minh và hệ thống có thể cấp token pair chuẩn.
5. OTP sai, hết hạn, dùng lại, sai purpose hoặc vượt giới hạn không kích hoạt tài khoản. Resend trước cooldown bị chặn.
6. Phản hồi đăng ký không tiết lộ email đã tồn tại, trạng thái account hoặc chi tiết provider.
7. Gmail OTP là email verification; Google OAuth/Google Sign-In vẫn ngoài phạm vi.

## Thay đổi tài liệu và kế hoạch

| File | Thay đổi |
|---|---|
| `docs/adr/002-authentication.md` | Bổ sung amendment, pending account, OTP security, register/verify/resend, Gmail adapter boundary và task P1-13/P2-13; phân biệt Gmail OTP với OAuth. |
| `docs/diagram/RoadGuard_Data_Dictionary_v1.md` | Bổ sung email confirmation/registration source và `EmailVerificationChallenge`; OTP plaintext/provider secret bị cấm lưu. |
| `docs/diagram/RoadGuard_ERD_v1.md` | Bổ sung quan hệ User–EmailVerificationChallenge và các thuộc tính OTP chính. |
| `docs/diagram/RoadGuard_Domain_Model_v1.md` | Bổ sung invariant Gmail-only, pending Reporter, activation sau OTP và challenge bảo mật. |
| `docs/diagram/Dac_ta_UseCase_v2.md` | Bổ sung CN11/CN12, actor Reporter self-registration và điều kiện verify trước login/report. |
| `docs/diagram/User_Stories_Acceptance_Criteria_v2.md` | Bổ sung US-27 với acceptance criteria register, OTP, resend, replay, rate-limit, idempotency và audit. |
| `docs/api-errors.md` | Đăng ký error codes cho Gmail validation, OTP invalid/expired/locked/cooldown, email verification required và provider unavailable. |
| `docs/RoadGuard_Backend_Scope.md` | Ghi Reporter Gmail self-registration vào In Scope và OAuth vào Out of Scope. |
| `docs/README.md` | Cập nhật bản đồ tài liệu có Gmail self-registration. |
| `docs/hotfix/RoadGuard_Workflow_Hotfix_Plan_v1.md` | Bổ sung HF-02A đến HF-02D và mapping P1-13/P2-13. |
| `planning/RoadGuard_Plan_Person_1.md` | Thêm P1-13, readiness assessment, release checkpoint và coverage US-27. |
| `planning/RoadGuard_Plan_Person_2.md` | Thêm P2-13, readiness assessment, persistence acceptance và handoff trước P1-13. |
| `.agents/skills/roadguard-endpoint-delivery/SKILL.md` | Giữ rule auth/Reporter/OTP trong context ngắn cho các slice endpoint sau này. |

## Dependency và slice triển khai

| Thứ tự | Slice | Owner | Kết quả bắt buộc |
|---:|---|---|---|
| 1 | P2-13 | Person 2 | Role catalog, User confirmation fields, challenge schema/migration, SQL constraints and tests. |
| 2 | P1-13 | Person 1 | DTO/service/controller, Gmail adapter/fake, rate-limit/idempotency, auth issuance and `.http` smoke. |
| 3 | HF-02D | Both | Outbox/provider config, secrets, FE contract, integration/release evidence. |

Không thêm package Gmail, migration hoặc secret vào checkout chỉ vì Change 1 Record. Package/provider cụ thể phải được ghi trong scope card của P1-13/HF-02D và được owner duyệt.

## Hợp đồng API mục tiêu

- `POST /api/v1/auth/reporter/register`: anonymous; nhận Gmail, display name, ReporterType, password, confirm password, idempotency key; trả registration intent/status và không trả token.
- `POST /api/v1/auth/reporter/verify-email`: anonymous; nhận intent + OTP; consume một lần, activate account và có thể trả token pair.
- `POST /api/v1/auth/reporter/resend-otp`: anonymous; nhận intent; kiểm tra cooldown/rate-limit, vô hiệu challenge cũ và gửi challenge mới.

Stable errors: `reporter_email_invalid`, `reporter_otp_invalid`, `reporter_otp_expired`, `reporter_otp_locked`, `reporter_otp_resend_cooldown`, `reporter_email_verification_required`, `email_provider_unavailable`.

## Kiểm tra đã thực hiện

- Đã đọc source auth hiện có: `AuthController` hiện chỉ có login/refresh/forced-password-change/logout; `UserRoleCode` runtime mới có giá trị 1–4; seed hiện có bốn role; `UserStatus.Pending` đã tồn tại.
- Đã kiểm tra dependency hiện có: không có Gmail register/OTP endpoint hoặc challenge persistence trong source hiện tại.
- Tài liệu sau record phải chạy link check, fence/placeholder check, `git diff --check`; code slice sau này phải chạy build project, đúng một test breadth, `.http` smoke và SQL Server/Testcontainers khi có migration.

## Không thay đổi

- Không sửa lịch sử `Done` hoặc worklog cũ.
- Không đổi workflow IncidentCase/segment/AI đã ghi trong các tài liệu mục tiêu.
- Không đưa cost, ngân sách hoặc dữ liệu tài chính trở lại phạm vi.
