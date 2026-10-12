# Staff/Membership Read — assigned implementation and handoff

TARGET_CONFIRMED: owner directly approved RoadGuard-Staff-Membership-Read-Codex.md on 12/10/2026, including contact visibility. Later chat authorizes local commits of GET Projects, Supervisor-global and this slice on exact branch huy-review; no push/merge/deploy.

Baseline CURRENT_VERIFIED: D:/Project BE/RoadGuardSystem, huy-review, HEAD b70433c2e19431b4cfd27c3e237a2e8c31ba7497. No staged paths; 39 inherited dirty/untracked paths captured with raw files/hashes in external review baseline.json. Preserve all inherited content except additive shared DI registration. Codex is sole writer for this slice, including DI and integrated Postman. No schema/entity/seed/auth pipeline changes.

## Implementation plan

Architecture: Controller → IStaffReadService → IStaffReadRepository, separate staff directory and project roster projections. Existing UsersController account GET gets a dedicated contact detail DTO; shared ActorResponseDto and mutation/me responses unchanged. All read methods opt into cookie individually. Authoritative principal/account/role and PM project validity checked before SQL paging; current member/primary projection joins current account and returns that same membership rowversion. No write commands added.

- [x] Add real-host/SQL HTTP regression tests; prove missing routes/contact behavior RED.
- [x] Implement data-only DTOs, scoped repository, service query validation/filter-bound cursor and controller methods. Reuse enum codes, UTC TimeProvider and safe error convention (PM outside scope403).
- [x] Verify current/history/privacy, null contacts/PM, stable paging and invalid filters, real bearer/cookie, revocation and GET→PUT→GET/stale version chain.
- [x] Add additive Postman Nhân sự và phân công folder; no embedded credentials, preserve existing requests/environment tokens.
- [x] Two self-review passes, independent read-only review/fix and rerun invalidated focused checks; final standard analyzer-enabled build.
- [x] Prepare explicit per-file staging and review inventory on huy-review; final local commit/source hashes and ZIP recorded in the external handoff manifest after committing this document.

Review focus: directory Reporter exclusion and personal-contact separation; PM nonprimary/effective membership boundaries; cross-project cursor reuse and filter binding; membership vs account/project rowversion; future/ended primary assignment and nullable contact/PM. Tests pin each boundary. Implementation follows owner-approved external specification; no renewed design approval required.

Inherited GET Projects + Supervisor-global committed unchanged from pre-task snapshot as `cb612bd40ea6c984a9b5a3145d3c4c9199c49cc9` (39 paths). This Staff slice is a separate child commit; exact final SHA is in the external review manifest, avoiding a self-referential hash. The two DI Staff additions were excluded from that inherited commit and belong to this delta. No source edits after the final test/build except documentation/Postman/tooling, verified separately.

## Delivered contract and verification (CURRENT_VERIFIED)

| Surface | Actor | Contract |
|---|---|---|
| GET /api/v1/users | current ACTIVE Supervisor only | search name/email; role SUPERVISOR/PM/OPERATOR/CREW/REPORTER; status ACTIVE/SUSPENDED/PENDING; default internal staff all statuses, Reporter only explicit role |
| GET /api/v1/projects/{projectId}/members | global Sup or effective scoped PM (no IsPrimary requirement) | search name only; role as above; includeHistory bool defaultfalse; status ACTIVE/ENDED only Sup history; current memberships/account ACTIVE and matching current role otherwise |
| GET /api/v1/projects/{projectId}/primary-project-manager | same as members | no query/body; primaryProjectManager nullable; single joined membership/user projection, IsPrimary PM ACTIVE/effective/current ACTIVE PM |
| GET /api/v1/users/{userId} | current ACTIVE Supervisor only | existing id/displayName/role/version and ETag retained; email/phoneNumber nullable added using dedicated account-read DTO |

