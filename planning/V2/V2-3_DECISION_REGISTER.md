# RoadGuard V2 decision and delivery register

**Revision:** `V2-ALIGN-2026-09-28`  
**Authoritative business source:** `RoadGuard_Prompt_Hop_Nhat_Sua_Docs_Ke_Hoach_V2_3 (2).md`, sections B and appendices D/E/F.  
**Scope:** documentation, target design contracts, generated artifacts and planning.  
**Runtime status:** no row in this register proves backend implementation, deployed schema, provider behavior or field verification.

## Status vocabulary

| Field | Values and meaning |
|---|---|
| `businessStatus` | `APPROVED`, `APPROVED_DESIGN`, `APPROVED_PILOT_CONFIG`, `APPROVED_TARGET`, `APPROVED_PROJECT_POLICY`, `APPROVED_OWNERSHIP`, `SUPERSEDED`, `OPEN_TECHNICAL` |
| `contractStatus` | `DRAFT`, `REVIEWED`, `APPROVED`, `PROPOSED_DELTA`; this is independent of business approval |
| `implementationStatus` | `NEEDS_REPO_CHECK`, `PARTIAL`, `IMPLEMENTED`, `EXTERNAL` |
| `verificationStatus` | `NOT_RUN`, `DOCS_PASS`, `FOCUSED_PASS`, `BLOCKED`, `VERIFIED` |
| `dependencyType` | `contract`, `data-fixture`, `integration-release` |

Approved business behavior is not the same as an approved wire schema, implemented runtime or passed test. Historical `OPEN` labels are superseded only for the specific decision stated below; any remaining technical gate stays explicit.

## D01-D28

