# Rà soát nghiệp vụ, code và tài liệu RoadGuard — 30/10/2026

**Kết luận:** hai bộ docs chính thức ngày 27/10 khớp snapshot API/schema/source hiện tại qua kiểm tra tự động. Dự án vẫn còn lỗi truy cập ảnh tái hiện được ở tầng service, matching chưa đủ yêu cầu, nhánh Case FIELD/DRONE bị chặn có chủ đích, khoảng trống phục hồi Reporter Pending và các cổng nghiệm thu ngoài BE. Một số tài liệu trung tâm gắn ACTIVE đang mô tả code cũ. Không thể kết luận toàn bộ nghiệp vụ đúng hoặc sẵn sàng triển khai chỉ từ các kết quả PASS dưới đây.

Đây là audit, không phải gói sửa code. Không thay đổi implementation, schema, contract, DB hoặc các tài liệu đã dirty. Mức ưu tiên dưới đây là đề xuất thứ tự xử lý; không phải owner đã giao thêm scope.

## 1. Baseline, nguồn và giới hạn bằng chứng

- Workspace `D:\Project BE\RoadGuardSystem`, branch `huy-review`; base = HEAD = `01cc4725b178199215b02f4ddf7093491a16f2f7` lúc kiểm tra. Không fetch/commit/push/merge.
- Đầu phiên có **127 file tracked modified, 10 untracked**; đây là thay đổi có sẵn, chủ yếu docs. Kiểm tra docs dùng working tree này, không đồng nghĩa clean clone/HEAD đã chứa tất cả docs mới.
- Authority: [AGENTS](D:/Project%20BE/RoadGuardSystem/AGENTS.md), [HUY-FINAL-INTEGRATION](D:/Project%20BE/RoadGuardSystem/planning/development/HUY-FINAL-INTEGRATION.md:637), [confirmed decisions 32–44](D:/Project%20BE/RoadGuardSystem/docs/product/confirmed-decisions.md:7). R01–R30, clocks và **persistent login** trong spec mới supersede phần xung đột cũ. Không lấy timeout 36A/37 cũ để yêu cầu khôi phục ordinary idle/absolute logout.
- Đọc code/controller/service/repository/domain và test liên quan; đối chiếu docs chính thức, product/backend, contracts, module map, planning và FE handoff. Không coi mỗi API đã được chạy HTTP/SQL chỉ vì có trong inventory.
- Nhãn: `CURRENT_VERIFIED` = source/check thực sự quan sát; `TARGET_CONFIRMED` = quyết định có nguồn; `PROPOSED` = đề xuất/rủi ro; `UNKNOWN` = thiếu quyết định/bằng chứng; `HISTORICAL` = checkpoint cũ. Nhãn source không chứng nhận runtime hoặc business acceptance.
- Evidence gốc mới: [thư mục kiểm tra](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030). Source probe, TRX, logs, API/Swagger/EF capture và baseline ở đây. Không sử dụng PASS lịch sử thay cho test mới.
- **Local-only:** các link tuyệt đối Windows trong báo cáo phục vụ người dùng Codex desktop trên máy này; không phải link GitHub/clean-clone portable. Evidence ngoài repo là private/local-only, không được coi là artifact đã phát hành. CSV có đường dẫn repository-relative ở cột file và số dòng ở source_line để người nhận định vị; cần đóng gói/chuyển link riêng nếu xuất báo cáo ra ngoài workstation.
- Không kiểm thử shared DB, không chạy migration/seed trên môi trường chung. Capture API/EF vô hiệu hosted services và initialization, không thực hiện truy vấn DB. Probe ảnh dùng fake repository/storage; không gọi storage thật.

## 2. Các phát hiện cần sửa hoặc chốt

### A01 — P1: quyền/metadata có thể đổi trong lúc mở ảnh Reporter

