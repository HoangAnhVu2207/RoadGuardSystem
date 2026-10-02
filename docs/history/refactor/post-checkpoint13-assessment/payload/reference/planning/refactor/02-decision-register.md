# RF-02: Decision provenance and questions

## Identity and rule of use

- Branch `anh`; local HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; working tree dirty. Accepted requirements here do not activate a new runtime contract, schema, deletion process or agent workflow.
- Current owner request confirms the RF-00..RF-03 survey boundary. The owner also **reconfirmed the complete Appendix D interpretation of decisions 32-44 on 2026-09-30**. Those requirements are Accepted for planning and reconciliation, but this does not approve production code, active contract, schema, data or legacy-doc changes.
- Historical source artifact: `C:\Users\HoangAnhVu\Downloads\RoadGuard_Prompt_Hop_Nhat_Sua_Docs_Ke_Hoach_V2_3 (2).md`, SHA-256 `AB34ADA1B3F08450BEC912A42663531E2D7F3300CAADC0C1560C0A7FF64ED26E`. §B has a written D01-D28 summary; Appendix D cites a direct owner answer on 2026-09-28 as the code list `32A, 33A, 34A, 35A, 36A, 37 đồng ý, 38 đồng ý, 39A, 40 đồng ý, 41A, 42A, 43A, 44 tôi sẽ bàn giao cho bên khác` and then interprets each row. The source does **not** include the original numbered question/option wording. Checked sibling versions `(1)` and unsuffixed; neither supplied that wording. The interpretation table is recorded target evidence, not permission to invent extra terms.
- The original numbered questions/options dated 2026-09-28 remain unavailable. This provenance gap is recorded, but it no longer blocks use of the exact interpretation reconfirmed by the owner on 2026-09-30. No unstated detail is inferred from a code alone.
- `planning/V2/V2-3_DECISION_REGISTER.md` is a secondary crosswalk. Its `businessStatus` and `contractStatus` are preserved as historical claims; RF-02 does not edit them.

## Accepted decisions 32-44, reconfirmed 2026-09-30

Source of wording: Downloads prompt above, Appendix D, rows 32A-44. Source of current authority: owner's 2026-09-30 message explicitly reconfirming the full table, including its interpretations. **Status of every row: Accepted — người dùng tái xác nhận ngày 30/09/2026.** The historical table's pilot/design/target qualifiers remain part of each decision. Accepted requirement does not mean implemented, contract-approved, benchmarked or tested.