| decisionId | Source | Statement | Supersedes | businessStatus | contractStatus | remainingGate | Affected requirements/tasks |
|---|---|---|---|---|---|---|---|
| D01 | §B, câu 1 | Hoàn thiện tài liệu toàn sản phẩm, phân rõ Sprint 1 và giai đoạn sau. | Scope tài liệu rời rạc | APPROVED | REVIEWED | Không suy timeline/năng lực từ nhóm Sprint. | Overview; planning index |
| D02 | §B, câu 2/Q01 | Đợt gom luôn chỉ đo; PM giao task sửa riêng sau đo, có thể Fast Track nếu đủ policy; không hồi tố mode. | Q01 OPEN; amendment đo-trước quá rộng | APPROVED | PROPOSED_DELTA | Wire link measurement-to-new-task, snapshot và tests. | BR-09/10; FR-16/18; P1-046..052/065 |
| D03 | §B, câu 3/Q02 | Supervisor ban hành khung công ty; PM cấu hình/kích hoạt trong khung; vượt khung cần phê duyệt; không duyệt từng ca Fast Track. | Q02 OPEN; PM tự ban hành không giới hạn | APPROVED | PROPOSED_DELTA | Company framework/version/exception wire contract. | FR-15/18; P1-043..045 |
| D04 | §B, câu 4,11-13/Q03 | Fast Track chỉ cho đường đã bàn giao đang bảo hành/bảo trì; ứng viên joint reseal và local shallow spall; chưa có hồ sơ/ngưỡng production. | Phạm vi Fast Track chung chung | APPROVED | PROPOSED_DELTA | Hồ sơ method/material, threshold và production activation. | BR/FR policy; P1-043..052 |
| D05 | §B, câu 5,16/Q04 | Đội mới chỉ bắt đầu cùng scope sau xác nhận đội cũ dừng/bàn giao; PM có thể ghi căn cứ xác nhận ngoài app; việc chưa sync vào conflict, không tự nghiệm thu. | Q04 business OPEN | APPROVED | PROPOSED_DELTA | Acknowledgement/conflict schema, race tests và atomic start gate. | FR-22; P1-057; P2-029 |
| D06 | §B, câu 6/Q17 | Supervisor cho phép bàn giao dữ liệu; PM đúng dự án tiếp nhận; giữ actor/source gốc, không đổi ownership để vượt quyền. | Q17 business OPEN | APPROVED | PROPOSED_DELTA | Rescue grant/receipt, device/key verification và security tests. | FR-22; P2-029; auth/account tasks |
| D07 | §B, câu 7/Q05 | Giữ phần đo đạt; đo lại/bổ sung phần thiếu; PM mở rộng đo lại khi sai dụng cụ/phương pháp ảnh hưởng nhiều kết quả. | Q05 OPEN | APPROVED | PROPOSED_DELTA | Measurement validity/impact model và acceptance tests. | FR-16; P1-051/052 |
| D08 | §B, câu 8,15/Q06 | BEFORE phải bền vững trước thi công; nếu mất sau thi công, PM lập incident và Supervisor quyết định; chưa đủ căn cứ không nghiệm thu. | Q06 OPEN | APPROVED | PROPOSED_DELTA | Incident contract, evidence durability và state tests. | FR-21/23; P1-058..061 |
| D09 | §B, câu 9/Q07 | PM mở lại hồ sơ Fast Track do PM đóng; hồ sơ Supervisor đóng cần PM đề nghị và Supervisor xác nhận; prior failure tiếp tục lỗi cũ, recurrence sau acceptance tạo hồ sơ liên kết mới. | Q07 OPEN | APPROVED | PROPOSED_DELTA | Reopen operations, versions, audit và authorization tests. | FR-23..25; P1-040 plus backlog |
| D10 | §B, câu 10/Q08 | PM công bố từng defect đạt cho đúng Reporter dù case tổng còn mở; cần Report-Defect mapping/public projection, không lộ nguồn khác. | Q08 OPEN; whole-case-only publication | APPROVED | PROPOSED_DELTA | Selector, mapping, projection và privacy tests. | FR-24/25; P1-033/034 |
| D11 | §B, câu 14 | PM điều phối mở giao thông; đội trưởng được phân quyền trước xác nhận checklist kể cả offline; tách physical completion, traffic release và acceptance. | Trạng thái hoàn thành gộp | APPROVED | PROPOSED_DELTA | Curing/release record, predelegation và conflict tests. | P1-058..061; model backlog |
| D12 | §B, câu 17,24 | AI FastAPI gợi ý trùng; PM quyết định gộp/tách; BE giữ nghiệp vụ; pipeline VIDEO_ANALYSIS rồi DUPLICATE_MATCHING tự động sau trigger. | Một-stage mock/callback | APPROVED | PROPOSED_DELTA | AI v2 service schema, artifact/receipt, PM review contract và E2E provider tests. | P2-030..036; P1-035..040 |
| D13 | §B, câu 25A | PM bấm Phân tích; upload không tự trigger; dataset phải đủ điều kiện; các stage sau tự động, không trao đổi JSON thủ công. | Auto-trigger-on-upload | APPROVED | PROPOSED_DELTA | Trigger/readiness/idempotency state contract. | P2-019/025/030..033 |
| D14 | §B, câu 18/Q10 | Cho slab/lưới dự kiến với nhãn chưa xác minh; report không bắt buộc slab; không mặc định tấm dài 4 m. | Q10 business OPEN | APPROVED | PROPOSED_DELTA | Provisional status/schema và later verification. | P2-009/010; P1-066 |
| D15 | §B, câu 19/Q11 | Tách aircraft position, data quality và coverage; thiếu căn cứ là UNKNOWN, không tự PASS/FAIL. | Q11 business OPEN | APPROVED | PROPOSED_DELTA | Threshold/calibration còn mở; schema/status mapping cần review. | P2-020/021; AI result |
| D16 | §B, câu 20/Q12 | PM đặt/kéo ghim/nhập tọa độ access/destination; Crew/Operator đề nghị chỉnh; destination tách route centerline. | Q12 OPEN | APPROVED | PROPOSED_DELTA | Provenance/coordinate validation và permission tests. | P2-018 |
| D17 | §B, câu 21/Q13-Q14 | Pilot dự kiến Vĩnh Long; GPX đo sau; DJI Mini 2 SE; video/SRT có theo xác nhận nhưng bytes chưa kiểm. | Q13/Q14 thiếu context | APPROVED | PROPOSED_DELTA | CRS/parser/calibration/device-file verification; không suy capability. | P2-003..008/019..022 |
| D18 | §B, câu 22/Q15 | GPS recorder trong app để giai đoạn sau; PM nhập GPX ngoài app hoặc ordered points. | Q15 OPEN | APPROVED | PROPOSED_DELTA | Parser/import contract; mobile recorder deferred. | P2-003/005; Android external |
| D19 | §B, câu 23/Q16 | Vẫn nhận report ngoài bảo hành/chưa baseline; điều phối xác định trách nhiệm; không dùng quyền hợp đồng cũ; nguy hiểm đi luồng an toàn tạm. | Q16 OPEN | APPROVED | PROPOSED_DELTA | Triage/responsibility and emergency scope tests. | P1-024/030/062 |
| D20 | §B, bổ sung sau câu 25 | Operator theo segment/observation scope; mỗi video một primary segment; segment có nhiều video/lần bổ sung; assignment không xác nhận vị trí mọi lỗi. | Mảng video/telemetry không identity | APPROVED | PROPOSED_DELTA | Video-telemetry mapping, versions và history. | P2-011..020 |
| D21 | §B, bổ sung sau câu 25 | BE chia segment dọc ordered polyline/GPX; PM preview/chỉnh ranh, giữ/gộp đoạn dư; không gap/overlap; route đã dùng tạo version mới. | Segment generation chưa đủ remainder/version | APPROVED | PROPOSED_DELTA | Geometry/CRS algorithms and fixtures. | P2-003..008/059 |
| D22 | §B, baseline | PM xác nhận phương án chia; Supervisor xác nhận route version; không gộp hai quyền. | Actor ambiguity | APPROVED | PROPOSED_DELTA | Authorization/state tests. | P2-006..008 |
| D23 | §B, câu 26 | Web cho Supervisor/PM; Reporter dùng web mobile; Android cho Crew/Operator. | Reporter Android baseline cũ | APPROVED | REVIEWED | External team handoff contract. | FE scope; D44 |
| D24 | §B, câu 27A | Android tác nghiệp offline; web cần mạng cho nghiệp vụ, không cam kết full offline. | FE-GAP-13 OPEN | APPROVED | REVIEWED | Web drafts/cache semantics; Android sync contract. | FE/Android workstreams |
| D25 | §B, câu 28 | Email/password; Reporter OTP một lần để verify email, không OTP mỗi login; chưa có Google/Microsoft SSO. | Auth scope ambiguity | APPROVED | PROPOSED_DELTA | Runtime compatibility and auth transport implementation. | P1-001..015; FE auth |
| D26 | §B, câu 29 | Phân nhóm Sprint 1 được duyệt, không phải estimate thời gian/năng lực. | Timeline inference | APPROVED | REVIEWED | Dependency plan only. | Planning README/plans |
| D27 | §B, câu 30 | Giá trị thiếu căn cứ vẫn chờ; được thiết kế cấu hình/test data có nhãn, không production default tùy tiện. | Proposal treated as production | APPROVED | REVIEWED | Source/provenance per value. | All config/model tasks |
| D28 | §B, câu 31 | Lượt bàn giao cũ chỉ tạo một tài liệu chỉ dẫn, không sửa V2. Yêu cầu owner hiện tại đã supersede giới hạn này và cho sửa repo. | Scope của lượt cũ | SUPERSEDED | REVIEWED | Không còn gate cho alignment hiện tại. | Governance tasks |

