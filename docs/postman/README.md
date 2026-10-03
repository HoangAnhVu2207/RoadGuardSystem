# Chạy thử RoadGuard API bằng Postman

## HUY-01 current opt-in workflows (2026-10-04)

Collection now includes `HUY-01 AI, reporting and export - opted-in`,
`HUY-01 Case lifecycle - opted-in`, `HUY-01 current authority - opted-in`,
and `HUY-01 candidate and label - opted-in`. The first three preserve the
scoped Anh handoff requests; the last follows current Huy AI candidate and
manual label DTOs. They are disabled by default with `anh02Enabled`,
`huyLifecycleAndCaseEnabled`, and `huyCandidateEnabled`. Set these only on a
disposable local project with verified survey/video/model and the appropriate
role tokens. The AI folder captures detection ID; the trusted metadata
request captures source version and frame file ID for Candidate/Label.

State-changing requests use idempotency keys. Keep the same key only for an
exact replay, clear `huyCandidateKey` before a distinct mutation, and keep
the stored `If-Match` for label review. A full collection run does not prove
these opt-in workflows. JSON parsing was checked; live Postman/network,
real MinIO, and external AI provider acceptance remain separate gates.

This section supersedes the historical route-count and verification snapshot
below; those counts describe the earlier collection, not the current HUY-01
folders. HUY-02 retention/hold/evaluation requests were not imported.

## Vì sao đăng nhập không được

API tại `http://localhost:5112` đang chạy: `GET /health` trả `200 Healthy`, và OpenAPI v1 trả được danh sách endpoint. Nhưng login seed Supervisor hiện trả `401 auth_invalid_credentials`.

Đã kiểm tra trực tiếp database Development `RoadGuardPostmanTest`: tài khoản seed Supervisor bị đổi role từ `SUPERVISOR` sang `DRONE_OPERATOR`, đổi trạng thái từ `ACTIVE` sang `SUSPENDED`, và mật khẩu không còn khớp fixture. Audit trail ghi nhận `auth_password_changed` lúc 07:51 UTC và `user_account_updated` lúc 07:58 UTC. Collection cũ đã tự chép user đăng nhập vào `targetUserId`; vì vậy chạy change-password/account-update/password-reset trong cùng collection có thể phá chính tài khoản đang dùng. Collection mới không còn tự gán `targetUserId` khi login và chặn thao tác destructive theo mặc định.

`PostmanUserSeedStep` chỉ tạo fixture còn thiếu và chỉ rehash mật khẩu nếu mật khẩu hiện tại hợp lệ; nó không khôi phục role, status hoặc mật khẩu đã đổi. Tôi không sửa/reset dữ liệu database. Để khôi phục full Supervisor flow, dùng một database local riêng dành cho Postman, giữ lại/backup DB hiện tại nếu cần dữ liệu, tạo lại riêng `RoadGuardPostmanTest`, rồi chạy API Development với initializer/seed đã bật. Không xóa database dùng chung/live. Sau đó chạy lại `00 - Preflight`.

Các fixture PM, Drone Operator và Repair Crew hiện vẫn có đúng role/status và mật khẩu fixture. Chúng dùng để thử luồng không cần quyền Supervisor trong lúc xử lý DB riêng.

## Import và chuẩn bị environment

1. Import `RoadGuardSystem-V2.postman_collection.json` và `RoadGuard.local.postman_environment.json`.
2. Chọn đúng environment `RoadGuard.local` ở góc trên bên phải. Nếu Postman đang có nhiều environment trùng tên, đổi tên hoặc xóa bản import thừa sau khi kiểm tra; đừng gửi request khi không rõ environment nào đang active.
3. `baseUrl` mặc định là `http://localhost:5112`. Local API phải đang chạy trên đúng port này.
4. Environment có sẵn email fixture `*.postman@example.test`, nhưng password field để trống. Nhập password seed Development vào Current Value của các biến `supervisorPassword`, `projectManagerPassword`, `operatorPassword`, `repairCrewPassword`. Không nhập chúng vào Initial Value, không lưu/export environment lên Git hoặc Postman workspace được chia sẻ.
5. Email thật chỉ dùng trong manual email scenarios. Điền chúng vào Current Value của `reporterRegistrationEmail`, `otpResendEmail`, `invitationEmail`, `recoveryEmail`; không đưa email, password, OTP, token hoặc invitation secret vào committed environment.

