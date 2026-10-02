# Bổ sung RF-03: Hai người triển khai độc lập, tự review và phối hợp

> **Current assignment amendment, 2026-10-01:** This document is a historical/proposed parallel-work design. The owner has assigned the entire **current refactor** to Anh, sole writer on `anh`; Huy has no current task and no parallel branch work starts now. The line below that once mapped A to Anh is superseded. For the **later development phase only**, A = Huy on `huy`, B = Anh on `anh`, after a reviewed common baseline. Consumer Web/Android/AI owners are still UNKNOWN. Use [10-refactor-slices.md](10-refactor-slices.md) for current execution and [11-development-plan.md](11-development-plan.md) for the future assignment. Shared hotspots still get an exact per-slice allowlist, but no two-writer coordination is required in this checkout.

Ngày: 30/09/2026. Đây là cập nhật kế hoạch theo yêu cầu chủ dự án, không phải bằng chứng đã sửa repository hoặc chạy runtime. Baseline nguồn vẫn là báo cáo khảo sát local HEAD `2efc8a5`; cần kiểm tra lại HEAD và working tree trước triển khai.

## 1. Nguyên tắc phân việc

- A = người 1 (`anh` theo phân công trước); B = người 2, chưa gán danh tính. Phân công dưới đây là đề xuất để hai người chốt khi nhận task. Không suy ra tên hoặc quyền Git của B.
- Mỗi lát công việc có một người sửa chính, bao trọn Controller → Service → Repository → test → tài liệu module. Không chia A chỉ code Controller và B chỉ code Repository vì tạo phụ thuộc ở mọi endpoint.
- Một task cha có thể có nhiều lát nhỏ, nhưng mỗi lát chỉ một owner; báo cáo riêng theo `<TASK-ID>-<SLICE>-<A|B>.md`.
- Không thể bảo đảm không có conflict. Mục tiêu là giảm sửa đồng thời cùng file và phát hiện xung đột hành vi trước merge.
- Mỗi người dùng branch và checkout/worktree riêng từ cùng commit nền đã thống nhất. Không để hai phiên agent ghi chung working tree. Bảo toàn dirty changes đang có; chủ thay đổi phân loại/commit riêng trước khi lập nền tích hợp. Không tự stash/reset/clean.
- Giữ Controller–Service–Repository; không tạo interface, event bus hoặc abstraction mới chỉ để chia việc. Tái sử dụng điểm nối đang có khi phù hợp.
- Không sử dụng hướng dẫn/skill RoadGuard cũ làm quy trình điều hành trong chương trình refactor. Tài liệu này là kế hoạch; không tự kích hoạt AGENTS.md/.agents mới.

## 2. Phân công theo task hiện tại

ID dưới đây theo chính bộ RF-03 trong archive; không dùng ý nghĩa RF-04..11 của bản đề xuất trước đó.

| Task | Người sửa chính đề xuất | Phần người còn lại làm độc lập | Điểm cần tích hợp |
|---|---|---|---|
| RF-04 | A: BE docs + draft contracts + tổng hợp crosswalk | B: product docs + nguồn nghiệp vụ + ADR product + bảng crosswalk theo module của B | A sở hữu file tổng hợp; B nộp bảng riêng, không cùng sửa canonical OpenAPI |
| RF-05 | A: agent/validator/CI và bản tích hợp | B: kiểm tra map module của B, tình huống thử và nhận xét ở file riêng | Kích hoạt theo điều kiện chấp nhận hiện có; không hai người cùng sửa root AGENTS |
| RF-06 | A: fixture/host/SQL dùng chung | B: ca characterization và fixture dữ liệu riêng module B | B chạy khi host cô lập khả dụng; thiết kế ca kiểm tra không phải đợi |
| RF-07 | A: pilot work-package | B: đọc/review diff và chuẩn bị characterization upload trong phạm vi task được giao | Không sửa đồng thời read-model project |
| RF-08 | A: error/auth/idempotency/transaction lõi | B: characterization upload/AI và phân tích producer/consumer outbox | B không tự đổi dispatcher/outbox lõi; đề nghị bằng note |
| RF-09 | A: cơ chế transition/consumer registry/migration tích hợp | B: phân tích CG17, dữ liệu survey và provider compatibility theo module | Schema/migration có một writer theo lượt |
| RF-10-01 | A: identity/access | B: khảo sát yêu cầu media/AI dùng actor/scope | Chốt actor/scope contract trước integration |
| RF-10-02 | A: project/road/GIS/scope | B: upload/storage core và test boundary | Verified file/project scope là điểm nối, không chép authorization sang B |
| RF-10-03 | B: survey/dataset | A: Reporter intake/PM workflow có thể tách khỏi AI | Survey cũ–V2 vẫn chờ quyết định đúng checkpoint |
| RF-10-04 | B: upload/storage/CG17 | A: identity/project scope | DTO/SQL type và migration cần lượt tích hợp; không coi 8 GiB đã chạy thật |
| RF-10-05 | B: processing/AI adapter | A: Reporter/defect manual review | Candidate schema/provider protocol phải chốt; fake chỉ chứng minh contract cục bộ |
| RF-10-06 | A: Reporter/defect/PM review | B: AI producer và provenance | PM workflow manual độc lập; AI candidate integration chờ B |
| RF-10-07 | A: inspection/Fast Track/repair | B: offline envelope/replay và handler không đụng repair | Repair sync adapter tích hợp sau command contract của A; policy chưa chốt vẫn chặn |
| RF-10-08 | B: offline sync/handover | A: command nghiệp vụ được sync | B sở hữu envelope/replay/routing; owner module sở hữu command/invariant, không hai implementation |
| RF-10-09 | A: notification/outbox/audit core, retention orchestration | B: reporting/export projections, bằng chứng file và phân tích storage retention | B không xóa object hay đổi hold/retention; chia slice và báo cáo riêng |
| RF-11 | A: tích hợp release/retirement | B: bằng chứng hồi quy và consumer của module B | Chốt trạng thái từng slice; không tự push/merge/xóa docs khi chưa giao |

