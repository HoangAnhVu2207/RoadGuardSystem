# Nonfunctional requirements (RF-04 historical transfer)

These are source contents, not newly Accepted rules. The per-item source status is retained as evidence; current authority and implementation readiness are separate. Owner review is required before each named module uses unconfirmed detail. Accepted 32-44 are in [requirements](requirements.md).

<a id="nfr-01"></a>

### NFR-01 - Phân quyền nhất quán ở API và tệp; không log password/OTP/token

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:351`; primary `RF-10-01` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-01.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-01 | Phân quyền nhất quán ở API và tệp; không log password/OTP/token | Test vai trò đúng, sai role, ngoài project, sai owner và URL tệp |</pre>

**NFR-01.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 01 | Bảo mật và phân quyền | Không đọc/ghi/tải tệp trái scope; không secret trong log; quyền server request kế tiếp | Ma trận 5 role × đúng/sai project/owner; thử ID trực tiếp và file download; thu hồi role/token; kiểm log đã redaction | 0 lần truy cập trái quyền trong bộ ca được duyệt; không khẳng định bao phủ mọi tấn công | BE/Security; TC-N01 |</pre>

Review gate: Review NFR-01 criterion against owner source before RF-10-01 release. Checkpoint: RF-10-01 acceptance/release; RF-11 retirement.

<a id="nfr-02"></a>

### NFR-02 - Dữ liệu quan trọng bền vững, retry không nhân đôi

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:352`; primary `RF-10-08` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-02.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-02 | Dữ liệu quan trọng bền vững, retry không nhân đôi | Restart worker/app giữa thao tác; replay key cùng/khác payload |</pre>

**NFR-02.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 02 | Bền vững và idempotency | Request cùng key+payload không tạo hai tác động; khác payload trả conflict | Ngắt sau DB commit trước response; retry; kill worker; so sánh số object/event/item | Một kết quả nghiệp vụ/key; không mất commit đã ACK; key retention phải chốt cho offline replay | BE/P2; TC-N02 |</pre>

Review gate: Review NFR-02 criterion against owner source before RF-10-08 release. Checkpoint: RF-10-08 acceptance/release; RF-11 retirement.

<a id="nfr-03"></a>

### NFR-03 - Chống ghi đè bản cũ, audit quyết định

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:353`; primary `RF-08` / proposed A; coordination RF-10-07, RF-10-03. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-03.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-03 | Chống ghi đè bản cũ, audit quyết định | Hai phiên PM đổi cùng đối tượng; thao tác stale bị phát hiện |</pre>

**NFR-03.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 03 | Concurrency và audit | Stale write không ghi đè; quyết định truy được actor/before/after/version | Hai PM đọc v1, A lưu v2, B ghi từ v1; xem audit | B bị conflict; dữ liệu A giữ nguyên; quyết định mới có audit | BE/P2; TC-N03 |</pre>

Review gate: Review NFR-03 criterion against owner source before RF-08 release. Checkpoint: RF-08 acceptance/release; RF-11 retirement.

<a id="nfr-04"></a>

### NFR-04 - Ngoại tuyến sau restart giữ dữ liệu đã lưu

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:354`; primary `RF-10-08` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-04.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-04 | Ngoại tuyến sau restart giữ dữ liệu đã lưu | Airplane mode, force-stop, khởi động lại, sync; không hứa app luôn chạy nền |</pre>

**NFR-04.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 04 | Offline bền vững | Task/policy/ảnh đã lưu tồn tại sau restart; không hết quyền tác nghiệp chỉ do thời gian mất mạng | Airplane, force-stop, reboot; hết token rồi sync; đầy đĩa lúc ghi | Không báo đã lưu trước durable write; phục hồi queue, không xóa pending; Q04/17 cho conflict/recovery | Android; TC-N04 |</pre>

Review gate: Review NFR-04 criterion against owner source before RF-10-08 release. Checkpoint: RF-10-08 acceptance/release; RF-11 retirement.

<a id="nfr-05"></a>