| Code | Full reconfirmed decision | Historical qualifier / implementation note |
|---|---|---|
| 32A | PM tự gom đợt đo; hệ thống nhắc rà soát hằng tuần; không tự tạo/giao task hoặc cấp quyền sửa. | Cập nhật BR-10 và reminder/batch. |
| 33A | Matching trong cùng dự án, ưu tiên segment giao và lân cận; dùng vị trí/sai số/lịch sử; thiếu GPS dùng scope+ảnh; PM tìm mở rộng trong dự án. | Candidate selection có version, quyền, không auto merge. |
| 34A | PM duyệt/từ chối nhãn trong dự án; chỉ nhãn đã duyệt được xuất cho training; AI không tự duyệt. | Bổ sung FR-36, US-10; P2-036/056 không còn chờ chọn role. |
| 35A | PM giao xử lý an toàn tạm trong phạm vi/phương án cho phép; thông báo Supervisor; không đóng lỗi/thay nghiệm thu. | P1-062 và flow, checklist, audit. |
| 36A | Web cookie bảo mật + session server; Android access/refresh token. | APPROVED_DESIGN; ghi compatibility delta và CSRF, không âm thầm phá client hiện có. |
| 37 | Web idle 30 phút/max 12 giờ; Android access 15 phút/refresh luân chuyển max 30 ngày từ login; OTP 10 phút/5 lần thử, resend >= 60 giây và <= 3 lần/15 phút. | APPROVED_PILOT_CONFIG; token hết hạn không xóa offline; kiểm hiện quyền khi reconnect. |
| 38 | Ảnh 20 MiB/video 8 GiB/SRT 10 MiB; dataset 32 GiB; upload chia phần/resume. | APPROVED_PILOT_CONFIG; không giới hạn tổng số video cả đời segment; báo rõ nếu file vượt. |
| 39A | Segment gợi ý 100 m, PM đổi được và giữ/gộp đoạn dư. | APPROVED_PILOT_CONFIG; không là tiêu chuẩn công trình/độ phủ video. |
| 40 | 50 user đồng thời; metadata server p95 <= 2 giây; RPO <= 15 phút; RTO <= 4 giờ. | APPROVED_TARGET; chưa benchmark/restore thì chưa VERIFIED. |
| 41A | Bằng chứng trong phạm vi lưu hết bảo hành + 5 năm; video nguồn đi cùng hồ sơ; log kỹ thuật 90 ngày, export tạm 30 ngày, backup 35 ngày; hold chặn xóa. | APPROVED_PROJECT_POLICY; không là tuyên bố luật chung; audit nghiệp vụ theo hồ sơ, không dọn như log thường. Khi thiếu căn cứ ngày hết bảo hành/nhiều nghĩa vụ: WAITING_RETENTION_BASIS, không suy ngày xóa từ ngày upload; chỉ xóa khi mọi nghĩa vụ hợp lệ đều cho phép và không hold. |
| 42A | Export bàn giao mã hóa khi còn truy cập thiết bị; mất thiết bị/khóa trước sync có thể không cứu được. | APPROVED_PILOT_SCOPE; chưa xây khóa khôi phục tổ chức. Supervisor cho phép, PM đúng dự án nhận, giữ actor gốc. |
| 43A | Offline có route/segment/destination/task đã tải; nền map offline chưa bắt buộc, thêm sau khi chọn provider/license. | Thiếu nền không làm mất tác nghiệp theo geometry. |
| 44 | Web, Android và AI giao bên khác. | APPROVED_OWNERSHIP; không gán xây FE/AI cho P1/P2. BE owner chịu adapter, contract, fixtures và tích hợp; đầu mối/ETA bên nhận chưa cung cấp. |

Appendix D explicitly excludes unasked values such as recall@5, evaluation dataset size, timeout/retry/heartbeat, orphan 7 days and response-cache 90 days. They remain proposed/unknown, not Accepted by this reconfirmation.

## Contract, code and test reconciliation by decision

This is a static crosswalk, not an API test result. `Rxx` refers to the module matrix in `02-requirements-traceability.md`; `CGxx` to `02-contract-gaps.md`. A missing in-repo action/test does not prove an external client is absent.

