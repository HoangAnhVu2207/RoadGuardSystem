# Chia lane API và SQL integration trên GitHub Actions

**Trạng thái** · Gói 2 được giao triển khai. Benchmark dưới 5 phút đang chờ kết quả.
**Base sau Gói 1** · `b29d7fade213cb0647ad323454f087854358321e` trên `anh` và `develop`. [Gói 1, run 45644468742](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45644468742).

## 1. Báo cáo hiện trạng

`HISTORICAL` · Lần xác minh server gần nhất của phiên triển khai dùng BE và AI cùng build `6769253448c3ac915e2dce9bd7982e74071de29a`. Cả hai dịch vụ healthy và BE gọi worker AI qua mạng nội bộ với xác thực. Mốc backup là `/srv/roadguard/backups/20261125T104050Z`.

`HISTORICAL` · Server đã có `Duong Kenh Dong` (13 đoạn) và `Duong so 2` (6 đoạn). Cả 7 tài khoản seed đã được kiểm tra đăng nhập qua endpoint public. Năm bộ video/SRT thật chạy đến kết quả AI mà PM đọc được, gồm 4 bộ của Kênh Đông và 1 bộ của Số 2. Một ca retry DEMO giữ lịch sử lần đầu `RUNNER_STOPPED`, lần hai hoàn tất và replay trả cùng kết quả. [Bàn giao Web và Mobile](<D:/RoadGuard-AI-LocalTest/server-demo-replay-20261124/FE-QUICKSTART.md>).

`UNKNOWN` · Phiên lập spec dùng bằng chứng đã ghi từ server. Các bước Crew kiểm tra và PM hoàn thành sửa chữa theo Normal/FastTrack nằm ngoài bằng chứng BE–AI đã thu.

`HISTORICAL` · Run `45598662474` trên `develop` tại `fbde7c43923f38f969af2ac75b82c56c4d0770f6` hoàn tất thành công trong **24 phút 12 giây** tính từ khi GitHub tạo run đến khi job cuối hoàn tất. Kết quả dưới đây lấy từ thời gian job/step và TRX của chính run đó. [GitHub Actions run 45598662474](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45598662474).

| Job | Thời gian job | Riêng bước test | TRX |
| --- | ---: | ---: | --- |
| API tests | 23m42s | 18m48s | 1.716 pass, 1 skip MinIO smoke đã được cấu hình cho phép |
| SQL lane 1 | 13m03s | 7m54s | 204 pass |
| SQL lane 2 | 12m32s | 7m26s | 179 pass |
| SQL lane 3 | 8m03s | 3m01s | 217 pass |
| SQL lane 4 | 9m21s | 3m28s | 208 pass, thêm 39s kiểm tra Seeder |
| Unit tests | 2m12s | 19s | 1.155 pass |
| Verify source contracts | 2m23s | | Thành công |
| SQL integration tests | 16s | | Đối soát 808 ca, 0 fail, 0 skip |

- Job API build MinIO trong 2m32s và build .NET trong 1m37s. Các job SQL build MinIO trong 2m33s–2m45s và build .NET trong 1m19s–1m29s. Đây là các bước nối tiếp trong từng job.
- TRX API có 235 lớp. `ManualSwaggerContractTests` cộng dồn 103s, nhóm Authentication cộng dồn 244s. Tổng duration TRX có thể lớn hơn thời gian chạy do test song song.
- Trong SQL lane 2, `SandboxSyntheticSeedTests` cộng dồn 283s. Lane 1 có `ExportRendererRecoverySqlTests` 108s và `H4CorrectionAuthorityTests` 103s. Dồn các lớp này theo thư mục sẽ giữ lại lane chậm.

`CURRENT_VERIFIED` · TRX tại base Gói 1 ghi 1.777 pass và 1 skip MinIO tùy chọn (1.778 danh tính API), 897 SQL pass và 1.312 unit pass. Cả ba suite có 0 fail. Run hoàn tất trong 19 phút 01 giây tính từ lúc tạo đến job cuối. [Gói 1, run 45644468742](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45644468742).

