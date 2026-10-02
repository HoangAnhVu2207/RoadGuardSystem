# RoadGuard V2 governance tasks

Các task trong thư mục này quản lý việc đồng bộ tài liệu, contract thiết kế và quy trình agent. Chúng không nằm trong `task_manifest.json.tasks`, vì manifest đó giữ quy tắc một operation API tương ứng một task.

| Task | Owner | Branch | Status | Kết quả |
|---|---|---|---|---|
| [DOC-V2-RECON](DOC-V2-RECON.md) | anh | anh | DONE | 8/8 tham chiếu đã phân loại; link/manifest/contract guards đạt |
| [DOC-V2-DECISIONS](DOC-V2-DECISIONS.md) | anh | anh | DONE | 28 decision + 13 config/ownership decisions; current OPEN labels corrected |
| [AGENT-V2-ROUTING](AGENT-V2-ROUTING.md) | anh | anh | DONE | Five skills valid; source/checkpoint/completion routing; 8 scenarios pass |
| [TASK-V2-LIFECYCLE](TASK-V2-LIFECYCLE.md) | anh | anh | DONE | Lifecycle/template/status sync and append-only completion records |
| [DOC-V2-MODEL](DOC-V2-MODEL.md) | anh | anh | DONE | ERD/domain/state/code map; current vs target/proposed separated |
| [DOC-V2-CONTRACT](DOC-V2-CONTRACT.md) | anh | anh | DONE | Canonical OpenAPI draft + generated artifacts; runtime NOT_ENABLED |
| [PLAN-V2-REBUILD](PLAN-V2-REBUILD.md) | anh | anh | DONE | 133/133 metadata/refs/checkpoints; IDs/owners/history preserved |
| [DOC-V2-GUARDS](DOC-V2-GUARDS.md) | anh | anh | DONE | Alignment/contract/manifest guards pass |
| [CODE-V2-RECON](CODE-V2-RECON.md) | anh | anh | DONE | 133-row source inventory; 5 reuse candidates, 14 partial, 114 no-symbol; no implementation claim |
| [ALIGNMENT-FINAL-REVIEW](ALIGNMENT-FINAL-REVIEW.md) | anh | anh | DONE | Final decision/contract/model review; corrected proposed snapshots/fixtures and scoped P1-001..005 |
| [FIX-V2-ALL-API-REVIEW](FIX-V2-ALL-API-REVIEW.md) | anh | anh-review | PARTIAL | RV-12 and seeder findings verified; 133/133 required metadata guard passes; status/evidence conflicts remain open |
| [COV-BASELINE-Q11-CONTRACT](COV-BASELINE-Q11-CONTRACT.md) | anh | anh | TODO | Chot Q11 coverage/position/quality va schema baseline theo segment/band truoc migration/API |

Trạng thái `DONE` chỉ được ghi sau khi completion history có file thay đổi, acceptance criteria, lệnh kiểm tra, kết quả thật và phần chưa xác minh. Tài liệu governance không chứng minh backend, SQL, API, thiết bị hoặc provider đã chạy.
