# Câu hỏi nghiệp vụ và kết quả dọn dự án — 30/10/2026

Code và docs **chưa khớp hoàn toàn**. Snapshot API/schema trong bộ docs chính thức khớp source được kiểm tra; một số trang ACTIVE/module map vẫn ghi thiếu tính năng đã có. Ngoài ra còn giới hạn nghiệp vụ thật: Reporter Pending khó phục hồi, matching thiếu facts, Case FIELD/DRONE trước Defect chưa kết luận được, GPS chưa có source binding và công thức thống kê công khai theo kỳ chưa chốt.

Đây là bộ câu hỏi để ra quyết định, chưa sửa code nghiệp vụ. Sáu câu Q01–Q06 bên dưới đều có câu trả lời hiện tại và hai đề xuất. T01–T05 là câu hỏi về cách giao việc/kiểm chứng; không biến yêu cầu đã chốt thành lựa chọn có thể bỏ qua. Mọi A/B là `PROPOSED`, chưa được bạn duyệt.

## Dọn dẹp thực tế

`CURRENT_VERIFIED`: lần này xóa4 thư mục cache, **7.600 file, 957.185.703 bytes (~912,84MiB)**: Go build cache và3 Python bytecode caches. Tất cả không tracked, nằm trong project, không reparse point, không là dữ liệu mounted của Docker đang chạy. Cache có thể sinh lại khi build/chạy Python. Manifest: [chi tiết đã xóa](D:/RoadGuard-Review-Evidence/BUSINESS-QUESTIONS-CLEANUP-20261030/deleted-cache-manifest.json).

Lần trước đã dọn32 đường dẫn/~16,70GiB; không cộng lại thành kết quả lần này. Giữ nguyên source/docs, TestResults, bằng chứng lịch sử, cấu hình/khóa, object data và Docker volumes. `artifacts/anh02-live/objects` còn khoảng8,68GiB là dữ liệu object, không tự coi là rác. Một thư mục obj trong docs/history/extract được giữ vì thuộc snapshot lịch sử. Go dependency/toolchain và hai MinIO binaries được giữ để không phá khả năng tái hiện các script cũ. Không dọn Docker thêm trong lần này.

## Cách đọc và trả lời

Baseline `huy-review` / `01cc4725b178199215b02f4ddf7093491a16f2f7`;147 dirty paths có sẵn được ghi hash trước thao tác. `CURRENT_VERIFIED` dưới đây chủ yếu là **source hiện tại**, không đồng nghĩa đã chạy HTTP/SQL. `TARGET_CONFIRMED` là quyết định có nguồn; `UNKNOWN` là phần còn thiếu. Chi tiết audit/test trước đó ở [báo cáo đối chiếu](BUSINESS-DOCS-AUDIT-20261030.md:15). Báo cáo local, không phải tài liệu đã phát hành/clean-clone portable.

Bạn có thể trả lời `Q01=A, Q02=B, ...`, kèm người phụ trách/nguồn tài liệu và mốc cần hoàn thành. Với Q05/Q06 cần bổ sung thông số ghi ở cuối câu; chỉ chọn A/B chưa đủ để lập contract triển khai. Trạng thái hiện tại của cả6 câu: `AWAITING_OWNER_DECISION` (Q02/Q03 chỉ chờ quyết định phân kỳ/cung cấp nguồn).

## Q01 — Reporter chưa kích hoạt, mất phiên đăng ký thì lấy lại tài khoản bằng cách nào?

**Trả lời hiện tại:** `CURRENT_VERIFIED` đăng ký email đã tồn tại tạo intent có UserId=null; verify intent ấy trả NotFound. Login/reset-password chỉ phục vụ Active. Vì thế đăng ký lại không tự phục hồi Pending mất intent. Đây là khoảng trống trải nghiệm, chưa kết luận có lỗ hổng chiếm tài khoản. Nguồn: [repository](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Identity/IdentityOnboardingRepository.cs:63>), [verify](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Authentication/IdentityOnboardingService.cs:134>), [audit chi tiết](BUSINESS-DOCS-AUDIT-20261030.md:48). `UNKNOWN`: quy trình tự phục hồi hay hỗ trợ.