RF-10-09 và RF-04 bắt buộc tách slice trước khi chạy song song. Số task cha vẫn 16; slice bổ sung không làm thay đổi lịch sử ID. Khối lượng chưa được đo; cân bằng theo acceptance criteria và test scope sau RF-04, không theo số task. Nếu A quá tải, bàn giao nguyên slice chưa bắt đầu qua note, không chia đôi file đang sửa.

## 3. Dependency để bắt đầu khác dependency để tích hợp

Mỗi task ghi ba loại:

| Loại | Ý nghĩa | Cách xử lý |
|---|---|---|
| START | Thiếu thì không thể triển khai slice đúng nghĩa | Chờ đúng điều kiện; vẫn hoàn thành khảo sát độc lập |
| INTEGRATE | Có thể code bằng contract đã thống nhất và test double | Chưa tuyên bố Done toàn task trước kiểm tra với implementation thật |
| RELEASE | Có thể hoàn tất code/test cục bộ nhưng chưa đưa vào sử dụng | Ghi Ready for integration/Partial; nêu consumer/data gate |

Chuỗi mũi tên trong master plan cũ tiếp tục có giá trị như cổng bằng chứng/tích hợp. Nó không buộc mọi việc phân tích, test hoặc code độc lập phải chờ cả module trước hoàn tất. Muốn đổi cổng phải ghi rõ slice, chứng cứ và được giao phạm vi; không dùng bản bổ sung để bỏ business/data gate.

Lịch đề xuất sau nền docs/agent/fixture/pilot được chấp nhận:

| Đợt | A | B | Điều kiện để làm song song |
|---|---|---|---|
| 1 | Identity/project scope và shared seams cần thiết | Upload/storage boundary, CG17 design/core | Giữ scope interface hiện hành; schema chung đặt lịch riêng |
| 2 | Project/GIS còn lại; Reporter intake manual | Survey/dataset trên verified-file contract | Phần sử dụng route/version mới chờ đúng contract; không đợi toàn project |
| 3 | Defect/PM review manual | AI adapter/callback | Candidate shape, trạng thái, provenance và retry được hai phía chốt |
| 4 | Inspection/repair/Fast Track phần đủ quyết định | Offline envelope/replay + survey/upload handlers | Handler repair chờ command contract của A; không tự triển khai lại nghiệp vụ |
| 5 | Notification/audit/retention orchestration | Reporting/export projections | Event/query schema cố định; retention deletion chờ thẩm quyền và hold |

Không ghi ETA giả. Mỗi lần nhận slice, ghi độ lớn tương đối S/M/L, số acceptance criteria và rủi ro để điều chỉnh tải.

## 4. Hồ sơ điểm nối trước khi triển khai song song

Tạo `planning/refactor/interfaces/<INTERFACE-ID>.md`, một owner cho mỗi file. Chỉ tạo cho điểm nối thực sự được hai task dùng.