### NFR-05 - Toàn vẹn tệp và tham chiếu

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:355`; primary `RF-10-04` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-05.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-05 | Toàn vẹn tệp và tham chiếu | Kiểm checksum, thiếu part, URL hết hạn, tệp sai MIME/nội dung; không coi ETag multipart mặc định là MD5 toàn tệp |</pre>

**NFR-05.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 05 | Toàn vẹn tệp | Đúng nội dung/checksum/reference trước xác nhận hoàn tất | Thiếu part, đổi byte, MIME giả, URL hết hạn, retry multipart | Không complete thiếu/hỏng; không dùng ETag multipart giả MD5; resume có kiểm quyền | BE/Android; TC-N05 |</pre>

Review gate: Review NFR-05 criterion against owner source before RF-10-04 release. Checkpoint: RF-10-04 acceptance/release; RF-11 retirement.

<a id="nfr-06"></a>

### NFR-06 - Hiệu năng API metadata tách tác vụ dài

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:356`; primary `RF-10-09-B` / proposed B; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-40`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-06.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-06 | Hiệu năng API metadata tách tác vụ dài | Đo p50/p95/p99 dưới workload được duyệt; mục tiêu số ms, concurrent users là PERF-TBD |</pre>

**NFR-06.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 06 | Hiệu năng API | Metadata không chờ video/AI dài; pagination và tải theo scope | Đo p50/p95/p99, error rate, throughput; cold/warm riêng, workload §14.3 | PERF-TBD; đề xuất metadata p95 ≤2s, lỗi server &lt;1% ở tải baseline; chưa là SLA | Tech Lead/QA/PO; TC-N06 |</pre>

Review gate: Review NFR-06 criterion against PR-40 before RF-10-09-B release. Checkpoint: RF-10-09-B acceptance/release; RF-11 retirement.

<a id="nfr-07"></a>

### NFR-07 - Bản đồ theo viewport/zoom, không tải tất cả video/tấm toàn dự án

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:357`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-07.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-07 | Bản đồ theo viewport/zoom, không tải tất cả video/tấm toàn dự án | Test tuyến mẫu + lượng đối tượng được chốt; ngân sách bộ nhớ/FPS là MAP-TBD |</pre>

**NFR-07.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 07 | Bản đồ và khả năng mở rộng truy vấn | Viewport/zoom; không tải toàn bộ video/tấm dự án | Pan/zoom, đổi nhánh với dataset đã chốt; đo network/memory/render | MAP-TBD; đề xuất time-to-interactive overlay p95 ≤3s trên thiết bị baseline; chưa chốt FPS/RAM | FE/BE; TC-N07 |</pre>

Review gate: Review NFR-07 criterion against owner source before RF-10-02 release. Checkpoint: RF-10-02 acceptance/release; RF-11 retirement.

<a id="nfr-08"></a>

### NFR-08 - Khôi phục backup DB và object storage đồng bộ

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:358`; primary `RF-10-09-A` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-40`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-08.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-08 | Khôi phục backup DB và object storage đồng bộ | Restore thử, kiểm referential integrity/checksum; RPO/RTO là OPS-TBD, chưa có SLA cam kết |</pre>

**NFR-08.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 08 | Sao lưu/khôi phục | Restore DB và object storage cùng căn cứ nhất quán | Restore môi trường cách ly; đối chiếu FK/checksum/đếm hồ sơ; không gửi thông báo thật | OPS-TBD; đề xuất RPO ≤24h, RTO ≤8h để thảo luận, phải được chủ dự án ký chốt | Ops/P2; TC-N08 |</pre>

Review gate: Review NFR-08 criterion against PR-40 before RF-10-09-A release. Checkpoint: RF-10-09-A acceptance/release; RF-11 retirement.

<a id="nfr-09"></a>

### NFR-09 - Quan sát vận hành có correlation ID

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:359`; primary `RF-10-09-A` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-09.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-09 | Quan sát vận hành có correlation ID | Theo dấu report → task → upload/job → kết quả; alert lỗi retry/exhaustion và queue age |</pre>

**NFR-09.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 09 | Quan sát vận hành | Correlation xuyên request/outbox/job; lỗi retry và queue age có theo dõi | Gây lỗi storage/AI; trace từ report đến job; kiểm redaction | Tra được nguyên nhân, retry count và owner xử lý; ngưỡng alert/retention log OPS-TBD | Ops/BE; TC-N09 |</pre>

Review gate: Review NFR-09 criterion against owner source before RF-10-09-A release. Checkpoint: RF-10-09-A acceptance/release; RF-11 retirement.

<a id="nfr-10"></a>

### NFR-10 - Tách mock/real, tái lập phân tích

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:360`; primary `RF-10-05` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-10.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-10 | Tách mock/real, tái lập phân tích | Cùng manifest/model/config cho phép truy lại nguồn; không dùng mock trong bảng accuracy thật |</pre>