**A — Tự phục hồi qua email (khuyến nghị):** sau chứng minh sở hữu email, cấp intent recovery hợp lệ với cooldown, giới hạn thử và chống replay; không thay credential trước xác minh. Ít phụ thuộc hỗ trợ nhưng cần thiết kế transaction/concurrency và UX mới.

**B — Bộ phận hỗ trợ phục hồi:** quy trình xác minh riêng, có audit và người xử lý; phản hồi công khai vẫn chống dò email. Triển khai tự động ít hơn nhưng người dùng phải chờ, cần đầu mối và SLA hỗ trợ.

**Bạn chốt:** A/B và đầu mối vận hành (Owner + identity). **Nghiệm thu sau giao sửa:** new/Pending/Active, mất/hết hạn intent, OTP/resend/replay/concurrent recovery; kiểm cả retry503 giữ idempotency key rồi resend. Không ghép mọi intent email trùng vào user.

## Q02 — Khi cần kiểm tra hiện trường/drone trước khi xác nhận có Defect, triển khai giai đoạn nào?

**Trả lời hiện tại:** `TARGET_CONFIRMED` R10 yêu cầu task pre-Defect riêng. `CURRENT_VERIFIED` final conclusion khác NeedsEvidence bị409 source_not_ready nếu Case dùng FIELD/DRONE; ExistingEvidence vẫn có. Nguồn: [guard](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Cases/CaseWorkflowRepository.cs:149>), [R10](<D:/Project BE/RoadGuardSystem/planning/development/HUY-FINAL-INTEGRATION.md:689>). `UNKNOWN`: gói thiết kế producer/consumer và lịch giao thực tế; không hỏi lại có cần R10 không.

**A — Ưu tiên làm task pre-Defect (khuyến nghị nếu pilot cần khảo sát trước xác nhận):** giao owner thiết kế task, completion evidence và liên kết Case; hỗ trợ hành trình đầy đủ nhưng cần contract/schema riêng được duyệt trước triển khai.

**B — Phân kỳ:** giai đoạn đầu chỉ kết luận bằng ExistingEvidence hoặc yêu cầu bổ sung; giữ chặn final FIELD/DRONE, giao R10 sang mốc kế tiếp có người và thời hạn. Ít việc trước mắt nhưng hồ sơ cần khảo sát có thể phải chờ; không được quảng bá là đã đủ flow.

**Bạn chốt:** A/B, owner và mốc bàn giao. **Nghiệm thu:** Case→task riêng→completion→conclusion→publication, thiếu/sai quyền/sai evidence bị từ chối. Không tạo Defect hoặc Survey giả để qua guard.

## Q03 — Nguồn dữ liệu để gợi ý trùng lỗi sẽ được cung cấp ngay hay theo giai đoạn?

**Trả lời hiện tại:** `TARGET_CONFIRMED`33A yêu cầu cùng project, vị trí/sai số/lịch sử, thiếu GPS dùng scope+ảnh và PM tìm mở rộng. `CURRENT_VERIFIED` code đưa distance=null, metricGpsAvailable=false, image score=null; FIELD_OBSERVATION còn source_not_ready. Nguồn: [matching](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Defects/CandidateMatchingService.cs:30>), [facts bị thiếu](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Defects/CandidateMatchingService.cs:88>), [33A](<D:/Project BE/RoadGuardSystem/docs/product/confirmed-decisions.md:8>). `UNKNOWN`: nguồn tọa độ/accuracy/provenance và cách đánh giá ảnh được chấp nhận; không thiếu quyết định PM duyệt.

