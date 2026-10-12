# Data meaning, quality và evidence boundaries

**ACTIVE · DOCUMENTATION_UPDATED · DOC-AUDIT-01**, baseline `f0c8762`. Physical schema/dictionary/relationships tại [data baseline](../backend/data/README.md); latest expected rules tại [requirements](requirements.md).

Report/private photo, IncidentCase, report links/supplements/publications, Defect/source/candidate/label, FIELD sessions/measurements, Repair obligations/items/attempts/reviews, Offline proof/receipts và notification sidecars đã là entity/configuration hiện hành. Tên concept trong V2 không bắt buộc ánh xạ 1:1 sang tên bảng; không tạo CRUD/schema chỉ vì concept có trong DD.

Giữ riêng raw aircraft GPS, projected station, camera footprint và defect/field location. Candidate AI là video detection và provenance, không thay physical measurement do Crew thu. Zero là giá trị biết; null/UNKNOWN không được chuyển thành0, PASS hoặc LOW. CRS/profile/sampleOnly/TEST_ONLY fixture không chứng nhận coordinates thực, executable coverage hoặc real FT permission.

Source file size hiện là long/bigint; pilot admission nằm trong `UploadAdmissionPolicy` (20 MiB/8 GiB/10 MiB và dataset32 GiB). Multipart/session/recovery/verification đã có. Bản audit không truyền 8 GiB qua proxy/storage thật; metadata tests không là large-stream E2E.

BE giữ immutable source/model/attempt/result/frame manifests và explicit human decisions. ANH02 synthetic worker có result/provenance guards riêng; legacy callback mới kiểm attempt thuộc job + manifest/model/mode/raw file, chưa latest-attempt fence. Candidate matching hiện scope/neighbor/history; GPS metric/accuracy và image comparison chưa wired. [GAP](../audit/DOC-AUDIT-01-OPEN-GAPS.md) ghi rõ confidence và remediation đề xuất.

Publication là per-report recipient projection, không lộ report khác/case-private evidence. Supervisor-only directory/account có email/phone nullable; members/primary-PM chỉ membership identity/version, không contacts. Notification recipient scope không đổi khi Supervisor có global project authority.

Retention PR-41A: warranty+5 years evidence/source video, technical logs90 days, temp export30 days, backups35 days, hold blocks deletion; missing/multiple obligations cần valid basis, không suy từ upload date. Audit không xóa dữ liệu hoặc xác nhận production retention/deletion.

PR-40 (50 concurrent users, p95≤2s, RPO≤15min, RTO≤4h) vẫn **NOT_VERIFIED** nếu chưa benchmark/restore drill. Tests in repo chứng minh điều kiện fixture cụ thể; source, BE executed, mock executed và external/deployment evidence phải ghi tách biệt. Offline queue survives restart/kill, camera/storage failure, device key/decrypt và live FastAPI/model accuracy vẫn **PENDING_ENVIRONMENT_E2E**.
