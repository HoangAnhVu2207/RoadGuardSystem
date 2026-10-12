# Requirements — authority và acceptance hiện hành

**ACTIVE · DOCUMENTATION_UPDATED · DOC-AUDIT-01**, source `f0c8762`. Đây là bảng dẫn nguồn; không đổi nội dung [confirmed decisions](confirmed-decisions.md) hoặc [assigned R01–R30](../../planning/development/HUY-FINAL-INTEGRATION.md).

| Requirement | Expected boundary | Implementation/acceptance boundary hiện tại |
|---|---|---|
| PR-32A / R28 | PM tự gom đo; weekly reminder không tự giao task/cấp quyền sửa | H6 calendar/weekly recovery và field task commands hiện hữu; real schedule/deployment là gate riêng. |
| PR-33A | Matching trong project, assigned/neighbor scope, vị trí/accuracy/history, fallback thiếu GPS | Candidate suggestion/decision có source/version/current scope; metric/accuracy hiện luôn unavailable và image matching chưa nối (G01). |
| PR-34A | PM review nhãn; export training chỉ approved | Candidate/label revisions và approved-facts reader hiện hữu; live AI training consumer chưa xác minh. |
| PR-35A | PM giao temporary safety, thông báo Supervisor, không đóng defect | Safety workflow/proof/notification có source; chưa chạy real field/storage. |
| PR-36A/37 + persistent H1 | Secure web cookie/server session; Android bearer/rotating refresh, current session/revocation | Hai transport có source và focused tests. Cookie eligibility theo route/method; GET Projects/staff đã opt in; no-auth/revoked bị từ chối. |
| PR-38 | 20 MiB image / 8 GiB video / 10 MiB SRT / 32 GiB dataset, multipart/resume | `UploadAdmissionPolicy` và SQL bigint đã có; không còn blanket int.MaxValue video gap. Truyền 8 GiB thật không chạy trong audit. |
| PR-39A / H2 | 100 m suggestion, PM đổi và xử lý remainder; immutable published geometry | Geometry/Pavement commands và versions đã có; official CRS/control-point accuracy chưa chứng nhận. |
| PR-40 | 50 users, p95≤2s, RPO≤15min, RTO≤4h | NOT_VERIFIED: không suy benchmark/restore từ unit/API/SQL. |
| PR-41A | Warranty+5 years evidence, hold và unknown/multiple obligations fail-closed | Retention inventory/basis/holds/evaluation hiện hữu; physical deletion/deployed backup không được audit cấp phép. |
| PR-42A/43A + R20–R22/R29/R30 | Downloaded task/geometry, original actor, encrypted approved handover, preserve unsynced data | Device/snapshot/signed sync/package/grants/import/recovery hiện hữu. Latest assigned R21 quyết định recipient scope; không dùng short old row để bác scoped Crew recipient. Device E2E còn pending. |
| PR-44 | Web/Android/FastAPI thuộc nhóm ngoài; BE sở hữu boundary/fixtures/integration | Synthetic BE AI chain có; live FastAPI protocol/adoption chưa chứng nhận, legacy callback có G02. |
| R01–R09/R11–R17/R19/R20 | Normal/FT permission, PM policy, physical measure/evidence, original clock, correction và obligation | Repair source chains có; R03 FT-final informational notification còn G04. R01 loại curing/material/traffic-opening management; extra-after-expiry deferred theo latest ND01. |
| R10/R18/R26 | Case/report riêng Defect; conclusion/publication không suy từ AI/no detections | ExistingEvidence conclusion có; FIELD/DRONE pre-Defect producer chưa tích hợp (G05), fail-closed. |
| Latest project/staff assignment | Supervisor-global, PM membership, own-recipient notifications, separate contacts/roster | GET Projects/users/members/primary-PM/account contacts đã giải quyết; không làm lại hoặc tự thêm membership commands. |

[Ma trận DOC-AUDIT-01](../audit/DOC-AUDIT-01-MATRIX.md) liên kết controller→DTO/service→repository→SQL/test cho mỗi slice. FR/BR/UC lịch sử chỉ là cross-reference; không dùng toàn bộ historical 148 requirements như scope đã duyệt. Recall@5, timeout/retry/orphan-window/cache duration không được confirmed table chấp thuận. Các localized pending phải theo latest assigned spec, không tái mở quyết định đã chốt.