**A — Cấp nguồn đủ rồi hoàn thiện matching (khuyến nghị):** giao GIS/AI/BE thống nhất source version, tọa độ/CRS/accuracy và semantics ảnh; test chất lượng trước công bố đầy đủ. Đúng đích33A nhưng phụ thuộc dữ liệu thực và người nghiệm thu.

**B — Phân kỳ minh bạch:** dùng phần scope/lân cận/lịch sử đã có, hiển thị rõ thiếu distance/image facts; giao bổ sung scope+ảnh và đầy đủ facts ở mốc tiếp. Có thể demo sớm nhưng phần hiện tại vẫn **chưa đạt đầy đủ33A**, không được gọi fallback hoàn chỉnh.

**Bạn chốt:** A/B, đơn vị cấp facts và mốc. **Nghiệm thu:** known/missing GPS, accuracy, ảnh, project isolation, expanded search, stale source/geometry/cursor. Cả hai giữ PM quyết định; không auto-merge hay tự đặt threshold/recall.

## Q04 — Ai chịu trách nhiệm cung cấp và duyệt căn cứ GPS/GIS chính thức?

**Trả lời hiện tại:** `CURRENT_VERIFIED` GPS_CAPTURE kiểm range nhưng luôn thêm GPS_SOURCE_BINDING_UNAVAILABLE. POSITION_CHECKLIST có segment/slab/chainage thay thế. Nguồn: [GPS guard và fallback](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.Intake.cs:140>). `TARGET_CONFIRMED`: dữ liệu mẫu không đủ để nghiệm thu độ chính xác. `UNKNOWN`: project CRS/profile, operation, tolerance và control points được đơn vị có thẩm quyền chấp nhận; xem [localized gate](<D:/Project BE/RoadGuardSystem/planning/development/HUY-FINAL-INTEGRATION.md:1718>).

**A — Hồ sơ GIS riêng từng dự án (khuyến nghị khi mỗi công trình khác nguồn):** đơn vị quản lý/khảo sát giao bundle có nguồn, version, operation, điểm kiểm độc lập và tolerance được ký duyệt. Rõ trách nhiệm, ít rủi ro dùng nhầm chuẩn; cần duyệt nhiều lần.

**B — Registry GIS dùng chung có mapping dự án:** authority duyệt bộ profile/version và phạm vi sử dụng; mỗi project vẫn phải được gắn đúng profile và kiểm độc lập. Dễ tái dùng nhưng cần quản trị registry và chống áp nhầm chuẩn.

**Bạn chốt:** A/B, tên đơn vị duyệt và nơi lưu hồ sơ (Owner + GIS). **Nghiệm thu:** điểm kiểm độc lập/transform/sai số nguồn thật, version thay đổi và location proof; không đoán EPSG/tolerance, không bỏ guard, không yêu cầu mọi trường hợp thiếu GPS đều bị cấm.

## Q05 — Báo cáo đã chốt kỳ sẽ xử lý correction về sau ra sao?

**Trả lời hiện tại:** R19 đã chốt đọc kết quả effective và giữ lịch sử/snapshot gốc. `CURRENT_VERIFIED` statistics code tách current-stock facts, vẫn để public-period/cohort allocation thành owner gate. Nguồn: [source](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Reporting/DefectStatisticsRepository.cs:140>), [LD08b](<D:/Project BE/RoadGuardSystem/planning/development/HUY-FINAL-INTEGRATION.md:2119>). `UNKNOWN`: công thức theo kỳ, mẫu số và phân bổ qua kỳ; không thiếu quyết định giữ lịch sử.

**A — Kỳ đã chốt giữ nguyên, điều chỉnh ở kỳ sau (khuyến nghị cho báo cáo phát hành):** công bố adjustment có tham chiếu kỳ gốc; dashboard vận hành vẫn đọc effective. Dễ đối soát bản đã gửi nhưng cần phân biệt số phát sinh mới với điều chỉnh, có thể khác số effective của kỳ cũ.