All four GETs support real web cookie and bearer through method metadata; mutations and global auth selector unchanged. No body. Invalid query400 validation_error; unsupported role/PM outside project403 access_forbidden; missing project404 project_not_found; missing account404 identity_user_not_found; unauthenticated/revoked/suspended/current-role-mismatch401 through existing authoritative pipeline. Existing valid empty scope returns200 items=[], primary missing valid head returns200 primaryProjectManager:null.

Directory item: userId,displayName,email,phoneNumber,role,status. Members item: membershipId,userId,displayName,role,isPrimary,status,validFrom,validTo,membershipVersion. Primary item: membershipId,userId,displayName,validFrom,validTo,membershipVersion. List envelope: items,nextCursor,asOf. camelCase/nulls follow existing serializer. Paging limit20 default/range1..100; search trimmed max255, role/status trimmed uppercased API codes; numeric/unknown rejected. Cursor opaque version1 base64 GUID + SHA256 binding of actor/surface/project/history/filter; SQL ascending unique ID ordering (SQL Server GUID semantics), filters/current facts before Take(limit+1), no totals/pageSize, no N+1. Omit cursor on first page; empty/invalid or changed-scope/filter cursor400. asOf is request observation time; no frozen multi-request snapshot/version guarantee.

membershipVersion is the exact projected ProjectMember.RowVersion, base64; confirmed consumable unchanged by existing PUT ExpectedCurrentMembershipRowVersion. Current primary identity/name/version come from one joined SQL projection, not separate head reads. Date boundaries inclusive UTC DateOnly from injected TimeProvider. Existing PUT EffectiveFrom/required role/state/locks/concurrency/idempotency unchanged. Stale-head test mutates only a fixture-owned valid head's ValidTo to simulate a concurrent persisted change; stale PUT409, refreshed GET version then valid current-date PUT200 and GET reflects new membership. No shared DB touched.

### Execution evidence

- RED real-host/SQL: 15/15 new cases failed for missing read routes before implementation; staff-red.trx retained.
- Initial green: 14/15, one test assembled duplicate search query; corrected request to change the actual bound filter. Then15/15 PASS. Strengthened test fixtures and added nullable/nonactive contact + current-role revocation + scoped-PM actual cookie cases.
- Final focused command: `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter 'FullyQualifiedName~StaffMembershipReadHttpTests|FullyQualifiedName~ProjectListTests|FullyQualifiedName~SupervisorGlobalAuthorityHttpTests|FullyQualifiedName~V2AccountFlowTests|FullyQualifiedName~PrimaryProjectManager' --logger 'trx;LogFileName=staff-final-focused.trx' --results-directory <review>/tests` — exit0, **80 PASS/0 FAIL/0 SKIP**: 17 staff +55 ProjectList +3 Sup-global +5 V2AccountFlow. Analyzer defaults enabled, no --no-build/stale binaries or RunAnalyzers=false. Overlapping reruns are not added.
- Final build: `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore -t:Rebuild` — exit0, **0 errors, 222 warnings** under standard repository analyzer configuration. No diagnostics in new Staff service/repository/controller; warnings remain baseline debt visible (including existing migration CA1861). No analyzer/guard suppression changed. Full analyzer-build.log included.
- `node tools/verify-staff-postman.cjs` — exit0. 19 GETs/no bodies/blank placeholder variables; script syntax and fixture transitions verify current-PM exclusion, empty/error selection clearing, null/error head clearing. This is tool-fixture verification, not live Postman acceptance.
- Inherited preservation: 37/39 initial paths byte-identical; two DI files each add exactly one StaffRead registration. Initial staged set empty. Original GET Projects, Supervisor-global implementation and tests unchanged. No schema/entity/migration/seed changes, shared SQL writes, push/merge/deploy. Tests are production DI/auth HTTP TestServer + real disposable SQL, not live staging/browser/external acceptance.

### Two self-review passes and independent review

