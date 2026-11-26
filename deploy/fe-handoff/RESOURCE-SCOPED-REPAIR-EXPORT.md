# Repair dossier theo quyền tài nguyên — Hoàng

Owner bổ sung quyền ngày 25/11/2026. Base `fbde7c43923f38f969af2ac75b82c56c4d0770f6`.
Đây là quyết định mới, không phải diễn giải rằng ngoại lệ đã tồn tại trước đây.
Xem [spec và bằng chứng kiểm](../../planning/development/RESOURCE-EXPORT-OFFLINE-ADMISSION.md).

## Phạm vi và proof

| Actor / phạm vi | Điều kiện |
|---|---|
| PM | Account/role hiện hành, membership PM còn hiệu lực trong project context; từng item thuộc nghĩa vụ đang chịu trách nhiệm tại project đó |
| Supervisor có membership | Giữ kiểm membership đúng role và nguồn hiện hành |
| Supervisor không membership, NORMAL | Đúng current item của formal obligation; `ApprovedBy` là Supervisor đó và `ApprovedPlanHash == ProposalPlanHash` còn hiệu lực; không cancelled/superseded; đúng project nguồn |
| Supervisor không membership, Fast Track | Đúng current formal item, không cancelled/superseded; head phân công Supervisor còn hiệu lực và generation tương ứng, đúng item/package/project |
| Supervisor chỉ có role/global reporting | Không đủ proof |

Tận dụng proof lưu trên item và phân công `notification-supervisor` hiện có. Quyết định mới chỉ dùng proof này cho **đọc/export**, không cấp quyền approval/final Fast Track, quản trị hay thi công. Không tạo membership hoặc tự gán dữ liệu cũ. Legacy thiếu current item không tự được cấp ngoại lệ Supervisor; membership cũ vẫn được xét theo contract nguồn.

Sau transfer được chấp nhận A → B, PM B dùng URL project **B**, chọn đúng item đang nhận. Snapshot giữ project nguồn A trong history/sourceContext, không copy hoặc đổi project gốc. Không cấp quyền project A hoặc private Report/Case/AI. Transfer formal không chuyển quyền safety: mỗi obligation kiểm trách nhiệm riêng. Package lẫn item ngoài quyền bị từ chối toàn bộ (`422 export_source_unavailable` khi selection không đầy đủ; thiếu proof trên selection hợp lệ trả `403 forbidden`), không tạo dossier thiếu item âm thầm.

## API và headers

Prefix `/api/v1/projects/{responsibilityProjectId}/exports`:

| Method/path | Contract |
|---|---|
| `POST` prefix | `202`, job `id`, `snapshotId`, `snapshotHash`, `status`, Location/ETag. `Idempotency-Key` bắt buộc; không cần If-Match |
| `GET` prefix `?limit=…&cursor=…` | Collection **creator-only**, lọc RequestedBy trước pagination; kiểm quyền mỗi job. Membership hoặc proof tài nguyên hiện hành để vào collection, kể cả rỗng |
| `GET /{exportId}` | Trạng thái job; người khác creator vẫn có thể đọc nếu có quyền toàn bộ scope |
| `GET /{exportId}/manifest` | Snapshot/hash/nguồn/version/files/sections; không biến metadata thành quyền file |
| `GET /{exportId}/content` | Artifact khi `SUCCEEDED`, còn hạn và quyền hiện hành còn đủ; kiểm lại sau mở storage |

Bearer hoặc Web session. Web dùng `credentials: "include"`; lấy token tại `GET /api/v1/auth/web/csrf`, gửi `X-CSRF-TOKEN` cho POST. GET không cần CSRF. Retry mất response dùng nguyên key/body. Cùng key khác body `409 idempotency_conflict`; replay vẫn kiểm quyền snapshot gốc. Không chuyển receipt/creator job A cho B.

```json
{
  "kind": "DOSSIER",
  "format": "ZIP",
  "repairItemIds": ["<itemId từ lifecycle/repair item>"],
  "includeOriginalFiles": true
}
```