| Decision | Current contract/code/test comparison | Follow-up |
|---|---|---|
| 32A | R10: measurement entities and validation worker exist; no PM batch/weekly reminder action or test was identified in RF-01 endpoint/non-HTTP inventory. | Specify reminder event and PM batch contract; verify no automatic assignment/edit grant. |
| 33A | R09/CG08: defect and AI detection entities exist; no matching candidate/PM merge action or focused test found. | Define project/segment-scoped search and no-auto-merge tests. |
| 34A | R09/CG08: draft `reviewTrainingLabel`; no production PM label approval endpoint or export gate test found. | Trace approved-label provenance and export authorization. |
| 35A | R12/CG09: repair/safety action is draft-only; no production temporary-action/acceptance test found. | Separate safety assignment, Supervisor notice and acceptance states. |
| 36A | R02/CG02: current authentication registers JWT bearer, not web cookie; `/profile` and `/me` differ. | Plan versioned compatibility, CSRF and client test matrix. |
| 37 | R01/R02: `IdentityOnboardingOptions.cs:9-17` defaults match the OTP values; JWT lifetime options are configurable, but web idle/max-session behavior is not established. API host baseline failed before assertions. | Verify effective pilot config, rotation and expiry behavior in isolated HTTP/SQL. |
| 38 | R13/CG17: upload supports parts/resume, but `UploadService.cs:191` rejects above `int.MaxValue`, conflicting with 8 GiB video; dataset cap and per-media limits unverified. | Plan compatible size-type/schema transition and boundary tests. |
| 39A | R05/CG05: segment entities exist; route/segment publish actions and 100 m preview test not found in production inventory. | Version pilot default and PM remainder choice, verify geometry fixtures. |
| 40 | R18/CG14: dashboard/export and benchmark/restore evidence not found in RF-02; Accepted values are targets. | Define measurement environment and restore drill before VERIFIED claim. |
| 41A | R18/CG14: `StoredFile.RetentionUntil` exists; `RoadGuardDbContext.cs:270` bars generic file deletion; no retention/legal-hold action or purge test found. | Model warranty-end/hold/backups, WAITING_RETENTION_BASIS and dry-run deletion first. |
| 42A | R15/CG12: no production `syncOperations`/encrypted handover contract or rescue test found. | Negotiate Android wire/key ownership and actor-preserving audit. |
| 43A | R15/CG12: route/segment/task downloads and offline basemap are outside the inspected backend runtime; sync action absent. | Obtain Android consumer evidence and define geometry download/version contract. |
| 44 | R14/CG11: BE processing callback/outbox code exists; no external AI provider/deployment contract or named owner/ETA verified. | Assign cross-team contract/fixture contacts; do not infer FE/AI completion from BE tests. |

## Recorded product directions versus unresolved executable detail

| Cluster | Recorded source and direction | What can be used now | What remains unknown |
|---|---|---|---|
| Identity | §B D25: email/password and one-time Reporter email OTP; reconfirmed 36A/37: web cookie + server session design and pilot lifetimes | Model auth actors/session, compare bearer runtime to Accepted target transport | Compatibility/migration for existing bearer web clients, CSRF/CORS, provider/abuse tuning |
| Project/survey/GIS | §B D18/D20..D22 and reconfirmed 39A/43A: ordered route input, segment versioning, PM/Supervisor split, pilot 100 m suggestion, offline downloaded geometry | Keep road/survey source facts separate and draft test fixtures | Real GPX/JSON CRS/order, remainder rule, non-pilot length defaults and outside-repo clients |
| Reporter/defect | §B D10/D12/D14/D19; reconfirmed 33A/34A: project-scoped candidate matching and PM training-label approval | Design privacy/actor trace and candidate-versus-decision model | Person-data projection, exact report-defect mapping, matching algorithm parameters |
| Measurement/Fast Track/repair | §B D02..D11; Appendix E: measurement-only batch, framework authority, BEFORE, handover, reopen, traffic release and reusable templates | Model separate states and fail-closed missing evidence | Method/material dossiers and thresholds, workflow/public contract, legal/engineering approval, relation to generic PM repair summary |
| AI/upload/offline | §B D12/D13/D15/D17; Appendix B/F and reconfirmed 38/42A: PM-triggered two-stage pipeline, source video/SRT, unknown coverage, resumable uploads, encrypted rescue | Keep manifest/attempt/source provenance and no-auto-trigger distinction; plan CG17 size migration | Original bytes, provider contract, retry/heartbeat/timeouts, active-attempt receipt protocol, rescue keys |
| Reporting/retention | Reconfirmed 40/41A: pilot performance/restore targets and project retention policy | Treat as Accepted target/policy for design; no live deletion | Warranty-end basis per record, legal-hold authority, deletion/backups/restore mechanics, KPI formula, measured SLO proof |

## Blocking questions for owner/source review

These questions affect public behavior, authorization, data preservation or irreversible policy. Options are decision frames for review, **not** inferred historical alternatives. The recommendation is a safe interim design direction and is not an Accepted outcome.