**CURRENT_VERIFIED ở service với delayed storage; HTTP/SQL/storage thật chưa tái hiện.** [ReporterEvidenceService](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Files/ReporterEvidenceService.cs:172) đọc metadata, kiểm active Reporter/ownership/status, rồi `await OpenFileAsync` và trả stream ngay. Không đọc lại authority hay pin metadata sau thời gian chờ. Nhánh [DownloadPublicationAsync](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Files/ReporterEvidenceService.cs:158) có cùng cấu trúc, nhưng chưa được probe độc lập.

Probe độc lập ngoài repo trì hoãn OpenFileAsync và thay trạng thái trước khi cho mở xong:

| Trường hợp | Kết quả quan sát |
|---|---|
| Reporter đã mất quyền trước request | NotFound; storage không mở |
| Mất active status trong lúc storage đang mở | Success; trả stream đọc được; chỉ kiểm quyền một lần |
| File đổi version v1→v2 trong lúc mở | Success với metadata cũ; chỉ đọc metadata một lần |

**Tác động:** cửa sổ trả nội dung private dựa trên quyền hoặc pin đã cũ. Không suy ra đã có khai thác thực tế hoặc mọi thay đổi version đều phải cấm download. Quy tắc pin nào cần giữ phải theo đúng loại evidence/publication hiện tại. [UploadService](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Files/UploadService.cs:198) đã có post-open guard cho FIELD/AI_FRAME, không phải tất cả purpose.

**Đề xuất Huy/security:** re-resolve current quyền và trạng thái/pin thích hợp sau open, dispose stream nếu từ chối; thêm delayed-open regression cho owner và publication, gồm account revoke, membership/publication withdrawal và metadata đổi. Không đổi public error/wire tùy tiện. Evidence: [probe log](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/reporter-race-probe.log), [probe source](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/probe/Program.cs). Exit0 của probe nghĩa là **quan sát được rủi ro**, không phải sản phẩm an toàn.

### A02 — P1: matching chưa có đủ facts vị trí/sai số/ảnh theo 33A

**CURRENT_VERIFIED source limitation; TARGET_CONFIRMED 33A.** [CandidateMatchingService](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Defects/CandidateMatchingService.cs:88) luôn đưa distance=null và metricGpsAvailable=false; DTO image score cũng null. Phần hiện có là scope segment/lân cận/lịch sử và tìm mở rộng trong project, không phải matching đầy đủ GPS/accuracy/image. FIELD_OBSERVATION hiện trả source_not_ready tại dòng30; [CandidateDecisionService](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Defects/CandidateDecisionService.cs:57) có gate tương ứng.

**Tác động:** PM không có khoảng cách/độ tin cậy matching như requirement; không nên mô tả engine đã hoàn chỉnh. **Đề xuất Anh + owner facts:** xác định nguồn tọa độ/CRS/accuracy và semantics ảnh đã được chấp nhận; nối facts, kiểm thử có/thiếu GPS, project isolation, scope mở rộng, stale geometry/source/cursor. Không tự đặt threshold hoặc tuyên bố recall/accuracy.

### A03 — P2: Case FIELD/DRONE chưa kết luận được khi thiếu completion producer thật

**CURRENT_VERIFIED gate; TARGET_CONFIRMED boundary R10.** [CaseWorkflowRepository](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Cases/CaseWorkflowRepository.cs:149) trả409 source_not_ready nếu kết quả khác NeedsEvidence và verification method không phải ExistingEvidence. [R10](D:/Project%20BE/RoadGuardSystem/planning/development/HUY-FINAL-INTEGRATION.md:689) yêu cầu task pre-Defect riêng và không giả Defect/Survey.

ExistingEvidence/NeedsEvidence/publication và FIELD cho Defect hợp lệ là các nhánh riêng đã có; không gọi toàn bộ FIELD bị hỏng. **Đề xuất owner + assigned producer:** giao thiết kế/contract cho pre-Defect inspection và completion evidence, sau đó test Case→task→completion→conclusion→publication. Đây là tính năng còn giới hạn được công khai, không phải lý do bỏ guard hoặc tự thêm schema/route trong audit.

