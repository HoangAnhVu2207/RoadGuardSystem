# Lưu và đọc cấu hình pavement layout

Contract hiện hành lấy từ `/swagger/v1/swagger.json` trên API Development.

## Tạo và mở lại pavement layout

PM có quyền project gửi `POST /api/v1/projects/{projectId}/route-versions/{routeVersionId}/segment-sets/{segmentSetId}/pavement-plans` với `Idempotency-Key`. Tạo mới trả `201`, replay cùng key và nội dung trả `200`, cùng key nhưng đổi cấu hình trả `409 idempotency_conflict`. Lưu `id` trong body làm `layoutId` hoặc lưu `Location`. Sau F5, PM có quyền project hoặc Supervisor gọi `GET /api/v1/projects/{projectId}/pavement-layouts/{layoutId}`, rồi đọc `layout` để khôi phục form. `ETag` là `contentHash` có dấu ngoặc kép. Web cookie được hỗ trợ trên cả hai route, CSRF chỉ cần cho POST.

| Trường trong `layout` | Đơn vị và giá trị |
|---|---|
| `slabThicknessCentimeters` | cm, lớn hơn 0 nếu có |
| `contractionJointSpacingMeters` | m, lớn hơn 0 nếu có |
| `expansionJointSpacingMeters` | m, lớn hơn 0 nếu có |
| `expansionJointGapMillimeters` | mm, lớn hơn hoặc bằng 0 nếu có |

Bốn giá trị là JSON number, lưu bằng `decimal` .NET với tối đa khoảng 28–29 chữ số có nghĩa, không áp số lẻ cố định và không làm tròn theo đơn vị. Bỏ trường hoặc gửi `null` đều là chưa khai báo. GET bỏ các trường chưa khai báo, kể cả bản PLANNED cũ. `layout: null` với AS_BUILT hoặc khi không có input gốc. Nếu cần form của bản PLANNED nguồn, mở `sourcePlanId` bằng GET layout riêng. Receipt tạo cũ có thể chưa có `layout`, dùng GET để lấy cấu hình gốc. Giá trị ngoài giới hạn trả `422 pavement_layout_invalid`.

`stripWidthMeters`, `slabLengthMeters` và `widthProfile` vẫn điều khiển cách sinh tấm. Width profile phải liên tục, phủ hết chiều dài phân tích và chia được thành số nguyên dải rộng `stripWidthMeters`. Khôi phục `displayToleranceMeters` từ `geometry.displayToleranceMeters`. Dùng `geometry.plan.cells` cho thứ tự, lý trình và khoảng ngang. Dùng `geometry.slabs` cho mã tấm, kích thước và footprint. Các pin `routeVersionId`, `segmentSetId` và `crsProfileRevisionId` vẫn trả như trước.

Thông số khe được lưu cùng cấu hình và tham gia fingerprint/content hash. Hình học tấm vẫn theo snapshot đã sinh. Việc sinh khe vật lý còn chờ quy tắc đã duyệt về mốc và vị trí khe, xử lý khe trùng hoặc tại biên, và ảnh hưởng độ hở lên footprint. Publication SAMPLE dùng snapshot tấm. Điều kiện OFFICIAL giữ nguyên.

### Ví dụ chạy trên Compose local

Body POST cho tuyến phân tích dài 20 m trong bộ dữ liệu thử.

```json
{
  "layout": {
    "stripWidthMeters": 4,
    "slabLengthMeters": 10,
    "widthProfile": [
      { "fromOffsetMeters": 0, "toOffsetMeters": 10, "widthMeters": 12 },
      { "fromOffsetMeters": 10, "toOffsetMeters": 20, "widthMeters": 8 }
    ],
    "slabThicknessCentimeters": 25.25,
    "contractionJointSpacingMeters": 4.5,
    "expansionJointSpacingMeters": 37.125,
    "expansionJointGapMillimeters": 12.75
  },
  "displayToleranceMeters": 0.1
}
```

Trích response GET thực tế, giữ phần cấu hình và phần tử đầu của từng mảng hình học. Response đầy đủ có 5 cell và 5 tấm. Các ID dưới đây thuộc dữ liệu thử local.

