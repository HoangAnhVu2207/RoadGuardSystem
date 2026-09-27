# RoadGuard Backend — In Scope và Out of Scope

Ngày: 22/09/2026. Trạng thái: phạm vi mục tiêu để lập kế hoạch hotfix; chưa phải bằng chứng runtime đã triển khai.

## In scope

Backend ASP.NET Core C# là nơi giữ nghiệp vụ, quyền, dữ liệu bất biến, audit và hợp đồng cho FE. Phạm vi mục tiêu gồm:

- Role `REPORTER`, phân loại `CITIZEN` hoặc `INVESTOR_REPRESENTATIVE`; Reporter gửi phản ánh, bổ sung ảnh và xem tiến độ/hình ảnh sau sửa trong phạm vi của mình.
- Reporter tự đăng ký bằng Gmail (`gmail.com`/`googlemail.com`), nhận OTP qua Gmail adapter và chỉ được kích hoạt sau khi xác minh. Luồng này dùng email + mật khẩu RoadGuard; Google OAuth/Sign-in remains out of scope.
- Mỗi `ReportPhoto` lưu tọa độ và nguồn tọa độ riêng (`DEVICE_CAPTURE`, `EXIF`, `MANUAL`), độ chính xác khi có, thời điểm và thông tin kiểm chứng. Không thay tọa độ ảnh cũ bằng vị trí lúc upload.
- `IncidentCase` với luồng `New -> Assigned -> Open -> Fixed -> Retest -> Verified -> Closed`, lịch sử chuyển trạng thái và timeline công khai riêng cho Reporter. `Defect.VERIFIED` vẫn là quyết định xác nhận lỗi trước sửa; `IncidentCase.VERIFIED` là retest sau sửa đạt.
- PM chọn kiểm chứng bằng drone hoặc Repair Crew; đo thực địa là điều kiện theo bằng chứng/rule khi cần, còn track nghiên cứu ground-truth vẫn bắt buộc và tách khỏi nghiệp vụ vận hành.
- Supervisor/PM nhập, vẽ hoặc import polyline tuyến cong; backend xác nhận SRID, chiều dài, lý trình và tạo bộ segment theo station. PM được đổi 100 m, 250 m, 500 m, 1 km hoặc độ dài hợp lệ khác; được chia, gộp và kéo ranh giới trước khi công bố `RoadSegmentSet` phiên bản mới.
- Nhiệm vụ khảo sát khai báo `SURFACE`, `LEFT_EDGE`, `RIGHT_EDGE`; coverage được đánh giá độc lập theo segment, band và phiên bản dữ liệu. GPS máy bay, station trên tuyến, footprint camera và vị trí lỗi là các fact riêng.
- Adapter AI ngoài bất đồng bộ theo manifest bất biến, segment set/version, target band, video interval, telemetry/SRT, model/config và checksum. Kết quả có provenance, chất lượng/coverage và candidate detections; AI không tự quyết định “không phải lỗi”, sửa, verify hoặc close.
- PM nhập phương án sửa tổng quát; Supervisor duyệt phạm vi/phương án; Crew nộp kết quả và ảnh sau sửa; PM/Supervisor kiểm tra theo quyền.
- Hợp đồng API, ProblemDetails/error code, idempotency, retry, concurrency, audit, notification/outbox và read model cần thiết để FE tích hợp.
- Backend Research Validation nhập/ghép dữ liệu có provenance, đo ground-truth và tính sai số; không ghi ngược vào `Defect`/`Warranty` vận hành.

## Out of scope

- Android App, Web Dashboard, MapLibre/Terra Draw UI và UX chi tiết; backend chỉ cung cấp hợp đồng, geometry validation và dữ liệu để FE hiển thị.
- Huấn luyện mô hình, GPU/YOLO/DSM/orthophoto pipeline, chứng nhận độ chính xác AI hoặc quyết định nghiệp vụ tự động từ model. Python là một triển khai bên ngoài, không phải ràng buộc kiến trúc.
- Dùng hai tọa độ đầu-cuối để suy đoán đường cong; tự sửa tuyến chuẩn theo track bay lần sau; kéo mọi GPS lệch về tim đường để làm đạt coverage.
- Thiết kế biện pháp thi công từng bước, chia phase kỹ thuật, vật liệu/chủng loại/số lượng/đơn giá chi tiết, định mức, nhân công, máy móc, kho, mua sắm, nhà thầu hoặc điều hành công trường.
- Quản lý chi phí, ngân sách, dự toán, đơn giá hoặc giá trị thanh toán dưới bất kỳ hình thức nào.
- Theo dõi tiến độ thi công chi tiết. Các trạng thái tiếp nhận, kiểm chứng, phê duyệt, sửa xong và retest là tiến độ hồ sơ; không phải các phase thi công.
- Tự động phát hành kết luận “không phải lỗi” khi AI không có detection hoặc video đã chạy thành công.
- Thêm package, migration, đổi schema, broker, cloud provider hoặc giao thức AI thật chỉ từ tài liệu này. Mỗi thay đổi phải có scope card và owner approval riêng.

## Ranh giới trách nhiệm

| Thành phần | Trách nhiệm |
|---|---|
| FE | Vẽ/chỉnh route, xem preview segment, chọn band, upload và hiển thị timeline; gửi GeoJSON/command hợp đồng |
| Backend | Quyền và scope, geometry/station/version, state machine, manifest/job, validation, audit, persistence và response |
| AI ngoài | Phân tích dữ liệu đã cấp quyền và trả kết quả có version/provenance |
| PM/Supervisor | Quyết định nghiệp vụ, yêu cầu kiểm chứng, duyệt sửa, retest và công bố kết quả |
| Drone Operator/Repair Crew | Thực hiện nhiệm vụ được giao và nộp bằng chứng |

## Tiêu chí phạm vi

Một slice chỉ được coi là hoàn tất khi có hợp đồng API/domain, persistence phù hợp, quyền/audit, idempotency hoặc concurrency nếu có, test theo rủi ro và cập nhật tài liệu liên quan. Tài liệu mục tiêu không được dùng để tuyên bố tính năng đã chạy.
