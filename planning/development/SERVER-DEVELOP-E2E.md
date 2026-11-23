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

### Sau khi chủ server chạy update — supersedes pending deployment above

CURRENT_VERIFIED từ output chủ server: updater kết thúc `Deployment verified: fe3828d10da2c0274d916657d596650a1a7587d8`; API image đúng SHA, API/SQL/MinIO đều healthy. Entry point chỉ đến activation sau khi backup command thành công; chưa nhận đường dẫn backup/checksum manifest để đối chiếu độc lập. SSH kiểm tra lại xác nhận current symlink đúng SHA và HTTPS `/health` Healthy. Không chạy lại update/initialize; thư mục shell cũ của người dùng không phải bằng chứng server còn chạy release cũ.

Live smoke: health 200 PASS, anonymous session 401 PASS, lấy CSRF token 200 PASS; Web login trả 401 (FAIL so với 200 mong đợi). Các bước authenticated read, CSRF write denial và logout chưa chạy. G5 chưa đạt và không tuyên bố hoàn thành live E2E. Đang chẩn đoán credential/account; không tự reset mật khẩu hoặc sửa dữ liệu. So sánh nguồn xác nhận bootstrap và credential verifier không thay đổi từ release cũ; bootstrap chỉ tạo Supervisor khi Users trống, update không đặt lại mật khẩu. 401 chưa đủ phân biệt sai thông tin, account/role không active hoặc session admission bị từ chối. Đã hỏi người dùng loại mật khẩu đã nhập, không yêu cầu nội dung mật khẩu.

Chủ tài khoản xác nhận không nhớ mật khẩu RoadGuard. PROPOSED, đang chờ xác nhận riêng theo rule về secret: khôi phục duy nhất tài khoản Supervisor được chủ xác nhận qua sudo, kiểm tra identity/status trước, dùng cơ chế reset transactional hiện có (hash/stamp, revoke sessions/tokens, MustChangePassword, audit), nhập mật khẩu riêng trên terminal và chạy lại smoke. Không có reset hoặc thay đổi quyền/dữ liệu được thực hiện tại checkpoint này.

### Approved Supervisor recovery — 2026-11-08

TARGET_CONFIRMED: owner explicitly approved recovery of one exact Supervisor account via sudo, private temporary password entry, revocation and mandatory change. Supersedes the PROPOSED/pending approval paragraph above. No new approval required for this operation.

Prepared `deploy/server/recovery` console helper and `recover-supervisor.py`. Uses the existing pinned API image/deps and `IdentityV2Service.ResetPasswordAsync`; does not start the API host, workers, migrations or seeds. Checks exact email/normalized email/username, active Supervisor and active role; pins database, release and image. Root wrapper holds existing maintenance lock, limits temporary container to 512 MiB/1 CPU/128 processes, uses no Docker logs or exposed ports, mounts config read-only and removes its temporary runtime copy. One fixed correlation `f18ba948-fce4-4727-b77d-508e6293d852` rejects a completed operation on repeat. Existing reset audit source remains `AdminApi` because the existing repository assigns that constant; this correlation/spec identifies the actual sudo recovery (no claim that an HTTP admin request performed it). The approved account is passed as actor/target only in this local privileged recovery tool.

CURRENT_VERIFIED: helper Release build passed (0 errors); 15 target/password guard cases passed; 4 Python preflight tests passed on Linux in an automatically removed temporary directory; launcher bash syntax passed. Production source directories have no diff from runtime SHA. Attempted 12 focused API tests locally: all failed during Docker fixture initialization (Docker unavailable); not behavioral PASS. Prior exact-SHA CI evidence for the unchanged reset implementation remains historical to this recovery check. No new SQL tests run against server data.

Self-review pass 1: target binding, password privacy, existing atomic reset/rowversion, revocation, fixed correlation, postconditions and no new endpoint. Pass 2: release/image/config pinning, one lock, resource limits, immutable releases, no service restart and controlled temporary paths. Fixed exception reporting so only explicit helper failures display messages; unexpected exceptions print type only. External review not performed for this helper.

Staged only three non-secret artifacts under `/tmp/roadguard-recovery-f18ba948`: `RoadGuardRecovery.dll`, `recover-supervisor.py`, `recovery-run.sh`. SHA256 respectively `a82a55c7fc23b52826e3214ffdb3e02a02401407e3d537ec9d010d4fca8c0d08`, `457a8d83f01d1d257b6fd1cbcf747025fee8f1721add374d4977a21dca29c03f`, `d128d6ed796bc3de5d42c2631e57b98788c0b4723a0e0a5ce8a61bf982c20be5`; remote checksums match. Launcher checks script/DLL before use. Clean these exact task-owned artifacts after recovery and smoke are observed complete. Runtime symlink still `fe3828d10da2c0274d916657d596650a1a7587d8`, CA-verified health Healthy. Root access still requires interactive sudo; neither reset nor helper runtime binding against the live image has yet executed. No success claim for account recovery/G5 until user runs the staged command and live smoke.

