# Kiểm tra gói phân công V2

Thực hiện 2026-09-27 trên các file trong gói.

| Kiểm tra | Kết quả |
|---|---|
| OpenAPI operation ↔ task 1–1 | PASS: 133/133, không thừa/thiếu/trùng |
| Owner duy nhất | PASS: P1 71, P2 62 |
| File task tồn tại | PASS: 133 |
| Dependency chỉ trỏ operation có thật, không chu trình | PASS |
| Link nội bộ trong plan/task mới | PASS: 1673 links |
| Bản gốc hai plan giữ nguyên byte | PASS |
| YAML chính = baseline và SHA-256 đúng manifest | PASS |
| Script check_contracts.py của docs | PASS: CONTRACT_HASH_PASS |

Đây là bằng chứng tạo gói ban đầu, khi đầu vào chỉ có tree và hai plan; không phải test backend. Không chạy dotnet, SQL, HTTP endpoint hay xác nhận implementation. Các mẫu HTTP chứa fixture placeholder nên chưa dùng trực tiếp làm bằng chứng PASS. Link lịch sử trong hai bản gốc giữ nguyên vị trí cũ để đối chiếu, không nằm trong kiểm link mới. Source docs khác được giữ nguyên snapshot REVIEW-01. Kết quả sau khi nhập vào checkout được ghi ở mục kiểm tra đồng bộ bên dưới.

## Kiểm tra đồng bộ skill và ownership V2 — 2026-09-27

Sau khi chấp nhận ADR 006 và mô hình owner theo từng endpoint, đã chạy lại các kiểm tra tài liệu/tooling trong checkout Windows hiện tại:

| Kiểm tra | Kết quả |
|---|---|
| `roadguard-endpoint-delivery` quick validation | PASS: `Skill is valid!` |
| `roadguard-persistence` quick validation | PASS: `Skill is valid!` |
| `roadguard-test-selection` quick validation | PASS: `Skill is valid!` |
| OpenAPI operation ↔ task sau cập nhật boilerplate | PASS: 133/133; P1 71, P2 62; dependency đều là operation hợp lệ |
| Ownership/skill boilerplate | PASS: đúng một lần trong 133 task; không còn câu hai-skill/ownership cũ |
| Contract hash | PASS: `dbde8b756bbc1bf83dabfbfe395eb11e53b378e7338815b1f3ca019aa7f3f806` |
| Structural package validator | PASS: 153 schema, 133 operation, 208 Markdown link, 8 positive fixture, 4 negative fixture |
| Contract guard self-tests | PASS: 7/7, gồm mismatch snapshot/canonical/lock, missing snapshot, auth mapping và sync kind chưa duyệt |
| Windows encoding portability | PASS: validator và self-test chạy không cần `PYTHONUTF8=1` sau khi mọi repository text read dùng UTF-8 tường minh |

Đây vẫn là bằng chứng documentation/tooling. Không chạy build .NET, backend unit/integration/API test, SQL Server, HTTP smoke, provider/device E2E, performance hoặc UAT trong thay đổi governance/skill này.
