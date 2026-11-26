# Multipart URL: replay và xin cấp lại an toàn

Nguồn quyết định owner: 2026-11-27, spec `planning/development/UPLOAD-PART-URL-REPLAY.md`.
Baseline sau đồng bộ leader: `07a892984cd288bed631ebf98f1b0ba009c44a73`. Không có migration mới.

## Request và quyền

`POST /api/v1/uploads/{uploadId}/part-urls`, JSON `{"partNumbers":[1,2]}`, header `Idempotency-Key` và Bearer. Không cần If-Match. Mobile giữ bền uploadId/key/body trước lần gửi đầu. ID phiên lấy từ `id` của response tạo upload, không phải fileId.

Web chỉ dùng opt-in hiện hành cho DOCUMENT của chính Supervisor đủ quyền (IsOwnProjectDocumentAsync vẫn chỉ cho Supervisor), cookie + CSRF `X-CSRF-TOKEN`; không mở cookie cho ảnh Crew/Operator. Reporter tiếp tục dùng route riêng `POST /api/v1/reporter-evidence/uploads/{uploadId}/part-urls` theo owner/Bearer. Quyền account, owner, project, assignment/task, offline capture/importer/grant hoặc staging được kiểm hiện hành trước khi đọc receipt và sau storage signing. Key không cấp quyền.

Scope receipt giữ nguyên actor + project (có thể null) + operation `UploadPartUrlsIssued` + key. Fingerprint SHA-256 từ uploadId định dạng N và part tăng dần. Hoán vị tương đương; khác upload/tập part trong cùng scope là khác intent. Khác actor/project không mặc nhiên conflict. Không đưa key vào query, URL hay log.

## Response và thứ tự lỗi

| HTTP/code | Ý nghĩa | Mobile xử lý |
|---|---|---|
| 200 `parts[{partNumber,url,expiresAt}]` | Lần mới hoặc replay receipt còn hạn; expiry gốc giữ nguyên | PUT đúng part; dùng ETag thực của provider khi complete |
| 409 `idempotency_key_reused` | Cùng scope/key khác fingerprint | Dừng, đối soát request đã lưu; không có metadata receipt khác |
| 409 `upload_session_not_uploadable` | Session hết hạn/VERIFYING/VERIFIED/FAILED/cancelled | renewalAllowed=false; GET session để đối soát, không chỉ đổi key |
| 409 `upload_part_urls_multipart_conflict` | Receipt không khớp multipart hiện hành hoặc transition bị fence | renewalAllowed=false; đối soát session, không tự dựng multipart |
| 409 `upload_part_urls_expired` | Chính receipt khớp request đã hết hạn, session còn upload được | Chỉ khi renewalAllowed=true mới tạo key mới cho lần xin URL mới |
| 503 `upload_storage_unavailable` | Provider/recovery chưa sẵn sàng | Retry request cũ; không tự coi là expiry |
| 400 `validation_error` | JSON/DTO, gồm mảng part rỗng | Sửa input, không diễn giải thành receipt expired |
| 422 `upload_validation_failed` | Part trùng, <1 hoặc vượt số part của file | Sửa input |
| 428 `validation_error` | Thiếu Idempotency-Key | Gửi key đã lưu |
| 401 `auth_unauthorized`/`auth_session_revoked`; 403 `access_forbidden`/`csrf_failed`; 404 `upload_session_not_found` | Auth/current authority/resource guard theo route hiện hành | Không đọc được metadata receipt; xử lý đăng nhập/quyền |

Precedence sau auth và input validation: fingerprint khác → session không upload được → multipart mismatch → receipt hết hạn → ký URL/503. Cả receipt và session hết hạn: session thắng, không có renewal. Không có receipt thì là ý định cấp mới, không giả thông tin receipt từ key client. Ngay trước trả URL/metadata, hai action kiểm lại phiên bằng AuthoritativeSessionValidator hiện có; phiên bị thu hồi trong lúc presign bị 401 auth_session_revoked, không trả URL/receipt metadata. Các lỗi đều có correlationId; metadata nhạy cảm, signed URL cũ, object key, StorageUploadId không xuất hiện trong ProblemDetails. Response part URLs có Cache-Control private,no-store.

Metadata receipt-expired ở top-level ProblemDetails: uploadId, partNumbers chuẩn hóa từ receipt đã lưu, expiresAt của receipt, serverTime, renewalAllowed, sessionStatus, reason=receipt_expired. renewalAllowed chỉ đúng tại serverTime; command kế tiếp kiểm lại quyền/trạng thái. Lỗi session chỉ có metadata phiên, không lộ part/expiry của receipt. Fingerprint conflict không có metadata receipt. Dữ liệu schema nullable thể hiện trường có thể vắng mặt theo nhánh, không bắt FE suy từ null.