## Approved decisions 32-44

| decisionId | Source | Statement | businessStatus | contractStatus | remainingGate | Affected tasks |
|---|---|---|---|---|---|---|
| 32A | Appendix D | PM tự gom batch; hệ thống nhắc rà soát hằng tuần; không auto create/assign/grant repair. | APPROVED | PROPOSED_DELTA | Reminder configuration and tests. | BR-10; P1-047; P2-048/049 |
| 33A | Appendix D | Matching cùng project, ưu tiên assigned/neighbor segments, dùng position/uncertainty/history; thiếu GPS dùng scope+image; PM mở rộng tìm trong project. | APPROVED | PROPOSED_DELTA | Candidate snapshot/version/privacy. | P2 AI; P1 PM review |
| 34A | Appendix D | PM review/reject labels trong project; chỉ approved labels được export; AI không tự approve. | APPROVED | PROPOSED_DELTA | FR-36/US-10 trace, permission/schema tests. | P2-036/056 |
| 35A | Appendix D | PM giao temporary safety trong scope/plan cho phép và báo Supervisor; không close defect hoặc thay acceptance. | APPROVED | PROPOSED_DELTA | Checklist/audit and structural-work exclusion. | P1-062 |
| 36A | Appendix D | Web secure cookie + server session; Android access/refresh tokens. | APPROVED_DESIGN | PROPOSED_DELTA | Compatibility, CSRF/CORS/session endpoints and runtime rollout. | Auth/FE/API tasks |
| 37 | Appendix D | Web idle 30m/max12h; Android access 15m/rotating refresh max30d; OTP 10m/5 attempts, resend >=60s and <=3/15m. | APPROVED_PILOT_CONFIG | PROPOSED_DELTA | Config/runtime/security tests; offline data survives expiry. | Auth tasks |
| 38 | Appendix D | Image 20MiB, video 8GiB, SRT 10MiB, dataset 32GiB; multipart/resume. | APPROVED_PILOT_CONFIG | PROPOSED_DELTA | Provider constraints, multipart part sizing and benchmark. | P2-019/023..028 |
| 39A | Appendix D | Suggested segment length 100m; PM may change and keep/merge remainder. | APPROVED_PILOT_CONFIG | PROPOSED_DELTA | CRS/algorithm and geometry fixtures. | P2-003..008 |
| 40 | Appendix D | 50 concurrent users, metadata p95 <=2s, RPO <=15m, RTO <=4h. | APPROVED_TARGET | REVIEWED | Load/restore exercises; not VERIFIED. | NFR/OPS |
| 41A | Appendix D | Evidence through warranty +5y; source video follows case; technical logs 90d, temporary exports 30d, backups 35d; legal hold blocks delete. | APPROVED_PROJECT_POLICY | PROPOSED_DELTA | `WAITING_RETENTION_BASIS` where warranty end unknown; worker/race/restore tests. | P2-050..052/060 |
| 42A | Appendix D | Encrypted handoff export while device accessible; loss of device/key before sync may be unrecoverable; Supervisor authorizes and project PM receives with original actor. | APPROVED_PILOT_SCOPE | PROPOSED_DELTA | Rescue grant, encryption/key management and receipt. | P2-029; auth/account |
| 43A | Appendix D | Offline includes downloaded route/segment/destination/task; offline base map is not required before provider/license selection. | APPROVED | PROPOSED_DELTA | Snapshot pack and geometry tests; provider remains open. | Android/FE-GAP-07 |
| 44 | Appendix D | Web, Android and AI delivered by external teams; BE owners retain adapter/contracts/fixtures/integration responsibility. | APPROVED_OWNERSHIP | REVIEWED | External contact/ETA not provided. | All workstream planning |

