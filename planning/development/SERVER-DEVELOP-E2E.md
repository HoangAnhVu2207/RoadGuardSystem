# SERVER-DEVELOP-E2E — một gói tích hợp và kiểm thử BE

Status: ASSIGNED — chủ server giao ngày 08/11/2026: lấy develop, kiểm thử end-to-end BE, rà soát phần dư, làm một task có mục tiêu và không hỏi lặp. Writer: Codex tại worktree ubuntu-deploy, branch deploy. Dùng Superpowers brainstorming, writing-plans và execution; yêu cầu một spec và thực hiện liền mạch của chủ server thay thế các checkpoint hỏi duyệt lặp trong skill.

## Mục tiêu và bản nguồn cố định

- TARGET_CONFIRMED: tích hợp `origin/develop` vào bộ triển khai hiện có, kiểm chứng BE và báo trung thực luồng nào đã chạy thực tế. Không push main hoặc develop.
- Base deploy: `f3ac2e3ce3369e233a5b1d3bd2cf9232f2e1f682`.
- Develop đã fetch: `cda6d6384a69db23cafbdab430ffb523b0b5e6bc`.
- Server trước thay đổi: `21e517792e1e4ba20425d32797dea66dc806c29d`, BE/SQL/MinIO healthy theo chẩn đoán trước đó. Không coi bằng chứng cũ là health tại lúc deploy.
- Dirty đầu task cần giữ: AGENTS.md, deploy/server/README.md, planning/development/SERVER-DEPLOYMENT.md, deploy/server/SERVER-RULES.md; đều là quy tắc/bằng chứng của task server trước.

## Thiết kế và phạm vi

Chọn merge develop vào deploy trong worktree đã cô lập: giữ nguyên thay đổi nghiệp vụ upstream và bộ triển khai. Checkout develop trực tiếp sẽ thiếu server composition; thay server bằng stack sandbox sẽ không kiểm chứng Production hiện có. Không dùng hai cách đó.

Một chuỗi tuần tự: chốt nguồn → merge → kiểm tra tĩnh/build/test → kiểm chứng CI đúng SHA → kiểm tra server/backup → cập nhật đúng SHA → health và smoke HTTP thật → ghi kết quả. Mỗi lúc chỉ một tác vụ bảo trì nặng trên server. Không bật sandbox, mock AI, seed demo vào RoadGuard_Server hoặc giảm TLS/auth để vượt test.