### Recovery outcome and smoke terminal fix — 2026-11-09

CURRENT_VERIFIED from owner terminal: `recovery-run.sh` checksum checks passed; exact active Supervisor and database checks passed; reset committed; new credential verified; old sessions revoked; original role preserved; mandatory password change set. The first subsequent `live-smoke.py` reached health 200 and anonymous session 401, then stopped before sending login because `getpass` raised a terminal UTF-8 decode error. The error does not invalidate the committed reset and must not trigger another recovery.

The smoke client now reads ASCII password bytes from `/dev/tty` without echo or locale decoding. It rejects non-ASCII/control bytes with an actionable error before any HTTP request using that input. Four smoke guard tests passed on Ubuntu, including pseudo-terminal success, backspace and invalid-byte cases; 3 passed and the PTY case was correctly skipped on Windows. A patched smoke script and checksum launcher were staged in the same task-owned `/tmp/roadguard-recovery-f18ba948` directory; script SHA256 `458bdd556a34bde970d26c491cccab6dfcc53474a49bbb86d3b0070e8f90c4dd` matches local and server. Launcher bash syntax passed. Fresh SSH read check: hostname `roadguardsystem`, active release `fe3828d10da2c0274d916657d596650a1a7587d8`, CA-verified health `Healthy`. Owner was asked to run only `bash /tmp/roadguard-recovery-f18ba948/smoke-run.sh`, enter temporary/new passwords privately and report the output. Authenticated live checks and mandatory password change remain NOT VERIFIED until that run completes.

### Live forced password change diagnostic — 2026-11-09

CURRENT_VERIFIED from owner terminal: correct `smoke-run.sh` reached Web login 200 with the recovered temporary credential, then the anonymous forced-password-change endpoint returned HTTP 400 instead of 204. Health 200, anonymous session denial 401 and CSRF token 200 also passed. The reset is committed and must not be repeated. A 400 can represent rejected new-password policy or request validation; the original smoke client hid the response category, so the precise reason remains UNKNOWN. No evidence yet of a completed password change, authenticated read or logout.

Updated only the task smoke client to report the API's known static error title for this specific 400 without printing the response body, and to reject reusing the temporary password locally. SHA256 of staged script is `4684beaa523473ef75936ee4c399eadaffdd0bbc865764def9c2c8831db4a8dc`; launcher verifies that digest. Five smoke guard tests passed on Ubuntu (including pseudo-terminal), launcher syntax passed, and active release still matches `fe3828d10da2c0274d916657d596650a1a7587d8`. Owner was asked to rerun only the smoke client with a distinct strong password, not the recovery helper. G5 remains open pending that result.

Owner's next `smoke-run.sh` reached health 200, anonymous session denial 401 and CSRF token 200, but Web login returned 401 before submitting another forced change. This does not identify the credential typed; the previous new password was not confirmed as applied. Source review shows `CheckPasswordAsync` in this path does not increment lockout attempts, and the forced-change service returns 400 before successful persistence for invalid input or rejected policy. The next run must use the temporary password from the committed recovery at the initial `Supervisor password` prompt; the password intended at the failed change should not be used there. No new reset is authorized or required. The owner's deployment output also identifies the independently reported verified backup at `/srv/roadguard/backups/20261108T162721Z`; no restore was attempted. Authenticated read, logout and the precise 400 category remain UNKNOWN.

### Repeated login failure investigation — 2026-11-09

Owner reported three further `smoke-run.sh` attempts ending at Web login 401; no subsequent password-change request was sent. Stop repeated guesses. A separate read-only inspection helper was prepared and staged under the existing task-owned `/tmp/roadguard-recovery-f18ba948` path. It checks exact account/role, the approved recovery record, `MustChangePassword`, a password-change audit event after recovery, and whether one privately entered candidate matches the current hash using `IPasswordHasher` directly. It does not call `SaveChanges`, reset, migrations, web endpoints or service restart. The root wrapper pins release/image/network, uses the existing lock and a 512 MiB/1 CPU temporary container, with read-only config mounts and no exposed ports. Local .NET Release build: 0 warnings/errors; Ubuntu Python guard tests: 3 PASS; launcher syntax and staged script/DLL SHA256 checks PASS. DLL SHA256 `1f84545b54413a36ae1c21324e28d58c27eec337840b6a53e1b7dbb7d1b4ce0f`; wrapper SHA256 `7397b3172a39dc7c555e57cf3854c779e66340c05ec8b1e04639d76eee7109fd`. The helper has **not** yet run against live SQL; user was asked to run one command with sudo and private password entry. Do not infer a password mismatch or changed credential until its output is observed.