**B — Phát hành lại kỳ cũ có version:** bản restatement dùng effective result mới, có lý do và liên kết bản trước; tuyệt đối không ghi đè/xóa snapshot đã phát hành. Phản ánh đúng thông tin mới nhưng người dùng phải nhận biết version và tránh trộn các lần xuất.

**Bạn chốt:** A/B và **(1)** cohort theo ngày phát hiện/hoàn thành/nghiệm thu nào, **(2)** mẫu số mỗi chỉ tiêu, **(3)** múi giờ/mốc chốt, **(4)** UNKNOWN/unallocated, **(5)** phân bổ shared-road/cross-period. Owner + phụ trách thống kê ký ví dụ số cụ thể. **Nghiệm thu:** correction qua hai kỳ, shared part không đếm trùng, m² không cộng với đơn vị khác, UNKNOWN khác0, export cũ bất biến. Chọn A/B chưa tự phê duyệt công thức.

## Q06 — Giới hạn số lần và tổng thời gian gia hạn nên quản lý theo cách nào?

**Trả lời hiện tại:** `CURRENT_VERIFIED` đã có issuer guard và command gia hạn/bổ nhiệm, không còn chỉ là domain primitive như tiến độ cũ ghi. Response vẫn ghi numericalLimitPolicy=PENDING_OWNER_DECISION, additionalOfflineExecution=NOT_AUTHORIZED. Nguồn: [issuer guard](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/ClockDutyRepository.cs:29>), [pending limits](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/ClockDutyRepository.cs:121>), [domain giữ breach/history](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs:96>). `UNKNOWN`: numerical caps được owner chấp nhận.

**A — Một bảng giới hạn theo loại clock dùng chung (khuyến nghị ban đầu):** owner cấp số lần/tổng gia hạn tối đa và xử lý khi chạm trần; issuer đúng vai trò hiện có. Dễ vận hành/kiểm tra; ít linh hoạt với dự án đặc thù.

**B — Policy có version theo project/loại clock:** vẫn giới hạn định lượng và đúng issuer, có phê duyệt cấu hình/hiệu lực/audit. Linh hoạt nhưng phải quản trị nhiều policy và xử lý phiên bản thay đổi.

**Bạn chốt:** A/B, loại clock áp dụng, số lần/tổng thời gian, quy tắc sau khi đã quá hạn và người duyệt policy. **Nghiệm thu:** vượt trần, sai issuer, stale version, concurrent/replay, history/breach bất biến. Không thay clock durations đã chốt, không reset origin24h FastTrack, không coi gia hạn deadline là quyền tự sửa/offline bổ sung. Chính sách số chưa chốt không chứng minh mọi quyền gia hạn đều chưa có code.

## T01 — Lỗi trả ảnh theo quyền cũ cần sửa theo cách nào?

**Trả lời:** `CURRENT_VERIFIED source`, probe delayed-storage trước đó quan sát mất active status giữa lúc open vẫn trả stream; probe ở service/mock, chưa là khai thác HTTP thật. [ReporterEvidenceService](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Services/Implementations/Files/ReporterEvidenceService.cs:158>) chưa post-open recheck cho owner/publication. **Đây là việc cần sửa, không hỏi cho phép giữ truy cập trái quyền.**

**A — Sửa hẹp hai đường Reporter download (khuyến nghị):** re-resolve quyền/status/pin tương ứng sau open, dispose khi từ chối; ít phạm vi, regression owner/publication/revoke/version. **B — Dùng chung guarded-open abstraction cho các private evidence paths:** giảm lệch chính sách dài hạn nhưng phạm vi refactor/compatibility lớn hơn, cần giao gói riêng và regression từng purpose. Huy/security chọn cách triển khai; không đổi error/wire tùy tiện. Nghiệm thu HTTP+SQL+storage trì hoãn ngoài probe.

## T02 — Tài liệu nào cần sửa và nên đồng bộ ra sao?

