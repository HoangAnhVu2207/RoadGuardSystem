# Chia lane API và SQL integration trên GitHub Actions

**Trạng thái** `PROPOSED` · Chờ chủ dự án duyệt phạm vi triển khai và benchmark.  
**Người thực hiện đề xuất** Codex, một người sửa workflow và các file kiểm thử dùng chung.  
**Base đề xuất** `develop` và `huy-review` cùng trỏ tới `fbde7c43923f38f969af2ac75b82c56c4d0770f6` khi lập spec. Worktree hiện tại ở `codex/roadguard-server-release`, HEAD `cc18a3e230c80b4fac758281ceae2c4f29ff07f1`, sạch trước khi thêm spec. Workflow và manifest SQL của hai revision có blob khác nhau. Khi triển khai, lấy inventory từ base đã duyệt.

## 1. Báo cáo hiện trạng

`HISTORICAL` · Lần xác minh server gần nhất của phiên triển khai dùng BE và AI cùng build `6769253448c3ac915e2dce9bd7982e74071de29a`. Cả hai dịch vụ healthy và BE gọi worker AI qua mạng nội bộ với xác thực. Mốc backup là `/srv/roadguard/backups/20261125T104050Z`.

`HISTORICAL` · Server đã có `Duong Kenh Dong` (13 đoạn) và `Duong so 2` (6 đoạn). Cả 7 tài khoản seed đã được kiểm tra đăng nhập qua endpoint public. Năm bộ video/SRT thật chạy đến kết quả AI mà PM đọc được, gồm 4 bộ của Kênh Đông và 1 bộ của Số 2. Một ca retry DEMO giữ lịch sử lần đầu `RUNNER_STOPPED`, lần hai hoàn tất và replay trả cùng kết quả. [Bàn giao Web và Mobile](<D:/RoadGuard-AI-LocalTest/server-demo-replay-20261124/FE-QUICKSTART.md>).

`UNKNOWN` · Phiên lập spec dùng bằng chứng đã ghi từ server. Các bước Crew kiểm tra và PM hoàn thành sửa chữa theo Normal/FastTrack nằm ngoài bằng chứng BE–AI đã thu.

`CURRENT_VERIFIED` · Run `45598662474` trên `develop` hoàn tất thành công trong **24 phút 12 giây** tính từ khi GitHub tạo run đến khi job cuối hoàn tất. Kết quả dưới đây lấy từ thời gian job/step và TRX của chính run đó. [GitHub Actions run 45598662474](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45598662474).

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

## 2. Mục tiêu và thiết kế đề xuất

`TARGET_CONFIRMED` · Thêm lane SQL integration và chia API theo luồng nghiệp vụ. Mục tiêu run CI thường dưới **5 phút** từ lúc tạo run đến khi hoàn tất toàn bộ job, gồm thời gian chờ runner và đối soát.

`PROPOSED` · Bắt đầu với **12 lane API và 10 lane SQL**, cân lại theo thời gian từng lane sau benchmark. Dùng matrix chạy song song với `fail-fast: false`. Mỗi lane dùng tiến trình test và tài nguyên test riêng. Job tổng hợp giữ tên check `API tests` và `SQL integration tests`.

- **API** · Dùng các luồng Swagger/startup, đăng nhập, identity/phân quyền, dự án/GIS/bảo hành, khảo sát/upload, processing/AI, case, báo cáo/defect, inspection/offline, repair/FastTrack, notification/export và sandbox. Đặt tên lane theo luồng. Cân bằng theo duration TRX ở cấp lớp, ưu tiên giữ lớp cùng luồng. Nhóm lớn như Authentication hoặc Sandbox có thể có lane bổ sung mang tên luồng khi số liệu cần.
- **SQL** · Phân các lớp từ bốn lane hiện tại theo thời gian và fixture. Tách `SandboxSyntheticSeedTests` theo nhóm method gồm từ chối cấu hình, tính nguyên tử/cạnh tranh và chạy lại/bảo toàn dữ liệu. Giữ các dòng theory cùng method. Dùng SQL Server thật và database riêng theo fixture. Gắn kiểm tra Seeder CLI hai lần vào một lane có thời gian phù hợp, giữ các kiểm tra migration và storage.
- **Khởi tạo runner** · Chạy chuẩn bị .NET và dịch vụ song song trong lane khi chúng độc lập, thu exit code của từng bước. Dùng lại image MinIO build từ Dockerfile và upstream SHA đã pin, với cache định danh theo nội dung Dockerfile và nền tảng. Khi cache miss, build từ nguồn đã pin. Chỉ cấp image cho lane có fixture cần MinIO sau khi kiểm kê các fixture, gồm API, SQL và Seeder. Giữ lệnh khởi tạo SQL với thông tin đăng nhập tạm và cleanup trên runner.
- **Kết quả** · Mỗi lane ghi TRX và artifact mang tên lane. Đối soát bằng manifest lớp/method và định danh ca thực thi từ TRX. Kiểm tra discovery của assembly để bắt lớp hoặc method mới chưa gán. Job tổng hợp kiểm đủ lane, đủ kết quả, mỗi ca đúng một lần và đúng lane. Vẫn xuất TRX khi lane lỗi. Có thể dùng bản tóm tắt định danh nhỏ đã được xác thực từ TRX trên runner để giảm dung lượng phải tải xuống, đồng thời giữ TRX gốc để điều tra.
- **Các gate hiện có** · Giữ unit, source/security, migration, Seeder, coverage theo lịch hoặc bằng `collect_coverage`, và chính sách skip có điều kiện của duy nhất ca MinIO smoke nêu trên. Giữ ca acceptance AI thật thuộc chế độ opt-in riêng. CI dùng SQL Server, MinIO và dữ liệu test dùng một lần trên GitHub runner.

