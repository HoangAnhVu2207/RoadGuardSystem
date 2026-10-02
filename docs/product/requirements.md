# Product requirements and acceptance intent (RF-04 draft)

**Not active.** Source of TARGET_CONFIRMED 32-44: the owner's 2026-09-30 reconfirmation of the full Appendix D interpretation, reproduced without shortening in [confirmed decisions](confirmed-decisions.md) from the [decision register](../../planning/refactor/02-decision-register.md). The 2026-09-28 original question/options wording is unavailable. Older FR/BR are HISTORICAL reference in `docs/diagram/V2/02_Requirements/`; where this page does not mark a direction confirmed, it remains PROPOSED/UNKNOWN. These IDs are draft product ownership IDs, not new Accepted decisions.

## Confirmed rule register

| Product ID | TARGET_CONFIRMED requirement, including qualifier | Acceptance intent / implementation evidence |
|---|---|---|
| PR-32A | PM tự gom đợt đo; hệ thống nhắc rà soát hằng tuần; không tự tạo/giao task hoặc cấp quyền sửa. | Verify reminder and PM action are separate; R10 finds no production batch/reminder action. |
| PR-33A | Matching trong cùng dự án, ưu tiên segment giao và lân cận; dùng vị trí/sai số/lịch sử; thiếu GPS dùng scope+ảnh; PM tìm mở rộng trong dự án. Candidate selection có version, quyền, không auto merge. | Project and PM scope tests, no automatic merge; CG08 currently draft-only. |
| PR-34A | PM duyệt/từ chối nhãn trong dự án; chỉ nhãn đã duyệt được xuất cho training; AI không tự duyệt. | Export must filter approved provenance; no production review action found (CG08). |
| PR-35A | PM giao xử lý an toàn tạm trong phạm vi/phương án cho phép; thông báo Supervisor; không đóng lỗi/thay nghiệm thu. | Separate temporary-action, notification and acceptance states; target-only CG09. |
| PR-36A | Web dùng cookie bảo mật + session server; Android dùng access/refresh token. Đây là approved design, cần compatibility delta và CSRF, không phá client hiện có âm thầm. | R02/CG02: current bearer source; compatibility and consumer choice Q-RF02-02. |
| PR-37 | Pilot: Web idle 30 phút/max 12 giờ; Android access 15 phút/refresh luân chuyển max 30 ngày từ login; OTP 10 phút/5 lần thử, resend >= 60 giây và <= 3 lần/15 phút. Token hết hạn không xóa offline; kiểm quyền hiện tại khi reconnect. | Verify effective config and isolated HTTP/SQL behavior; RF-02 found OTP defaults, not complete session proof. |
| PR-38 | Pilot: ảnh 20 MiB/video 8 GiB/SRT 10 MiB; dataset 32 GiB; upload chia phần/resume. Không giới hạn tổng video cả đời segment; báo rõ nếu file vượt. | CG17: source `int.MaxValue` and `int` size conflicts with video target; migration and 8 GiB boundary remain untested. |
| PR-39A | Pilot segment gợi ý 100 m; PM đổi được và giữ/gộp đoạn dư. Không coi đó là tiêu chuẩn công trình hoặc độ phủ video. | Versioned geometry preview and PM choice; no production segment action (CG05). |
| PR-40 | Pilot target: 50 user đồng thời; metadata server p95 <= 2 giây; RPO <= 15 phút; RTO <= 4 giờ. | Benchmark/restore proof UNKNOWN, not a verified SLO (R18). |
| PR-41A | Chính sách dự án: bằng chứng lưu hết bảo hành + 5 năm, video nguồn đi cùng hồ sơ; log kỹ thuật 90 ngày, export tạm 30 ngày, backup 35 ngày; hold chặn xóa. Audit nghiệp vụ theo hồ sơ, không dọn như log kỹ thuật. | Unknown warranty end/multiple obligations => WAITING_RETENTION_BASIS; no deletion until all obligations allow and no hold. Mechanics/authority Q-RF02-08. |
| PR-42A | Pilot export bàn giao mã hóa khi còn truy cập thiết bị; mất thiết bị/khóa trước sync có thể không cứu được; chưa xây khóa khôi phục tổ chức. Supervisor cho phép, PM đúng dự án nhận, giữ actor gốc. | Android/BE wire and key design UNKNOWN; no sync action found (CG12). |
| PR-43A | Offline có route/segment/destination/task đã tải; nền map offline chưa bắt buộc, chỉ thêm sau khi chọn provider/license. Thiếu nền không làm mất tác nghiệp theo geometry. | External Android evidence and versioned geometry contract UNKNOWN. |
| PR-44 | Web, Android và AI giao bên khác. BE owner chịu adapter, contract, fixtures và tích hợp. | External owner/contact/ETA and provider proof UNKNOWN; source boundary is confirmed. |