CURRENT_VERIFIED from owner terminal: read-only inspection found the exact active Supervisor and the successful first recovery record; `MUST_CHANGE_PASSWORD: True`, `FORCED_CHANGE_AFTER_RECOVERY: False`, `PASSWORD_MATCHES_CURRENT_HASH: False` for the single privately entered candidate. This proves only that candidate did not match, not which password the owner entered. Repeated login 401 and the absent forced-change audit mean live authenticated checks remain open. Stop guessing and do not rerun the first recovery.

PROPOSED: one separately approved retry reset with new correlation `e49e904b-9619-4fea-8675-883b0d550555`. A new helper build rejects the retry unless the first recovery succeeded, the exact active Supervisor still requires a change, and there is neither a newer reset nor a password-change audit since the first recovery. It pins the same `fe3828d` release/image and private DB, reuses the existing transactional reset, and checks hash, role and revocations afterward. It has no API restart or schema change. A second secret change is outside the first approval and must be explicitly approved before any server upload or execution. If approved, the owner enters the new temporary password privately and then runs live smoke to change it to a distinct permanent password. A failed precondition makes no change; after a committed reset, do not rerun blindly.

TARGET_CONFIRMED: owner approved the second reset of the same exact Supervisor account after the read-only mismatch. The retry package has new correlation `e49e904b-9619-4fea-8675-883b0d550555` and distinct launcher/DLL names. Local .NET Release build: 0 warnings/errors; 19 recovery guard cases PASS; 5 Python wrapper preflight tests PASS; smoke client guard tests 4 PASS/1 Linux-only skip on Windows. Read-only preflight: hostname `roadguardsystem`, release `fe3828d`, CA-verified health `Healthy`, 13,703,076 KiB RAM available and 88 GiB disk free. Docker inspect requires sudo and is gated inside the root wrapper. No retry reset had executed at this checkpoint.

Staged exactly three non-secret artifacts at `/tmp/roadguard-recovery-e49e904b`: `RoadGuardRecoveryRetry.dll` SHA256 `87b957ec5a2eb2adf217fe4dae397c2e6329c490f78467e4934349006ad428ca`, `recover-supervisor.py` SHA256 `d310a31452a3630bb0da31d032d72d3a4a1aec8cae62c9df051875da52423aff`, `retry-run.sh` SHA256 `553befd7aa91cf44ba5226a58d9f3f6fefcf8884cdd2a8dec952379d687cda06`. Remote checksums match local; bash syntax and Python compilation passed without cache writes. No retry reset executed. Owner's sudo run should first make this exact staging directory root-owned, then execute the checksum launcher once. Do not use the old `recovery-run.sh`.

CURRENT_VERIFIED from owner terminal: `retry-run.sh` verified DLL and wrapper checksums, found the exact active Supervisor/database, committed correlation `e49e904b-9619-4fea-8675-883b0d550555`, verified the new credential hash, revoked previous sessions, preserved role and set mandatory password change. The earlier 401 output included in the paste predates this retry and must not be treated as a post-retry login result. Afterward, SSH confirmed active release still `fe3828d`, CA-verified health `Healthy`, and the staged `live-smoke.py` SHA256 `4684beaa523473ef75936ee4c399eadaffdd0bbc865764def9c2c8831db4a8dc` matches its launcher. Authenticated HTTPS login, forced password change, project read and logout remain NOT VERIFIED until the owner runs the smoke client with the newly entered temporary password. Do not rerun either reset helper.

### Final live BE smoke — 2026-11-09

CURRENT_VERIFIED from owner terminal: `live-smoke.py` passed health 200; anonymous session denial 401; CSRF token 200; Web login 200 with the second temporary credential; required password change 204; second Web login 200 with the permanent credential; authenticated session 200; project list via SQL 200; logout without CSRF denied 403; session remained valid 200; valid logout 204; post-logout session denied 401. This completes the bounded live BE auth/read smoke G5 on release `fe3828d10da2c0274d916657d596650a1a7587d8`. It does not verify live upload, multi-role writes, FE/mobile or AI. The CI suite covers BE behaviors in isolated fixtures; it does not replace those live integrations. A follow-up CA-verified `/health` check returned `Healthy`, with no release change. First recovery staging directory `/tmp/roadguard-recovery-f18ba948` was removed after confirming no recovery/smoke process remained. Owner's first sudo cleanup of `/tmp/roadguard-recovery-e49e904b` succeeded; a second invocation correctly reported the directory absent. SSH independently verified both exact staging directories absent. No release, backup, image or shared data was removed.