### A04 — P1: Reporter Pending mất intent chưa có đường tự phục hồi rõ ràng

**CURRENT_VERIFIED source behavior; recovery policy UNKNOWN.** [IdentityOnboardingRepository](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Identity/IdentityOnboardingRepository.cs:63) tạo intent UserId=null khi email đã tồn tại, kể cả Pending. [Service](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Authentication/IdentityOnboardingService.cs:110) vẫn gửi OTP, nhưng verify tại dòng134–138 trả NotFound cho intent không có UserId; resend vẫn có thể gửi lại. [Login](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs:73) và [password recovery](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.PasswordRecovery.cs:19) chỉ nhận account Active.

**Kịch bản:** tạo Reporter mới → account Pending → mất intent cũ/không còn sử dụng được intent → đăng ký lại cùng email nhận intent không verify được; login và quên mật khẩu không kích hoạt Pending. Source test duplicate-email có chủ đích chống dò tài khoản; đăng ký email của Active user không nhất thiết phải thành công.

**Đề xuất owner/identity:** chốt UX và giao thức phục hồi Pending/expired intent có chứng minh sở hữu email, không leak sự tồn tại tài khoản, giữ rate limit/replay/password safety. Sau đó thêm HTTP+SQL test new/Pending/Active, intent mất/hết hạn, OTP sai/đúng/resend/replay và concurrent recovery. Không sửa bằng cách liên kết mọi email trùng hoặc tự cấp token cho intent giả. Chưa gọi đây là khai thác auth hoặc kết luận không còn bất kỳ đường hỗ trợ thủ công nào.

**Điểm nối delivery cần kiểm:** intent đã persist trước khi gửi OTP. Nếu sender trả false, service trả DeliveryUnavailable kèm intent nhưng controller ánh xạ503 ProblemDetails không chứa intent; retry cùng key có thể lấy lại intent qua IdempotentReplay202 nhưng không gửi lại OTP, cần gọi resend. Resend không nhận intent đã hết hạn ([repository](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Identity/IdentityOnboardingRepository.cs:120)). Vì vậy không kết luận mọi503 đều gây kẹt vĩnh viễn; cần test client giữ key, retry lấy intent, resend đúng cooldown và xử lý expiry. Source behavior đã đọc; chưa freshHTTP/SMTP reproduction.

### A05 — P2: GPS_CAPTURE chưa thể tạo proof vị trí đủ điều kiện

**CURRENT_VERIFIED source gate.** [FieldInspectionWorkflowRepository.Intake](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.Intake.cs:140) kiểm hình thức GPS rồi luôn thêm GPS_SOURCE_BINDING_UNAVAILABLE. POSITION_CHECKLIST có nhánh segment/slab/chainage tại dòng150–173, nên không phải mọi location proof bị chặn.

**Tác động:** gửi tọa độ GPS đúng range chưa làm tác vụ đủ điều kiện vị trí; đặc biệt ảnh hưởng tự sửa Fast Track. **Đề xuất owner/GIS + implementer:** cung cấp source binding/CRS/operation/tolerance/control points được duyệt; kiểm independently trên dữ liệu thật. Không bỏ gate, dùng note làm verified location hoặc tự đoán ngưỡng để xanh test.

### D01 — P2: tài liệu ACTIVE và module map còn kết luận sai với code hiện tại

**CURRENT_VERIFIED docs drift.** Các điểm cần sửa trong một gói docs/guidance được giao, không mở lại feature đã có:

| Tài liệu hiện tại | Nội dung cũ | Source hiện tại |
|---|---|---|
| [product/workflows](D:/Project%20BE/RoadGuardSystem/docs/product/workflows.md:9) | late legacy callback còn gap; FT-final chưa producer (dòng12,16) | A1 fence/replay identity và FT final outbox đã có |
| [product/data-and-quality](D:/Project%20BE/RoadGuardSystem/docs/product/data-and-quality.md:11) | callback chưa latest-attempt fence | ProcessingV2PersistenceService128–212 |
| [backend README](D:/Project%20BE/RoadGuardSystem/docs/backend/README.md:12) | callback/FT thiếu; inventory cũ | fresh259 controller operations; FT producer hiện có |
| [module map](D:/Project%20BE/RoadGuardSystem/.agents/modules/README.md:15) | Reporter/Case/Defect chưa controller, offline chưa production sync, repair routing RF cũ | controllers/source hiện tại và module HTTP tests đã tồn tại |
| backend data index/dictionary/triggers | metadata/SQL checkpoint cũ được dẫn từ trung tâm | phải ghi rõ lịch sử; EF mới không thay thế SQL deployed audit |

Source đối chiếu: [A1 callback](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs:128), [FT final producer](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Producing.cs:74). Cập nhật nhãn/baseline/current disposition và link tới integration mới; vẫn giữ live AI/delivery/native acceptance chưa xác minh. Không lấy code mới để tự tuyên bố requirement đã được owner nghiệm thu.

### D02 — P2: snapshot V2/FE và lock không khớp

**CURRENT_VERIFIED check FAIL; contract adoption chưa được chứng minh.** `python docs/diagram/V2/09_Frontend/contracts/check_contracts.py` exit1, CONTRACT_LOCK_MISMATCH. Đây là draft/consumer artifact, không phải kết luận production Swagger sai. **Đề xuất contract/FE owner:** review delta source/snapshot/consumer và adoption trước khi cập nhật lock; không regenerate lock chỉ để PASS. [Log](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/v2-contract.log).

### D03 — P2: liên kết handoff/evidence thiếu và một file không đọc được UTF-8

**CURRENT_VERIFIED ở checkout này.** Scanner ban đầu ghi135 finding link và1 lỗi encoding. Sau phân loại lại từng target: **134 tham chiếu có đích thiếu, 1 parser false positive, 1 lỗi encoding**. Không phải134 target duy nhất hoặc134 lỗi nghiệp vụ. [CSV đầy đủ từng tham chiếu và classification](D:/Project%20BE/RoadGuardSystem/docs/audit/BUSINESS-DOCS-AUDIT-20261030-links.csv), [raw summary](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/active-link-summary.json), [classification sau review](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/link-classification.json). CSV giữ false positive với nhãn riêng để không che lịch sử scanner.

| Phạm vi | File quét | Local links | Lỗi link | Lỗi encoding |
|---|---:|---:|---:|---:|
| docs/product | 9 | 28 | 0 | 0 |
| docs/backend | 11 | 239 | 40 | 0 |
| contracts | 5 | 14 | 0 | 0 |
| planning/development | 19 | 73 | 62 | 0 |
| .agents/modules | 1 | 1 | 0 | 0 |
| deploy/fe-handoff | 56 | 1194 | 32 | 1 |

40 tham chiếu backend thuộc current-data-dictionary/current-triggers; 62 planning thuộc evidence/rg-ci-01/schema-source-audit. Finding audited-data-model.md → max là SQL inline code `CONVERT([varchar](max),...)`, **không phải link**. Nhiều reference là lịch sử, không phải134 lỗi nghiệp vụ đang chạy. Cột Local links giữ số candidate do scanner đếm ban đầu, gồm false positive đó; cột Lỗi link đã trừ false positive. 42 external links trong scan bổ sung không được kiểm tra mạng.

Handoff có ví dụ đang dẫn file không tồn tại: [README-FOR-FE](D:/Project%20BE/RoadGuardSystem/deploy/fe-handoff/README-FOR-FE.md:9) → FE-STARTER-CANDIDATE3-STATUS.md; dòng15 → review-evidence/fe-preview-closeout/FE-PREVIEW-STATUS.md; các tài liệu khác dẫn MASTER-HANDOFF/JSON dimension/source mapping không có trong checkout. `deploy/fe-handoff/OWNER-DECISIONS-FINAL.md` lỗi decode UTF-8 byte0x97, offset2583. **Đề xuất docs owner:** xác nhận encoding rồi chuyển đổi bảo toàn nội dung, cung cấp artifact/link portable hoặc ghi trạng thái unavailable; giữ lịch sử với nhãn thích hợp. Không dựng evidence thay thế.