The owner confirmation did **not** accept recall@5, evaluation dataset size, timeout/retry/heartbeat, orphan 7 days or response-cache 90 days; these remain PROPOSED/UNKNOWN per [decision register](../../planning/refactor/02-decision-register.md).

## Module requirements and acceptance checks

Each row links the complete RF-02 matrix rather than duplicating all historical BR/FR text. A check below is an **acceptance proposal** until its product detail and public wire are approved. The current implementation/test status is in [R01-R18 matrix](../../planning/refactor/02-requirements-traceability.md) and [CG01-CG17](../../planning/refactor/02-contract-gaps.md).

| Module/ID | Product intent, actor and state boundary | Status / acceptance checkpoint | RF task |
|---|---|---|---|
| Identity R01-R03 | Establish authenticated actor, live project membership and least-privilege scope; preserve `/profile` and `/me` until consumers resolved. | PR-36A/37 confirmed; response fields, migration and PII authorization UNKNOWN. Wrong actor/scope and expiry checks. | RF-10-01 |
| Project R04 | Supervisor/PM project, membership, handover, warranty facts and effective dates. | HISTORICAL FR-04; out-of-warranty route UNKNOWN. Validate effective membership and historical warranty data. | RF-10-02 |
| Road/GIS R05 | Ordered route/CRS, versioned segment geometry and PM pilot length choice. | PR-39A confirmed; real input CRS and remainder exception UNKNOWN. Spatial/version tests. | RF-10-02 |
| Survey R06-R07 | Plans, assignment, immutable submitted evidence, separately assessed position/quality/coverage and baseline decision. | HISTORICAL FR-26..30; old/V2 row authority Q-RF02-03, Q11 method Q-RF02-05. Do not convert UNKNOWN coverage to PASS. | RF-10-03 |
| Reporter/defect R08-R09 | Preserve each report and AI/field source; PM project-scoped candidate and approved label decisions. | PR-33A/34A confirmed; public projection/out-of-warranty Q-RF02-07. Prove no cross-project/PII leak or auto-merge. | RF-10-06 |
| Measurement/repair R10-R12 | PM assembles measurement batch; temporary safety action is distinct from repair proposal, execution and acceptance. | PR-32A/35A confirmed; Fast Track thresholds/dossier Q-RF02-04. Verify no implicit repair permission or closure. | RF-10-07 |
| File R13 | Admit image/video/SRT in pilot limits, resumable transfer, source provenance and verification state. | PR-38 confirmed; CG17 source conflict. Test size boundaries and durable metadata on isolated DB/storage. | RF-10-04 |
| Processing R14 | PM-triggered job and AI candidate provenance; BE handles adapter, external AI model stays separate. | PR-44 confirmed; two-stage receipt and active-attempt fencing Q-RF02-06. Replay/late-result checks. | RF-10-05 |
| Offline R15-R16 | Preserve pending evidence; recheck permission on sync; encrypted supervisor-authorized handover. | PR-37/42A/43A confirmed; wire/key/conflict behavior UNKNOWN. Test duplicate, stale permission, no evidence loss. | RF-10-08 |
| Messaging/reporting R17-R18 | Durable notification/audit; project reporting and hold-aware retention. | PR-40 target and PR-41A policy confirmed; KPI formulas/delete authority UNKNOWN. Dispatcher and restore proof required. | RF-10-09-A (notification/retention), RF-10-09-B (reporting) |

Historical FR-01..37 and BR-01..48 [retain their full source conditions and checks](historical-fr-br.md); [US-01..41](historical-us.md), [PF-01..08](historical-pf.md) and [NFR-01..14](historical-nfr.md) are also transferred in full. The [source crosswalk](../../planning/refactor/04-source-crosswalk.md) links each ID to its exact content, proposed primary module task and authority gate. Their unreviewed details are not promoted to TARGET_CONFIRMED; confirmed 32-44 overlaps are named separately per row.
