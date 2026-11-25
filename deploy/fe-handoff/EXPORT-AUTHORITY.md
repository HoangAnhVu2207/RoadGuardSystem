# Export — quyền hiện hành và xử lý 403 trên Web

Ngày25/11/2026; base1a2520f. Phạm vi server export, không phải encrypted device handover.
Owner giao giữ membership trong đợt này khi không có quyết định ngoại lệ rõ ràng.
`confirmed-decisions.md`41A quy định retention; B2 Supervisor notification và B3
scoped source read không cấp quyền export toàn project. Source/comment không được
coi là quyết định nghiệp vụ cũ. Chưa tìm thấy quyết định cho Supervisor không
membership export repair dossier; mở ngoại lệ là PROPOSED, chưa triển khai.

## Phản ánh và correlation

GET `/api/v1/projects/c6f615b2-e1dc-4875-bec0-b13474510a27/exports?limit=100`
được báo403 với correlation`6d46f061-7f15-47ba-bc6c-7edc5f8c8dc7`.
Không thấy ID này trong log container API hiện tại hoặc các log đã lưu có thể truy
cập dưới thư mục bằng chứng local. **NOT_CORRELATED**: chưa nối actor/session,
thời điểm và runtime của request gốc; không gán lỗi đó cho tài khoản Supervisor cụ thể.
Không có bằng chứng POST tạo job đã gửi. Các kết quả dưới đây là HTTP fixture cô lập.

## Quyền và discovery

Root `E=/api/v1/projects/{projectId}/exports`.
PM/Supervisor phải account/role active, không MustChangePassword, có membership
Active đúng role tài khoản, ValidFrom≤ngày UTC server và ValidTo null hoặc≥ngày đó.
Supervisor-global ở reporting/nghiệm thu không thay gate này. Không tự thêm membership.

| Method/path | Contract |
| --- | --- |
| GET E?limit=1..100&cursor=… | Chỉ job do actor đang gọi tạo. SQL filter RequestedBy trước pagination. Cursor gắn actor/project/query; không dùng cursor người khác. Gate quyền chạy cả danh sách rỗng. |
| POST E | Gate export + capture đúng nguồn; Idempotency-Key bắt buộc. 202 và Location/job id; cùng key/body replay202 cùng id. |
| GET E/{id} | Không giới hạn creator như collection: actor có quyền project và snapshot sources hiện hành có thể đọc job của creator khác. Không suy từ list sang quyền detail. |
| GET E/{id}/manifest | Snapshot bất biến; kiểm sources; repair dossier còn kiểm file authority. Metadata không cấp quyền tải nguồn. |
| GET E/{id}/content | SUCCEEDED, chưa hết hạn, nguồn/file còn quyền; kiểm lại sau OpenRead trước bytes. Không URL công khai, không Range. |
| Worker | Dùng actor tạo job, kiểm account/member/role và sources/files tại claim/render/complete, sau mở source stream. Worker không cấp quyền service-account thay actor. |

Hết quyền trước replay vẫn403; receipt không cấp quyền vĩnh viễn. Key cùng actor,
project,operation nhưng payload khác409idempotency_conflict. Persist request trước
gửi; timeout thì retry cùng key/body hoặc đối soát Location/job đã nhận. Không biết
job id thì collection creator-only giúp tìm; không tạo key mới ngay khi kết quả chưa rõ.

## Web/Bearer và ví dụ

Web dùng session `/auth/web/*`, `credentials: "include"`. Lấy
`GET /api/v1/auth/web/csrf`, gửi `requestToken` vào `X-CSRF-TOKEN` cho POST.
GET không CSRF. Bearer tiếp tục hỗ trợ; không ghép Bearer hỏng để mong fallback cookie.
POST cần `Idempotency-Key`, **không cần If-Match**. ETag response là job version;
manifest ETag là snapshotHash; content ETag là SHA256. Không dùng thay version repair command.

```http
POST /api/v1/projects/{projectId}/exports
Content-Type: application/json
Idempotency-Key: <key đã lưu cho thao tác>
X-CSRF-TOKEN: <requestToken cùng phiên Web>
```

```json
{"kind":"DOSSIER","format":"ZIP","includeOriginalFiles":true,"repairItemIds":["<itemId có quyền>"]}
```

Dùng `repairPackageIds` thay lựa chọn item nếu cần; tối đa20package/100item sau resolve.
Không trộn repair scope với segment/defect/time filter. PDF không includeOriginalFiles.
DOSSIER tổng hợp dùng kind/format, filter thời gian/segment theo contract reporting;
defectIds không hỗ trợ authoritative mapping nên bị từ chối thay vì bỏ qua.
TRAINING chỉZIP và approved label scope; originals cần training source authority.