### D04 — P2: docs mới ở working tree chưa đồng nghĩa artifact đã phát hành

Baseline có10 untracked và127 modified trước audit; gồm tài liệu/task/tools phục vụ sync mới. Cần review/commit đúng gói và đóng gói evidence cần cho người nhận trước khi gọi docs ở clean clone là mới nhất. Cổng H02–H05 còn OPEN, FULL_CONTRACT chưa được chứng nhận; không tự suy tiêu chí từ tên cổng hoặc nguồn bàn giao thiếu file. [Current integration](D:/Project%20BE/RoadGuardSystem/docs/RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md:9).

## 3. Những nhận định cũ không được đưa vào danh sách bug hiện tại

| Điểm cũ | Kết quả tái đối chiếu lần này |
|---|---|
| G01 matching thiếu facts | Giữ, A02; chưa hoàn thiện |
| G02 callback thiếu current/latest fence | Đã có A1 identity + job lock/latest attempt/replay guard; không mở lại chỉ từ bảng lịch sử. Live provider vẫn chưa verified |
| G03 Reporter delayed-open chỉ là rủi ro | Nâng thành tái hiện service A01; chưa gọi HTTP exploit |
| G04 FT-final không producer | Producer fast_track_confirmed đã có; B2 recipient hiện có; delivery thật chưa verified |
| G05 pre-Defect FIELD/DRONE | Giữ localized gate A03 |
| G06 V2 lock | Fresh FAIL D02 |
| G07 Swagger test excerpts cũ | ManualSwaggerContractTests nằm trong111 API PASS; không giữ FAIL cũ làm freshFAIL |
| G08/G09 links/module guidance | D03/D01, phân biệt snapshot lịch sử |
| G10 Survey SupplementRequired bị chặn submit | **Không phải confirmed defect.** Parent đã có supplementary child thì bị chặn đúng flow; child nhận assignment và nộp dataset riêng |
| Weekly catch-up thiếu | [Calendar](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Calendar.cs:40) đã có recovery period/digest/recipient/outbox; chưa freshSQL acceptance |
| LD07 chưa producer coverage | [RoadCoverageRepository](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Repairs/RoadCoverageRepository.cs:102) đã có mapping producer; authoritative source acceptance vẫn riêng |
| P5-G02 Crew thiếu repair bootstrap | API080 repairContext đã có [CrewRepairContext](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.CrewRepairContext.cs); native rendering chưa verified |

Survey evidence: [child creation](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs:313), [parent admission](D:/Project%20BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs:53), [acceptance test source](D:/Project%20BE/RoadGuardSystem/tests/RoadGuardSystem.ApiTests/Surveys/Anh01SurveyFlowTests.cs:262). Test tồn tại không có nghĩa bài SQL đó đã được chạy trong audit này. ChatGPT đã kiểm tra lại source và rút kết luận G10 ban đầu.

**PROPOSED / investigation only:** nhánh supplement không kết thúc assignment parent tường minh; ListMyTasksAsync lọc EndedAt=null, không lọc task status. Cần kiểm UX có chủ đích hiển thị parent lịch sử hay chỉ task còn thao tác; chưa đủ căn cứ gọi bug. Cũng chưa tái hiện khác biệt anti-enumeration do concurrent đăng ký/unique index/Conflict; không ghi thành security defect đã xác nhận.

Bảng G01–G10/E01–E05 trong docs chính thức có heading **Historical checkpoint** và cảnh báo recheck SHA. Không xóa lịch sử hoặc gọi toàn bộ bảng đó là kết luận hiện tại. Nên bổ sung bảng current disposition rõ ràng để người đọc không giao sửa lại tính năng đã có.