Mọi request hiện có dùng chung environment `RoadGuard.local`. File đã commit chỉ chứa URL local, fixture email test, IDs kiểm thử và placeholder rỗng cho bí mật. Các biến đã được gom vào cùng environment để chúng hiện đủ trong Postman, gồm role credentials/tokens, fixture IDs, row versions, OTP/invitation values, idempotency keys và các cờ bảo vệ.

## Thứ tự test theo role

Chạy từng folder bằng Collection Runner theo thứ tự dưới đây. `00 - Preflight` kiểm tra health và đăng nhập bốn fixture; nó sẽ báo lỗi ở Supervisor cho tới khi DB test được khôi phục.

| Folder | Luồng | Điều kiện / kết quả |
|---|---|---|
| `00 - Preflight` | Health → login Supervisor → PM → Operator → Repair Crew | Tất cả fixture phải ACTIVE, role không bị đổi, password đúng; token được lưu tách theo role. |
| `01 - Supervisor flow` | Tạo project → gửi invitation → đọc/quản trị account | Create project trả `201`, lưu `createdProjectId`; invitation gửi email thật chỉ khi bật manual scenario. Account mutations bị chặn mặc định. |
| `02 - Project Manager flow` | Đọc work package → tạo survey plan/request → postpone | Cần project do Supervisor tạo cho work-package; survey có thể dùng fixture IDs được seed. |
| `03 - Repair Crew flow` | Đọc inspection tasks được giao | Cần fixture assignment và `repairCrewAccessToken`. |
| `04 - Reporter flow` | Đăng ký → đọc OTP → verify → resend/old OTP/latest OTP | Manual email; collection không tự chạy gửi thư nếu `manualEmailScenario` rỗng. Chạy request gửi thư riêng, không chạy cả folder liên tục. |
| `05 - Shared authentication and sessions` | Invalid login, refresh, recovery, change password, logout | Recovery cần `runRecovery=true`; change-password và logout cần mở cờ destructive. |
| `06 - Self-service profile` | `/me` và `/profile` | GET chạy bình thường; PATCH/PUT bị chặn theo mặc định vì sửa account/profile. |
| `07 - Drone Operator authorization checks` | Gọi các route Supervisor/Crew bằng Operator | Các request này phải trả `403`; chúng kiểm quyền chứ không phải happy path. |

Đăng nhập và tạo project không có endpoint duyệt project trong OpenAPI đang chạy. API hiện công bố tạo/cập nhật project, phân công PM, work-package, road section/version, warranty và survey; không giả định `approve project` đã được triển khai.

### Cách chạy lần lượt

1. Chạy `00 - Preflight`. Nếu login thất bại, dừng tại đó; không chạy các folder phụ thuộc token.
2. Chạy `01 - Supervisor flow`. Tạo project trước. Response thành công lưu `createdProjectId`; các retry tạo request mới có operation ID mới.
3. Chạy `02 - Project Manager flow` bằng token PM đã lấy từ Preflight. Request work-package dùng project vừa tạo; survey requests dùng fixture `projectId`, `roadSectionId` và `surveyPlanId`.
4. Chạy `03 - Repair Crew flow` để kiểm danh sách việc được giao. Chạy folder Operator riêng để xác nhận các role không đủ quyền nhận `403`.
5. Chạy Reporter/invitation thủ công theo hướng dẫn bên dưới. Chỉ sau đó chạy login/profile cho account mới.
6. Không chọn `Run collection` để gửi thư hoặc thử password reset. Manual mail request được bỏ qua mặc định. State-changing requests cần `runDestructive=true` và `disposableAccountConfirmed=YES`; không dùng account fixture hoặc account đang dùng cho scenario khác.

Trong Postman, mở **View → Show Postman Console** nếu request bị skip. Collection sẽ in lý do skip ở Console; request bị skip không tạo HTTP response.

## Reporter OTP và invitation bằng email thật

### Đăng ký Reporter