202 trả `Location: E/{id}`, ETag và DTO gồm:
`id,projectId,kind,format,status,snapshotId,snapshotHash,artifactId,createdAt,completedAt,
expiresAt,errorCode,completeness,missingSections,version` (artifact/completed/expiry/error
có thể null khi chưa hoàn tất). Poll GET Location; SUCCEEDED mới tải content.
Snapshot sections giữ AVAILABLE/UNAVAILABLE/PARTIAL theo contract hiện có, không
coi thiếu nguồn thành COMPLETE. Partial không đồng nghĩa quyền tải mọi ảnh.

Mẫu cấu trúc response202 (ID/hash/version/thời gian là placeholder; lấy giá trị thật
từ server, không dùng ví dụ để gọi môi trường khác):

```json
{
  "id": "<jobId>", "projectId": "<projectId>", "kind": "DOSSIER", "format": "ZIP",
  "status": "QUEUED", "snapshotId": "<snapshotId>", "snapshotHash": "<sha256>",
  "artifactId": null, "createdAt": "<UTC ISO8601>", "completedAt": null,
  "expiresAt": null, "errorCode": null,
  "completeness": "PARTIAL", "missingSections": ["<tên section chưa đủ>"],
  "version": "<base64 rowversion>"
}
```

`completeness` là COMPLETE/PARTIAL của snapshot; không thay`status` của job.
Các section của manifest có`availability`; không đặt`availability` ở job DTO.
`expiresAt` chưa có khi QUEUED; khi SUCCEEDED được đặt bằng completedAt+30ngày.

| HTTP/code wire | FE xử lý |
| --- | --- |
| 401 unauthorized | Đăng nhập lại; không coi như danh sách rỗng. |
| 403 access_forbidden (collection), forbidden (create/detail/manifest/content) | Không đủ quyền export/nguồn; dừng retry tự động. Không suy đã tạo job. |
| 403 csrf_failed | Lấy token CSRF cùng session, kiểm credentials; không bỏ kiểm CSRF. |
| 400 validation_error | JSON/query/cursor không hợp lệ theo middleware/controller. |
| 404 not_found | Đã có quyền project nhưng job không có trong project đó. Không đổi quyền để dò job. |
| 428 precondition_required | Thiếu Idempotency-Key. |
| 409 idempotency_conflict | Key cũ khác body, cần đối soát thao tác. |
| 409 export_not_ready | Job chưa SUCCEEDED, poll status theo backoff. |
| 410 export_expired | Job quá30ngày; không tự kéo dài thời hạn. |
| 422 export_invalid/export_source_unavailable | Request/nguồn không đáp ứng contract. |
| 503 producer_unavailable/export_storage_unavailable | Dependency/storage chưa sẵn; giữ key khi chưa rõ kết quả tạo. |

ProblemDetails có `status,code,instance` (title theo route). Collection có thêm
`correlationId` trong body; direct export errors hiện chỉ có correlation ở response
header`X-Correlation-ID`, không hứa body có field đó. FE đọc header khi body thiếu.
Collection dùng`access_forbidden`; ExportsController giữ`forbidden` trong response wire.
Gửi correlation ID và method/path để BE đối chiếu; không gửi cookie/token.

## Phạm vi nguồn và lịch sử

- Repair selection resolve theo project gốc của item; ReadItem/History và current
  obligation responsibility tiếp tục kiểm từng item. Package hỗn hợp không mở item
  ngoài quyền: capture fail-closed, không tự bỏ item rồi báo COMPLETE.
- Sau chuyển trách nhiệm, quyền đọc riêng source ở B không tự cấp export dự án A.
  Item có nguồnA nhưng đã nhậnB không đồng nghĩa API repair export URLB hỗ trợ mọi
  nguồnA; selection và source-ref project guard vẫn áp dụng. Không dùng policy/membership
  khác project để vượt. Cần proposal riêng nếu muốn mở export sau chuyển có scope mới.
- Attempt/review/decision giữ snapshot/hashes; correction không sửa byte export cũ.
  Download vẫn kiểm quyền hiện hành. Source version/hash sai không bị refresh vào snapshot cũ.
- Reporter evidence owner-only không thành project-public khi includeOriginalFiles.
  DOSSIER tổng hợp ghi reporterEvidence UNAVAILABLE nếu producer chưa hỗ trợ;
  repair snapshot không dùng quyền dossier để mở file nguồn Report/Case/AI khác lineage.
- Survey file cần scopeSURVEY_VIDEO/TELEMETRY + VERIFIED; training originals cần
  approved lineage/reader; repair originals cần file metadata VERIFIED và exactversion/hash.
  Kiểm metadata và nội dung riêng; quyền export không bỏ guard nguồn.

## Tích hợp và kiểm chứng