## 4. Bao phủ nghiệp vụ và các điểm mở còn lại

Ma trận này là đọc nguồn và phân loại acceptance, không phải PASS từng vertical slice. R01–R30 không cần xin owner duyệt lại.

| Requirement/luồng | Phần hiện có/đối chiếu | Điểm mở và điều kiện đóng |
|---|---|---|
| Identity, 36A/37 + persistent-login mới | Web cookie/session/CSRF, Android token/refresh, OTP; persistent login supersede timeout thường cũ | A04; actual browser/Android renew, revoke, retry401 giữ key/body/version; proxy/TLS/keyring/replica restart chưa thử |
| Reporter→Case→publication, R10/R29 | private evidence, intake/supplement, triage/link/split, ExistingEvidence, public/recipient projections riêng | A01/A03; privacy/revocation SQL/HTTP và pre-Defect completion |
| Survey→dataset→AI→candidate→PM label, 33A/34A/44 | supplementary child, immutable dataset, explicit PM label/decision, A1 callback fence | A02; live FastAPI protocol/model/video/SRT/provenance/late callback; không lấy mock làm accuracy |
| FIELD measurement, R04/R06/R10–R12/R22 | task/assignment/session, physical measurement, missing khác known0, location/version/checklist | A05; authoritative calibrated facts; thiếu GPS có checklist theo thiết kế, không mặc định cấm lưu/nộp |
| NORMAL repair, R02/R12–R17 | PM proposal→Supervisor approval→Crew→PM review→Supervisor final; immutable submissions/obligations | Chưa freshSQL journey; rework/reassign/race/rollback và exact BEFORE/AFTER provenance cần regression môi trường |
| Fast Track, R03–R09/R20 | PM policy/version/pin, actual eligibility, assigned Crew, PM final, Supervisor information; ND01 original-window có implementation/evidence lịch sử | GPS/coverage facts A05; signed original24h không reset; live offline conflict/revoke; không thêm Supervisor approval hoặc maintenance module |
| Correction/recurrence/lifecycle, R15–R19/R26 | history/effective decisions/obligation/linkage/read policies và explicit command boundaries theo spec | Mọi activation phải theo adopted actor/procedure; không suy PM/Supervisor quyền chỉ từ role. Kiểm current commands thay vì dùng pending paragraph cũ. Cross-period public statistics LD08b còn quyết định công thức |
| Road/GIS/segment, 39A/R22–R25 | geometry/version/length identity, coverage mapping producer | Official CRS, operation/tolerance/control, real handover/coverage/warranty sources; TEST_ONLY/sampleOnly không executable authority; không ép length/stretch tọa độ |
| Notifications/clocks, 32A/35A/R27/R28/R30 | event/outbox, source/recipient authority, FT final info, weekly catch-up; read khác business ACK | Real email/FCM delivery, topology/restart; clock origins và overdue history phải giữ; thiếu freshSQL clock regression |
| Offline/handover, 42A/43A/R08/R09/R20–R22/R29 | signed finite envelope/receipts, original actor, current rights, scoped grants và dedup | Android kill/reboot/update/storage-full/camera/network, durable ACK loss, decrypt/import/key custody/backup chưa test thiết bị; không xóa local trước durable ACK |
| Upload, 38 | multipart/resume/checksum và giới hạn contract có trong source/docs | API030 real MP4 Content-Type/runtime parity NOT_VERIFIED; MinIO thật,8GiB/32GiB,resume/checksum chưa chạy |
| Retention/export, 41A | hold/basis checks và immutable exports, thiếu căn cứ thì WAITING_RETENTION_BASIS | Physical purge chưa activated không phải yêu cầu CRUD thiếu. Cần đủ mọi nghĩa vụ/hold và approved data policy trước xóa; export runtime/client/large-file acceptance |
| Operations, 40 | targets50 concurrent,p95≤2s,RPO≤15min,RTO≤4h đã chốt | Chưa load benchmark/consistent restore SQL+objects+keys, chưa chứng nhận deployment/release |
| FE/mobile/AI ownership, 44 | BE adapter/contracts/fixtures là trách nhiệm BE; external clients bên khác | Không kiểm actual FE workspace/browser165 consumers/native/liveAI trong audit; role adapter UNKNOWN, auth mock presence không chứng minh reachable |

