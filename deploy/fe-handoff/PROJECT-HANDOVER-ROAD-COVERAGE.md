# Bổ sung hồ sơ bàn giao cho dự án đã tồn tại

Supervisor dùng Bearer hoặc phiên Web HTTPS với `credentials: "include"`. Với mỗi POST qua cookie, gửi `X-CSRF-TOKEN` lấy từ `requestToken` của `GET /api/v1/auth/web/csrf` trong cùng phiên. Các GET bên dưới không cần CSRF.

## Upload tài liệu

1. `POST /api/v1/uploads` với `Idempotency-Key`. Ví dụ body:

```json
{"purpose":"DOCUMENT","projectId":"<projectId>","targetId":"<projectId>","fileName":"handover.pdf","mediaType":"application/pdf","sizeBytes":12345,"checksumSha256":"<64 ký tự hex SHA-256 chữ thường>"}
```

2. Lấy `id` làm `uploadId`. Gọi `POST /api/v1/uploads/{uploadId}/part-urls` với key riêng và `{"partNumbers":[1]}` (thêm các part theo `partSizeBytes`). PUT từng part tới URL ký sẵn, giữ ETag mà object storage trả. Browser cần đọc được ETag của từng part từ origin storage.
3. Gọi `GET /api/v1/uploads/{uploadId}` lấy ETag phiên, rồi `POST /api/v1/uploads/{uploadId}/complete` với `If-Match`, key riêng và `{"parts":[{"partNumber":1,"eTag":"<ETag từ PUT>"}],"checksumSha256":"<SHA-256>"}`. Poll GET phiên tới `VERIFIED` và dùng `fileId` trong response. `202` mới chỉ nhận yêu cầu xác minh.
4. Với Web cookie, bốn action upload trên chỉ dành cho Supervisor và tài liệu `DOCUMENT` có `targetId == projectId`, do Supervisor đang gọi sở hữu phiên. Sai role/purpose/owner trả `403 access_forbidden`. Bearer vẫn theo contract upload chung. Các lỗi upload giữ nguyên, gồm `422 upload_validation_failed` và `428 validation_error` khi thiếu header bắt buộc.

## Bổ sung hồ sơ

Trước khi chọn nhánh tạo hay attach, Supervisor gọi `GET /api/v1/projects/{projectId}/handover-documents`
bằng Bearer hoặc Web cookie. GET không cần CSRF và trả `Cache-Control: no-store`.
Đây là inventory toàn bộ hồ sơ, kể cả `fileId: null`; không dùng `road-coverage.sources`
để kết luận project chưa có hồ sơ. PM/Operator/Crew/Reporter không có quyền GET inventory này.

```json
{
  "projectId": "<projectId>",
  "items": [
    {
      "id": "<handoverDocumentId>",
      "documentNo": "BG-2026-01",
      "handoverDate": "2026-11-16",
      "fileId": null,
      "version": "<base64 document rowversion>",
      "isMapped": false,
      "canAttachFile": true
    }
  ]
}
```

GET trả đủ mọi hồ sơ của project, không phân trang, thứ tự ID theo SQL uniqueidentifier.
`items: []` chỉ nghĩa là chưa có hồ sơ; thiếu quyền là `403`, project không tồn tại
với Supervisor hợp lệ là `404`. Nếu có nhiều hồ sơ, FE hiển thị số/ngày hồ sơ để
Supervisor chọn `items[].id`, luôn gửi rõ `handoverDocumentId` khi attach. Không tự chọn
ID đầu tiên, không dùng lỗi `409` làm discovery. `canAttachFile` chỉ cho biết hồ sơ
chưa có file/chưa có mapping tại thời điểm đọc, không chứng minh file mới đủ điều kiện.
`isMapped` xét mọi lịch sử mapping, kể cả mapping đã thay thế. File đã gắn không được ghi đè.

`items[].version` là rowversion của hồ sơ, **không phải If-Match cho POST attach**.
Sau upload VERIFIED, luôn đọc lại road-coverage lấy aggregate ETag như bên dưới.
Attach không nâng TEST_ONLY thành REAL_SOURCE; nguồn còn phải qua provenance/eligibility.

Sau khi file đã VERIFIED, gọi `GET /api/v1/projects/{projectId}/road-coverage` để lấy ETag mới. `POST /api/v1/projects/{projectId}/handover-documents` với `Content-Type: application/json`, `Idempotency-Key` mới và `If-Match` bằng nguyên ETag đó. Nếu browser không đọc được header ETag của API, bọc trường `version` của response GET trong dấu ngoặc kép để gửi `If-Match`. Áp dụng tương tự cho GET phiên upload.

- Hồ sơ đã tồn tại và còn trống: `{"fileId":"<fileId>"}`. Nếu dự án có nhiều hồ sơ, thêm `"handoverDocumentId":"<id>"` để chọn đúng hồ sơ.
- Dự án chưa có hồ sơ: `{"fileId":"<fileId>","documentNo":"BG-2026-01","handoverDate":"2026-11-16"}`. Ngày phải là ngày thực tế trên hồ sơ.
- Thành công trả `200` với cấu trúc road coverage và ETag mới. Đọc `sources[]` để thấy `{"kind":"HANDOVER","id":"<handoverDocumentId>","fileId":"<fileId>","version":"<handoverVersion>"}`. Retry do timeout giữ nguyên key, body và If-Match. Hồ sơ đã gắn file hoặc có mapping không nhận lệnh thay file.