```json
{
  "id": "d4cd6c77-1c01-4c3e-8212-9b730ff8ba59",
  "projectId": "53973883-662b-4b74-a8b4-77ad69d5c051",
  "routeVersionId": "3cd174bb-cbbb-4013-b58f-98540598fba9",
  "segmentSetId": "ad4ee525-29a2-4561-8b63-bfdd8d1f9548",
  "crsProfileRevisionId": "1cfe9ab4-950e-463e-b732-693c68a94417",
  "kind": "PLANNED",
  "contentHash": "4910e65af0a8857306bdd3a9d0c6b49f1cc5bbea55bae2b25558956760b97067",
  "layout": {
    "stripWidthMeters": 4,
    "slabLengthMeters": 10,
    "widthProfile": [
      { "fromOffsetMeters": 0, "toOffsetMeters": 10, "widthMeters": 12 },
      { "fromOffsetMeters": 10, "toOffsetMeters": 20, "widthMeters": 8 }
    ],
    "slabThicknessCentimeters": 25.25,
    "contractionJointSpacingMeters": 4.5,
    "expansionJointSpacingMeters": 37.125,
    "expansionJointGapMillimeters": 12.75
  },
  "geometry": {
    "plan": {
      "cells": [
        {
          "sequence": 1,
          "longitudinalRow": 1,
          "strip": 1,
          "fromOffsetMeters": 0,
          "toOffsetMeters": 10,
          "fromLateralMeters": -6,
          "toLateralMeters": -2,
          "isTerminalResidual": false,
          "isWidthTransitionFragment": false
        }
      ]
    },
    "slabs": [
      {
        "key": "1",
        "plannedSequence": 1,
        "footprint": [
          { "x": 0, "y": -6 }, { "x": 10, "y": -6 }, { "x": 10, "y": -2 },
          { "x": 0, "y": -2 }, { "x": 0, "y": -6 }
        ],
        "lengthMeters": 10,
        "widthMeters": 4,
        "dimensionUnknownReason": null,
        "source": "ANALYTIC_PLAN",
        "sourceFileId": null,
        "provenanceStatus": "SERVER_ANALYTIC_MODEL"
      }
    ]
  }
}
```

## Footprint qua góc nối tuyến

Layout mới cắt các cạnh offset tại cùng mặt cắt khi hai tấm dùng chung biên. Fixture tuyến 119 tọa độ, dài 701.2905185122771 m, strip 3.5 m và tấm dài 5 m tạo 282 footprint. ID, thứ tự và kích thước thiết kế của cell tiếp tục lấy từ layout. FE dùng footprint từ response lưu hoặc GET lại. Polygon không hợp lệ vẫn bị từ chối.

## Tìm lại layout theo dự án

PM có quyền trong dự án hoặc Supervisor gọi:

```http
GET /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{segmentSetId}/pavement-layouts?pageSize=25
```

Kết quả xếp theo `layoutId` tăng dần. `pageSize` từ 1 đến 100, mặc định 25. Nếu `nextCursor` khác null, gửi lại đúng chuỗi ID với `cursor={nextCursor}`. Web dùng `credentials: "include"`. GET dùng cookie hoặc Bearer và không cần CSRF.

```json
{
  "items": [
    {
      "layoutId": "11111111-1111-4111-8111-111111111111",
      "projectId": "22222222-2222-4222-8222-222222222222",
      "roadSectionId": "33333333-3333-4333-8333-333333333333",
      "routeVersionId": "44444444-4444-4444-8444-444444444444",
      "segmentSetId": "55555555-5555-4555-8555-555555555555",
      "kind": "PLANNED",
      "sourcePlanId": null,
      "createdAt": "2026-11-10T05:00:00Z",
      "contentHash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
    }
  ],
  "nextCursor": null
}
```

Chuỗi ID đúng nhưng chưa lưu layout trả `200 {"items":[],"nextCursor":null}`. Một bản sau khi đã đọc hết trang thì FE có thể mở trực tiếp. Nhiều bản thì cho người dùng chọn theo loại và thời gian lưu. Sau đó gọi `GET /api/v1/projects/{projectId}/pavement-layouts/{layoutId}` để đọc cấu hình và footprint đã lưu. Bản `AS_BUILT` có `sourcePlanId` để mở cấu hình nguồn. Route và segment set lịch sử vẫn tra được theo đúng chuỗi ID.

Xử lý `401 unauthorized` khi thiếu phiên, `403 access_forbidden` khi thiếu quyền, `404 not_found` khi sai chuỗi ID và `400 validation_error` khi sai phân trang. Danh sách và chi tiết dùng dữ liệu đã lưu, bao gồm layout tạo trước khi có API này.