**NFR-10.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 10 | Tái lập AI | Manifest/model/config/raw immutable; mock tách real; late result không ghi đè | Replay job, model khác, split khác; kiểm hash và lineage | Mọi kết quả có nguồn/version; accuracy AI-TBD theo dataset; mock không chứng minh accuracy | AI/BE; TC-N10 |</pre>

Review gate: Review NFR-10 criterion against owner source before RF-10-05 release. Checkpoint: RF-10-05 acceptance/release; RF-11 retirement.

<a id="nfr-11"></a>

### NFR-11 - Migration và nâng cấp bảo toàn enum/dữ liệu cũ

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:361`; primary `RF-09` / proposed A; coordination RF-10-03, RF-10-04. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-11.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-11 | Migration và nâng cấp bảo toàn enum/dữ liệu cũ | Test migration trên snapshot có dữ liệu, không drop lịch sử để đạt AC |</pre>

**NFR-11.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 11 | Migration và tương thích | Không mất lịch sử/enum legacy; đọc được snapshot cũ | Migration bản sao data thực đã bảo vệ PII; backfill null có kiểm; restore trước rollback phá hủy | Đối chiếu counts/FK/enum/hash; không tạo GPS hoặc BEFORE giả; plan rollback được duyệt | P2; TC-N11 |</pre>

Review gate: Review NFR-11 criterion against owner source before RF-09 release. Checkpoint: RF-09 acceptance/release; RF-11 retirement.

<a id="nfr-12"></a>

### NFR-12 - Giao diện tiếng Việt, đơn vị rõ và thông báo có thể xử lý

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:362`; primary `RF-10-08` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-12.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-12 | Giao diện tiếng Việt, đơn vị rõ và thông báo có thể xử lý | Không đảo lat/lon, không nhầm m/mm, hiển thị pending/offline/conflict |</pre>

**NFR-12.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 12 | Khả dụng và ngôn ngữ | Tiếng Việt, đơn vị và source rõ; offline/conflict/pending khác nhau | Walkthrough WF, keyboard/focus, số thập phân/lat-lon/m-mm | Không nhầm đơn vị/trạng thái trong ca; lỗi có hành động phục hồi; accessibility chi tiết cần chốt | UX/FE/QA; TC-N12 |</pre>

Review gate: Review NFR-12 criterion against owner source before RF-10-08 release. Checkpoint: RF-10-08 acceptance/release; RF-11 retirement.

<a id="nfr-13"></a>

### NFR-13 - Hỗ trợ thiết bị/OS/browser xác định trước nghiệm thu

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:363`; primary `RF-10-08` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-13.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-13 | Hỗ trợ thiết bị/OS/browser xác định trước nghiệm thu | Ma trận Android, bộ nhớ, camera, drone, browser cần chủ dự án/nhóm cung cấp |</pre>

**NFR-13.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 13 | Tương thích thiết bị | Ma trận OS/browser/drone/firmware/video parser có version | Chạy thao tác trọng tâm trên từng thiết bị được chọn | Mỗi ô áp dụng có result; chưa chọn version/hardware thì BLOCKED, không “hỗ trợ mọi Android” | PO/QA; TC-N13 |</pre>

Review gate: Review NFR-13 criterion against owner source before RF-10-08 release. Checkpoint: RF-10-08 acceptance/release; RF-11 retirement.

<a id="nfr-14"></a>

### NFR-14 - Giấy phép, dữ liệu bản đồ và weights có nguồn

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:364`; primary `RF-10-05` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**NFR-14.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| NFR-14 | Giấy phép, dữ liệu bản đồ và weights có nguồn | Ghi package/version/license, model hash/dataset rights trước phát hành; không suy open source = mọi dữ liệu miễn phí |</pre>

**NFR-14.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>| 14 | Giấy phép và quyền dữ liệu | Package/model/weights/map/dataset có nguồn và quyền sử dụng | Kiểm BOM, license, model hash, quyền offline tiles, quyền ảnh huấn luyện | Không phát hành thành phần chưa được rà quyền theo phạm vi sản phẩm | Tech Lead/chủ dự án; TC-N14 |</pre>

Review gate: Review NFR-14 criterion against owner source before RF-10-05 release. Checkpoint: RF-10-05 acceptance/release; RF-11 retirement.