FE gặp list403: hiển thị không đủ quyền export; không dùng job người khác hoặc tự
thêm membership. Actor cần được cấp quyền qua quy trình quản trị hợp lệ; đợt này
không tạo endpoint capability hay workflow cấp membership Supervisor mới.
Route PM thêm project member hiện chỉ nhận Operator/Crew (`StaffReadRepository`),
không dùng nó để thêm Supervisor. Chưa bàn giao đường cấp membership Supervisor
mới trong đợt này; nếu môi trường không có actor đáp ứng, đây là điểm cần chủ quyền
quản trị chốt, không hướng dẫn FE/worker tự sửa DB hoặc đổi role để vượt gate.
Kết quả kiểm thực thi, SHA/runtime và CI ghi trong bàn giao cuối. Không nghiệm thu
UI từ HTTP/SQL; không sửa FE hoặc seed DB đang test. Không migration cho thay đổi này.

## Căn cứ source và sửa lỗi trong đợt này

- `ExportService.cs:35` và`:62`: gate chung/create/collection;`:131`,`:139`,`:150`
  phân biệt detail/manifest/content;`:164` kiểm lại sau OpenRead;`:231` worker.
- `ExportRepository.cs:43–54`: receiptAccessGuard đã yêu cầu đúng role membership.
  `ExportRepository.SourceAuthority.cs:9–16` trước bản sửa chỉ xét ngày/status,
  nên Supervisor có membership PM stale đọc/export worker được trong khi admission
  từ chối. HTTP cả Web và Bearer đã tái hiện Expected403/Actual200 trên source base.
  Sửa thêm phép so role tài khoản với role membership ở gate chung; không bỏ membership.
- `CollectionReadRepository.cs:21` filter RequestedBy ở SQL trước`:50` OrderBy/Take;
  thứ tự GUID SQL ổn định, không phải CreatedAt. Không trả tổng job actor khác.
- `ExportRepository.SourceAuthority.cs:29` kiểm source facts/current responsibility;
  `ExportService.Repairs.cs` giữ capture và UNAVAILABLE section hiện có.
- `ExportsController.cs` không đổi status/code; thêm tài liệu Swagger mô tả quyền
  và snapshot. Correlation header lấy từ middleware, không log cookie/token.

Các line là mốc source bản sửa, không thay evidence review. Không có quyết định mở
Supervisor-global export; cần owner chốt riêng nếu muốn thay chính sách đó.

Kiểm cuối phần HTTP:90PASS/0FAIL/0SKIP và unit export32PASS/0FAIL/0SKIP.
HTTP bao gồm phiên Web thật/CSRF và Bearer, Supervisor không membership bị từ chối
ở GET/POST/direct ID; membership đúng role đọc/tạo được, lệch role/hết hạn/future/
Ended bị từ chối; role/account inactive401. Ba job creator phân trang không lẫn jobPM,
cursor actor khác400, detail jobPM theo quyền riêng200. Replay không thêm job;
thu hồi trước worker làm FAILED/export_authority_revoked. MinIO thật trả PDF và
repair ZIP có byte ảnh khớp file gốc; download/expiry/post-OpenRead revocation và
training/source lineage được kiểm lại bởi các lớp hồi quy hiện có.
Đây là90ca tập trung, không ghi thành full API toàn repo. Fixture repair history
được tạo trên SQL cô lập; không tuyên bố đã chạy lại trọn Crew workflow/browser.
SQL export/admission/lease/renderer recovery:28PASS/0FAIL/0SKIP, gate TRX đạt.
Swagger/exact-source kiểm lại sau chỉnh mô tả:37PASS/0FAIL/0SKIP, có trùng nhóm90.
Build analyzer0error, format toàn solution,10source-contract gates và diff check
đạt. Không đổi tên/thêm SQL integration test nên lane manifest giữ nguyên.

Kiểm bổ sung HTTP25/11 cho cả nguồn REPORT và SURVEY sau chuyển nghĩa vụ (2PASS,
trùng identity đã có trong nhóm90, nhưng thêm assertion): A tạo job theo item trước
chuyển202 → B tiếp nhận đúng nghĩa vụ → A GET job/manifest/content403 và replay403;
A export package gồm cả itemA/itemB trả422export_source_unavailable, không âm thầm
bỏ itemB; B POST URLB với item có provenanceA cũng422export_source_unavailable;
B đoán jobA403. Worker job đã capture trước chuyển ghiFAILED/
export_source_access_revoked, không render; snapshotHash vẫn giữ nguyên. Chỉ có
một job trong project nguồn của fixture, không thêm job từ các request bị từ chối.
Kết quả này xác nhận giới hạn export hiện hành, không phải quyền export mới sau chuyển.