Pass1: traced controller→service→SQL; checked Sup-only contact exposure and default Reporter exclusion, scoped nonprimary PM membership validity, history isolation, exact membership rowversion, single-head primary projection, scope/filter before paging and safe errors. No command changes needed. Added tests for null contacts/nonactive directory and PM cookie/suspension; stale simulation retains all existing PUT preconditions.

Pass2: checked whole candidate boundaries, original39 hashes and two additive DI lines; shared ActorResponseDto/me/PATCH unchanged, method-only cookie/no bearer fallback changes, Postman actor/target variables untouched and environment not modified. Scripts clear stale selection/head so optional writes cannot reuse a failed search. Candidate paths scoped to the three authorized packages. No sensitive data or runtime artifacts staged; raw review artifacts kept outside repo.

Independent reviewer (read-only fresh context, separate from self-review): found P2 stale Postman PM selection on empty/error search. Fixed and added fixture validator, also excludes current primary after read and clears stale head on failure. Follow-up reported no remaining actionable findings, no backend defect. Reviewer did not run runtime checks; ChatGPT external review of the delivered package remains pending, not claimed as accepted.

### Postman and handoff

Canonical read collection docs/postman/RoadGuard-Staff-Membership-Read.postman_collection.json; separate optional existing PUT collection docs/postman/RoadGuard-Primary-PM-Optional-Write.postman_collection.json. Additive Nhân sự và phân công folder in Downloads/testing/RoadGuard-ANH-Manual.postman_collection.json; all9 original folders preserved, environment/tokens unchanged. No request bodies on GET or embedded credentials. First action after existing H00 login: **01 — Chọn dự án (Supervisor)**. Guide docs/postman/Staff-Membership-Read-Guide.md includes the acceptance chain, UTC EffectiveFrom limitation and response fixture with no contact data.

Review bundle outside repo: C:/Users/HoangAnhVu/Downloads/testing/Staff-Membership-Review-2026-10-12. Includes pre-task snapshot, inherited/new delta patches with untracked files, all candidate source/new files and hash manifest, TRX/portable check report/build log, Postman and README. Final commit SHAs/source tree hashes recorded externally after local commit to avoid self-referential hashes. Local commit authorized by latest owner chat; no push/merge/deploy.

## Approved specification (original; latest chat additionally authorizes local commits)

# RoadGuard — một task hoàn chỉnh: đọc nhân sự, membership và PM chính

## 1. Assignment đã được người dùng chốt

Người dùng chốt toàn bộ thiết kế trong chat ngày 12/10/2026, bao gồm quyền Supervisor xem email và số điện thoại ở API tài khoản. Thực hiện từ đầu đến cuối trong một phiên: focused preflight → implement → focused verification → self-review/fix → Postman → gói bàn giao để ChatGPT review. Không chỉ viết báo cáo hoặc hỏi lại các lựa chọn đã chốt. Không giao ngược cho người dùng nhiều prompt nhỏ.

Mục tiêu: người dùng mới đăng nhập có thể lấy đúng userId để chọn PM/Operator/Crew, đọc membership của dự án và lấy đúng current primary-PM membershipVersion trước PUT đổi PM. Không cần chép ID/version từ SQL, seed hoặc người thao tác trước.

### Ba API trong task

| Method | Endpoint | Mục đích |
|---|---|---|
| GET | /api/v1/users | Directory tài khoản cho Supervisor |
| GET | /api/v1/projects/{projectId}/members | Roster/current memberships theo quyền |
| GET | /api/v1/projects/{projectId}/primary-project-manager | PM chính hiện hành và membership rowversion |

Ngoài ra đối chiếu và bổ sung **field email/phoneNumber ở GET /users/{userId} nếu response hiện tại thiếu** để đáp ứng quyền xem chi tiết tài khoản của Supervisor. Đây là delta read/projection đã được chốt, không là task sửa thông tin liên hệ hoặc thay đổi credentials.

## 2. Preflight và preservation

