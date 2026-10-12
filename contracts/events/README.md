# Event, AI và offline boundary

**ACTIVE index · DOC-AUDIT-01**, baseline `f0c8762`. Trang này dẫn implementation/local contracts và external proposals, không tự tạo event schema đã adopted. [Local Huy contract](../http/huy-final.local-contract.md) và [API source inventory](../../docs/audit/DOC-AUDIT-01-API.md) là điểm đọc hiện hành.

| Boundary | Source implementation | Giới hạn/adoption |
|---|---|---|
| BE processing → AI/callback | `ProcessingV2Controller` → service/persistence; separate `AiMockRunsController`/ANH02 lease/result/provenance worker. Dataset/model/manifest and verified raw file guards hiện hữu. | Legacy callback chưa active/latest attempt fence (G02). FastAPI signed receipt/retry/late protocol ở [AI proposal](../../docs/diagram/V2/AI_Integration/README.md), chưa live adoption. Synthetic fixtures không là real model. |
| Source transaction → H6 dispatcher → recipient | Actual repair/field/calendar/deadline adapters và scoped outbox/receipts hiện hữu. Unknown source fail-closed; notification read recipient-scoped. | Registered FT-final event thiếu producer/source adoption (G04). Actual Web/Android receiving/delivery và production worker topology chưa E2E. |
| Android → offline admission → module core → receipt | `OfflineWorkflowController` v1 → engine/repository/admission → shared FIELD/repair core. Device keys/signed finite batch/snapshot/package/grants/import/artifacts/recovery/version/conflict/current-permission guards đã có. | External Android crypto/queue/decrypt/kill/reboot/camera/storage acceptance còn pending. Original actor giữ riêng current importer. Proposed V2 `syncOperations` không phải tên route runtime. |

Không còn kết luận “current sync endpoint absent” hoặc “dispatcher not found” từ RF-01. Các RF-04 CG11–13 vẫn là historical questions; [open gaps](../../docs/audit/DOC-AUDIT-01-OPEN-GAPS.md) chỉ giữ phần còn căn cứ ở HEAD. Không tự thêm timeout/recall/heartbeat/retry số mặc định hoặc đổi wire/lock.
