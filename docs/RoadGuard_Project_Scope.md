# RoadGuard — Phạm vi toàn bộ dự án

Ngày cập nhật: 22/09/2026  
Nguồn yêu cầu nền: `C:\Users\HoangAnhVu\Downloads\RoadGuard_Contractor_Warranty_Inspection_phuonglhk.md`  
Trạng thái: phạm vi sản phẩm/capstone đã chuẩn hóa; chưa có nghĩa mọi module đã được triển khai.

## Cách dùng tài liệu nguồn

Phiếu khảo sát ban đầu được dùng như nguồn yêu cầu và bối cảnh nghiệp vụ. Những đề xuất trong phiếu không tự trở thành cam kết triển khai nếu mâu thuẫn với quyết định sản phẩm mới hơn. Quyết định hiện hành được ưu tiên là:

- không quản lý cost, ngân sách, dự toán, đơn giá, retention value hoặc thanh toán trong hệ thống;
- PM chỉ nhập phương án sửa tổng quát và kết quả/ảnh sau sửa;
- AI đưa ra ứng viên và phép đo có nguồn gốc, không tự quyết định lỗi, trách nhiệm, sửa chữa hoặc đóng hồ sơ;
- Reporter tự đăng ký bằng Gmail và xác minh OTP trước khi dùng tài khoản;
- route cong phải có polyline chuẩn được vẽ/import/review trước khi chia segment;
- segment có độ dài cấu hình được và bộ đã công bố phải version hóa bất biến.

## In Scope toàn bộ dự án

### 1. Quản lý dự án, tuyến và warranty

- Đăng ký dự án, road section, ngày bàn giao/nghiệm thu, thời hạn warranty và trạng thái hồ sơ.
- Nhập, vẽ hoặc import polyline thực tế; lưu phiên bản hình học, CRS/SRID, lý trình và điểm đầu/cuối.
- PM tạo, chỉnh, chia, gộp và công bố `RoadSegmentSet` với chiều dài 100 m, 250 m, 500 m, 1 km hoặc giá trị hợp lệ khác.
- Giữ lịch sử road version, segment set, survey, defect, evidence và quyết định của người dùng.
- Lập kế hoạch baseline, periodic và incident survey; nhắc việc không tự tạo lệnh bay.

Phạm vi warranty ở đây là theo dõi thời điểm, tình trạng, lỗi, bằng chứng và thời hạn xử lý. Hệ thống không lưu giá trị hợp đồng, tiền giữ lại, ngân sách hoặc chi phí sửa.

### 2. Reporter và phản ánh hư hỏng

- Reporter là Citizen hoặc InvestorRepresentative.
- Reporter tự đăng ký bằng Gmail `gmail.com`/`googlemail.com`, nhận OTP, xác minh email và tạo tài khoản `REPORTER`.
- Tài khoản pending không được đăng nhập, gửi report hoặc nhận token.
- Reporter gửi mô tả và nhiều ảnh; mỗi ảnh có GPS thiết bị, EXIF hoặc vị trí nhập thủ công riêng.
- Reporter chỉ xem report của mình, timeline được công bố và ảnh sau sửa được PM chọn công bố.
- Report tạo `IncidentCase` với luồng `New -> Assigned -> Open -> Fixed -> Retest -> Verified -> Closed`.

### 3. Khảo sát drone và dữ liệu hiện trường

- PM lập lịch và giao phạm vi khảo sát; Drone Operator nhận nhiệm vụ và nộp video, ảnh, SRT/telemetry, metadata chuyến bay và quality checks.
- Survey có thể yêu cầu `SURFACE`, `LEFT_EDGE`, `RIGHT_EDGE`; coverage được đánh giá riêng theo segment, band và dataset.
- Lưu raw aircraft GPS, projected station, camera footprint, defect location và độ chính xác như các fact riêng.
- Hỗ trợ bay baseline/periodic/incident, nhiều segment trong một nhiệm vụ và nhiều video interval cho cùng vùng.
- Có offline upload, checksum, retry, idempotency, server confirmation và immutable source files.
- Quy trình vận hành phải ghi pre-flight check, điều kiện thời tiết/ánh sáng, failsafe và flight authorization.