Hoặc `repairPackageIds`, tối đa 20 package / 100 item; không kết hợp bộ lọc thời gian/segment/defect với repair scope. PDF không kèm originals. Job có thể `QUEUED`, `RUNNING`, `SUCCEEDED`, `FAILED`; FE poll có giới hạn, dừng khi trạng thái cuối.

Manifest của repair dossier có `definitionVersions: ["repair.dossier.v1"]`, `sourceRevisions` chứa đúng RepairItem/RepairDecision và DossierContent, `filters` giữ selection, `sections` giải thích nguồn tùy chọn unavailable. ZIP dùng `repair-evidence/{fileId}.png|jpg` và `repair-dossier.json`.

## Ma trận kiểm quyền

| Boundary | Kiểm hiện hành |
|---|---|
| Create / replay | Principal, loại export, toàn bộ selection; replay dựa snapshot producer gốc |
| Collection | Creator-only SQL; từng job được xét lại; cursor không mở quyền actor khác |
| Detail / manifest / content | Toàn bộ snapshot scope, không creator-only blanket; manifest/content kiểm file |
| Worker claim/render/complete | Quyền actor yêu cầu, trách nhiệm item và nguồn/file; không dùng quyền service account |
| Source/artifact OpenRead | Kiểm lại quyền sau mở stream trước khi trả bytes; thu hồi không biến snapshot thành capability |

General DOSSIER/TRAINING vẫn cần membership hiện hành. Adapter mới chỉ đọc ảnh FIELD của item: VERIFIED, đúng project/task/defect, immutable attempt/correction, file version/hash/purpose. Giữ global file API; không dùng adapter để mở ảnh Reporter private hoặc AI. Nguồn tùy chọn không đọc được ghi section unavailable; không bỏ item bắt buộc khỏi selection.

Khi chuyển tiếp B → C, B không còn quyền tải job B. A cũng không tải job cũ chỉ vì từng là creator. Snapshot/hash không đổi để hợp quyền mới; C cần tạo job mới trong context C.

## Kiểm chứng và triển khai

Kết quả local: nhóm API/Swagger cuối 52/52 (trùng một phần full run), unit 1156/1156, bốn SQL lane 808/808 và identity gate đạt. Full API trước sửa mô tả Swagger: 1717 PASS/1 FAIL/1 smoke SKIP; đã sửa và chạy lại class liên quan, không gọi đó là full API PASS mới. SQL lane 1 ban đầu 203 PASS/1 assertion cũ fail, sau cập nhật contract và kiểm class 11/11 thì lane 1 đạt 204/204. Kết quả chạy cuối, SHA và giới hạn được ghi trong spec; không cộng các lượt test trùng. Fixture SQL lịch sử không đồng nghĩa đã chạy Crew workflow qua UI. Browser/FE chưa nghiệm thu. Correlation cũ `6d46f061-7f15-47ba-bc6c-7edc5f8c8dc7` vẫn **NOT_CORRELATED**: chưa nối actor/session/runtime, chưa có bằng chứng POST gốc.

Không có migration mới trong thiết kế này. Cập nhật backend production revision rồi mở phiên Web mới; dùng runtime Swagger cho security/schema. Link [OpenAPI snapshot](openapi.json) được đồng bộ trên bản cuối. Việc push source không tự cập nhật backend của FE.

## Tin nháp

BE bổ sung quyền repair dossier theo đúng item: Supervisor không membership cần proof giám sát/nghiệm thu còn hiệu lực; PM B sau nhận nghĩa vụ dùng URL B và chỉ xuất item đang chịu trách nhiệm, giữ provenance A. Collection vẫn chỉ gồm job do chính mình tạo; detail/manifest/content và worker kiểm lại toàn bộ scope, kể cả replay và sau mở storage. Package lẫn item ngoài quyền bị chặn, không xuất thiếu âm thầm. Export tổng hợp/TRAINING và file private giữ quyền cũ. Web POST gửi cookie + CSRF và Idempotency-Key; retry nguyên key/body. Nhờ nối create → poll → manifest/content và retest thu hồi/chuyển tiếp theo handoff. Kết quả BE không thay nghiệm thu UI/runtime FE.
