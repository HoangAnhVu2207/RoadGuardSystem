# Bàn giao cho Tùng · Hình học bản đồ Operator và Crew

## Operator · API-104

Gọi `GET /api/v1/projects/{projectId}/geometry-package?routeVersionId=…&segmentSetId=…` với Bearer Operator. Dùng cặp ID đã pin của task khảo sát. BE kiểm assignment và project hiện hành. Nếu gọi cặp chưa được giao, xử lý 403 và tải lại task.

Trích `segmentSet.segments[]`:

```json
{
  "sequence": 1,
  "metricGeometry": {"type":"LineString","coordinates":[[600000,1200000],[600020,1200000]]},
  "wgs84Geometry": {"type":"LineString","coordinates":[[105.914900098,10.854121391],[105.915083063,10.854120848]]}
}
```

`wgs84Geometry.coordinates` dùng **[longitude, latitude]**. BE chiếu từng đỉnh của đoạn thật và giữ ID, thứ tự, lý trình và `metricGeometry`. Hỗ trợ WGS84 / UTM north EPSG:32648 và EPSG:32649 khi route dùng trực tiếp CRS chuẩn. Native profile và nguồn thiếu, không hợp lệ hoặc ngoài vùng hỗ trợ trả null. Khi null, bỏ lớp đoạn WGS84 hoặc báo chưa có hình học bản đồ. Giữ dữ liệu metric cho chức năng đã hỗ trợ CRS nguồn.

## Crew · API-080

Gọi `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}` với Bearer Crew. Dùng `defectTarget` làm marker. API-080 được chọn vì marker có thể đọc được khi thiếu segment set khiến API-087 trả `409 source_not_ready`. API-087 vẫn trả geometry nguồn và có thêm polyline WGS84 như bên dưới.

```json
{"defectTarget":{"longitude":105.914900098,"latitude":10.854121391}}
```

Điểm lấy từ defect của task, cùng project và route version. Điểm WGS84 EPSG:4326 hợp lệ được dùng trực tiếp. Điểm UTM dùng phép chiếu ngược khi SRID khớp route UTM chuẩn đã pin. Giá trị null nghĩa là không có điểm phù hợp để vẽ. Ẩn marker và báo chưa có vị trí hư hại. Không thay bằng tâm đoạn, tâm tấm hoặc vị trí hiện tại của Crew.

## Crew · API-087

Gọi `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/geometry` với Bearer Crew đang được giao task. Vẽ tuyến từ `route.wgs84Geometry`, vẽ từng đoạn từ `segments[].wgs84Geometry`. Hai trường dùng cùng định dạng với API-104.

```json
{"type":"LineString","coordinates":[[105.914900098,10.854121391],[105.915083062,10.854120847]]}
```

Thứ tự là **[longitude, latitude]**. BE dùng geometry đã pin theo task và phép chiếu chung với API-104 cho EPSG:32648/32649. Giữ `wkt`, `spatialSrid` và lý trình nguồn. Nếu geometry thiếu, SRID đoạn lệch nguồn hoặc CRS/profile không được hỗ trợ, trường tương ứng trả `null`. Bỏ qua lớp hoặc đoạn WGS84 đó. Khi API-087 trả `409 source_not_ready` vì chưa có segment set phù hợp, app vẫn có thể đọc marker qua API-080.

## Xác minh vị trí FIELD

Tiếp tục gửi `POSITION_CHECKLIST` theo [handoff FIELD hiện có](CREW-SAFETY-AND-REVIEW.md). `GPS_CAPTURE` với tọa độ hợp lệ vẫn có thể trả `GPS_SOURCE_BINDING_UNAVAILABLE`. Tuyến và marker WGS84 chỉ phục vụ hiển thị. Để tự xác minh GPS cần chốt geometry đã pin, CRS và phép chuyển đổi, dung sai không gian, cách đánh giá độ chính xác GPS và xử lý dữ liệu thiếu.

[OpenAPI](openapi.json)