**Trả lời:** `CURRENT_VERIFIED` source pins259 operations/1534 hashes PASS khi chạy lại; dùng capture trước đó, không fresh HTTP/schema deployment. Một số trang ACTIVE chưa khớp: [workflows](<D:/Project BE/RoadGuardSystem/docs/product/workflows.md:9>) vẫn ghi callback/FT-final gap; [backend entrance](<D:/Project BE/RoadGuardSystem/docs/backend/README.md:12>) và [module map](<D:/Project BE/RoadGuardSystem/.agents/modules/README.md:15>) còn trạng thái cũ. [Danh sách drift và134 missing-link references](BUSINESS-DOCS-AUDIT-20261030.md:64) có CSV từng dòng; con số này là kết quả scan lần audit trước, không134 lỗi nghiệp vụ. Docs mới còn dirty/untracked, chưa là artifact phát hành.

**A — Giao gói sửa tập trung các lối vào ACTIVE, ghi current disposition cho từng finding cũ, trỏ sang inventory có hash (khuyến nghị):** nhanh, giữ lịch sử và tránh mở lại bug đã sửa; cần docs owner review. **B — Đồng bộ toàn bộ các bản sao hiện hành rồi chuẩn bị gói phát hành docs/evidence có link portable:** đầy đủ hơn nhưng nhiều file và phối hợp writer. Cả hai giữ lịch sử, không bulk-delete docs; sửa encoding OWNER-DECISIONS-FINAL bảo toàn nội dung khi có scope riêng. Kiểm links/source pins và review trước commit/phát hành; lần này chỉ ghi đề xuất.

## T03 — Contract V2/FE đang lệch thì khóa lại hay đối chiếu thế nào?

**Trả lời:** `CURRENT_VERIFIED fresh check` V2 checker exit1 CONTRACT_LOCK_MISMATCH. Draft V2 không tự thành production contract; chưa chứng minh adoption tương đương consumer. [Current integration](<D:/Project BE/RoadGuardSystem/docs/RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md:11>) vẫn để H02–H05 OPEN, API030 MP4 parity NOT_VERIFIED.

**A — BE+FE review delta rồi adopt snapshot tương ứng runtime, cập nhật lock sau review (khuyến nghị nếu chuyển consumer ngay):** giảm phân nhánh nhưng cần consumer acceptance. **B — Giữ V2 draft với trạng thái chưa adopt, thiết kế compatibility adapter có phạm vi và kế hoạch chuyển đổi:** ít phá client hiện hữu, thêm chi phí hai contract và test adapter. Contract owner + FE chốt; không regenerate lock chỉ để xanh. Nghiệm thu method/path/actor/project/DTO/status/header/idempotency/concurrency và MP4 thực; cả hai chưa là FULL_CONTRACT certified.

## T04 — Nghiệm thu ngoài Backend nên tổ chức theo trình tự nào?

**Trả lời:** `UNKNOWN/external not verified` browser/phone/AI thật, native offline recovery, SMTP/FCM, tải50user/p95 và restore theo RPO/RTO chưa được chứng nhận bởi audit này. Docker local đã chạy kịch bản riêng với AI synthetic, không thay những cổng ấy. [Phạm vi đã kiểm](<D:/Project BE/RoadGuardSystem/planning/development/ROADGUARD-DOCKER-LOW-RESOURCE.md>), [client boundary](<D:/Project BE/RoadGuardSystem/docs/RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md:35>), [quyết định38–44](<D:/Project BE/RoadGuardSystem/docs/product/confirmed-decisions.md:14>).