1. Kiểm tra email chưa có account. Set `manualEmailScenario=REPORTER_REGISTRATION`, điền `reporterRegistrationEmail`, `reporterPassword` và `reporterRegistrationKey` mới.
2. Gửi riêng `POST /auth/reporter-registrations - success`. Khi API nhận yêu cầu, mở mailbox và tự nhập OTP vào `reporterOtp` Current Value.
3. Gửi verify một lần. Khi thành công, account mới được xác minh; sau đó login bằng email/password đó.
4. Xóa OTP và đặt `manualEmailScenario` rỗng. Không chạy resend trong full collection.

### Resend OTP

Chọn một email pending riêng trong `otpResendEmail`, set `manualEmailScenario=OTP_RESEND`, chạy setup một lần, rồi chạy resend. Đọc OTP mới và thử verify mã cũ (phải bị từ chối), sau đó verify OTP mới. Giữ nguyên key và payload khi retry cùng một operation; operation resend mới phải dùng key mới. Cooldown là điều kiện có chủ đích, không lặp request liên tục.

### Invitation

Login Supervisor trước. Set `manualEmailScenario=INVITATION`, `invitationEmail` và `invitationCreateKey` mới; gửi create invitation một lần. Mở thư, nhập invitation token vào Current Value `invitationToken`, đặt `invitationPassword`, rồi gửi accept. Retry chính request accept thì giữ nguyên key và payload. Không tự tạo account/accepted invitation trong DB.

Recovery hiện chỉ tạo request bền vững và thông báo nội bộ cho Supervisor; code hiện tại không gửi email recovery tới `recoveryEmail`. Gửi request này tạo dữ liệu, do đó phải bật riêng `runRecovery=true` và chỉ dùng database test.

## Bảo vệ tài khoản và idempotency

- Không chạy change-password, logout, profile update, account update hoặc password reset trên bốn account fixture. Các request có tác động này bị chặn mặc định.
- Để kiểm một thao tác destructive, tạo account disposable qua flow API phù hợp; nhập `targetUserId` riêng, đặt `runDestructive=true` và `disposableAccountConfirmed=YES`, rồi chỉ chạy request đó. Sau khi kiểm xong, tắt cờ.
- Không dùng lại một idempotency key với payload khác. Retry đúng operation thì giữ nguyên cả key và payload; thao tác mới dùng key mới. Các manual email key được giữ Current Value để retry có chủ đích.
- Không gửi bí mật trong chat, Console screenshot, collection export hoặc environment được chia sẻ.

## Phạm vi API hiện có

OpenAPI v1 tại server đang công bố 37 method/path. Collection hiện có 25 operation paths và nhiều request kiểm âm/replay; nó **chưa bao phủ toàn bộ API**, nên không thể kết luận “mọi API đều chạy” từ một collection run. 12 operation còn thiếu request Postman:

| Method | Path |
|---|---|
| `GET` | `/api/v1/notifications` |
| `GET` | `/api/v1/notifications/{notificationId}` |
| `POST` | `/api/v1/notifications/{notificationId}/read` |
| `POST` | `/api/v1/projects/{projectId}/road-sections` |
| `POST` | `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions` |
| `PUT` | `/api/v1/projects/{projectId}/primary-project-manager` |
| `PUT` | `/api/v1/projects/{projectId}` |
| `POST` | `/api/v1/projects/{projectId}/warranties` |
| `POST` | `/api/v1/projects/{projectId}/survey-plans` |
| `POST` | `/api/v1/survey-plans/{planId}/postpone` |
| `POST` | `/api/v1/projects/{projectId}/survey-tasks` |
| `GET` | `/api/v1/survey-tasks/{taskId}` |

Không tạo request mẫu cho các operation này bằng payload phỏng đoán. Cần bổ sung từ OpenAPI schema/DTO, role policy và dữ liệu phụ thuộc thực tế trước khi gọi đó là happy-path automation.

## Kết quả xác minh hiện tại

- API local: health `200 Healthy`; OpenAPI đọc được.
- Login seed Supervisor: `401 auth_invalid_credentials`; DB evidence cho thấy account bị đổi role/status/password. Chưa sửa DB.
- Seed PM, Operator, Repair Crew: role/status và seed password khớp tại thời điểm kiểm tra; chưa chạy hết các API folder bằng Runner.
- Static collection JSON parsing và toàn bộ 37-route parity: chưa xác minh lại sau lần chỉnh sửa hiện tại.
- Real SMTP send, mailbox receipt, OTP verify/login và invitation accept: chưa chạy trong lượt này.