| HTTP | Mã lỗi |
| --- | --- |
| 400 | `validation_error` do JSON, header hoặc dữ liệu hồ sơ sai quy cách |
| 401 | Thiếu phiên đăng nhập hợp lệ |
| 403 | `access_forbidden` hoặc `csrf_failed` |
| 404 | `not_found` hoặc `handover_document_not_found` |
| 409 | `handover_file_not_ready` khi file chưa VERIFIED, không đúng purpose/scope hoặc thuộc project khác. `handover_document_exists`, `handover_document_selection_required`, `handover_document_already_linked`, `concurrency_conflict` hoặc `idempotency_key_reused` theo từng điều kiện |
| 428 | `precondition_required` khi thiếu key hoặc If-Match |

## Xác nhận phạm vi và kiểm Fast Track

Gọi lại `GET /api/v1/projects/{projectId}/road-coverage`. Lấy nghĩa vụ từ `scopes[]` theo `obligationId` và kiểm `roadSectionId`, `locationVersion`, lý trình và bề ngang của đúng phạm vi. Dùng `scopeHash` của mục đó, `version` của nguồn `HANDOVER` và `version` của nguồn `MAINTENANCE_BASIS` hoặc `WARRANTY`. Gửi nguyên ETag của GET vào `If-Match`, kèm `Idempotency-Key` riêng cho `POST /api/v1/projects/{projectId}/road-coverage`.

```json
{
  "scopeObligationId": "<scopes[].obligationId>",
  "expectedScopeHash": "<scopes[].scopeHash>",
  "handoverDocumentId": "<HANDOVER.id>",
  "handoverVersion": "<HANDOVER.version>",
  "sourceKind": "MAINTENANCE_BASIS",
  "sourceId": "<MAINTENANCE_BASIS.id>",
  "sourceVersion": "<MAINTENANCE_BASIS.version>",
  "applicableFromUtc": "<ISO-8601 start>",
  "applicableToUtc": "<ISO-8601 exclusive end>",
  "provenance": "TEST_ONLY",
  "reason": "Kiểm thử nguồn và phạm vi"
}
```

Với fixture mẫu, chọn nguồn `MAINTENANCE_BASIS` có `provenance: "TEST_ONLY"` và `fileId` khác `HANDOVER.fileId`. Với tài liệu do FE upload, đối chiếu `fileId` đã xác minh với mục nguồn tương ứng.

Chọn khoảng hiệu lực bao phủ thời điểm kiểm thử, bắt đầu từ ngày bàn giao trở đi theo giờ Việt Nam. Với WARRANTY, khoảng này phải nằm trong hạn bảo hành, đúng đường và đúng hồ sơ nếu warranty đã pin các liên kết đó. Khi thành công `201`, đọc `history[]`. Khi scope, source hoặc ETag cũ (`409`), tải lại và xác nhận dữ liệu trước khi gửi lệnh mới.

Kiểm eligibility qua `GET /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/eligibility`. Fixture `TEST_ONLY` giúp kiểm chọn nguồn và xác nhận mapping. Eligibility ghi `TEST_ONLY_MAPPING` và giữ lý do `TEST_ONLY_SOURCE_NOT_EXECUTABLE`. Khi thử start bằng fixture này, BE trả `409 test_only_source_not_executable`. Ca chứng minh thực thi dùng môi trường test cô lập với nguồn đủ điều kiện, policy, binding và quyền Crew hiện hành.

## Seed và môi trường

- Seeder CLI với `ROADGUARD_SEED_PROFILE=Development` tạo `RG-CI-01-SYNTHETIC` qua `DevelopmentScenarios`. Sau khi cấu hình kết nối DB test cô lập, mật khẩu seed riêng và đường dẫn manifest theo `tools/RoadGuardSystem.Seeder/Program.cs`, chạy `dotnet run --project tools/RoadGuardSystem.Seeder`. Chạy lại cùng lệnh để bổ sung tài liệu khi hồ sơ fixture còn trống. Dùng nghĩa vụ đúng project từ `scopes[]`; cần tạo nghĩa vụ qua workflow repair nếu project chưa có.
- API Development chạy `PostmanScenarioSeedStep` cho `RG-POSTMAN-001` khi bật `RoadGuardDatabase:InitializeOnStartup` và `RoadGuardDatabase:SeedDevelopmentUsers` trên DB test cô lập. Khởi động lại với cùng DB để chạy lại step. Với DB disposable `RoadGuardPostmanTest` đúng target được bảo vệ, chạy `dotnet run --project tools/RoadGuardSystem.Seeder -- --postman-disposable` để bổ sung hai nguồn và nghĩa vụ mẫu nếu còn thiếu. Metadata file seed có prefix `TEST_ONLY/` và purpose `TEST_ONLY_COVERAGE`; các file seed SQL này không có object nội dung để tải.
- Docker FE handoff dùng `HandoffBootstrap`/Sandbox Seeder và script demo riêng. Cần xác nhận runtime/profile/project FE thực sự gọi trước khi chạy seed. Với project demo đã có nghĩa vụ, có thể dùng flow upload và bổ sung hồ sơ ở trên rồi xác nhận với `TEST_ONLY`.