Đọc AGENTS.md/rules áp dụng. Ghi đúng repo root, branch, HEAD, staged/unstaged/untracked paths. Session trước làm trên `huy-review`, HEAD b70433c2e19431b4cfd27c3e237a2e8c31ba7497; đây là tham chiếu, không lệnh checkout/reset.

Đọc phần cần thiết của GET-PROJECTS.md, SUPERVISOR-GLOBAL-PROJECT-AUTHORITY.md và báo cáo đối chiếu local nếu có. GET Projects và Supervisor global/cookie đã được làm local, có thể chưa commit. Lấy HEAD cộng working changes làm baseline, không chỉ đọc remote b70433c2. Không overwrite, revert, stage hoặc sửa lại task trước ngoài seam trực tiếp cần reuse. Không rerun audit toàn sản phẩm.

Các báo cáo trước chưa có gói raw diff đầy đủ và lệnh test cuối có RunAnalyzers=false. Trong task này cung cấp final build bật analyzers theo cấu hình chuẩn và gói diff/file mới cho **toàn candidate được review**, phân biệt phần inherited và phần task này. Không yêu cầu chạy lại toàn bộ 240 tests chỉ để hoàn tất paperwork; chỉ rerun checks bị source mới làm mất hiệu lực.

## 3. Quyết định quyền đã chốt

- ACTIVE/current Supervisor có quyền đọc directory và thông tin tài khoản toàn hệ thống. Supervisor đọc members và primary PM của mọi dự án, không cần ProjectMember riêng.
- PM chỉ đọc members/primary PM ở project có membership ACTIVE, đúng current role, đã đến ValidFrom và chưa hết ValidTo theo convention UTC/TimeProvider hiện hành. Không tự thêm IsPrimary restriction cho PM read.
- Crew/Operator/Reporter không đọc directory/roster/primary-PM endpoint mới. API task/snapshot riêng của họ không thay đổi.
- PM không được dùng GET users hoặc GET users/{userId} của Supervisor để xem contact cá nhân.
- Account/session/current role revocation vẫn có hiệu lực; không chỉ dựa role client/JWT cũ nếu pipeline có authoritative validation.
- Các GET mới phải hỗ trợ **bearer và web-cookie login** đúng pattern đã có. Cookie method eligibility đúng endpoint; không mở cookie toàn cục, không sửa pipeline để suy Supervisor từ request body/query.
- Quyền read mới không cấp quyền assign/update hay bỏ state/version checks của commands hiện có.
- Notification vẫn recipient-scoped, không mở rộng dispatch hoặc tạo membership giả cho Supervisor.

## 4. Contract đã chốt

### 4.1 GET /api/v1/users

Supervisor-only. Không request body. Default trả nhân sự nội bộ theo các role thực tế hiện có (Supervisor, PM, Operator, Crew). Reporter chỉ được liệt kê khi Supervisor **lọc rõ role Reporter**; không xuất hiện trong default staff directory. SSO, team entity hoặc account creation mới ngoài scope.

Query: search, role, status, limit, cursor. search tìm tên/email theo quyền Supervisor; trim/validation/collation dùng conventions thật. role/status theo API codes của enum/model hiện có, reject unknown/invalid thay vì đoán numeric enum. Không status filter nghĩa Sup có thể quản trị các status hợp lệ của nhân sự, không tự ẩn suspended accounts khỏi directory. Khi chọn người nhận việc, client lọc ACTIVE; service mutation hiện hành vẫn kiểm authoritative state.

limit default20, range1..100; cursor opaque theo GET Projects/conventions hiện có, stable order và unique tie-breaker; filter phải được bound/validated phù hợp. Response items, nextCursor, asOf. Không thêm page/pageSize/totalCount thứ hai trong task này. Client không dùng cursor/asOf như command version hoặc quyền.

Item: userId, tên hiển thị theo field/API naming hiện hành, email, phoneNumber nullable nếu không có, role, status. Không trả password hash, OTP, token/cookie, security stamp/version, device keys hoặc nội dung internal claims. Nếu model đã có PhoneNumber thì dùng dữ liệu đó; không bịa số điện thoại, thêm phone column/migration hoặc normalize/write account trong GET.