### 4. Photogrammetry và GIS

- Tạo orthomosaic georeferenced và digital surface model từ ảnh chồng lấn.
- Hỗ trợ ground control point/RTK hoặc nguồn định vị khác khi có, kèm provenance và uncertainty.
- Tính các phép đo geometry phù hợp: depression depth, ponding extent, slab faulting và edge/shoulder extent.
- Lưu CRS, phương pháp, độ chính xác, uncertainty và phiên bản pipeline cho mỗi kết quả.
- So sánh baseline/current và ánh xạ theo station/segment version.

### 5. AI defect detection và human-in-the-loop

- Hệ AI nhận manifest bất biến theo road version, segment set, segment, band, video interval, telemetry/SRT, model/config và checksum.
- Phân tích các defect type mục tiêu: deep crater, pothole, depression/ponding, slab edge breakage, slab cracking và shoulder erosion.
- Trả candidate detection, bbox/mask, frame/time, confidence, vị trí/phạm vi, measurement, quality/coverage và raw provenance.
- Theo dõi processing job async, retry, dedup, late result, model version và failure classification.
- PM/engineer review, confirm, correct, reclassify, reject, merge và ghi lý do; kết quả `No detections` không phải kết luận `NO_DEFECT`.
- Cause attribution chỉ là gợi ý theo construction, traffic, water/drainage hoặc third-party excavation; engineer/PM xác nhận.
- Matching giữa các kỳ khảo sát phân loại new/stable/growing và tốc độ thay đổi khi đủ dữ liệu.

### 6. Incident verification và repair workflow

- PM chọn kiểm chứng bằng drone hoặc Repair Crew; field measurement chỉ bắt buộc khi rule/bằng chứng cần.
- PM kết luận `DEFECT_FOUND` hoặc `NO_DEFECT` có lý do; `Defect.VERIFIED` là quyết định trước sửa.
- PM chỉ nhập phương án sửa tổng quát; không có financial fields hoặc chi tiết thi công.
- Supervisor duyệt phạm vi/phương án; PM giao Repair Crew.
- Crew nhận việc, làm offline, ghi tiến độ, ảnh trước/sau và kết quả; PM kiểm tra, yêu cầu sửa lại hoặc trình Supervisor.
- Retest đạt chuyển `IncidentCase.VERIFIED`; Supervisor xác nhận `Closed`; Reporter nhận kết quả được công bố.

### 7. Web, Mobile và báo cáo

- Web Dashboard: portfolio, project/warranty status, map route/segment, defect review, baseline/growth comparison, incident timeline, repair planning và evidence pack.
- Mobile Field Module: nhiệm vụ drone/crew, navigation, offline drafts, upload queue, evidence capture và synchronization.
- Evidence pack: baseline/current imagery, measurements, defect history, source/provenance, audit trail và quyết định human review.
- Role/project access, notification, audit, retention và export theo quyền.

### 8. Research Validation và field trial

- Thu thập ground truth bằng engineer với straightedge/depth gauge cho depression depth và slab faulting mẫu.
- Tạo annotated dataset theo defect type/severity và nguồn thật/synthetic rõ ràng.
- So sánh RGB-only với RGB-plus-surface-model; báo cáo precision, recall, F1 theo từng class.
- Tính bias, MAE, RMSE, measurement uncertainty và sample exclusions; không ghi ngược research result vào operational Defect/Warranty.
- Kiểm định liên hệ giữa shoulder erosion và slab edge breakage.
- Đo flight time, processing time, review effort/km và usability trong field trial.

## Out of Scope toàn bộ dự án

