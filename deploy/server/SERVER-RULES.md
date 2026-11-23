# Quy tắc vận hành server RoadGuard

**Bản áp dụng: 08/11/2026 — đã giản lược theo yêu cầu chủ server.** Mục tiêu: giữ dữ liệu, vận hành ổn định, thư mục gọn và hỏi trước quyết định quan trọng. Áp dụng cho người/agent thao tác server; một quy tắc chính, không tạo thêm biểu mẫu hoặc report cho mỗi lệnh.

`TARGET_CONFIRMED`: chủ server yêu cầu thận trọng nhưng không rườm rà. Công việc thường lệ được chủ động làm; công việc quan trọng được xác nhận **một lần theo cả gói**, không hỏi lặp từng bước đã nằm trong gói. Các ngưỡng bên dưới là `PROPOSED`, dùng để đánh giá tải; tài liệu này không tự cài công cụ hoặc thay đổi cấu hình host.

## 1. Quyết định nào cần hỏi?

| Nhóm | Cách thực hiện |
|---|---|
| Kiểm tra thường lệ | Tự làm: SSH chỉ đọc, health, trạng thái, log có giới hạn, RAM/đĩa; chuẩn bị bản vá và kiểm tra ở máy phát triển. Không xin lại quyền cho từng lệnh. |
| Công việc đã được giao rõ | Thực hiện trọn phạm vi đã duyệt: ví dụ một lần deploy gồm build/pull, backup, migration đã nêu, restart và health. Upload artifact và dọn file tạm của chính công việc đó là bước đi kèm, không cần hỏi riêng. |
| Quyết định mới có tác động đáng kể | Hỏi trước: deploy/đổi release, downtime ngoài phạm vi, migration hoặc sửa/restore dữ liệu, xóa backup/dữ liệu/release/image, đổi quyền/secret/mạng, cài/upgrade phần mềm, phân vùng ổ, thêm dịch vụ hay lịch tự động. |

- Trước khi hỏi, chuẩn bị phương án cụ thể: **làm gì, trên bản nào, ảnh hưởng gì, cách phục hồi**. Một tin nhắn ngắn là đủ; chỉ cần thêm chi tiết khi rủi ro lớn.
- Không tự coi toàn bộ công việc là chưa được duyệt chỉ vì mở phiên mới. Tìm lại quyết định đã có và kiểm tra trạng thái. Hỏi lại khi phạm vi/commit, dữ liệu hoặc downtime thay đổi đáng kể; log, im lặng và một lệnh dán nhầm không phải đồng ý.
- Build trên server được phép trong gói đã duyệt và đủ tài nguyên. Không bắt buộc tạo CI/registry mới trước khi deploy. Push Git và deploy là hai hành động khác nhau: quyền push không tự cấp quyền chạy server; tuân theo yêu cầu Git đã được giao.
- Routine cleanup chỉ gồm file tạm **do chính tác vụ tạo**, biết chính xác đường dẫn, không secret/data, đã dùng xong, không cần làm chứng cứ và không còn tiến trình sử dụng. File không rõ nguồn gốc hoặc ngoài phạm vi đó phải hỏi trước.

## 2. Một công việc bảo trì nặng, kiểm tra vừa đủ