Nguồn cổng còn mở: [current integration](D:/Project%20BE/RoadGuardSystem/docs/RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md), [HUY spec](D:/Project%20BE/RoadGuardSystem/planning/development/HUY-FINAL-INTEGRATION.md), [confirmed decisions](D:/Project%20BE/RoadGuardSystem/docs/product/confirmed-decisions.md). E01–E05 vẫn là acceptance gates, không phải năm lỗi BE tái hiện. Knowledge Space có **SECURITY_BLOCKED / sharing NOT_GRANTED** vì ACL BUILTIN\Users Modify theo tài liệu hiện tại; audit không kiểm lại ACL và không cấp chia sẻ.

## 5. Kiểm thử mới thực sự chạy

| Check | Kết quả mới | Giới hạn/evidence |
|---|---|---|
| Full UnitTests lần đầu | 1112 PASS /5 FAIL /0 SKIP | Cả5 lỗi export thiếu font Unicode test; [log](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/unit.log) |
| Full UnitTests sau cấu hình font local | **1117 PASS /0 FAIL /0 SKIP** | RG_TEST_UNICODE_FONT=C:\Windows\Fonts\arial.ttf; không sửa source/copy font vào repo; [TRX](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/unit-configured/unit-configured.trx) |
| API subset đầu có Sandbox SQL helper | 111 PASS /18 FAIL | 18 DockerUnavailableException, không tính là business assertions FAIL; [log](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/api-disconnected.log) |
| API safe subset không SQL | **111 PASS /0 FAIL /0 SKIP** | platform/problem/auth/OpenAPI/handoff/contracts/config/consumer/source checks; không fullAPI hoặc SQL-E2E; [TRX](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/api-safe/api-safe.trx) |
| Rf1005CallbackCharacterizationTests | 0 PASS /7 infrastructure FAIL | SQL override có sẵn không connect, fixture từ chối fallback; không suy callback broken. [Log](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/sql-callback.log) |
| Cùng SQL suite sau bỏ override chỉ trong process | 0 PASS /7 infrastructure FAIL | Testcontainers không có Docker engine; không fallback shared/localSQL. [Log](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/sql-callback-container.log) |
| Reporter delayed-open probe | 3 scenarios; observation reproduced | A01, mocked service/storage; không tính3 productPASS |
| Docs tools unittest | **17 PASS** | [log](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/docs-unit.log) |
| validate_phase5.py | **PASS, errors=[]** | counts/maps/schema/line links và offline invariants; không test runtime business |
| Fresh production APIExplorer/Swagger/EF capture | **exit0**, DBqueries0 | 50controllers/259ops/229paths/384schemas; EF200entities/195tables/1980props/1975columns |
| verify_current_docs.py với capture mới | **PASS**,259ops/1534sourcehashes | exact current Swagger/source pins match; lần đầu truyền nhầm capture directory gây FileNotFound, đã chạy lại đúng bin/Debug/net8.0; không che lỗi thao tác |
| HHWS official links | **PASS**,92md/6049local links | 2external unchecked |
| RoadGuard Backend official links | **PASS**,60md/8470local links | không chứng nhận nội dung nghiệp vụ |
| V2 draft lock | **FAIL exit1** | D02, CONTRACT_LOCK_MISMATCH |
| Supplemental link/encoding audit | **134 missing-target references +1encoding; 1false positive đã tách** | D03/CSV, gồm nhiều references lịch sử; không trộn với official linkPASS |