GET users/{userId} hiện có vẫn chỉ Supervisor; bổ sung email/phoneNumber read nếu thiếu. Giữ status/error/ETag account version hiện hành. Rà shared DTO callers trước thêm field để không vô tình trả contact qua /me/task/member response hoặc phá constructor/serialization. Có thể dùng DTO riêng ở responsibility đúng nếu shared DTO không an toàn.

### 4.2 GET /api/v1/projects/{projectId}/members

Supervisor toàn project hoặc current scoped PM. No body. Query search, role, limit, cursor; includeHistory default false. status filter chỉ dùng để xem lịch sử với Supervisor/includeHistory=true; không để filter mở rộng default current quyền. Ghi tên param/types/validation cụ thể vào contract final theo quyết định này.

Default chỉ trả memberships ACTIVE/effective, account ACTIVE và current account role khớp membership role. Membership future/expired/ended, account suspended hoặc role mismatch không là assignment candidate hiện hành. PM chỉ nhận danh sách hiện hành. Supervisor có thể includeHistory=true để xem các membership của dự án, gồm inactive/future/ended; giữ snapshot membership role/status/valid dates, không gán chúng là current quyền.

PM yêu cầu includeHistory=true bị từ chối theo role convention, không âm thầm bỏ tham số. Invalid combination/status/query trả validation error theo convention. search ở members tìm tên hiển thị, không search email/phone rồi lộ membership contacts gián tiếp. role dùng membership role; default current đã yêu cầu current account role khớp. Bộ lọc chỉ thu hẹp scope, không lấy userId/role trong query làm actor.

Response items,nextCursor,asOf. Item fields đã chốt: membershipId, userId, tên hiển thị, role, isPrimary, status, validFrom, validTo nullable, membershipVersion. Không email/phoneNumber. Một user có nhiều historical memberships thì giữ membership identities; default chỉ current facts theo model, không Distinct userId làm mất rowversion. Không tạo row giả cho global Supervisor không có membership.

membershipVersion là ProjectMember.RowVersion đúng record, không Project.RowVersion hay ApplicationUser.RowVersion. Với history, version là record lịch sử tương ứng, không được dùng nó làm current PM command token.

### 4.3 GET /api/v1/projects/{projectId}/primary-project-manager

Supervisor hoặc scoped PM như trên. No body/query cần thiết. Project tồn tại nhưng không có primary PM hợp lệ → 200, primaryProjectManager:null. Project không tồn tại →404. Object ngoài scope/unsupported role theo error convention an toàn hiện hành, không lộ tên/PM project khác.

Response primaryProjectManager nếu có: membershipId, userId, tên PM, validFrom, validTo nullable và membershipVersion. Chọn IsPrimary + membership role PM ACTIVE/effective + account current ACTIVE PM. Không lấy PM cũ từ field project hoặc record ended/future. Không trộn version/hash token khác. Thiết kế projection phải có snapshot đọc nhất quán, tránh user/version lấy từ hai head khác nhau.

membershipVersion phải dùng được nguyên giá trị trong body `ExpectedCurrentMembershipRowVersion` của PUT primary-project-manager hiện tại. Không đổi PUT sang If-Match hoặc đổi EffectiveFrom semantics dưới task read này. GET members cũng phải trả đúng cùng current record/version để người dùng nhận diện.

### 4.4 Errors và privacy chung

401 khi chưa auth/session không hợp lệ; unsupported role403; invalid query400; missing project404. Ngoài scope dùng convention hiện có, ghi đúng status/code và test, không tự sửa global error policy. Nếu project hợp lệ không có member phù hợp →200 items=[]. Các fields dùng camelCase/nullability theo serializer thật.

Filter quyền/current validity trước cursor/order/Take. Không paging trước scope rồi service lọc gây empty page giả. Không N+1 authorization queries. CancellationToken và TimeProvider đúng convention. Null contact/PM không gây500. Không log/export PII thật trong test report hoặc handoff.