- Tối đa **một công việc bảo trì nặng đang hoạt động trên host**: build, backup/restore, migration, import/export lớn hoặc test tải. Một gói deploy thực hiện các bước này tuần tự. API, SQL, MinIO và worker bình thường vẫn chạy; đọc health/log nhẹ có thể thực hiện đồng thời.
- Có một người/agent điều phối. Kiểm tra tiến trình và khóa trước khi bắt đầu; không mở nhiều SSH để chạy cùng lệnh. Mất kết nối thì kiểm tra job còn chạy không, không tự chạy lại.
- Dùng khóa bảo trì hiện có trong `manage.sh`. `flock` chỉ ngăn các lệnh cùng dùng khóa; không giới hạn compiler hoặc mọi lệnh Docker khác.
- Trước task nặng, xem đúng máy, công việc đang chạy, `MemAvailable`, swap và dung lượng filesystem đích; khi cần mới xem CPU/I/O. Không chạy full scan hoặc test suite để kiểm tra một sửa đổi tài liệu.
- Trong lúc thao tác, theo dõi khi có output mới hoặc khoảng 1–2 phút; kiểm tra sớm hơn nếu có dấu hiệu lỗi. Không tạo daemon/cron chỉ để theo dõi một lần deploy, không duy trì nhiều vòng polling. Kết thúc phiên không đồng nghĩa vẫn giám sát 24/7.
- CPU cao trong build là bình thường nếu dịch vụ vẫn đáp ứng và RAM/I/O ổn. Không hủy job chỉ vì vượt một con số hoặc build mất nhiều phút. Không có tiến triển khoảng 10 phút thì kiểm tra nguyên nhân và báo, không retry mù.

Ngưỡng tham khảo cho máy khoảng 16 GiB RAM; đây là đề xuất vận hành của dự án, **không phải tiêu chuẩn bắt buộc của Docker/Ubuntu**:

| Chỉ số | Cảnh báo / cần đánh giá | Hoãn task nặng mới, báo ngay |
|---|---|---|
| RAM khả dụng | Dưới 4 GiB: xem ngân sách task và swap; không tự kết luận hết RAM | Dưới 2 GiB, OOM hoặc swap liên tục làm dịch vụ chậm |
| Đĩa đích | Trống dưới 15 GiB hoặc 15%; ước lượng image/cache/backup sẽ ghi | Dưới 5 GiB hoặc 5%, inode gần hết, hay không đủ chứa task + khoảng dự phòng |
| CPU/I/O | CPU cao kéo dài: đối chiếu health/độ trễ; load phải so với số CPU logic | Health lỗi/độ trễ tăng kéo dài kèm áp lực tài nguyên; không dùng CPU 80% làm nút dừng cứng |

Ngưỡng thấp không phải ngân sách cho task: dù còn 20 GiB, một backup 25 GiB vẫn phải hoãn. Đo cả nơi dữ liệu, Docker và backup thực sự nằm; không tính HDD chưa mount là dung lượng dùng được. Khi chưa rõ tải build/AI lớn, ước lượng/đo thử có kiểm soát trước trong phạm vi được duyệt.

## 3. Quy ước riêng của dự án