## 2. Mục tiêu và thiết kế

`TARGET_CONFIRMED` · Thêm lane SQL integration và chia API theo luồng nghiệp vụ. Mục tiêu run CI thường dưới **5 phút** từ lúc tạo run đến khi hoàn tất toàn bộ job, gồm thời gian chờ runner và đối soát.

`TARGET_CONFIRMED` · Bắt đầu với **12 lane API và 10 lane SQL**, cân lại theo thời gian từng lane sau benchmark. Dùng matrix chạy song song với `fail-fast: false`. Mỗi lane dùng tiến trình test và tài nguyên test riêng. Job tổng hợp giữ tên check `API tests` và `SQL integration tests`.

- **API** · Giữ lớp cùng luồng. Đo lại thời gian từng lane sau benchmark.
- **SQL** · Cân 10 lane theo duration TRX. Tách 12 method của `SandboxSyntheticSeedTests` theo thời gian đo và giữ cùng lane các dòng theory của từng method. Giữ SQL Server thật, database riêng theo fixture, kiểm tra migration và storage. Lane 4 chạy Seeder CLI hai lần.
- **Khởi tạo runner** · API pull SQL image song song với restore/build. SQL khởi động container sớm để readiness diễn ra trong lúc build .NET. Workflow chỉ chuẩn bị MinIO ở lane dùng fixture cần image. Các lane dùng chung cache Docker image theo OS, kiến trúc và nội dung Dockerfile. Cache miss build từ nguồn đã pin. Giữ thông tin đăng nhập SQL tạm và cleanup trên runner.
- **Kết quả** · Mỗi lane ghi TRX và artifact mang tên lane. Đối soát bằng manifest lớp/method và định danh ca thực thi từ TRX. Kiểm tra discovery của assembly để bắt lớp hoặc method mới chưa gán. Job tổng hợp kiểm đủ lane, đủ kết quả, mỗi ca đúng một lần và đúng lane. Vẫn xuất TRX khi lane lỗi. Có thể dùng bản tóm tắt định danh nhỏ đã được xác thực từ TRX trên runner để giảm dung lượng phải tải xuống, đồng thời giữ TRX gốc để điều tra.
- **Các gate hiện có** · Giữ unit, source/security, migration, Seeder, coverage theo lịch hoặc bằng `collect_coverage`, và chính sách skip có điều kiện của duy nhất ca MinIO smoke nêu trên. Giữ ca acceptance AI thật thuộc chế độ opt-in riêng. CI dùng SQL Server, MinIO và dữ liệu test dùng một lần trên GitHub runner.

| Lane | Luồng API | Danh tính baseline |
| --- | --- | ---: |
| 1 | Startup and contracts | 397 |
| 2 | Login and sessions | 198 |
| 3 | Identity and authorization | 172 |
| 4 | Projects, GIS and warranty | 234 |
| 5 | Surveys and uploads | 98 |
| 6 | Processing, AI and defects | 261 |
| 7 | Cases and publication | 119 |
| 8 | Inspections and offline | 52 |
| 9 | Repairs and FastTrack | 50 |
| 10 | Notifications and retention | 86 |
| 11 | Reports and exports | 92 |
| 12 | Sandbox | 19 |