- Quản lý cost, ngân sách, dự toán, đơn giá, giá trị hợp đồng, retention value, thanh toán hoặc financial exposure.
- Tự động quyết định defect liability, trách nhiệm hợp đồng, warranty claim hoặc yêu cầu bồi hoàn.
- Tự động duyệt phương án sửa, giao sửa, nghiệm thu hoặc đóng IncidentCase từ AI.
- Chi tiết biện pháp thi công: phase kỹ thuật, vật liệu, khối lượng, nhân công, máy móc, procurement, kho hoặc điều hành công trường.
- Điều khiển drone trực tiếp, autonomous flight control hoặc thay thế giấy phép/tuân thủ bay của đơn vị vận hành.
- Cam kết độ chính xác AI, measurement uncertainty hoặc ngưỡng severity trước khi có dữ liệu validation và engineer approval.
- Suy đoán đường cong từ hai tọa độ đầu-cuối hoặc tự sửa route chuẩn theo GPS của lần bay sau.
- Huấn luyện mô hình production/GPU deployment/AI provider SLA trong backend core; AI thật là external integration, mock dùng cho software acceptance.
- Google OAuth/Google Sign-In; Gmail chỉ dùng để gửi OTP xác minh Reporter.
- Xóa hoặc viết đè raw media, survey, defect, measurement, evidence, audit hoặc quyết định đã công bố.
- Thay thế hoàn toàn kỹ sư/PM/Supervisor trong quyết định kỹ thuật và contractual dispute.

## Các điểm khách hàng dễ hiểu là In Scope

Bảng này dùng để chốt kỳ vọng ngay từ đầu. Một yêu cầu nằm ở cột giữa không tự động trở thành việc phải làm trong release hiện tại; nếu khách hàng yêu cầu, phải tạo change request với người phụ trách, dữ liệu đầu vào, tiêu chí nghiệm thu và tác động hạ tầng.