| ID / affected tasks | Question and needed source | Options; recommendation | Impact if unanswered |
|---|---|---|---|
| Q-RF02-01 / resolved 2026-09-30 | Owner explicitly reconfirmed Appendix D's full interpretation of 32-44 in this chat. | Accepted source for those exact rows; original 2026-09-28 questions/options remain unavailable as historical context. | **Semantic blocker removed.** Contract/code/test reconciliation proceeds; no runtime or contract change is approved by this resolution. |
| Q-RF02-02 / auth/profile client migration | For current `/profile` and `/me` and bearer web clients, what is the accepted forward contract and deprecation/version window for cookie web auth? | A: keep both routes and bearer while adding versioned cookie web flow; B: breaking switch at a coordinated release; C: retain bearer web. **Recommend A** for data/client compatibility. | Blocks auth transport ADR, CSRF/API design, PII field contract and route retirement. Existing auth implementation can still be characterized. |
| Q-RF02-03 / survey old-V2 data | Which plan/request workflow is authoritative for new writes, and how are existing old and V2 rows interpreted and exposed? | A: coexist with explicit row/contract discriminator and migration plan; B: convert old rows then deprecate old route; C: keep both as distinct business flows. **Recommend A for discovery**, not permanent design. | Blocks survey contract/schema consolidation and any route removal; current dirty slice can be reviewed independently. |
| Q-RF02-04 / Fast Track activation | Who supplies and approves method/material dossiers, numeric eligibility/curing/traffic-release thresholds and their applicability for a pilot project? | A: approved project documents/profile loaded by authorized roles; B: templates remain TEST_ONLY until supplied. **Recommend B until A is evidenced.** | Blocks production policy activation and acceptance behavior; does not block typed evaluator/template design. No numeric threshold is set here. |
| Q-RF02-05 / Q11 baseline/coverage | What evidence and review authority define aircraft-position, data-quality and observed-coverage assessment separately per segment/band, including UNKNOWN and override rules? | A: documented versioned method with project calibration; B: expose UNKNOWN pending evidence. **Recommend B** until method is approved. | Blocks `confirmBaseline`, automated PASS/FAIL, migration/coverage schema; source-file admission can be examined independently. |
| Q-RF02-06 / AI integration | Is the proposed two-stage FastAPI manifest, active-attempt fencing and completion receipt in Appendix B the intended external contract, and who operates the dispatch consumer? | A: accept versioned Appendix B after provider review; B: use a separately negotiated provider contract. **Recommend A as review baseline**, not auto-accept. | Blocks AI service OpenAPI freeze, worker/receipt delivery and late-result resolution. Existing callback can be characterized in isolated SQL. |
| Q-RF02-07 / reporter privacy and repair | Confirm how a report outside warranty routes to project/responsibility, which Reporter may see each defect/public image, and whether Fast Track method/material policy is distinct from PM's generic repair proposal. | A: per-report ownership mapping and separate policy/proposal aggregates; B: another owner-defined model. **Recommend A** consistent with recorded D10/D19 and Appendix E. | Blocks case/defect public projection, authorization, repair schema and non-leak tests. |
| Q-RF02-08 / retention/deletion mechanics | 41A is Accepted project policy. Identify warranty-end basis for each record, hold authority, backup handling, deletion approval and existing data populations. | Preserve WAITING_RETENTION_BASIS where end date/obligations are unknown; design authorization and backup policy before any deletion. | Blocks destructive retention worker/schema/backfill; does not reopen the accepted durations. Audit/report read work may continue. |

No question asks for a reversible parsing/tooling choice. Q-RF02-01 is resolved by the owner's 2026-09-30 confirmation. Q-RF02-04/05 and the execution details of Q-RF02-08 remain open; elapsed time is not approval for them.

## Technical decisions made within RF-02

- Use parsed YAML path operations (133) instead of counting raw `operationId:` strings (138) for contract inventory, because YAML anchors are not endpoint operations.
- Keep canonical OpenAPI, FE snapshot and lock untouched; record the actual hashes and mismatch.
- Preserve both `/profile`/`/me` and both survey route families in the map pending consumer/data decisions.
- Do not run API/SQL suites or live migrations in this documentation task; RF-00 API fixture is not safely isolated for general reruns. These choices are reversible and do not change behavior.