- Máy đối chiếu: `roadguardsystem`, `dev@192.168.1.11`; xác minh hostname và SSH fingerprint khi kết nối mới/chuyển máy. Không tắt kiểm tra host key/TLS để chữa lỗi.
- Nguồn release là `origin/deploy`; phân biệt HEAD GitHub, release tải về và release chạy. Ghi commit đầy đủ trong bằng chứng triển khai. Không sửa trực tiếp `/opt/roadguard/releases/<SHA>` hay `git pull` trong release; sửa ở máy phát triển, kiểm tra rồi phát hành theo phạm vi duyệt. Không push `main`.
- `manage.sh update` hiện lấy HEAD mới nhất của `deploy`, chưa nhận SHA mục tiêu: không chạy mù khi người dùng chỉ duyệt một SHA. Kiểm soát nhánh không đổi trong cửa sổ update hoặc chuẩn bị cơ chế pin SHA; nếu không bảo đảm được đúng bản thì hỏi trước khi chuyển hướng. Đây là giới hạn hiện có, chưa được sửa bởi rule.
- Gói `SERVER-DEVELOP-E2E` bổ sung `update-reviewed.sh EXPECTED_ACTIVE_SHA TARGET_SHA` để cập nhật theo SHA cố định. Chỉ dùng script thuộc bản đã kiểm thử/đối chiếu checksum; quy tắc backup, lock và quyền thao tác vẫn áp dụng. Yêu cầu ngày 08/11 lấy develop/test BE cho phép thực hiện gói trong spec liền mạch; không xin lại từng bước, nhưng không mở rộng thành reset dữ liệu hoặc thay đổi hạ tầng ngoài spec.
- Phạm vi hiện tại là BE + SQL + private MinIO phục vụ tích hợp/demo. FE/mobile/AI, SMTP, public domain hoặc dịch vụ mới là công việc riêng cần giao rõ. Không cài thêm stack giám sát, Kubernetes hay registry chỉ để làm bộ quy tắc trông đầy đủ.
- Container limits trong `compose.yaml`: API/maintenance mỗi service 1 GiB/4 CPU; SQL 4 GiB/4 CPU, SQL memory setting 2048 MB; MinIO 1 GiB/2 CPU. Đây là cấu hình source, phải kiểm tra runtime nếu đánh giá limit đang có hiệu lực. Không mặc định tăng limit khi lỗi.
- Build có giới hạn riêng: MinIO đã đặt `GOMAXPROCS=2`, `GOMEMLIMIT=768MiB`, `go build -p 2`; `GOMEMLIMIT` là [mục tiêu mềm của Go](https://go.dev/doc/gc-guide#Memory_limit), không phải trần toàn build. API `dotnet publish` chưa đặt ngân sách riêng. Một lệnh Compose có thể build nhiều image; chỉ giảm parallelism khi tài nguyên/đo thực tế yêu cầu. Không suy ra runtime limits cũng giới hạn build. [Docker runtime resources](https://docs.docker.com/engine/containers/resource_constraints/), [BuildKit parallelism](https://docs.docker.com/build/buildkit/configure/).

## 4. File đúng chỗ, dọn đúng phạm vi

| Nơi lưu | Mục đích |
|---|---|
| `/opt/roadguard/repository.git`, `releases/<SHA>`, `current` | Git bare, release cố định và symlink release active |
| `/opt/roadguard/roadguard-lan-ca.crt` | CA công khai cho client |
| `/etc/roadguard` | Config/secret/TLS riêng, quyền tối thiểu |
| `/srv/roadguard/{sql,objects,files,keys,backups}` | Dữ liệu bền vững và backup |
| Docker data directory | Image/cache/container log do Docker quản lý |
| `/tmp/roadguard-<purpose>-<revision-or-time>` | File tạm của một công việc, dọn khi hoàn tất theo mục 1 |

- Không rải clone, zip, slide hoặc report ở `/home/dev` và nhiều thư mục tùy ý. Docs và bằng chứng thao tác lưu ở máy phát triển trong spec/task hiện có. Không sinh bản “final2/new/latest” không biết nguồn.
- Upload script/artifact thực thi phải biết nguồn, đường dẫn và đối chiếu checksum; không truyền secret trong file tạm. Dọn đúng file tạm đã xác định, kiểm tra symlink/đường dẫn và tiến trình còn dùng; tránh wildcard xóa cả thư mục cha.
- Không xóa trực tiếp dữ liệu Docker hoặc truncate file log Docker. Dùng log rotation hiện có (10 MB × 3/container trong Compose); nó không giới hạn SQL log, host journal hoặc mọi log ứng dụng. [Docker JSON logging](https://docs.docker.com/engine/logging/drivers/json-file/).
- Không tự chạy `docker system prune`, `docker volume prune`, `compose down -v` hay xóa cache/image/release/backup theo tuổi. Chuẩn bị danh sách và dung lượng để duyệt một lần cho cả đợt dọn; bảo vệ bản active và bản phục hồi. Docker coi “unused” không đồng nghĩa dữ liệu vô giá trị. [Docker pruning](https://docs.docker.com/engine/manage-resources/pruning/).

## 5. Dữ liệu, backup và bí mật

- Không yêu cầu gửi mật khẩu qua chat hoặc đặt secret trong command line/history/log. User nhập sudo/tài khoản trực tiếp; không tự cấp NOPASSWD hoặc Docker group. Chỉ đọc trường cấu hình cần thiết, không dump JSON private, toàn bộ env/inspect hay dữ liệu người dùng.
- SQL và console MinIO giữ trong mạng Docker nội bộ; bucket private. Mở Internet/port mới, phân phối CA hoặc thay quyền cần duyệt; không phát private key/PFX. Có SSH không tự đồng nghĩa được thao tác root.
- `manage.sh backup` hiện **dừng API và MinIO**; update dùng bước backup này. Vì vậy backup phải nằm trong cửa sổ downtime đã được duyệt, không gọi là thao tác chỉ đọc. Lỗi có thể để dịch vụ dừng; báo ngay và dùng kế hoạch phục hồi đã duyệt.
- Backup có checksum và SQL `RESTORE VERIFYONLY` chưa thay thế thử restore. Backup cùng SSD chưa bảo vệ khi hỏng ổ. Cần chốt nơi lưu bản sao ngoài máy và retention trước khi giao hệ thống giữ dữ liệu quan trọng; không bắt thử restore cho mỗi lần kiểm tra thường lệ. [Microsoft VERIFYONLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql?view=sql-server-ver17).
- Không sửa migration đã áp, reset/seed demo/restore đè để làm health xanh. Rollback code sau migration chỉ khi đã xác minh tương thích; không tự xóa DB hoặc chạy lại wizard khi config/data đã có.

## 6. Báo sự cố đúng mức

**Báo ngay trong phiên khi phát hiện:** nguy cơ mất/lộ dữ liệu, OOM, đĩa sắp hết/không đủ cho bước tiếp, truy cập nhầm máy, dịch vụ lỗi ngoài cửa sổ cho phép, backup/migration thất bại, health không phục hồi sau thời gian chờ của bước deploy, hoặc xuất hiện job nặng cạnh tranh. Một lỗi health thoáng qua khi khởi động trong timeout đã dự kiến không tự coi là sự cố. Không chờ hết task mới báo sự cố thật.

Thông báo chỉ cần: **vấn đề → ảnh hưởng đã biết/chưa biết → bước đã làm → lựa chọn cần bạn chốt**. Không đổ nguyên log; đưa đoạn liên quan đã che secret. Cảnh báo không ảnh hưởng dịch vụ và nằm trong phạm vi xử lý thì tự điều tra, tổng hợp khi xong; không gửi lặp cùng một cảnh báo.

Khi sự cố, tạm ngừng khởi chạy thêm job. Chỉ hủy một task riêng của mình khi nó được xác định có thể hủy an toàn và cách hủy đã nằm trong phương án được duyệt; không tự kill SQL, migration, restore, reboot hoặc xóa dữ liệu. Nếu không có phương án an toàn đã duyệt thì thu thập thông tin nhẹ và hỏi chủ server.

## 7. Quy trình ngắn và bằng chứng

**Kiểm tra → trình cả gói nếu cần → thực hiện → xác minh/báo kết quả.** Một cập nhật trong task/spec hiện có là đủ: máy/commit, tác động, kết quả và việc còn thiếu. Không lập report tám trường cho một lệnh health; không hỏi lại từng lần `df`, xem log hoặc dọn file tạm hợp lệ.

Xác minh theo thay đổi: deploy cần trạng thái/health và luồng liên quan; sửa tài liệu chỉ cần kiểm tra tài liệu. Không restart để “chắc ăn”, chạy full E2E hay tạo dữ liệu thử trên DB dùng chung ngoài phạm vi. `Healthy` chứng minh health probes đạt ở thời điểm kiểm tra; đăng nhập, upload, backup restore và FE/AI phải có bằng chứng riêng.

**Nguồn và giới hạn:** policy dựa trên yêu cầu chủ server, `compose.yaml`, `manage.sh`, `common.sh`, hai Dockerfile và tài liệu chính thức đã đối chiếu ngày 08/11/2026. Ngưỡng RAM dùng `MemAvailable`, không chỉ cột RAM free: [Linux kernel /proc](https://docs.kernel.org/filesystems/proc.html). Các giá trị cảnh báo là đề xuất của dự án và có thể điều chỉnh theo số đo. Không tuyên bố đã có giám sát 24/7, resource enforcement hoặc tự phục hồi nếu mới chỉ viết rule.