Phạm vi file của writer: merge nguyên bản nguồn develop; deploy/server/** cho công cụ kiểm tra/triển khai cần thiết; spec này và bốn tài liệu đã sửa. Không chỉnh trực tiếp migration, DbContext, hợp đồng nghiệp vụ hoặc code của writer khác để làm test xanh. Nếu lỗi nằm ở nghiệp vụ upstream, ghi bằng chứng và điểm chặn phát hành, không lén sửa semantics.

Rà soát phần dư gồm endpoint/controller, DI/service/repository, project/package references, fixture/sandbox/tooling và file vào Docker context/release. Phân loại: đang dùng, chỉ dùng dev/test, ứng viên dư cần kiểm chứng, có thể loại bỏ với bằng chứng. Không coi route chưa có caller FE là code rác; reflection, DI, serializer và migrations cần được tính tới. Task yêu cầu kiểm tra, không cho phép xóa hàng loạt hoặc đổi public API.

## Ma trận chấp nhận

| Gate | Điều kiện đạt / bằng chứng |
|---|---|
| G1 nguồn | Develop SHA là ancestor của candidate; deploy composition được giữ; không thay migration so với server cũ nếu không có migration upstream |
| G2 build/local | Build Release thành công; unit tests và focused deployment/API contract checks đạt; không coi test skip do thiếu Docker là PASS |
| G3 CI | Exact-SHA source/unit/API/SQL lanes đạt; API/SQL suites dùng DB fixture cô lập, không dùng DB server |
| G4 deploy | Preflight tài nguyên/khóa; backup đủ bộ + checksum trước activation; candidate pin theo 40-char SHA, không lấy HEAD trôi |
| G5 live | TLS hợp lệ, active revision/image đúng SHA, API/SQL/MinIO healthy; kiểm tra login/first-password-change và authenticated read qua HTTPS bằng người dùng nhập secret riêng |
| G6 giới hạn | Upload/download, nghiệp vụ nhiều vai trò, FE/mobile/AI chỉ PASS khi có luồng thật; CI TestServer hoặc mock phải ghi nhãn riêng |
| G7 audit | Findings có đường dẫn, loại tham chiếu, rủi ro và hành động; không có xóa dựa trên tìm chuỗi đơn thuần |

Phạm vi kiểm thử hồi quy BE: identity/session/CSRF; Supervisor project/staff/road sections; layout preview/apply/read; Crew bootstrap/current attempt/FastTrack; inspection/repair/offline; upload lifecycle và SQL invariants theo suites hiện có. CI thật không đồng nghĩa tất cả các luồng đã chạy qua API Kestrel trên LAN.

## Kế hoạch thực hiện trong cùng task

- [x] P1: Fetch hai nhánh, ghi base/dirty, so sánh migrations/config và caller/test của thay đổi develop. Kiểm tra worktree hiện tại, không tạo clone phụ.
- [x] P2: `git merge --no-commit --no-ff cda6d6384a69db23cafbdab430ffb523b0b5e6bc`; review diff và giữ server hooks. Build Release, unit và focused API checks. Không nâng package.
- [x] P3: Chuẩn bị một entrypoint update có SHA cố định và kiểm tra ancestor/active release; giữ lock xuyên các bước, không activate khi backup thất bại. Thêm kiểm tra hồi quy cho thay đổi script nếu cần.
- [x] P4: Rà soát code dư và packaging; ghi findings ngay trong spec này. Thực hiện hai self-review: dữ liệu/auth/backup; scope/ref/release/test evidence.
- [x] P5: Commit/push chỉ deploy, chờ đúng candidate SHA CI. Nếu CI lỗi, chẩn đoán và sửa chỉ trong phạm vi; không deploy candidate đỏ.
- [ ] P6: Chuẩn bị một lệnh sudo cho chủ server nhập mật khẩu trực tiếp. Chạy gói tuần tự: preflight → build → backup → activation → health/status → live HTTP smoke. Không xin lại quyết định cho các bước đã mô tả; quyền sudo vẫn do chủ server cung cấp qua terminal.
- [ ] P7: Tổng hợp PASS/FAIL/NOT RUN, bản chạy, backup path, audit findings và việc còn chặn. Không tuyên bố hoàn tất E2E nếu thiếu xác thực hoặc luồng ghi thật.

## Dữ liệu, downtime và phục hồi

Task triển khai này bao gồm downtime của API/MinIO khi backup/chuyển release, không reboot host. Giữ nguyên SQL, object/files/keys và tài khoản hiện có. Secret nhập qua getpass/terminal, không chat/history/report. Test nghiệp vụ ghi dữ liệu chỉ chạy trên fixture cô lập; không trỏ integration suite vào RoadGuard_Server.

Preflight kiểm tra dung lượng đúng filesystem và công việc nặng đang chạy. Nếu không đủ dung lượng hoặc có task cạnh tranh, dừng trước thao tác nặng. Backup lỗi: giữ bản hiện tại và bằng chứng, không activate; khôi phục API/MinIO bản cũ khi xác định chưa có migration hay đổi dữ liệu. Candidate lỗi sau maintenance: không tự downgrade schema/restore đè; báo rõ và giữ backup. Chỉ rollback code nếu schema tương thích đã được chứng minh. Không prune/xóa volume/backup, không đổi quyền sudo hoặc mở cổng mới.

## Bằng chứng thực hiện

CURRENT_VERIFIED: fetch thành công; develop có thay đổi Web session, pavement configuration và Crew/FastTrack so với base chung d810cc5. Docker Linux daemon ở máy Windows hiện không khả dụng; không dùng điều này làm lý do chạy suite trực tiếp trên DB server. CI sẽ cung cấp môi trường SQL/MinIO cô lập. Kết quả live trước task là HISTORICAL cho candidate mới.

Merge không xung đột; migrations không thay đổi từ 21e5177. Release build: 0 errors, 224 warning occurrences thuộc baseline. Unit lần đầu: 1112 PASS/5 FAIL do thiếu biến font; TRX xác định cả năm lỗi bắt nguồn thiếu font test. Chỉ đặt RG_TEST_UNICODE_FONT=C:/Windows/Fonts/arial.ttf rồi chạy lại: 1117 PASS/0 FAIL/0 SKIP. Focused API deployment/policy/config/Swagger: 52 PASS/0 FAIL/0 SKIP. Không hạ assertion hoặc đổi code nghiệp vụ để chạy xanh.

SHA-pinned updater: red 4 tests khi thiếu entrypoint; green 4/4 trên Linux trong TemporaryDirectory, giả lập orchestrator/Docker, không sửa server thật. Live smoke guard: red 3, green 3/3 (target, redirect, secret-free errors). Compose/source, CI workflow và agent guidance validators PASS. Hai helper là tooling mới trong deploy/server, không endpoint mới. Live smoke có ghi session/audit và có bước đổi mật khẩu đầu tiên do chủ tài khoản tự nhập; không tự đọc secret bootstrap.

## Audit phần dư — phạm vi và kết luận

Static scan: 1017 file C# production, bỏ migration/generated; đối chiếu định danh với code/test/tool/config. 146 loại chỉ có một lần xuất hiện là danh sách sàng lọc, KHÔNG phải 146 code rác. Phần lớn là EF configuration và static extension được gọi qua method.

| Nhóm / file | Bằng chứng hiện tại | Hành động |
|---|---|---|
| DTO Commons: BatchIdsRequest, BatchOperationResultDTO, ErrorResponse, PagedResponse, PaginationDto | Không có tham chiếu tên trong active code/test/tool/config ngoài khai báo; xuất hiện trong tài liệu inventory | Ứng viên dư. Chưa xóa public DTO khi chưa đối chiếu consumer ngoài repo |
| DTOs/Repairs/RepairFieldSeams.cs: RepairFirstStartV1, RepairSubmissionFactsV1, EffectiveRepairFactsV1 | Chỉ khai báo trong active source scan; DTO khác cùng file vẫn có quan hệ | Ứng viên hợp đồng chưa được nối; không xóa cả file hoặc đổi wire contract |
| Repositories/Commons/ApiResult.cs, Helpers/PagedList.cs: PaginationParams | ApiResult tìm thấy tự tham chiếu trong chính file; PaginationParams chỉ khai báo | Ứng viên helper cũ; cần compile/consumer validation trước gói loại bỏ riêng |
| Services/Implementations/Authentication/UnavailableIdentityMessageSender.cs | Không có caller/DI reference; AuthenticationServiceCollectionExtensions.cs:38 đăng ký GmailIdentityMessageSender | Ứng viên fallback không nối; giữ trong lần deploy này |
| API csproj: jose-jwt 5.2.0 | Không thấy Jose namespace/JWT calls trong active API/Services; thư viện JWT khác đang có | Ứng viên dependency dư; chưa gỡ package vì có thể thay dependency closure và cần kiểm chứng riêng |
| Repositories/Configurations và Models/UploadMultipartSweep.cs | DbContext.cs:184 ApplyConfigurationsFromAssembly; UploadPersistenceService.Recovery.cs đọc/ghi sweep | Đang dùng qua EF/reflection, tuyệt đối không coi là rác |
| Sandbox/Handoff/Anh02 fixtures | Có DI/Program/tests và publish references; Production server guard cấm bật mock/sandbox | Dev/test tooling vẫn có mục đích. Fixtures khoảng 10.5 KB được publish theo csproj; tối ưu packaging cần tách profile, không xóa mù |
| docs/history, planning, knowledge | Lần lượt khoảng 14.9 MB/1714 files, 21.5 MB/1046 files, 0.32 MB/3 files tại lúc scan | Tài liệu/evidence lịch sử, không runtime. common.sh archive và Docker allowlist loại khỏi release/image; giữ lịch sử Git |
| tools/postman/run_smoke.cjs | Guard localhost + RoadGuardPostmanTest + expected SQL instance | Không dùng để test server thật và không bỏ guard |

Không chứng minh endpoint bất kỳ là an toàn để xóa chỉ từ BE repo; FE/mobile/AI consumers không được checkout/audit trong gói này. Không xóa code, package, lịch sử hoặc file server dựa trên heuristic.

Self-review 1: merge giữ Production hooks, auth/CSRF và migrations; updater khóa cùng manage.sh, pin target, kiểm tra ancestor và dừng khi backup lỗi. Self-review 2: giữ dirty docs cũ, không public port/privilege/secret changes; test giả lập updater và live guard không được gọi là live E2E. Giới hạn còn lại: backup cùng SSD, chưa restore thử; thiếu mật khẩu nhập riêng để test live login, thiếu Docker local cho toàn bộ API suite. External review: NOT REQUESTED.

## Candidate và ma trận bằng chứng

Candidate đã commit/push chỉ `origin/deploy`: `fe3828d10da2c0274d916657d596650a1a7587d8`, merge parents `f3ac2e3` và `cda6d63`. Develop đã là ancestor; không thay migrations. Hosted run: https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/42498004003. Bằng chứng TRX tải về nằm trong `TestResults/ServerDevelop/ci-42498004003` (ignored, không đưa vào release).

| Luồng | Bằng chứng cần đọc | Giới hạn môi trường |
|---|---|---|
| Web login/session/CSRF/logout | Huy01WebTransportTests, WebSession*, WebLogout*, Csrf*; live-smoke.py sau deploy | CI dùng TestServer/SQL fixture; live smoke dùng HTTPS thật và tài khoản người dùng |
| Supervisor staff/project/road sections | StepTwoWebWritesHttpTests, ProjectCreateWebCookieHttpTests, StaffMembershipReadHttpTests | Invitation sender trong một số tests là fixture, không chứng minh SMTP thật |
| Pavement layout | H2PavementWorkflowHttpTests, H2PavementWorkflowSqlTests | Database cô lập, không tạo dự án trên RoadGuard_Server |
| Normal/FastTrack/Crew | CloseoutNormalJourneyHttpTests, H4RepairExecutionSqlTests, FastTrackReviewNotificationSqlTests | Kiểm tra HTTP/DI và SQL; fixture khởi tạo actor/source, không phải UI E2E |
| Inspection/offline | H3FieldInspectionWorkflowHttpTests/SqlTests, H5OfflineRepairSqlTests, ND01OriginalWindowHttpTests | Thời gian/nguồn dữ liệu kiểm thử có kiểm soát |
| File upload/download | UploadEndpoints_OwnedMinioPreservesBytesAndRejectsPrivateFileOutsider | TestServer + SQL + MinIO container thật của CI; không chứng minh TLS/private bucket trên server LAN |
| MinIO được cấu hình ngoài fixture | UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio | CI cho phép đúng test tùy chọn này skip; phải ghi riêng, không gọi skip là PASS |
| AI/FE/mobile | Không có bằng chứng triển khai trong gói này | NOT VERIFIED; callback/contract test không thay thế engine hoặc giao diện thật |

Preflight SSH gần thời điểm phát hành: hostname đúng, current vẫn `21e5177`, TLS health `Healthy`, RAM khả dụng 13,355 MiB, swap chưa dùng, SSD trống 89 GiB. Không thấy build/backup cạnh tranh trong snapshot tiến trình; cần kiểm tra lại lúc thực thi. `sudo -n true` báo cần mật khẩu. Đã stage đúng một script `/tmp/roadguard-update-fe3828d.sh`, SHA256 local/server cùng `791b2746617ad590219badbb4a30b522603e457e02653354c95685963076e5e5`; chưa thực thi. Linux regression lần cuối: 14 PASS, 0 SKIP và bash syntax PASS, dùng TemporaryDirectory tự dọn; fixture tài nguyên của updater đã được cố định để test không phụ thuộc RAM trống trên CI.

Checkpoint thực thi: chưa có quyền sudo không tương tác, không thay sudoers/Docker group và không thu mật khẩu. Chủ server nhập sudo trực tiếp khi được cung cấp lệnh sau CI đạt; đây là bàn giao quyền thực thi, không hỏi duyệt lại phạm vi. Chưa deploy candidate, chưa có backup của lần update này và chưa chạy live smoke. P6/P7 vẫn mở.

### Kết quả CI của candidate — CURRENT_VERIFIED

Run 42498004003 kết thúc SUCCESS đúng SHA `fe3828d10da2c0274d916657d596650a1a7587d8`; cả 8 jobs đạt. Đã tải và đọc TRX: unit 1117/1117 PASS; SQL lane 1/2/3/4 lần lượt 203/179/217/208 PASS (tổng 807, không skip); API 1581 PASS, 0 FAIL, 1 `NotExecuted` trong 1582 tests. Test bỏ qua duy nhất là `UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`, đúng ngoại lệ đã công bố. `UploadEndpoints_OwnedMinioPreservesBytesAndRejectsPrivateFileOutsider`, ba tests StepTwoWebWrites và CloseoutNormalJourney đều PASS. Tổng C# đã chạy đạt: 3505, không cộng lại 52 focused tests vốn đã nằm trong API suite. Script Linux: 14 PASS riêng.

Đã kiểm tra lại trước bàn giao sudo: server vẫn chạy `21e5177`, TLS health Healthy, RAM khả dụng 13,368 MiB, SSD trống 89 GiB, checksum script không đổi. Không tự nâng quyền hoặc chạy update khi chưa nhập sudo. Đã gửi một lệnh liên hoàn update + live smoke; chờ kết quả thực thi. G1/G2/G3/G7 đạt theo phạm vi; G4/G5 chưa thực thi; G6 giữ phân biệt môi trường, không tuyên bố full live E2E. Bản ghi bằng chứng bổ sung chỉ đổi spec, không đổi artifact runtime `fe3828d` đã qua CI.