Still proposed: AI recall@5/evaluation sample size, dispatch timeout/retry/heartbeat, orphan cleanup 7 days, response cache 90 days, multipart part size/concurrency, signed URL/session TTL and research-only parameters not listed above.

## Appendix E/F design authority

| Source | Approved design direction | Status | Remaining gate |
|---|---|---|---|
| E1-E3 | Reusable method library -> Supervisor-approved company framework -> material/method profile -> PM project config -> immutable task/attempt snapshot; typed rule schema, no executable code. | APPROVED_DESIGN / PROPOSED_DELTA | Physical table/API names, technical values and runtime evaluator. |
| E2 | Provide TEST-only templates `FT-CONCRETE-JOINT-RESEAL` and `FT-CONCRETE-LOCAL-SPALL` without invented production thresholds. | APPROVED_DESIGN | Technical dossier for production deployment. |
| E4-E6 | STOP checkpoints, curing, traffic release, predelegation and symmetric online/offline test vectors. | APPROVED_DESIGN / PROPOSED_DELTA | Wire/state/storage contracts and runtime tests. |
| F | Video/SRT/route JSON exist by owner statement but bytes are unavailable here; JSON is not automatically GPX. Support ordered points and adapter validation without guessing CRS/order. | APPROVED_SCOPE / NOT_VERIFIED | Inspect real bytes only for parser/integration validation. |

## Backlog outside the 133 baseline

These are capabilities, not automatically public endpoints: company policy framework/exception; authority-specific reopen; partial publication/Report-Defect projection; detection group/split/match review; handover/conflict/rescue; BEFORE-loss incident; curing/traffic release; AI manifest/candidate/artifact/event receipts; legacy warranty/handover/work-package; research import/pair/export; retention dry-run. Assign an operation ID only after deciding whether an existing operation can be extended without semantic break.

## Packaging and evidence

`planning/V2` is an overlay that points to canonical design under `docs/diagram/V2`; it is not a self-contained ZIP containing `docs/`. Historical originals and historical PASS/DONE evidence remain unchanged. Validation evidence must state date, exact command and limits.