| Chủ đề | Khách hàng có thể nghĩ hệ thống phải làm | Ranh giới của dự án hiện tại |
|---|---|---|
| Ứng dụng Web/Mobile | Có sản phẩm production hoàn chỉnh, publish store và hỗ trợ mọi thiết bị | Có module FE theo workflow đã chốt; publish store, device matrix và bảo trì dài hạn là release/operations riêng |
| Điều khiển drone | Backend tự điều khiển drone, tự cất/hạ cánh và tự tránh vật cản | RoadGuard lập nhiệm vụ, lưu flight metadata và nhận dữ liệu; điều khiển bay thuộc hệ thống vận hành drone bên ngoài |
| Giấy phép bay | Dự án tự xin và bảo đảm mọi giấy phép bay | Dự án ghi nhận compliance checklist và hồ sơ được cung cấp; pháp lý/giấy phép do đơn vị vận hành chịu trách nhiệm |
| Thiết bị bay | Nhóm dự án cung cấp drone, camera, RTK/GCP và phụ tùng | Thiết bị, thuê bay, khảo sát và GCP là điều kiện triển khai thực địa; không nằm trong backend build mặc định |
| Photogrammetry | Hệ thống luôn tạo DSM/orthomosaic chính xác cho mọi video | Pipeline và hợp đồng kết quả nằm trong scope; chất lượng phụ thuộc ảnh, GCP/RTK, thời tiết và phải được validation |
| AI accuracy | AI nhận diện đúng 100%, đo đúng tuyệt đối và tự đưa ra kết luận | Có model/adapter, provenance, metric và human review; accuracy/production model là phần nghiên cứu hoặc release riêng |
| AI training | Nhóm phải thu thập đủ dữ liệu và train model production | Dataset, annotation và thí nghiệm nghiên cứu là deliverable có điều kiện; training GPU production ngoài backend core |
| Ground truth | Mọi segment/mọi lỗi đều phải được đo vật lý | Ground truth bắt buộc cho mẫu Research Validation hoặc khi PM/rule cần căn cứ; không đo mọi operational case |
| Cause attribution | Hệ thống xác định chắc chắn lỗi do nhà thầu, xe quá tải, ngập hoặc bên thứ ba | Hệ thống chỉ gợi ý cause category và lưu evidence; engineer/PM chịu trách nhiệm xác nhận |
| Warranty/legal | Hệ thống tự phân xử tranh chấp và xác định nghĩa vụ bảo hành | Theo dõi timeline, baseline, defect history và evidence pack; không thay tư vấn pháp lý hoặc quyết định trách nhiệm |
| Cost/financial | Có dự toán sửa, cost exposure, retention, ngân sách và thanh toán | Toàn bộ financial data bị loại khỏi sản phẩm; chỉ lưu phương án sửa tổng quát và kết quả thực hiện |
| Thi công | Có BOQ, vật liệu, định mức, nhân công, máy móc, procurement và tiến độ từng phase | Repair workflow chỉ quản lý assignment, progress, evidence, review và retest |
| Bản đồ và routing | MapLibre tự cung cấp bản đồ, routing, geocoding và dữ liệu đường | FE dùng map provider/routing được chọn; BE xác nhận geometry/version/station, không cam kết dữ liệu bản đồ bên thứ ba |
| Tile/API bên ngoài | Dự án thanh toán và bảo đảm quota của Map provider, OSRM, Gmail, storage hoặc AI | Provider contract, secret, quota, billing và SLA phải được chốt riêng theo môi trường triển khai |
| Gmail | Gmail OTP đồng nghĩa Google OAuth hoặc đăng nhập bằng Google | Chỉ gửi OTP đến Gmail để xác minh Reporter; đăng nhập dùng credential RoadGuard |
| Notification | Có SMS, Zalo, WhatsApp, push và email transactional đầy đủ | Backend có notification contract/outbox; từng provider/channel phải là integration slice riêng |
| Offline | Mọi màn hình và mọi thao tác đều hoạt động khi mất mạng | Field draft, upload queue, evidence và sync là scope; thao tác cần server authority vẫn phải chờ mạng |
| Dữ liệu cũ | Hệ thống tự import toàn bộ hồ sơ, video, GPS và Excel lịch sử | Migration/backfill chỉ làm khi có inventory, format, mapping, owner và acceptance riêng |
| Tích hợp doanh nghiệp | Kết nối ERP, accounting, CMMS, document management, SSO hoặc investor portal | Chỉ có API/domain contract của RoadGuard; mỗi hệ thống ngoài là integration project riêng |
| Báo cáo | Có mọi biểu mẫu pháp lý, dashboard tùy ý và báo cáo theo mọi mẫu khách hàng | Có evidence pack và các report đã định nghĩa; report mới cần field mapping và acceptance riêng |
| Hiệu năng | Bất kỳ video/road length nào cũng xử lý trong vài phút | Có mục tiêu đo turnaround và queue metrics; SLA phải chốt theo dataset, hardware và provider |
| Scale | Hệ thống chạy vô hạn cho mọi công ty/tỉnh/dự án | Kiến trúc có project scope và storage/versioning; capacity, sharding, multi-region là phase vận hành riêng |
| Bảo mật | Có chứng nhận ISO, penetration test, SOC 2 hoặc compliance pháp lý hoàn chỉnh | Có auth, authorization, audit, secret handling và security tests; chứng nhận/đánh giá độc lập là deliverable riêng |
| Backup/DR | Có backup, disaster recovery và RTO/RPO production đầy đủ | Có persistence contract và release runbook; backup policy, restore rehearsal, RTO/RPO phải được owner vận hành duyệt |
| Monitoring/SLA | Có on-call 24/7, alerting và support production | Có job status, audit và error handling; vận hành 24/7 và support contract nằm ngoài capstone build |
| Đào tạo người dùng | Nhóm phải đào tạo toàn bộ nhân viên và hỗ trợ tại công trường | Có thuật ngữ, acceptance flow và tài liệu sử dụng tối thiểu; training/onsite support là hạng mục riêng |
| Độ chính xác vị trí | Mọi pixel lỗi đều có tọa độ centimeter | Hệ thống lưu phương pháp và uncertainty; độ chính xác phụ thuộc camera, pose, GPS, RTK/GCP và phải hiển thị unknown khi thiếu căn cứ |
| An toàn công trường | Phần mềm chịu trách nhiệm an toàn người, xe và chuyến bay | Phần mềm lưu checklist/evidence; người vận hành và đơn vị thi công chịu trách nhiệm safety procedure |
| Dữ liệu cá nhân | Có thể lưu mọi thông tin người dân, khuôn mặt, biển số và vị trí | Chỉ lưu dữ liệu tối thiểu cho ownership/evidence; masking, retention và legal basis cần policy riêng |
| Mở rộng loại lỗi | Khách hàng có thể thêm mọi defect type mà không đổi thiết kế | Catalog có version; loại lỗi mới phải có schema/rule/model/label/acceptance tương ứng |
| Thay đổi state | Có thể thêm trạng thái tùy ý trong UI | State machine là hợp đồng; thêm trạng thái phải cập nhật domain, API, migration, audit và test |
| API public | API hiện tại là public contract ổn định cho mọi bên | API version/error code là contract của release; public partner API cần security, quota và compatibility review |