## 5. Implementation boundaries

Giữ Controller → IService → IRepository. DTO data-only; services orchestration/actor policy; repository EF SQL/scoped facts theo kiến trúc của repo. AsNoTracking/projection, server-side query/read matching existing seams. Không package/SDK upgrade, MediatR/AutoMapper/new architecture hoặc expose IQueryable qua interface.

Tạo public types ở responsibility phù hợp; reuse current role parser/auth/error/DI/time/cursor seams. Có thể thêm DI registrations additive. Không sửa entity/mapping/migration/seed để làm read. Nếu thiếu dữ liệu phone thì trả null; nếu gặp model invariant genuinely không cho thực hiện decision đã chốt thì báo một blocker cụ thể và tiếp tục phần độc lập, không tự tạo thiết kế mới.

Ngoài scope: POST add/end membership, sửa account/contact, invite resend/revoke, repair package/context API, policy/catalog/AI selectors, deploy, commit/push/merge, broad refactor. Không tự phân công một API cho actor khác vì “cấp cao hơn”.

## 6. Focused acceptance tests

Viết meaningful real-host/SQL-backed tests cho query scope, role/current membership và version. Dữ liệu fixture-owned disposable DB; không sửa DB local/shared/staging hoặc official seeder để test dễ hơn.

1. Supervisor không membership vẫn đọc directory/members/primary-PM của project khác; no-auth401; PM/Crew/Operator/Reporter bị chặn directory.
2. Directory default không Reporter, role Reporter explicit chỉ Sup đọc; search/role/status không mở quyền; nullable contacts. GET account detail Sup đọc email/phone nếu có; các role khác không lấy được contact qua shared DTO hoặc members.
3. PM-A đọc current members/primary PM project A, không đọc project B hoặc lịch sử. Future/ended/expired/wrong-role PM membership deny; current account/session revoked/suspended deny request tiếp theo.
4. Default members không trả suspended/role-mismatched/expired/future/ended candidates. Supervisor includeHistory giữ identity và validity/version đúng; PM includeHistory bị từ chối. Mỗi membership có đúng RowVersion.
5. Primary PM chọn đúng current IsPrimary ACTIVE/effective account PM, không fallback stale/future. Project tồn tại không hợp lệ →null; missing project404. Boundary ValidFrom/ValidTo theo date convention hiện có.
6. Stable cursor paging, filter binding và scope trước paging; invalid limit/cursor/filter400. Không duplicate current records do join, không bỏ dữ liệu hợp lệ vì scope lọc sau page.
7. **Chuỗi quan trọng:** Sup GET projects → chọn project → GET users chọn PM khác ACTIVE → GET primary-PM lấy membershipVersion → PUT đổi PM hiện có bằng version đó → GET primary-PM phản ánh membership mới. GET version stale → PUT bị conflict theo contract. EffectiveFrom fixture phải hợp lệ theo service hiện tại; không nới rule để test xanh.
8. Bearer và actual web csrf/login-cookie (không bearer) cho cả ba GET; cookie account detail nếu surface này là web action trong phạm vi read. No cookies/tokens forged thay actual auth pipeline.
9. Existing GET Projects/Supervisor-global/contact detail and affected account/reassign contracts không bị regression. Không chạy lại full suite vì thêm vài read APIs; dùng focused impacted checks, giữ counts distinct không cộng rerun trùng.

Final build bật analyzers theo cấu hình repo chuẩn, không RunAnalyzers=false như bằng chứng build cuối. Ghi đúng exit code/errors/warnings; không tắt analyzers/guard hoặc weaken tests để báo PASS. Nếu test binaries cần rebuild, không dùng stale --no-build. SQL/Docker unavailable ghi BLOCKED_ENV, không mock-source thành real SQL PASS.

## 7. Postman và hướng dẫn người dùng