Commands: `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj`; configured rerun `--no-build`; API project `dotnet test ... --no-build --filter` với class allowlist; SQL filter `FullyQualifiedName~Rf1005CallbackCharacterizationTests`; `python -m unittest discover -s docs/HoangHaiWarrantySystem-Official/tools -p test_*.py`; `validate_phase5.py`; `check_links.py` (mỗi bộ docs); `verify_current_docs.py --capture <fresh capture output>`; V2 `check_contracts.py`. TRX lưu tên từng test để truy chính xác allowlist/subset, không cộng hai lần111API đã chạy lại.

Inventory từng test thực thi: [executed-tests.csv](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/executed-tests.csv), [TRX counters](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/trx-summary.json); đây cũng là private/local-only evidence.

Các kết quả lịch sử Unit1117/API1533/SQL807 trong integration docs giữ nguyên nhãn HISTORICAL, không được nâng thành lần chạy mới. SQL, actual deployed HTTP, real storage/mail/FCM, actual browser/Android/AI, performance và restore **chưa được xác minh** trong audit này. Không có cơ sở đảm bảo đã tìm hết mọi bug ở những bề mặt chưa chạy.

## 6. Tự review và phản biện bên ngoài

- **Self-review 1 — evidence/false positives:** phân biệt source/mock/HTTP/SQL/external; rút Survey G10, callback/FT producer/weekly/coverage absence cũ; không coi duplicate-email anti-enumeration là auth exploit; kiểm persistent-login supersession. Những check lỗi môi trường/font giữ nguyên log và mô tả nguyên nhân.
- **Self-review 2 — preservation/traceability:** kiểm cuối source pins259/1534 errors=[], các link file/line trong report tồn tại, CSV136records phân loại134missing/1false-positive/1encoding. Giữ137 baseline status entries; chỉ thêm2file audit. SHA256 file reserved G02-G06-OWNER-DECISIONS.md vẫn `4f8b0c96401233b758c5fda350bc64571ed715d8390311e2fba2bc398e205b9b`. [Kết quả kiểm](D:/RoadGuard-Review-Evidence/BUSINESS-DOCS-AUDIT-20261030/self-review.json).
- **ChatGPT independent review:** dùng plugin Codex with ChatGPT · RoadGuardSystem trong [chat dự án](https://chatgpt.com/g/g-p-6ad1a06c91048191b5effb313db74b6c-roadguardsystem/c/6ae3c94d-6418-83ec-8c9d-b20942feb287), Thinking High. INIT lập hướng; vòng1 đọc source độc lập, xác nhận Pending recovery/docs ACTIVE drift và rút Survey defect. Vòng2 đọc output Unit1117PASS độc lập; batch còn lại bị truncated nên không independently chứng nhận toàn bộ API/docs/probe raw logs. Review yêu cầu phân loại false positive và làm rõ tính local-only; đã xử lý. **Vòng3: DONE / INDEPENDENT_PASS / LOCAL_READ_ONLY_AUDIT_ACCEPTED**, kiểm CSV và execution outputs1342/1343, Git scope và evidence limits. Chỉ là chấp nhận bàn giao audit local; bugs chưa sửa, FULL_CONTRACT/FE handoff/release chưa certified. Không mở thêm vòng review cho cập nhật ghi nhận verdict này.

## 7. Thứ tự giao sửa đề xuất

1. Huy/security nhận A01, thêm regression và sửa bounded trong service; identity owner chốt A04 trước dependent implementation.
2. Anh/owner facts giải quyết A02/A05; owner giao contract/producer đúng scope cho A03.
3. Docs/guidance owner sửa D01, review D02, xử lý D03/D04; dùng bảng disposition để tránh sửa lại historical gaps.
4. QA/operations và external owners cung cấp isolatedSQL rồi chạy regression callback/repair/offline/clocks; tiếp tục client/provider/realdata/load/restore gates. Không đóng các gate này bằng mock/source count.

Không xin lại các quyết định R01–R30 đã giao. Những quyết định còn thiếu chỉ cần chốt tại checkpoint phụ thuộc; độc lập có thể tiếp tục ngay. Báo cáo không sửa hộ requirement/contract/data policy để làm test xanh.