- Producer task/owner; consumer task/owner; trạng thái Proposed/Agreed/Integrated; ngày và nguồn thống nhất.
- Symbol/interface/DTO/event hiện có hoặc đường dẫn contract đề xuất; version/baseline commit.
- Inputs/outputs, nullability, actor/project scope, status/error, idempotency key, concurrency, transaction/outbox và provenance nếu liên quan.
- Tình huống thành công, lỗi, retry và ví dụ fixture dùng chung; điều kiện consumer/provider contract test.
- Trách nhiệm cụ thể của hai phía; thay đổi tương thích hay breaking; consumer ngoài repo chưa kiểm chứng.
- Ai sửa điểm nối, ai cập nhật consumer, thứ tự merge và tiêu chí tích hợp.

Ưu tiên các điểm nối: actor/scope; verified file với SizeBytes phù hợp yêu cầu 8 GiB; immutable survey dataset; AI candidate; defect/repair commands; sync dispatch; outbox event. Đây là danh sách điểm cần đối chiếu, không phải schema đã được phê duyệt.

## 5. File dùng chung và quyền sửa

| Nhóm | Writer mặc định đề xuất | Quy tắc |
|---|---|---|
| Program/DI/auth middleware/ApiErrorCodes/shared transaction/outbox | A | B gửi thay đổi cần thiết dưới dạng note/patch đề xuất; không áp vào file chung |
| RoadGuardDbContext, migration chain, model snapshot | Một writer theo lượt, A điều phối | Owner module chuẩn bị entity mapping riêng; writer tạo migration từ model đã tích hợp. Chuyển lượt sang B phải được ghi nhận |
| Canonical OpenAPI/baseline/FE lock/Postman collection chung | A tích hợp | Mỗi owner chuẩn bị delta/requests theo module ở file riêng; chỉ một người sinh/cập nhật bản tổng tại một thời điểm |
| Fixture SQL/API host dùng chung | A | B sở hữu test và dữ liệu test riêng module; không đổi seed chung để làm test mình pass |
| AGENTS, CI, validator, master plan, bảng ownership | A | B cập nhật báo cáo riêng và đề nghị sửa; không ghi đồng thời bảng trung tâm |
| Shared DTO/interface/entity/symbol chưa rõ owner | Chưa phân công | Tạo coordination note và chọn owner trước khi sửa |

Đường dẫn/symbol phải đối chiếu code thực tế; bảng trên không phải allowlist file đã xác minh. Quyền sở hữu module không đồng nghĩa được sửa mọi partial của một type dùng chung. Hai file khác nhau vẫn có thể xung đột hành vi hoặc EF model.

Trước khi code: ghi exact allowlist/file patterns đủ hẹp trong task, vùng chỉ đọc, hotspots và reservation đang có. Khi phát hiện vượt scope, không tự mở rộng allowlist. Task owner được phép sửa/test/review trong phần đã giao, không cần hỏi lại mỗi bước.

Migration: tránh hai người cùng tạo snapshot/chuỗi migration từ hai model khác nhau. Không chỉnh/xóa migration đã áp dụng. Migration chưa áp dụng có thể tạo lại sau tích hợp theo quy trình được giao; cần verify upgrade, existing data và model snapshot trên DB cô lập. Không chạy vào DB chung để giải conflict.

## 6. Quy trình self-review bắt buộc

Self-review thực hiện trong cùng phiên code; không cần gọi agent thứ hai hoặc skill cũ. Phân biệt tự review với peer review.

1. Ghi baseline commit, dirty files trước khi sửa và scope được giao. Chỉ review diff của task; không nhận thay đổi có sẵn là của mình. Dùng `git status --short`, `git diff --stat`, diff unstaged/staged và commit range phù hợp thực tế; bao gồm file mới chưa tracked.
2. Lượt 1 — correctness: đọc diff, trace caller/consumer và chỉ mở thêm dependency liên quan. Kiểm tra contract/status/header/DTO, actor/project scope, transaction/outbox, idempotency/retry, concurrency, offline, migration/data, side effect và evidence theo rủi ro task. Không tick toàn bộ khi task không liên quan; ghi N/A và lý do.
3. Lượt 2 — tác động phối hợp: đối chiếu allowlist, owner, shared symbols, migrations, contract fixtures, consumer ngoài repo. Tìm cả xung đột hành vi không gây Git conflict.
4. Mỗi phát hiện ghi ID, mức độ, file/symbol, bằng chứng, ảnh hưởng và hướng sửa. Critical/High về mất dữ liệu, quyền hoặc sai contract chặn Ready for integration. Mức thấp hơn phải sửa hoặc ghi defer có căn cứ/người nhận; không tự hạ severity để qua gate.
5. Tự sửa lỗi trong phạm vi đã giao và chạy lại kiểm chứng liên quan. Lỗi cần đổi contract/nghiệp vụ chưa chốt hoặc file của người kia: tạo coordination note, tiếp tục phần độc lập, không tự sửa hộ.
6. Kiểm tra build/test nhỏ nhất đủ rủi ro; ghi đúng command, commit/diff được kiểm tra, số pass/fail/skip. Không dùng kết quả `--no-build` của binary cũ sau khi sửa. Không cần full suite chỉ vì đổi tài liệu; chỉ mở rộng khi shared change hoặc gate yêu cầu.
7. Đọc lại diff cuối sau autofix; `git diff --check` khi có Git. Nếu base đã thay đổi, chạy lại phần kiểm chứng bị ảnh hưởng sau tích hợp. Không dùng conflict-free merge làm bằng chứng đúng nghiệp vụ.
8. Kết luận riêng: self-review PASS/BLOCKED/NOT-RUN; integration NOT-RUN/PASS/FAIL. Test double PASS không đồng nghĩa provider/consumer thật PASS.