## Mất response → đối soát → renewal

1. Retry nguyên uploadId/key/body. Receipt còn hạn trả lại cùng scope và expiry; chuỗi URL có thể khác khi provider ký lại.
2. Chỉ `upload_part_urls_expired` với renewalAllowed=true xác nhận đúng receipt cũ hết hạn. Lưu key mới rồi xin các part cần gửi/đối soát; giữ uploadId/fileId/capture/admission và signed batch cũ.
3. Timeout, generic 409, 5xx, PUT lỗi hoặc provider 403 không tự chứng minh expiry. Không tự tạo lại upload/session/offline operation, đổi signed payload hoặc bịa ETag.
4. Signed URL và upload session có hai hạn khác nhau: project URL 15 phút và bị cap bởi session; Reporter private 300 giây giữ contract cũ. Session TTL hiện hành 24 giờ; GET session.expiresAt không phải hạn receipt.
5. Server không kéo dài receipt khi replay. Lần cấp mới có receipt/key mới, có thể cập nhật LastUrlExpiresAt của part; trường đó không chứng minh receipt cũ đã hết hạn.

Storage presign nằm ngoài transaction SQL. Sau presign BE khóa/kiểm lại current authority, session, multipart và clock; cancel/complete/revocation/expiry thắng thì loại bỏ URLs vừa ký. Không tuyên bố có thể thu hồi tức thì một URL provider đã phát trước đó; quyền được kiểm tại các checkpoint BE, không mỗi PUT trực tiếp MinIO. Create/complete/cancel giữ error mapping hiện có.

## Kiểm chứng và cập nhật runtime

Local focused: nhóm API 129 PASS / 0 FAIL / 1 SKIP; sau sửa nhánh storage, nhóm multipart/Swagger/controller kiểm lại 70 PASS / 0 FAIL / 0 SKIP (có trùng lượt trước, không cộng số ca). SQL upload/recovery/idempotency/offline liên quan: 73 PASS / 0 FAIL / 0 SKIP sau sửa, giữ nguyên assertion mất ACK cleanup. Ca skip duy nhất là UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio vì không bật ROADGUARD_MINIO_SMOKE và credential cấu hình ngoài; thuộc whitelist cũ. Ca UploadEndpoints_OwnedMinioPreservesBytesAndRejectsPrivateFileOutsider dùng MinIO cô lập thật đã chạy và đạt, gồm BE receipt-expiry bằng clock kiểm soát, key mới, PUT, complete, VERIFIED và đối chiếu byte. Không chờ TTL provider thực trong ca renewal này. Giữ các gate và danh sách skip cũ.

Build/analyzer API và IntegrationTests đạt; runtime Swagger/snapshot, source evidence, giới hạn mô tả, test discovery/identity và TRX đã đối chiếu. Các checkpoint Unit/API lane trước được giữ riêng trong bằng chứng local; full local không được tuyên bố PASS. Owner yêu cầu dừng full lanes và không theo dõi CI. **Đã push thì GitHub CI tự chạy; chưa theo dõi/chưa xác nhận kết quả.** SHA/image/readiness runtime ở biên bản bàn giao cuối; không suy trạng thái runtime từ việc push source. HTTP test dùng pipeline auth và SQL cô lập; storage giả lập cho race và clock, MinIO cô lập thật cho renewal → PUT → complete → VERIFIED và byte download. Chưa phải nghiệm thu Mobile. Không có schema/migration mới; không seed/reset dữ liệu. OpenAPI runtime và snapshot FE phải đối chiếu hai route part-urls và schema UploadPartUrlsProblemResponseDto.

## JSON thực từ HTTP test

SQL cô lập, storage fixture, clock kiểm soát tiến 16 phút; ID dưới đây là dữ liệu test, không phải phiên runtime dùng chung. Không chứa signed URL hay storage identifier.

```json
{
  "title": "Part URLs unavailable",
  "status": 409,
  "detail": "The upload request could not be completed.",
  "instance": "/api/v1/uploads/45e8e167-e06e-4ba6-90e8-94b25c494167/part-urls",
  "code": "upload_part_urls_expired",
  "correlationId": "1a7a52da-bc0e-4b38-ab28-16d948ff95bc",
  "renewalAllowed": true,
  "reason": "receipt_expired",
  "uploadId": "45e8e167-e06e-4ba6-90e8-94b25c494167",
  "partNumbers": [
    1
  ],
  "expiresAt": "2026-11-26T20:20:28.8686339+00:00",
  "serverTime": "2026-11-26T20:21:28.8686339+00:00",
  "sessionStatus": "UPLOADING"
}
```
