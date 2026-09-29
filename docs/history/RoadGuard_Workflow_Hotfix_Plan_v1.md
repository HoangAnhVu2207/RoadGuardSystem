# RoadGuard Workflow Hotfix Plan v1

Ngày: 22/09/2026. Trạng thái: đề xuất đợt hotfix cho các task đã `Done` bị ảnh hưởng bởi yêu cầu Reporter, IncidentCase, route/segment, edge coverage và AI ngoài.

## Nguyên tắc

- Không đổi trạng thái lịch sử của task `Done` và không sửa worklog để giả định tính năng mới đã được nghiệm thu.
- Hotfix là một đợt thay đổi mới, có scope card, owner, dependency, migration/backward-compatibility plan và verification riêng.
- Không thêm package, migration, enum seed hoặc đổi hợp đồng production nếu slice chưa được owner duyệt.
- Giữ tương thích với dữ liệu khảo sát/repair cũ; mọi kết quả cũ vẫn trỏ về `RoadSectionVersion` và `RoadSegmentSet` cũ.

## Các task Done bị ảnh hưởng

| Owner | Task lịch sử | Delta cần hotfix |
|---|---|---|
| Person 1 | P1-02, P1-10, P1-11, P1-12 | Reporter role/claim, report scope, ảnh có GPS riêng và quyền timeline |
| Person 1 | P1-20, P1-21 | Polyline cong, route capture/import, station và segment-set version |
| Person 1 | P1-22, P1-23, P1-30 | Incident-triggered survey, band/coverage, manifest theo segment/video interval |
| Person 1 | P1-31, P1-32 | AI adapter async ngoài, candidate detection và PM decision boundary |
| Person 1 | P1-40, P1-41, P1-42 | Chọn drone hoặc field check; tách `Defect.VERIFIED` khỏi `IncidentCase.VERIFIED` |
| Person 1 | P1-50, P1-51, P1-52, P1-53 | Repair method summary, after-repair evidence và IncidentCase lifecycle |
| Person 2 | P2-10, P2-11, P2-20, P2-21 | Reporter seed/authority, route geometry/version và segment persistence |
| Person 2 | P2-22, P2-23, P2-30 | Incident survey request, target bands, segment/video/telemetry manifest |
| Person 2 | P2-31, P2-32, P2-40, P2-41 | Async processing provenance, detection/case history, conditional measurement |
| Person 2 | P2-50, P2-51, P2-52, P2-53 | Repair method summary, assignment, progress và after evidence |

Các task trên vẫn giữ nghĩa `Done` tại thời điểm đã nghiệm thu. Bảng này chỉ là impact map, không phải lệnh tự động mở lại.

## Bổ sung Reporter self-registration

Luồng Gmail OTP là delta mới, chưa có trong các task `Done`. Không đánh dấu P1-10/P2-10 là đã bao phủ tính năng này. Tạo hai task tiếp theo:

- `P2-13` (Person 2): persistence shape/migration cho `email_confirmed`, `email_confirmed_at`, `registration_source`, `EmailVerificationChallenge`, role REPORTER và SQL constraints/indexes.
- `P1-13` (Person 1): contract/service/API cho register, verify OTP và resend; Gmail adapter/outbox boundary; auth/session issuance sau verify và rate-limit/error mapping.

Gmail OTP là email verification provider flow, không phải Google OAuth. Reporter tự đăng ký chỉ tạo role REPORTER và không tạo ProjectMember.

## Slices hotfix đề xuất

| Slice | Owner | Kết quả | Dependency | Verification |
|---|---|---|---|---|
| HF-01A | Person 1 + Person 2 | Chốt contract Reporter, IncidentCase, role=5 target, status/timeline và error codes | ADR 005, scope doc, current source | Link/diff/contract review; chưa migration |
| HF-01B | Person 2 | Entity/mapping/migration cho report photo GPS, case history, route/segment set version, band/manifest facts | HF-01A; SQL owner approval | SQL Server/Testcontainers, pending-model, migration downgrade/reapply |
| HF-01C | Person 1 | API/service cho Reporter submit, PM receive/verify/no-defect reason, route/segment draft/publish, repair summary | HF-01B | API build, focused API tests, `.http` smoke, authorization/idempotency |
| HF-01D | Person 1 + Person 2 | AI async adapter/worker, manifest/result validation, retry/dedup/late result và coverage per band | HF-01B; existing ProcessingJob boundary | Adapter contract tests, worker restart/late-result and SQL persistence tests |
| HF-01E | Both | Compatibility, seed/permission, docs, FE contract and release evidence | HF-01B–D | Affected-project breadth; full solution only for release/owner request |
| HF-02A | Person 1 + Person 2 | Chốt contract register/verify/resend, Gmail-only policy, pending account, OTP/error/security rules | ADR 002 amendment, US-27, current Identity source | Documentation/contract review; chưa migration |
| HF-02B | Person 2 | Persist pending Reporter, email confirmation and hashed OTP challenge; seed role catalog only | HF-02A; P2-10 identity boundary | SQL Server/Testcontainers, migration upgrade/downgrade, concurrency/rate-limit persistence |
| HF-02C | Person 1 | Implement anonymous register/verify/resend endpoints, deterministic fake sender and Gmail adapter contract | HF-02B; P1-10 auth service | API build, focused API tests, `.http` smoke, idempotency/abuse cases |
| HF-02D | Both | Provider secrets/config, outbox delivery, session issuance after verify, FE contract and release evidence | HF-02B–C | Affected-project breadth; real Gmail sandbox only in integration/release environment |

## Điều kiện phát hành hotfix

1. Reporter chỉ xem report/timeline/media thuộc quyền; ảnh thiếu tọa độ hợp lệ bị từ chối hoặc yêu cầu bổ sung rõ ràng.
2. PM có thể chọn drone hoặc Repair Crew; AI `no detections` không tự tạo kết luận không lỗi.
3. Segment 100 m và độ dài tùy chỉnh hoạt động trên tuyến cong; bộ đã công bố bất biến và job cũ không bị đổi phạm vi.
4. Left/right được hiểu theo chiều tăng lý trình; coverage thiếu một band vẫn hiển thị là thiếu, không coi là đạt.
5. `IncidentCase` đi đủ trạng thái và giữ audit; `Defect.VERIFIED` không bị đổi nghĩa.
6. Repair chỉ lưu phương án tổng quát; không xuất hiện financial fields hoặc schema chi tiết vật liệu/giai đoạn thi công.
7. AI retry cùng manifest không nhân đôi job/detection; result sai version, late hoặc payload khác idempotency bị từ chối.
8. Migration có đường nâng cấp/hạ cấp, seed role/permission được kiểm tra và dữ liệu cũ vẫn đọc được.
9. Reporter đăng ký bằng Gmail không nhận token trước OTP; OTP hash/expiry/attempt/cooldown/replay và provider failure được kiểm thử; Google OAuth không xuất hiện trong MVP.

## Rủi ro cần theo dõi

- Dữ liệu cũ không có route polyline hoặc photo GPS: cần trạng thái thiếu dữ liệu và backfill có phê duyệt, không suy đoán.
- Đổi segment set làm thay ID; phải dùng lý trình/geometry mapping có provenance và PM review khi mơ hồ.
- Video lớn và AI ngoài có thể làm worker nghẽn; đo queue/attempt trước khi chọn broker hoặc package mới.
- Thay đổi role/claim ảnh hưởng cache token; phải test revoke/session và project membership trên request kế tiếp.