Cập nhật additive collection manual đang dùng ở Downloads/testing nếu có, đồng thời repo canonical phù hợp. Không ghi đè old requests/environment/current tokens. Nếu người dùng đang dùng Bruno, ghi nhận nơi import nhưng không yêu cầu tạo thêm hệ thống test mới.

Folder tiếng Việt: “Nhân sự và phân công”. GET no body, query hiển thị rõ; không {{requestBody}} hoặc hidden opt-in flags. Roles dùng biến token hiện có; thêm biến chưa có dưới dạng placeholder rỗng, không credentials/PII thật committed.

Requests: Supervisor users default/filter Reporter/search; Supervisor/PM members; Supervisor history; primary PM current; noauth/wrongrole; Sup account detail contact. Lưu userId được chọn và current membershipVersion riêng; không overwrite actorUserId/target account bằng login self rồi sửa fixture. Có paired GET/read sau đổi PM nếu người dùng chọn test PUT; thao tác PUT existing không tự chạy default whole collection.

User experience: chọn environment → login → GET projects → chọn projectId → GET users/members → GET primary PM. Body PUT cần version có thể lấy từ GET tự động. Không bắt người dùng nhập GUID từ SQL/copy JSON dài hoặc hiểu scripts để bấm. Hướng dẫn đúng file import và đúng request đầu tiên; file collection không tự reload Postman. Summary không log email/phone thật/token/password/cookie. Các mẫu handoff dùng fixtures hoặc redact contact.

## 8. Review, handoff và stop condition

Tự review hai lượt (scope/SQL/privacy/version rồi wire/auth/preservation/impact). Sửa lỗi có căn cứ trong slice này và verify phần bị invalidate; không thêm requirement mới. Nếu chạy independent review theo skill/rules, ghi đúng outcome, không gọi self-review độc lập.

**Bắt buộc bàn giao gói review thật, không chỉ danh sách file/report:**

- STAFF-MEMBERSHIP-READ.md: scope, exact final contract/query/status/fields, actor matrix, branch/HEAD, inherited changes vs own delta, tests/build results, limitations.
- Patch/diff của **delta task này** so với snapshot ngay trước task; có cả new/untracked files, không chỉ git diff tracked.
- Toàn bộ file mới task này và source vùng sửa trực tiếp, kèm manifest/hash đối chiếu patch.
- Candidate diff HEAD→working của các task trước liên quan (GET Projects và Supervisor-global) nếu còn uncommitted, kèm nhãn INHERITED, để ChatGPT không tưởng chúng do task này. Đừng bỏ new files inherited khỏi candidate inventory.
- TRX của focused runs hoặc portable report chứa command/filter/config/source/time/pass/fail/skip distinct, cùng final analyzer-enabled build log. Không rerun test còn hiệu lực chỉ để lấy file.
- Collection Postman cập nhật và ví dụ JSON fixtures đã redact; README ngắn cách reproduce acceptance chain.

Có thể đóng gói một ZIP review không chứa bin/obj, DB dumps, .env, credentials, real contacts hoặc auth artifacts. Chuỗi hash/source/test phải gắn đúng final working tree, không chỉ HEAD chưa chứa changes.

Kết thúc khi cả ba API và field account detail đã đáp ứng scope, checks phù hợp hoàn tất, artifacts đã có và request đầu tiên đã được chỉ rõ. Nếu chưa có runtime/SQL evidence thì ghi thực tế, không tuyên bố Done/PASS toàn luồng. Chưa deploy/commit/push. Không dừng sau preflight, không hỏi lại “đối chiếu hay triển khai”, không đưa ra chuỗi task mới cho người dùng.

**Tiến hành toàn task này ngay.** Nếu cần một lựa chọn kỹ thuật thường lệ (DTO naming, query precision, files) dùng conventions/code hiện hành và ghi trong contract. Chỉ hỏi khi có xung đột thực sự với decision đã chốt hoặc thiếu dữ kiện không thể xác định từ source, sau khi đã hoàn thành phần độc lập.
