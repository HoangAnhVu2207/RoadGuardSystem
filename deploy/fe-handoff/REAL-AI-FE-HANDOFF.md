# Kiểm thử hai tuyến với AI thật trên Docker Desktop

Gói local chạy BE, FT95 CPU, SQL Server, MinIO và Mailpit. Web gọi BE trên máy chạy Docker. AI nhận yêu cầu ở `http://ai:8999/` bên trong mạng Compose. Đăng nhập bằng tài khoản của đúng bộ volume. Với ứng dụng mobile trên thiết bị riêng, các địa chỉ loopback trỏ về chính thiết bị. Bản này được kiểm thử từ máy chạy Docker.

## Chạy bản đóng gói

Tại thư mục chứa `images.tar`, dùng Python 3.11 trở lên có thư viện `requests`, Docker Desktop ở chế độ Linux và .NET 8 với ASP.NET Core runtime.

```powershell
python package_local.py replay --work ../rg-fe-data
```

BE dùng `http://127.0.0.1:38081`, Mailpit dùng `http://127.0.0.1:38026`. Trong thư mục dữ liệu vừa chọn, mở `private/TAI-KHOAN-DOCKER-LOCAL.md` để lấy 14 tài khoản. Mỗi bộ volume sinh mật khẩu và ID nghiệp vụ riêng. ID dự án khởi tạo được giữ cố định. Dùng lại cùng thư mục với cùng bộ volume khi chạy tiếp.

```powershell
docker --context desktop-linux compose -p rg-fe-real-replay -f compose.json stop
docker --context desktop-linux compose -p rg-fe-real-replay -f compose.json start
```

## Tra kết quả qua BE

Các đường dẫn sau dùng tiền tố `/api/v1`. Đăng nhập bằng PM phụ trách dự án. Kênh Đông dùng video `1123-part-00`, Đường số 2 dùng `11234`.

| Thao tác | API hoặc dữ liệu |
| --- | --- |
| Tìm lần chạy AI của dataset | `GET /projects/{projectId}/ai-videos?datasetId={datasetId}` |
| Kiểm tra job | `GET /processing-jobs/{jobId}` |
| Đọc SRT đã ghép | `GET /projects/{projectId}/ai-videos/{workId}/telemetry` |
| Đọc đề xuất | `GET /projects/{projectId}/ai-candidates/{detectionId}` |
| Tải ảnh frame | `GET /files/{frameFileId}/content` |
| Tra lỗi hiện có theo một phát hiện | `GET /projects/{projectId}/candidates?sourceKind=AI_DETECTION&sourceId={detectionId}` |
| PM giữ hoặc từ chối | `POST /projects/{projectId}/candidate-decisions` với `KEEP_NEW` hoặc `REJECT`, version của đề xuất, version hình học và idempotency key |
| Xem lỗi được giữ | `GET /projects/{projectId}/defects/{defectId}` |

Job trả ID kết quả đầu tiên. Để thử toàn bộ đề xuất trên bộ local, dùng `projects.<DKD hoặc DS2>.inference.candidates` trong `real-flow.json`. Trình kiểm thử lấy danh sách ID từ SQL local ở chế độ đọc rồi đọc từng đề xuất qua BE với quyền PM. API phân trang toàn bộ phát hiện theo job còn thiếu. API tra lỗi hiện có nhận một `detectionId` và trả 404 khi gửi `jobId` làm `sourceId`. PM khác dự án, Crew và Reporter nhận 403 ở danh sách video AI. Request ẩn danh nhận 401. Version nguồn cũ khi PM quyết định trả 409.

## Biên bản để tìm dữ liệu cho từng màn hình

| Tệp trong thư mục dữ liệu | Nội dung |
| --- | --- |
| `real-flow.json` | Dataset, công việc AI, model, đề xuất, frame, quyết định giữ và từ chối. GPS từ SRT tham chiếu drone. Vị trí lỗi trên đường vẫn là UNKNOWN. |
| `real-field.json`, `real-normal-repair.json` | Năm lỗi AI đã đo và hai lỗi đã sửa theo Normal, mỗi tuyến một lỗi. Ảnh và số đo hiện trường là DEMO. |
| `review-queues.json` | Ảnh đo đang chờ PM và đề xuất sửa Normal đang chờ Supervisor. |
| `demo-flow.json` | Case Reporter, nhiệm vụ Operator, quyết định PM, sửa chữa Normal, đánh giá điều kiện FastTrack, thông báo. |
| `offline-flow.json` | Thiết bị, gói ký, quyền bàn giao và kết quả import được xác nhận khi gửi lại. |

FastTrack trên hồ sơ DEMO dừng khi Crew bắt đầu sửa. Để chạy đến kết thúc, cần hồ sơ bàn giao thực tế, căn cứ bảo hành và phạm vi tuyến có hiệu lực cho đúng hạng mục. Các lần dispatch email thất bại, quyền offline hết hạn và lần inference thất bại cần biên bản kiểm thử riêng. Trạng thái triển khai Ubuntu được xác nhận riêng trên server.
