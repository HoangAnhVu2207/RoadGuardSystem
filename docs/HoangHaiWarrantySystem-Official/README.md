# HoangHaiWarrantySystem — Official documentation

**Điểm vào canonical · Phase 1 PASS · Phase 2A business workflows chờ independent JEV review · 14/10/2026.**

Đây là điểm truy cập chính cho việc phát triển bộ tài liệu HoangHaiWarrantySystem. Phase 1 thiết lập authority, phạm vi, glossary, actor/client matrix và source registry. Phase 2A bổ sung 10 business workflows được đối chiếu quyết định và current source; không phải acceptance của Web/Expo/provider runtime. Các chương Backend, Web, Mobile và FastAPI đầy đủ chưa được viết lại; người đọc dùng các tài liệu nguồn được dẫn ở crosswalk và kiểm giới hạn xác minh của từng nguồn.

Tên sản phẩm là **HoangHaiWarrantySystem**. `RoadGuardSystem` trong namespace, repository, database, route và artifact là **legacy technical identifier** được giữ nguyên. Mobile dùng **React Native + Expo**; tên API `/api/v1/auth/android/*` vẫn là wire hiện hữu, không được tự đổi thành `/auth/mobile/*`. iOS production readiness chưa được xác minh.

## Bắt đầu đọc

| Cần biết | Trang canonical hiện có |
|---|---|
| Nguồn nào quyết định hành vi, cách xử lý mâu thuẫn, glossary | [Authority and source of truth](00_Governance/01_Authority-and-Source-of-Truth.md) |
| Product naming, MVP, quyết định đã chốt và deferred scope | [Product decisions and scope](00_Governance/02_Product-Decisions-and-Scope.md) |
| Phần đã thấy trong source, test thực chạy và gates chưa đóng | [Implementation and verification status](00_Governance/03_Implementation-and-Verification-Status.md) |
| Scope Phase 1, ownership, thay đổi và review gate | [Document control and change history](00_Governance/04_Document-Control-and-Change-History.md) |
| Tài liệu cũ dẫn về đâu; nguồn API/workflow/operations | [Source registry and migration crosswalk](00_Governance/05_Source-Registry-and-Migration-Crosswalk.md) |
| Supervisor/PM Web, Reporter/Operator/Crew Mobile và quyền theo nguồn | [Actor/client/permission matrix](01_Product-and-Roles/02_Actor-Client-Permission-Matrix.md) |
| Business workflows: intake/candidates/survey/FIELD/NORMAL/FT/offline/operations | [Canonical business workflows](02_Business-Workflows/README.md), [Phase 2A review report](02_Business-Workflows/PHASE2A_REVIEW_REPORT.md) |

## Quy tắc sử dụng

- Quyết định owner xác định hành vi mong đợi; source xác định implementation hiện có; test chỉ chứng minh điều kiện đã chạy. Đọc [precedence](00_Governance/01_Authority-and-Source-of-Truth.md#precedence-and-conflict-handling) trước khi hợp nhất hai nguồn mâu thuẫn.
- Dùng [Phase 0 full API inventory](../audit/HHWS-DOCS-REBUILD-01/02_FULL_API_INVENTORY.md) và controller/DTO cho wire hiện tại. OpenAPI/V2 draft, Postman subset hoặc tên test không tự là approval hoặc E2E acceptance.
- Backend hiện hữu là trung tâm integration cho Web, Mobile và FastAPI. Khi source khác quyết định owner, ghi `PRODUCT_SOURCE_GAP`; FE không được tự sửa authority bằng local state hoặc endpoint giả.
- Tài liệu cũ vẫn ở nguyên vị trí. Crosswalk phân biệt nguồn hiện hành, nội dung stale đúng phạm vi và checkpoint lịch sử; chưa archive/delete/move hay cập nhật contract lock.

## Lộ trình canonical sau review

| Nhóm | Trạng thái hiện tại | Nguồn hiện dùng |
|---|---|---|
| `00_Governance`, actor/client matrix | Phase 1 PASS theo owner | Các trang ở trên |
| `01_Product-and-Roles` overview/requirements/MVP chi tiết | Chưa viết lại | [Product source](../product/README.md), áp quyết định mới của Phase 1 khi client wording cũ mâu thuẫn |
| `02_Business-Workflows` | Phase 2A source-backed docs, independent review pending | [10 workflows và evidence](02_Business-Workflows/README.md) |
| `03_Backend-and-Data`, `04_API-Contracts` | Phase 2B chưa bắt đầu | [Backend Official](../RoadGuard-Backend-Official/README.md), [full source inventory](../audit/HHWS-DOCS-REBUILD-01/01_SOURCE_AND_DOCS_INVENTORY.md) |
| `05_Web`, `06_Mobile-Expo` | Phase 3 chưa bắt đầu | [Actor/client matrix](01_Product-and-Roles/02_Actor-Client-Permission-Matrix.md), [consumer/source limits](00_Governance/03_Implementation-and-Verification-Status.md) |
| `07_AI-FastAPI` | Phase 4 chưa bắt đầu | [Existing AI boundary](../RoadGuard-Backend-Official/06_Integrations/02_BE_FastAPI_Integration_Boundary.md), [Phase 0 integration gaps](../audit/HHWS-DOCS-REBUILD-01/03_BUSINESS_AND_PLATFORM_CONFLICTS.md) |
| `08_Testing-and-Operations`, `99_Archive` | Phase 5 chưa bắt đầu; không di chuyển archive | [Evidence/gaps](00_Governance/03_Implementation-and-Verification-Status.md), [crosswalk](00_Governance/05_Source-Registry-and-Migration-Crosswalk.md) |

Đường dẫn chương tương lai chỉ là kế hoạch trong code notation; không có link tới file chưa tồn tại và không tạo placeholder giả như tài liệu đã hoàn thành.

## Kiểm tra link

Link checker chỉ đọc file, không sửa tài liệu/source hay gọi network:

```powershell
python docs/HoangHaiWarrantySystem-Official/tools/check_links.py
```

Tool kiểm inline/reference Markdown links, local target existence, local Markdown heading anchors và target trong repository. External URL, deployed API, quyền truy cập và runtime E2E không nằm trong kết quả này. Giới hạn parser và evidence nằm trong [change control](00_Governance/04_Document-Control-and-Change-History.md#verification-and-review).

**STOP sau Phase 2A push.** Chờ independent JEV review business workflows; Phase 2B và các chương Web/Mobile/FastAPI chưa được thực hiện.
