# DOC-V2-RECON

## Task metadata

- Owner: `anh`
- Branch: `anh`
- Delivery status: `DONE`
- Started: `2026-09-28`
- Base revision: `49c7eae`
- Scope: phân loại và sửa tám tham chiếu `missing-01..08`; không thay đổi OpenAPI hoặc runtime.

## Goal

Đối chiếu tên tham chiếu cũ với file thật và nội dung liên quan trong checkout. Phân biệt nguồn hiện hành, nguồn lịch sử và nguồn đã bị tài liệu V2 canonical thay thế; không suy hiệu lực chỉ từ tên file.

## Acceptance criteria

- Tám mục có actual path, nội dung đã đọc, trạng thái và quan hệ với V2.
- Các link đang dùng trỏ tới file thật; anchor `missing-01..08` vẫn tồn tại để giữ tương thích.
- ADR 003 được nhận diện là accepted/current reference; các thiết kế ngày 22/09 được giữ làm nguồn lịch sử/proposal, không được mô tả là runtime.
- OpenAPI canonical và FE baseline giữ nguyên byte/hash.
- Contract hash, structural/link validator và `git diff --check` đạt.

## Reconciliation

| Anchor | Original reference | Actual path | Content checked | Classification | V2 relationship |
|---|---|---|---|---|---|
| `missing-01` | `../adr/003-backend-delivery-and-ai-boundary.md` | `docs/adr/003-backend-delivery-and-ai-boundary.md` | Status/authority, acceptance boundaries, V2 planning amendment | `FOUND_CURRENT_REFERENCE` | ADR accepted; ADR 006 amendment governs V2 ownership. |
| `missing-02` | `Dac_ta_UseCase_v2.md` | `docs/diagram/Dac_ta_UseCase_v2.md` | Header, source precedence, state notes, source list | `SUPERSEDED` | Historical source; canonical active use cases are `docs/diagram/V2/02_Requirements/04_Use_Cases.md`. |
| `missing-03` | `Domain_Model.md` | `docs/diagram/RoadGuard_Domain_Model_v1.md` | Target-design notice, aggregates, decision status | `FOUND_HISTORICAL` | Historical target model; not current EF/runtime evidence and not a substitute for the V2 target model task. |
| `missing-04` | `ERD.md` | `docs/diagram/RoadGuard_ERD_v1.md` | Target-design notice, conventions, logical relations | `FOUND_HISTORICAL` | Historical logical ERD; not physical schema or migration evidence. |
| `missing-05` | `RoadGuard_AI_Segment_Edge_Design_v1.md` | `docs/diagram/RoadGuard_AI_Segment_Edge_Design_v1.md` | Status, design decisions, async/manifest sections | `FOUND_HISTORICAL` | Proposal dated 22/09/2026; retained as input where not superseded by later decisions. |
| `missing-06` | `RoadGuard_Domain_Model_v1.md` | `docs/diagram/RoadGuard_Domain_Model_v1.md` | Target-design notice, aggregate boundaries | `FOUND_HISTORICAL` | Same historical model as `missing-03`, now linked by its exact filename. |
| `missing-07` | `RoadGuard_Incident_Segment_Design_v1.md` | `docs/diagram/RoadGuard_Incident_Segment_Design_v1.md` | Status, roles, route/segment and case state sections | `FOUND_HISTORICAL` | Proposal dated 22/09/2026; later V2 decisions and canonical requirements win on conflict. |
| `missing-08` | `User_Stories_Acceptance_Criteria_v2.md` | `docs/diagram/User_Stories_Acceptance_Criteria_v2.md` | Header, scope, common rules, state table | `SUPERSEDED` | Historical source; canonical active stories are `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md`. |

## Delivery status

`DONE`

## Completion history

### 2026-09-28 - IN_PROGRESS

Đã xác nhận đúng branch/base revision, đọc tám source candidates và lập mapping nội dung. Chưa đánh `DONE` cho tới khi link, manifest và validators được cập nhật/chạy.

### 2026-09-28 - DONE

- Đã sửa: phân loại 8/8 tham chiếu, thay link vòng qua missing register bằng actual paths, giữ đủ anchor lịch sử và cập nhật delivery index/manifest.
- Files: `docs/diagram/V2/02_Requirements/04_Use_Cases.md`, `05_User_Stories_Acceptance_Criteria.md`, `03_Data/01_Data_Dictionary.md`, `08_Delivery/00_Original_Documentation_Index.md`, `04_Missing_Referenced_Documents.md`, `08_Delivery/README.md`, `docs/diagram/V2/README.md`, delivery `manifest.json`, governance index và task này.
- AC: 8 mục có path/content/classification; ADR 003 là current reference; source proposal/history không bị gọi là runtime; OpenAPI canonical/baseline giữ nguyên byte.
- Verification: `check_contracts.py` PASS với SHA-256 `a98f43c662e574b37b2619b25e25b894419a0f0725c8af7e66c2c3c7e3143992`; `validate_package.py` `STRUCTURAL_CHECKS_PASS` với 220 Markdown links, 153 schemas, 133 operations, 8 positive và 4 negative fixtures; manifest check 67 files, 0 mismatches; anchor check đủ `missing-01..08`; `test_contract_guard.py` 7/7 PASS; `git diff --check` PASS.
- Corrected command: lần chạy anchor check đầu dùng regex escape sai và fail trước khi kiểm nội dung; lệnh sửa đã PASS đủ 8 anchor.
- Chưa xác minh: Mermaid rendering, backend build/test, API/SQL/provider/device/browser/performance/UAT; các hạng mục này ngoài scope docs-only và không được suy là PASS.