## 3. Phạm vi sửa sau khi duyệt

| Đường dẫn | Trách nhiệm |
| --- | --- |
| `.github/workflows/ci.yml` | Matrix, khởi tạo runner, artifact, coverage và tên check tổng hợp |
| `tests/CI/api-lanes.json` và `tests/CI/sql-lanes.json` | Danh sách lane và phân công lớp, method, định danh dự kiến |
| `tests/CI/Invoke-ApiLane.ps1`, `tests/CI/Invoke-SqlLane.ps1`, `tests/CI/Verify-SqlLaneDiscovery.ps1` | Discovery và runner lọc đúng phạm vi, dùng chung phần đối soát phù hợp của SQL |
| `tests/CI/Verify-ApiLanes.ps1`, `tests/CI/Verify-SqlLanes.ps1`, `tests/CI/Verify-TestResults.ps1` | Xác thực TRX và kết quả tổng hợp |
| `tests/CI/Verify-CiWorkflow.ps1` và `tests/CI/Verify-TestResults.Tests.ps1` | Kiểm cấu hình workflow và kiểm hồi quy của các gate |

Bắt đầu từ commit `develop` đã xác nhận. Benchmark trên nhánh ứng viên đề xuất `codex/roadguard-ci-lanes` với `workflow_dispatch`, sau đó bàn giao diff để review theo quy trình `huy-review`. Phạm vi duyệt gồm sửa các file trên, đẩy nhánh ứng viên và chạy benchmark GitHub Actions. Người thực hiện duy nhất phụ trách các file CI trong bảng cho đến lúc bàn giao. API contract, role/project scope và SQL production nằm ngoài gói CI này.

## 4. Trình tự và tiêu chí nghiệm thu

- [ ] Chụp inventory lớp, method, ca và thời gian từ TRX của base. Kiểm tra chính xác lane cần SQL và MinIO. Tạo manifest, tính thời gian dự kiến dựa trên duration và chi phí fixture. **1.717 ca API và 808 ca SQL** là mốc so sánh cho base hiện tại.
- [ ] Viết ca kiểm tra bộ đối soát với dữ liệu hợp lệ, thiếu lane, thiếu ca, trùng ca, sai lane, zero test, skip ngoài chính sách, fail và lane bị hủy. Chạy ca hồi quy trước khi nối các gate vào workflow.
- [ ] Bổ sung API matrix và mở rộng SQL matrix. Chạy `pwsh -File tests/CI/Verify-CiWorkflow.ps1`, `pwsh -File tests/CI/Verify-TestResults.Tests.ps1`, kiểm tra discovery trên assembly đã build, đối soát bằng TRX và `git diff --check`.
- [ ] Benchmark trên GitHub Actions ở commit ứng viên. Kiểm tra cả trường hợp cache sẵn và cache miss, lưu thời gian chờ runner, khởi tạo, test, upload và tổng hợp. Đối chiếu đủ danh tính ca API và SQL, cùng kết quả unit, source gate và Seeder. Kiểm tra thêm lượt bật coverage có artifact từng lane.
- [ ] Nghiệm thu tốc độ khi **hai lượt CI thường liên tiếp** tại cùng commit đều xanh và đều dưới 5 phút, tính từ `createdAt` đến `completedAt` của job cuối. Báo riêng thời gian đo được của cache miss và coverage. Con số 12/10 là cấu hình khởi điểm. Nếu benchmark vượt 5 phút, xác định bước chậm thực tế, cân lại lane hoặc bước chuẩn bị trong phạm vi và chạy lại. Không tính workflow lỗi, hủy hoặc thiếu ca là đạt.
- [ ] Tự review hai lượt. Lượt một đối chiếu discovery, ca bắt buộc, fixture, lỗi và artifact. Lượt hai đối chiếu diff, job name, trigger, cache, cô lập SQL/MinIO và đường lùi khi workflow có vấn đề. Bàn giao link run, SHA, thời gian từng job, số pass/fail/skip, diff và trạng thái review ngoài.

`UNKNOWN` · Thời gian của matrix mới và tỷ lệ cache hit chưa có bằng chứng GitHub Actions. Mốc dưới 5 phút được xác nhận bằng benchmark sau duyệt.  
**Quyết định cần duyệt** Bắt đầu triển khai theo phạm vi mục 3, với 12 lane API và 10 lane SQL làm cấu hình benchmark đầu tiên.