### Quy tắc xử lý yêu cầu phát sinh

Một yêu cầu mới chỉ được đưa vào scope khi trả lời được sáu câu hỏi: actor nào gọi, dữ liệu đầu vào nào có thật, module nào sở hữu, output/tiêu chí nghiệm thu là gì, ai vận hành/chi trả dịch vụ ngoài và ảnh hưởng tới schema/hạ tầng/bảo mật ra sao. Nếu chưa trả lời được, ghi nhận là `Candidate Change`, không hứa trong scope hiện tại.

## Phân chia trách nhiệm

| Module | Nằm trong dự án | Ranh giới |
|---|---|---|
| Backend/API | Auth, Reporter, project, route/segment, survey, defect, IncidentCase, repair evidence, jobs, audit, spatial persistence | Không chứa UI, AI model training hoặc điều khiển drone |
| Web | Dashboard, map editing, review, approvals, evidence export | Gọi backend; không tự quyết định nghiệp vụ hoặc tự đo làm nguồn chính |
| Mobile | Field task, offline, upload, navigation, evidence | Không tự xác nhận Verified/Closed |
| AI/Photogrammetry | Pipeline, detection, geometry, matching, metrics | Candidate/provenance; engineer/PM quyết định |
| Operations/Research | Flight safety, permits, GCP/RTK, ground truth, field trial | Không biến tài liệu khảo sát thành runtime feature tự động |

## Điều kiện nghiệm thu cấp dự án

1. Có baseline và current survey truy vết được tới route version, segment set và source media.
2. Có thể chạy report → PM verification → repair evidence → retest → Reporter publication.
3. AI/photogrammetry có provenance, uncertainty/quality và không tự tạo quyết định pháp lý/nghiệp vụ.
4. Dữ liệu raw và lịch sử quyết định không bị ghi đè; retry/late result không nhân đôi.
5. Offline field workflow đồng bộ được mà không làm mất evidence.
6. Research Validation có ground truth và báo cáo theo từng defect class/measurement type.
7. Không có financial field nào trong product schema, API contract hoặc dashboard.

## Tài liệu liên quan

- [Backend scope](RoadGuard_Backend_Scope.md)
- [Change 1 Record](Change_1_Record.md)
- [Incident/segment design](diagram/RoadGuard_Incident_Segment_Design_v1.md)
- [AI/edge design](diagram/RoadGuard_AI_Segment_Edge_Design_v1.md)
- [Use cases](diagram/Dac_ta_UseCase_v2.md)
- [User stories](diagram/User_Stories_Acceptance_Criteria_v2.md)