**A — Pilot một project có dữ liệu thật và owner Web/Mobile/AI/Ops rõ, nghiệm thu từng hành trình (khuyến nghị):** phát hiện lỗi liên thông sớm, giới hạn người tham gia; vẫn phải đóng mọi gate liên quan trước công bố phạm vi hỗ trợ. **B — Hoàn tất test integration từng hệ trước rồi chạy E2E đủ ma trận:** dễ tách trách nhiệm, tốn thời gian và có thể phát hiện lỗi giao tiếp muộn. Owner giao tên đầu mối/ETA theo44; BE giữ adapter/contract/fixtures. Checklist bắt buộc:401 retry giữ key/body/version, phone kill/reboot/ACK-loss/encryption/backup, real AI provenance, delivery/revoke,8GiB/32GiB resume, load/restore. Không hỏi lại mức50user/RPO/RTO, không yêu cầu escrow mới;42A đã chốt export khi còn device/key và nguy cơ không cứu được khi mất khóa.

## T05 — Nguồn bảo hành/bàn giao/coverage và identity thống kê đưa vào hệ thống bằng cách nào?

**Trả lời:** `CURRENT_VERIFIED` đã có [coverage mapping producer](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Repairs/RoadCoverageRepository.cs:102>), [statistics source checks](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Reporting/DefectStatisticsRepository.cs:117>). Thiếu nguồn thật/nguồn đã được chấp nhận không đồng nghĩa thiếu toàn bộ endpoint. TEST_ONLY/sampleOnly không phải căn cứ kích hoạt thật; shared identity phải tường minh, không suy từ polygon chồng nhau. `UNKNOWN`: bộ hồ sơ thực tế và người cung cấp cho pilot.

**A — Nhập pilot theo từng hồ sơ, người có thẩm quyền kiểm đúng road/scope/time và provenance (khuyến nghị):** nhỏ, dễ audit nhưng cần thao tác thủ công; kiểm sai nguồn/ngày/phạm vi/version và quyền Supervisor. **B — Adapter import từ hệ thống quản lý nguồn được đơn vị phụ trách xác nhận, sau đó người có thẩm quyền xác nhận mapping:** tiết kiệm nhập lâu dài, cần mapping/version/reconciliation và chủ hệ thống ngoài. Không đổi vai trò đã chốt. Huy + Owner/source provider nghiệm thu REAL_SOURCE độc lập; UNKNOWN giữ chưa đủ điều kiện, không tự coi toàn project đều được bảo hành.

## Những việc không hỏi lại

ACK nghiệp vụ đã có [BusinessDutyController](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.API/Controllers/BusinessDutyController.cs:36>) và [transaction nguồn](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/BusinessDutyRepository.cs:105>); notification read khác ACK. [Weekly catch-up](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Calendar.cs:40>) đã tạo latest-period digest; [lifecycle commands](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Projects/ProjectLifecycleRepository.LD06.cs:16>) đã có. Không dùng các dòng “NOT IMPLEMENTED” tiến độ cũ để hỏi lại LD02/04/05/06/07/08a.

Callback A1 latest fence, FT-final informational producer, Crew repairContext và Survey supplementary-child đã có source. Không thêm Supervisor approval cho FT, không cho AI tự merge/approve, không reset original clock, không biến UNKNOWN thành0, không xóa bằng chứng khi thiếu retention basis hoặc còn hold. Persistent login supersede timeout cũ theo assigned spec. Xem [các false positive đã rút](BUSINESS-DOCS-AUDIT-20261030.md:103).

## Bằng chứng và giới hạn bàn giao

Nguồn kiểm tra/manifest/hash/outputs: [evidence local](D:/RoadGuard-Review-Evidence/BUSINESS-QUESTIONS-CLEANUP-20261030/verification.json). Hai self-review và external-review status nằm trong [assigned spec](<D:/Project BE/RoadGuardSystem/planning/development/BUSINESS-QUESTIONS-CLEANUP-20261030.md>). Đây là static/source recheck, cleanup và questionnaire; không chạy lại toàn bộ unit/SQL/E2E ở nhiệm vụ này, không sửa các lỗi code nêu trên. Các kết quả test trong audit/Docker trước vẫn có phạm vi và thời điểm riêng. Owner chọn phương án sẽ cần spec triển khai hẹp, contract và kiểm thử tương ứng; chưa tự kích hoạt policy mới.