## 7. Phát hiện liên quan người còn lại

Tạo note theo [mẫu phối hợp](templates/coordination-note.md), ID `<TASK>-<A|B>-CN-001` để tránh trùng. Lưu file riêng ở `coordination/`; không để hai người cùng sửa một log trung tâm.

Trình tự: Open → Discussing → Assigned → Implemented → Verified → Closed; có thể Deferred kèm lý do/tác động. Assigned phải có người sửa chính do hai người thống nhất; agent chỉ đề xuất, không tự nhận sửa phần người kia.

- Note nêu đủ vấn đề, bằng chứng, task/owner ảnh hưởng, điểm nối, lựa chọn và khuyến nghị.
- Ưu tiên owner của invariant/dữ liệu gốc sửa producer; consumer owner sửa adapter/caller của mình. Nếu chỉ sai mapping phía consumer thì consumer sửa. Nếu shared primitive sai thì writer dùng chung sửa, cả hai chạy test module bị ảnh hưởng.
- Nếu chuyển ownership, bàn giao nguyên slice cùng baseline, file reservation, changes và test; không để hai nhánh sửa đồng thời.
- Chưa thống nhất: để checkpoint liên quan Partial/Blocked; hoàn thành phần không phụ thuộc. Không copy logic nghiệp vụ sang module khác để vượt blocker.
- Không tự gửi tin/email cho người kia; trả note và câu hỏi cụ thể cho hai người thảo luận.

## 8. Tích hợp và trạng thái

Implementation status: Planned / In progress / Partial / Blocked / Done. Delivery stage riêng: Draft / Ready for integration / Integrated / Released. Một slice có thể hoàn tất local nhưng task cha Partial khi integration/decision gate chưa đạt.

Được ghi Ready for integration khi: scope sạch, self-review xong, test liên quan đạt hoặc hạn chế được chấp nhận rõ, không còn Critical/High chưa xử lý, note ảnh hưởng hành vi chung đã Assigned/resolved phù hợp, contract fixtures và điều kiện consumer đủ. Note thảo luận chưa chốt có thể cho phép tích hợp phần độc lập, không phần tranh chấp.

Người tích hợp theo lượt (đề xuất A) nhận PR/patch nhỏ theo slice; so base hiện tại, review shared-file reservation, kiểm tra diff + test trên kết quả tích hợp. Branch dạng `refactor/<task>/<slice>-a` và `refactor/<task>/<slice>-b`; tên integration branch do nhóm chọn. Không tự merge/push chỉ vì tài liệu này có quy trình.

Docs-only không cần runtime tests. Schema/security/public contract/shared changes cần peer review của người còn lại trước tích hợp; reviewer ghi nhận xét, không sửa branch của author. Module nhỏ giữ hành vi có thể self-review và focused tests theo gate đã giao. Không tự đánh dấu peer review đã làm.

## 9. Bước triển khai kế hoạch này

1. Chốt A/B và writer các file chung khi nhận task; chưa cần khóa toàn bộ backlog.
2. RF-04 phân docs và crosswalk theo bảng; A tổng hợp một lần sau khi B hoàn tất slice riêng.
3. Bổ sung exact file/symbol ownership, START/INTEGRATE/RELEASE dependencies và điểm nối trong mỗi slice trước code.
4. Dùng mẫu báo cáo mới, mẫu coordination note và self-review ngay cho task được giao. Không cần chờ xây xong agent mới để báo cáo theo mẫu này.
5. RF-05 đưa quy trình này vào bộ agent mới dưới dạng tham chiếu, tránh chép lặp; activation vẫn theo điều kiện hiện có.