Matrix có thể yêu cầu 24 runner cùng lúc, gồm 22 lane, unit và source verify. `UNKNOWN` · GitHub API chưa trả gói tài khoản. Free giới hạn 20 job và Pro giới hạn 40 job. Nếu số đo xác nhận quota 20 là nguyên nhân chờ runner, phương án trình owner là 10 lane API và 8 lane SQL, cùng 2 job unit và source verify. Cấu hình thực hiện theo Gói 2 vẫn là 12/10. [Giới hạn GitHub Actions](https://docs.github.com/en/actions/reference/limits).

## 3. Phạm vi Gói 2

| Đường dẫn | Trách nhiệm |
| --- | --- |
| `.github/workflows/ci.yml` | Matrix, khởi tạo runner, artifact, coverage và tên check tổng hợp |
| `.github/actions/pinned-minio/action.yml` | Chuẩn bị image MinIO đã pin với cache theo nền tảng và Dockerfile |
| `tests/CI/api-lanes.json` và `tests/CI/sql-lanes.json` | Danh sách lane và phân công lớp, method, định danh dự kiến |
| `tests/CI/Invoke-ApiLane.ps1`, `tests/CI/Invoke-SqlLane.ps1`, `tests/CI/Verify-SqlLaneDiscovery.ps1` và bộ chạy/discovery dùng chung | Lọc lớp và method, đối chiếu discovery |
| `tests/CI/Verify-ApiLanes.ps1`, `tests/CI/Verify-SqlLanes.ps1`, `tests/CI/Verify-TestLanes.ps1` | Xác thực TRX và kết quả tổng hợp |
| `tests/CI/Verify-CiWorkflow.ps1` và `tests/CI/Verify-TestLanes.Tests.ps1` | Kiểm cấu hình workflow và kiểm hồi quy của các gate |

Làm việc trong worktree riêng. Tự review hai lượt và đẩy ứng viên lên `anh`. Chờ CI xanh, đối soát đủ danh tính ca trên đúng SHA, rồi fast-forward `develop`. Nếu `develop` có commit mới, merge và kiểm chứng lại ứng viên trước khi fast-forward. Task F chạy song song với các task chuẩn bị E2E sau khi có ứng viên CI.

## 4. Trình tự và tiêu chí nghiệm thu

- [ ] Chụp inventory lớp, method, ca và thời gian từ TRX của base. Kiểm tra chính xác lane cần SQL và MinIO. Tạo manifest, tính thời gian dự kiến dựa trên duration và chi phí fixture. **1.778 API, 897 SQL và 1.312 unit** là inventory quan sát được ở base Gói 1. Khi base thay đổi, đối chiếu lại TRX và discovery của SHA mới.
- [ ] Viết ca kiểm tra bộ đối soát với dữ liệu hợp lệ, thiếu lane, thiếu ca, trùng ca, sai lane, zero test, skip ngoài chính sách, fail và lane bị hủy. Chạy ca hồi quy trước khi nối các gate vào workflow.
- [ ] Bổ sung API matrix và mở rộng SQL matrix. Chạy `pwsh -File tests/CI/Verify-CiWorkflow.ps1`, `pwsh -File tests/CI/Verify-TestResults.Tests.ps1`, kiểm tra discovery trên assembly đã build, đối soát bằng TRX và `git diff --check`.
- [ ] Benchmark trên GitHub Actions ở commit ứng viên. Kiểm tra cả trường hợp cache sẵn và cache miss, lưu thời gian chờ runner, khởi tạo, test, upload và tổng hợp. Đối chiếu đủ danh tính ca API và SQL, cùng kết quả unit, source gate và Seeder. Kiểm tra thêm lượt bật coverage có artifact từng lane.
- [ ] Nghiệm thu tốc độ khi **hai lượt CI thường liên tiếp** tại cùng commit đều xanh và đều dưới 5 phút, tính từ `createdAt` đến `completedAt` của job cuối. Báo riêng thời gian đo được của cache miss và coverage. Con số 12/10 là cấu hình khởi điểm. Nếu benchmark vượt 5 phút, xác định bước chậm thực tế, cân lại lane hoặc bước chuẩn bị trong phạm vi và chạy lại. Không tính workflow lỗi, hủy hoặc thiếu ca là đạt.
- [ ] Tự review hai lượt. Lượt một đối chiếu discovery, ca bắt buộc, fixture, lỗi và artifact. Lượt hai đối chiếu diff, job name, trigger, cache, cô lập SQL/MinIO và đường lùi khi workflow có vấn đề. Bàn giao link run, SHA, thời gian từng job, số pass/fail/skip, diff và trạng thái review ngoài.

Gate ứng viên và deploy yêu cầu CI xanh trên đúng SHA, đủ danh tính ca và các duyệt triển khai theo kế hoạch E2E. Nếu benchmark vượt 5 phút, ghi `PENDING` cùng thời gian đo và giữ gate fast-forward theo kết quả CI.
