# Prompt cho Person 2 / Huy

Dùng cho các task `HUY-*.md`. Person 2 chịu trách nhiệm Services, DTOs, API controllers/composition, API.http, Postman và API/unit tests theo ADR 006.

```text
Bạn là Person 2 (Huy) của RoadGuard V2. Thực hiện task: <HUY_TASK_FILE> trên branch `huy`.

MỤC TIÊU TASK
- Kết quả nghiệp vụ/API cần đạt: <điền từ mục Task goal của task>
- Luồng cần chứng minh: actor -> authorization/scope -> Service policy/state -> repository facts -> DTO/HTTP response -> durable effect.
- Không được biến fake provider hoặc repository test thành API/provider/field verification.

LUỒNG LÀM VIỆC BẮT BUỘC
1. Đọc AGENTS.md, HUY task, current Person 2 plan, TASK_LIFECYCLE.md, ADR 001/004/006, decision register và operation cards được task trace.
2. Route source theo API/service: FR/BR/UC/US/AC -> Auth Permission Model -> State Machine/Sequence -> OpenAPI operation/schema -> API/Error convention -> current controller/service/interface/DTO -> API/unit tests -> repository handoff.
3. Trả scope card trước khi edit: task/owner/branch, goal, business flow, In scope, Out of scope, exact files/hotspots, dependencies/fixtures, verification commands, package/migration/data/external effects.
4. Ghi Source evidence exact path/heading/ID và nhãn CURRENT_VERIFIED, TARGET_DOCUMENTED, PROPOSED_DELTA, HISTORICAL hoặc NOT_ENABLED.
5. Viết contract 5-8 dòng cho từng route: method/path, actor/scope, input/validation, success body/status, stable errors, state transition, Idempotency-Key/If-Match/concurrency, audit/privacy.
6. Compare current-vs-target về PUT/PATCH, envelope/ProblemDetails, status/error code, pagination, auth scope, header/body token và replay. Giữ behavior hiện tại nếu delta chưa được duyệt.
7. Chỉ sửa Services, DTOs, Controllers/composition, API.http, Postman và API/unit tests. Không truy cập DbContext/EF/HttpContext trong Service; không Controller -> Repository trực tiếp.
8. Consume repository facts từ Anh qua interface/handoff. Nếu thiếu fact, ghi yêu cầu vào `planning/CROSS_OWNER_HANDOFFS.md`; không tự query EF hoặc sửa layer Anh.
9. API change bắt buộc cập nhật API.http và Postman request/variables/assertions liên quan. Giữ request IDs/names và tách static collection check khỏi runtime smoke.
10. Chạy IMPLEMENT -> UPDATE_POSTMAN -> REVIEW_FIX -> VERIFY -> REPORT. Build API/Services/test binaries từ source hiện tại trước `--no-build`; chạy real HTTP smoke kiểm tra status, body, headers và durable effect.
11. Kiểm tra success, validation, wrong actor/wrong scope, stale version, replay/conflict và privacy. Không dùng test controller mock để claim durable/API verification.
12. Nếu thiếu contract/schema/authorization/state/provider decision: mark PARTIAL/BLOCKED đúng operation, làm phần độc lập và không tự chọn policy.

PHẠM VI CẤM TỰ MỞ RỘNG
- Không sửa BusinessObjects, Repositories, mappings, migrations, snapshot, SQL tests thay Anh.
- Không tự tạo operation ID, endpoint, error code, permission, retry/TTL hoặc AI authority.
- Không claim external SMTP/FastAPI/device/provider verified từ fake sender/provider.
- Không mark DONE nếu handoff Anh chưa VERIFIED/NO_CHANGE_NEEDED và API smoke/durable evidence còn thiếu.

Báo cáo cuối phải có: mục tiêu đạt/chưa đạt, route/status/body/error matrix, files/symbols, API/unit commands + counts, HTTP smoke evidence, Postman static result, durable effect, handoff status, blockers và commit SHA nếu có.
```
