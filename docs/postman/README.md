# Chạy thử RoadGuard API bằng Postman

## Current local disposable target — owner decision 2026-10-03

Owner cho phép tạo lại hoàn toàn **chỉ `.\HANHNAV / RoadGuardPostmanTest`**,
không cần recovery dữ liệu cũ. DB đã recreate một lần, apply production
migrations tới `20261003160000_AnhHuyDependencyDefectConcurrency` (45 migrations),
seed lại hai lần thành công. Incident cũ bên dưới là HISTORICAL, không bị xóa.
Không dùng quyền này cho instance/DB/storage khác. Reset hiện tại dùng DROP
thông thường: nếu có session khác đang dùng DB thì dừng, không kill session.

1. Build API và `tools/RoadGuardSystem.Seeder` bằng SDK hiện có. Đặt
   `ROADGUARD_CONNECTION_STRING` trong process environment từ cấu hình private;
   không đưa connection/password/token vào command line, Git hoặc báo cáo.
2. Chạy `dotnet run --project tools/RoadGuardSystem.Seeder -- --postman-disposable`.
   Chỉ thêm `--recreate` khi cần reset; seed lại không nhân fixture. `--verify-only`
   kiểm exact configured/live instance và catalog mà không migrate/seed.
3. Cấu hình JWT/MinIO/font/worker trong process environment riêng rồi chạy
   `python tools/postman/start_local.py`. Launcher kiểm live target trước khi
   chạy API, buộc startup migration/seed=false. Không dùng launch profile khác
   để suy đoán API đã trỏ đúng DB. Trên Windows, signing-key dictionary suffix
   phải khớp chính xác ActiveKeyId sau khi environment key được viết hoa.
4. Import canonical JSON collection và environment JSON hiện có. Password
   placeholders vẫn rỗng; điền fixture credentials riêng. Reporter synthetic
   là `reporter.runtime@example.test`; model synthetic-road-v1 là mock fixture,
   không phải provider thật. Không seed file VERIFIED thiếu object bytes.
5. Mặc định Huy vẫn intake-only. Chỉ với API đã bật
   `Huy01__EnableLifecycleAndCase=true`, đặt environment
   `huyLifecycleAndCaseEnabled=true` để chạy hai folder opted-in mới.
   KEEP_NEW/LINK_EXISTING, approved labels/matching/CaseDefect readers vẫn có
   dependency gates; không tạo approval/reader giả để chạy collection.

Có thể chạy Newman hiện có qua runner không log response/token/signed URL:

```powershell
# NODE_PATH trỏ tới Newman đã cài nếu Node chưa resolve được package.
node tools/postman/run_smoke.cjs <private-environment.json> "00 - Preflight" "HUY-01 current authority preflight - opted-in"
node tools/postman/run_smoke.cjs <private-environment.json> "00 - Preflight" "ANH-02 assigned - AI reporting export retention" "--request=Login seeded Project Manager" "--request=Reporting summary" "--request=Training missing Huy approved reader"
node tools/postman/run_smoke.cjs <private-environment.json> "00 - Preflight" "ANH-02 project hold - storage independent" "--export-environment=artifacts/anh02-postman/private-resume.json"
```

Runner kiểm SQL target trước HTTP, chỉ nhận loopback URL và reuse requests,
IDs/scripts canonical; không tự cài package. Dùng `--request=<exact leaf name>`
để chạy phần độc lập và giữ folder guards. Full storage/upload→AI→PDF/ZIP và
hold/evaluation phải dùng bytes/object test thật cùng state của demo hiện có;
không dùng dummy GUID hoặc bypass verify. Kết quả network/counts và NOT RUN
được ghi trong `planning/development/ANH-02-summary.md`; JSON parse không thay
network evidence. Docker/MinIO hiện blocked, không coi full flow đã pass.

Folder PROJECT hold độc lập đã chạy create/replay, PM read, Crew403,
release/replay và evaluator COMPLETE với inventory thực rỗng. Đây không phải
acceptance cho inventory chứa Huy evidence. Giữ keys/original ETag khi retry;
CLI có thể export environment chứa tokens **chỉ vào JSON ignored dưới artifacts**
và dùng file đó cho lần chạy lại. Không commit/export vào environment mẫu.
Poll tối đa khoảng 22 giây, dài hơn worker interval15 giây; không sửa worker
hoặc giả trạng thái COMPLETE để làm smoke pass.

## HISTORICAL — vì sao đăng nhập trước đây không được

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

## ANH-02 — manual local package

Folder `ANH-02 assigned - AI reporting export retention` thêm 23 requests; giữ nguyên 14 folders và identifiers cũ. Import lại JSON collection/environment. `anh02Enabled=false` mặc định; chỉ bật cho DB disposable Development/Test đã chuẩn bị current actor/project/geometry/dataset. Dùng video thật `contracts/ai/fixtures/anh02/synthetic-road-v1.mp4` trong upload verification; model Released và CRACK catalog phải có từ nguồn được quản trị. Không seed/migrate DB chung. `anh02GeometryVersion` lấy từ trusted geometry producer; HTTP API tests cho ví dụ capture chính xác.

Keys tách từng command và giữ ổn định khi replay. Chọn `anh02DossierFormat=ZIP` hoặc `PDF`; PDF cần cấu hình licensed Unicode font. Sau admission, poll worker tới terminal rồi mới lấy result/content. Matching trả409 source_not_ready và training trả503 producer_unavailable khi Huy reader chưa có; không xem chúng là integration đã hoàn tất. Basis request dự kiến409 khi reference inventory chưa đủ; đừng thay inventory version bằng giá trị đoán. Hold không gia hạn download expiry30 ngày. Không có request xóa thật.

Runner gửi HTTP chưa chạy; JSON, scripts và compatibility được kiểm riêng, kết quả thực nằm trong `planning/development/ANH-02-summary.md`. Những nhận định về DB/API local ở phần đầu README là lịch sử, không phải trạng thái môi trường ANH-02 hiện tại.
Private REPORT_PHOTO multipart correction: keep the part-issuance key stable for
replay. SQL retry after acknowledged storage success reuses the same multipart ID.
A durable unresolved initialization claim returns503 storage-unavailable even with
a fresh key; do not loop new keys or reset DB metadata to bypass it. Storage/process
acknowledgement loss requires authorized reconciliation (no automatic abort/list
adapter exists). Fault-injection/recovery/concurrent-key coverage runs in owned SQL
fixtures, not by modifying the shared/deployed database or deleting storage objects.
