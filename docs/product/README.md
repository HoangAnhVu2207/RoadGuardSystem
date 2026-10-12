# Product authority và phạm vi hiện hành

**ACTIVE · DOCUMENTATION_UPDATED · DOC-AUDIT-01**, 2026-10-12. Quyết định nghiệp vụ và kết quả implementation là hai nguồn khác nhau; passing test không tự chấp nhận nghiệp vụ.

| Nguồn | Phạm vi áp dụng |
|---|---|
| [Confirmed decisions 32–44](confirmed-decisions.md) | Giữ nguyên bản owner reconfirmed; các qualifier, privacy, lifetime, retention và external ownership không bị audit viết lại. |
| [HUY-FINAL-INTEGRATION](../../planning/development/HUY-FINAL-INTEGRATION.md) | Assigned R01–R30, clocks/persistent login/correction/lifecycle; latest ND01/activation supersede các checkpoint cũ đúng phạm vi. Không áp curing/material/traffic-opening từ V2 lịch sử. |
| [GET Projects](../../planning/development/GET-PROJECTS.md), [Supervisor global](../../planning/development/SUPERVISOR-GLOBAL-PROJECT-AUTHORITY.md), [Staff/Membership Read](../../planning/development/STAFF-MEMBERSHIP-READ.md) | Additive owner-approved local scopes, đã có trong hai commit baseline. Supervisor-global không mở PM-only commands. |
| [Requirements](requirements.md), [workflows](workflows.md), [data/quality](data-and-quality.md) | Derived current guide; không bổ sung quyết định mới. Implementation/execution evidence nằm trong audit. |
| [Historical FR/BR](historical-fr-br.md), [US](historical-us.md), [PF](historical-pf.md), [NFR](historical-nfr.md), V2 R3 | Review material; chỉ accepted excerpts có authority mới áp dụng. Các schema/route chưa adopted vẫn PROPOSED. |

Web: Supervisor/PM/Reporter. Android: Crew/Operator và mobile scope. BE: ASP.NET Core, Services/Repositories, SQL/object storage, current rights và durable workflows. FastAPI: nhóm AI bên ngoài; video drone detection không thay physical measurement của Crew. Không hồi sinh photogrammetry/DSM/3D/depth scope.

Reporter sở hữu report/private evidence; publication chỉ cấp projection/evidence cho recipient tương ứng. Supervisor được thao tác Supervisor trên tất cả dự án, PM theo effective membership và task actors theo assignment. Notification luôn recipient-scoped. Đọc [backend hiện hành](../backend/README.md), [ma trận](../audit/DOC-AUDIT-01-MATRIX.md) và [GAP](../audit/DOC-AUDIT-01-OPEN-GAPS.md) để biết phần đã có, partial, pending E2E và chưa xác minh.
