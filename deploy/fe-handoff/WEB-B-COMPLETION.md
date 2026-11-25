# Web B — hoàn thiện matching và kết thúc safety

Ngày 25/11/2026. Base `71d8ae85612fcc11b2b96227cc87981da383e2d5`.
Đọc cùng [contract gốc](WEB-B-READ-CONTRACTS.md). Đây là phần bổ sung BE;
không có thay đổi FE hoặc tự gửi thông báo cho người khác.

## B1 — khoảng cách, CRS và uncertainty

API giữ nguyên `GET /projects/{projectId}/candidates?sourceKind=REPORT&sourceId=…`;
PM có quyền hiện hành, Bearer hoặc Web cookie, GET không CSRF.
`algorithmVersion: report-confirmed-2`; AI vẫn giữ version cũ. Confirmation do PM
chọn evidence/segment qua `matching-location` như contract trước. Không tự chọn GPS
ảnh, gom ảnh, snap, thay bằng centroid hoặc suy rằng hai hồ sơ cùng hư hỏng.

Kiểm kê thực tế: `SpatialConstants`/Project chỉ hỗ trợ engineering WGS84 UTM north
EPSG:32648/32649; GPS dùng EPSG:4326. Native geometry có `CrsProfileRevisionId`
và profile operation/provenance riêng. `CrsProfileEngine` hiện chỉ thực thi
`SAMPLE_AFFINE_TO_WGS84` khi SampleOnly; phép này **không** dùng cho khoảng cách thật.
Tên VN2000, SRID tùy ý, tham số thiếu hoặc profile native không được coi là WGS84.

| Nguồn đã PM xác nhận → đích | Kết quả |
| --- | --- |
| WGS84 GPS → Point EPSG:4326 hợp lệ, route không có custom profile | SQL Server geography STDistance, mét theo ellipsoid; `WGS84_ELLIPSOID_POINT_DISTANCE` |
| WGS84 GPS trong múi → geometry EPSG:32648/32649 hợp lệ, route không có custom profile | Chiếu điểm sang đúng UTM WGS84; NTS Distance tới geometry gốc, mét trên lưới chiếu; `WGS84_UTM_GRID_MINIMUM` |
| GPS thiếu/chưa xác nhận/confirmation stale | Không tính; giữ đúng trạng thái nguồn, không gắn GPS_MISSING sai |
| Profile native/VN2000 hoặc SRID khác | null, `NATIVE_CRS_TRANSFORM_UNAVAILABLE`/`TARGET_CRS_UNSUPPORTED` |
| Thiếu/rỗng geometry; invalid; ngoài miền tọa độ | null, lý do tương ứng |
| Geometry geographic khác Point | null, `GEOGRAPHIC_GEOMETRY_UNSUPPORTED`; chưa có geodesic shape adapter |

Phép UTM là forward ellipsoidal Transverse Mercator (WGS84 a/f, k0=.9996,
false easting=500000m, false northing=0), giới hạn vĩ độ 0–84N và đúng múi ±3°.
Tham khảo [PROJ UTM](https://proj.org/en/stable/operations/projections/utm.html),
[PROJ Transverse Mercator](https://proj.org/en/stable/operations/projections/tmerc.html).
Vector kỳ vọng được tính độc lập bằng PROJ, gồm cả hai múi và sát biên; test cho phép
sai số số học 1cm so với vector. Đây không phải cam kết accuracy nguồn hoặc khoảng
cách mặt đất chính xác mọi nơi. Khoảng cách tới LineString/Polygon/geometry UTM là
khoảng cách tối thiểu tới hình thực, bằng 0 nếu điểm thuộc vùng; không dùng centroid.
Không loại ứng viên theo radius, không dựng adjacency bằng khoảng cách.

Một item thêm:

```json
{
  "distanceMeters": 10.000000003,
  "accuracyMeters": null,
  "distanceStatus": "MEASURED",
  "distanceMethod": "WGS84_UTM_GRID_MINIMUM",
  "overlapStatus": "SOURCE_UNCERTAINTY_UNAVAILABLE"
}
```

`distanceMeters`/`accuracyMeters` nullable theo dữ liệu. `distanceMethod` bỏ qua khi
không đo được; `accuracyOverlap` bỏ qua khi chưa xác định, **không dùng false/0 thay
UNKNOWN**. `accuracyMeters` là accuracy đã lưu của evidence nguồn được chọn, không
phải accuracy của Defect hay độ chính xác phép chiếu. Defect hiện không lưu bán kính,
mức tin cậy, sai số tương thích có provenance; không tìm thấy số đo thật bị bỏ khỏi
schema. Không thêm cột hay dữ liệu giả. Dù source có accuracy, overlap vẫn chưa biết
và trả `TARGET_UNCERTAINTY_UNAVAILABLE`; source thiếu trả `SOURCE_UNCERTAINTY_UNAVAILABLE`.
Khoảng cách đo được không có nghĩa uncertainty đầy đủ hoặc đủ giống để gộp.
Cursor hash bao gồm kết quả/trạng thái phép đo, cùng source/geometry/target version.

## B12 — PM xác nhận kết thúc safety

Root `M=/api/v1/projects/{responsibilityProjectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}`.

- Khi chỉ safety được chuyển sang B, dùng GET `/projects/{B}/lifecycle` sau reload:
  chọn `responsibilities[]` đúng `obligationId`, lấy `safetyContext` gồm
  `{packageId,itemId,measureId,formalObligationId}` để dựng M bằng URL B.
  Context chỉ có khi trách nhiệm safety hiện hành thuộc project đang đọc và lineage
  measure/item/package khớp duy nhất. Thiếu proof thì bỏ trường, không chọn đại ID.
  `currentRepairItemId` không bị thay bằng item formal: nó vẫn đúng nghĩa cũ và có
  thể null trên safety. Không dùng context mới để mở/ghi formal item A. Sau chuyển
  tiếp, project cũ không nhận context dưới trách nhiệm đã mất; GET M kiểm lại quyền.
- `GET M`: dùng phiên mới sau reload, nhận ETag measure và `obligation.version`.
- **POST `M/resolution`**: PM có quyền hiện hành tại project đang chịu trách nhiệm
  **chính safety**. Supervisor/Crew không có quyền quyết định. Bearer hoặc Web cookie;
  cookie cần `X-CSRF-TOKEN` từ `/auth/web/csrf`, `credentials: include`.
- Header bắt buộc `If-Match` của GET M, `Idempotency-Key` riêng. Body dùng
  `obligationVersion` từ GET M, không dùng item/task/package version thay thế.

```json
{
  "checkId": "<currentCheckId từ GET M>",
  "outcome": "REMOVED",
  "conditionSatisfied": true,
  "checklistConfirmation": "Đã đối chiếu điều kiện tháo dỡ đã lưu và xác nhận hiện trường an toàn",
  "reason": "Xác nhận kết thúc biện pháp trong đúng phạm vi",
  "evidenceIds": ["<evidenceLinkId của current check, không phải fileId>"],
  "obligationVersion": "<obligation.version từ GET M>"
}
```

`REMOVED` xác nhận RemovalCondition; `REPLACED` xác nhận ReplacementCondition đã
lưu trên biện pháp. Domain hiện lưu điều kiện dạng văn bản, chưa có bộ máy diễn
giải tiêu chí đó. Lệnh lưu attestation rõ của PM + checklist + evidence, không tự
coi formal final hay Safe check là đủ để resolve. Thiết kế tối thiểu yêu cầu đã
installation, check hiện hành `Safe` có chứng cứ verified đúng lineage, không còn
Danger warning chưa ACK; PM đối chiếu điều kiện văn bản rồi xác nhận rõ. Không có
ngưỡng hay phép xác minh vật lý tự phát sinh.

Chứng cứ phải là subset evidence link của check hiện hành, giữ fileId/version/hash,
capture/uploader và đúng binding/item/project nguồn. Quyền PM theo responsibility
safety, không bắt Crew mất assignment phải xác nhận lại check lịch sử. Phiên và
trách nhiệm PM vẫn kiểm lại trong cùng serializable transaction và khi replay.

Lần đầu **201**, replay đúng key/body **200**, cùng resolution ID, không thêm audit.
POST không resolve formal. Sau thành công: `GET M` có `obligation.resolved=true`
và `resolution` (nullable/omitted với dữ liệu cũ) chứa ID, actor/time/outcome/check,
reason, condition, checklistConfirmation, versions đã đối chiếu, evidence snapshots.
Metadata evidence không thay quyền tải file. GET measure history của PM/Sup vẫn
đọc dưới trách nhiệm safety hiện hành; Crew không đọc/ghi measure resolved. Collection
`temporary-safety-obligations` giữ unresolved/chưa dùng và formal NORMAL; không dùng
history để chọn hoặc mở lại nghĩa vụ cũ. Formal chuyển không tự chuyển safety.

| HTTP/code | FE xử lý |
| --- | --- |
| 400 validation_error | Thiếu/không hợp lệ attestation, outcome, evidence/version |
| 401 / 403 access_forbidden | Phiên/quyền PM; cookie thiếu/sai CSRF trả `403 csrf_failed` từ middleware |
| 404 not_found | Sai package/item/measure hoặc không còn trách nhiệm safety |
| 428 precondition_required | Thiếu If-Match/key |
| 409 concurrency_conflict | GET lại measure/obligation, PM đối soát trước lệnh mới với key mới |
| 409 safety_resolution_not_ready | Chưa install/current Safe check, hoặc còn warning chưa ACK |
| 409 safety_resolution_evidence_conflict | Link/file/version/hash/purpose/lineage không khớp |
| 409 safety_already_resolved | Đã có resolution; đọc lại, không tạo lượt kết thúc mới |
| 409 idempotency_key_reused | Cùng actor/project/operation/key nhưng khác payload |

Timeout: đối soát GET M, retry request đã lưu với **cùng key/body/If-Match gốc**;
receipt kiểm quyền hiện hành, không kiểm stale version như lệnh mới. Hai request
đồng thời serialize bằng khóa package/item/obligation; chỉ một resolution bền vững.
Resolve lưu cùng transaction: immutable audit snapshot → FK head safety → touch
measure version → receipt. Retention inventory có reference riêng tới các file của
resolution; không cấp quyền xóa. Không có tự resolve/bulk/reopen dữ liệu cũ.

Migration mới `20261125072411_TemporarySafetyResolution`: một cột nullable FK tới
audit bất biến + index + trigger bảo vệ head; không sửa migration đã phát hành,
không backfill/đổi formal decisions. Snapshot/catalog giữ 198 bảng, 15 migration.
Down bị chặn nếu đã có resolution. Trước áp dụng: backup checksum/VERIFYONLY →
restore DB riêng và kiểm nâng cấp → migration → backend → FE phiên Web mới.

## Ví dụ readback thực từ DB/MinIO test cô lập

Trích GET measure sau resolution và đăng nhập PM phiên mới. Đây là UUID/rowversion
của fixture đã thực thi, không phải ID để gọi trên DB FE. Dữ liệu không chứa URL ký
sẵn hoặc credential. Dùng ID thật từ GET của môi trường đang kiểm.

```json
{
  "id": "052af74c-a99f-4431-9f23-a98db80e95e8",
  "version": "AAAAAAAACDY=",
  "obligation": {
    "resolved": true,
    "originProjectId": "6f4d9e9b-48d7-47fd-b6df-538ba0ba51c0",
    "responsibilityProjectId": "6f4d9e9b-48d7-47fd-b6df-538ba0ba51c0",
    "version": "AAAAAAAACDQ=",
    "scope": {
      "physicalRoadId": "1a379abd-21a5-49f6-a2ab-f84b33b83bd4",
      "locationVersion": "h4-frame-v1:f944b341d84dc46f7bee8aa0e5136525505fea8914ab6be9ba4ed90b151e1f9d",
      "routeLabel": "actual road",
      "fromMeters": 1.0,
      "toMeters": 2.0,
      "offsetFromMeters": 0.0,
      "offsetToMeters": 1.0
    }
  },
  "resolution": {
    "id": "e9e95f8d-d5f8-465b-9492-d5bba22542ad",
    "actorId": "cfbeb80a-8dad-4937-b2ba-e247dca4bc64",
    "at": "2026-11-25T07:53:42.4263031+00:00",
    "outcome": "REMOVED",
    "checkId": "06fccbc0-834c-4fc9-bfe0-78d7e8c1bdb7",
    "reason": "PM confirms independent safety closure",
    "condition": "area made safe before removal",
    "checklistConfirmation": "Recorded removal condition and site safety confirmed",
    "measureVersion": "AAAAAAAACDI=",
    "obligationVersion": "AAAAAAAACAk=",
    "evidence": [
      {
        "fileId": "b6239139-6cc9-4d66-96ae-114f650202ce",
        "fileVersion": "AAAAAAAACCw=",
        "hash": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
        "evidenceLinkId": "35561703-cfba-47a2-9133-5ff895683f66",
        "capturedAt": "2026-11-25T07:53:41.6681014+00:00",
        "actualUploaderId": "49b51390-e874-43f3-95da-85cc9eec4d3e"
      }
    ]
  }
}
```

## CI và crash139

Hai run base 71d8ae8 đều **1705 PASS / 3 FAIL / 1 SKIP**. Ba nguyên nhân: Swagger
description dài hơn giới hạn 320; source evidence excerpts cũ sau refactor; test PM
safety read còn kỳ vọng 403 thay vì collection được duyệt. Sửa mô tả ngắn, đồng bộ
excerpts chính xác và kiểm PM 200 với đúng danh sách. Giữ exact wire/source gate,
quyền negative và skip whitelist. Ca skip là MinIO smoke cấu hình ngoài; các ca
owned MinIO chạy độc lập, không được tính như skip tùy ý.

Tìm thấy container **rg-ft03-verify-api-1** exit139 lúc **2026-11-17 16:47:08Z**,
image `sha256:5d5492a2ba242f0fdc2f760251076c6b2c21014335eeea25fa27f14067725952`,
entrypoint `dotnet RoadGuardSystem.eAPI.dll`, OOMKilled=false. Log trước đó có
Unhandled InvalidOperationException SchemaMismatch từ SandboxSyntheticSeeder khi
HandoffBootstrap khởi động. Image cũ mang catalog 9 migration, signature
66F44303… trong khi log schema thực D650E32E…; đây là lỗi khởi động có bằng chứng.
Không đủ dump để gán cơ chế native exit139 cho lỗi đó; **NOT_REPRODUCED /
INSUFFICIENT_EVIDENCE cho SIGSEGV root cause**, không ghi FIXED.

Container này là stack thử cũ, không phải rg-step23-local đang phục vụ. Không khởi
động lại stack cũ, không seed/reset để tái hiện. Không có core/dump trong evidence
đã tìm và /tmp container; Docker event lịch sử không còn kết quả. Script read-only
`tools/Test/Capture-RoadGuardCrash.ps1` thu inspect state/log/events ngoài repo,
không dump environment/credential. Log/dump riêng có thể chứa dữ liệu nhạy cảm,
cần rà trước chia sẻ. Lần tái diễn cần command/process/time/image/core đúng sự cố;
chỉ bật dump và tái hiện trên stack/DB cô lập.

## Hồi quy và giới hạn bàn giao

| Mục | Trạng thái phần BE trong đợt hoàn thiện | Bằng chứng/giới hạn |
| --- | --- | --- |
| B1 | Mở rộng khoảng cách UTM/shape và lý do thiếu, giữ explicit confirmation | HTTP/SQL matching cả 48N/49N, vector PROJ độc lập; không uncertainty đích hoặc adapter native thật thì vẫn chưa xác định |
| B3 | Giữ item/current responsibility, không mở project nguồn | Report source và transfer read đã kiểm byte thực trên owned MinIO; quyền ngoài item/membership vẫn bị chặn |
| B5 | Giữ AI facts/Sup assignment và PM-only decisions/FT final | Hồi quy AI genuine MOCK có storage riêng; chưa có AI thật hoặc UI |
| B6–B7 | Giữ revision/review/reuse riêng | Reuse/readback và byte nguồn qua owned MinIO; metadata không thay file authority |
| B10 | Giữ inventory và attach đã có | Không thêm route; full API/LD07 có các ca quyền/version/replay; không seed hay attach trên DB dùng chung trong đợt này |
| B12 | Thêm resolution có attestation/check/evidence và immutable head | Chuỗi HTTP multipart VERIFIED → FIELD link → check → resolve → phiên mới; NORMAL, transfer safety riêng; không mở Fast Track safety |
| B13 | Giữ conclusion200/publication201 | Chỉ rút gọn mô tả Swagger, không đổi status nghiệp vụ |
| B14 | Giữ submit/readback/confirm và journal | Bốn test script geometry đạt; crash capture là công cụ đọc, không chạy bootstrap cũ |
| B15 | Giữ pinned authoritative policy/coverage và fail-closed | Không đổi provenance, TEST_ONLY, start guard hoặc policy threshold |
| Crash139 | Có log lỗi khởi động; chưa tái hiện SIGSEGV | Old test container/image riêng; không đồng nhất với runtime hiện hành, không ghi FIXED |

B3/B6–B7 giữ scope item/trách nhiệm, exact submission/review/reuse, kiểm quyền file
riêng. B5 giữ Sup facts/assignment, PM-only matching/decisions, FT final PM. B10
inventory khác coverage sources, attach dùng aggregate ETag mới sau VERIFIED.
B15 giữ pins/sourceVersion, TEST_ONLY fail-closed. B13 conclusion 200/publication201;
B14 submit→readback→confirm và journal retry không đổi. Kết quả từng lượt test,
CI/SHA/image/backup cuối nằm trong biên bản bàn giao, không suy từ health.
Không nghiệm thu UI, AI thật hoặc deploy bên ngoài từ kiểm thử backend.

## Kiểm chứng local bản hoàn thiện

- Build API với analyzer: `dotnet build tests/RoadGuardSystem.ApiTests --no-restore --no-incremental`,
  0 error; vẫn có warning hiện hành, không tắt analyzer.
- Unit: 1155 PASS / 0 FAIL / 0 SKIP trên nội dung C# cuối.
- Nhóm API cuối: 46 PASS / 0 FAIL / 0 SKIP, gồm hai ca owned MinIO từng bị lỗi
  bucket trùng. Helper chỉ tạo bucket khi caller opt-in; caller cũ giữ hành vi cũ.
- SQL lane 1–4: lần lượt 204, 179, 217, 208 PASS; không FAIL/SKIP.
  `Verify-SqlLanes.ps1` xác nhận 808 identity duy nhất. Manifest không đổi.
  Binary checkpoint trước phần nullable lifecycle safetyContext; nhóm lifecycle/safety
  bị ảnh hưởng được chạy bổ sung sau khi rebuild bản cuối, ghi riêng trong biên bản.
- Development Seeder trên DB cô lập: chạy hai lần giữ ID/số dòng, 199 assessment
  gồm bảng migration; các GAP được catalog ghi rõ, không coi synthetic là nguồn thật.
- EF 8.0.17 model parity, format toàn solution, 10 source gates và diff check đạt.
- Lượt full API trước bản helper cuối: 1711 PASS / 2 FAIL / 1 SKIP. Hai FAIL là
  fixture bucket trùng đã được sửa và kiểm lại ở nhóm 46 ca; kết quả full API cuối
  ghi riêng trong biên bản bàn giao, không cộng các lượt trùng nhau thành ca mới.
- SKIP được phép: `UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`
  thiếu cấu hình MinIO ngoài fixture. Các ca owned MinIO thực sự chạy, không dùng skip này.

Migration `20261125072411_TemporarySafetyResolution`: backup COPY_ONLY/CHECKSUM,
VERIFYONLY, restore riêng, CHECKDB và so số dòng trước/sau đã kiểm. Bản restore
nâng 14→15 khớp catalog signature `F8C5EDC1ABCAE0D596A4B2097C949C55005FD9998C3B67A0CA83EF97AAFBB699`.
Lệnh triển khai theo design-time factory của repo:

```powershell
# Cấp ROADGUARD_MIGRATION_CONNECTION_STRING qua cấu hình bí mật cho đúng DB.
dotnet ef database update 20261125072411_TemporarySafetyResolution --project RoadGuardSystem.Repositories --context RoadGuardDbContext
```

Dùng EF 8.0.17, backup và thử nâng cấp trước; không seed khi khởi động runtime.
Giữ image cũ. Downgrade có history bị chặn; không tự restore đè dữ liệu phát sinh
sau backup. SHA runtime, image, backup và trạng thái CI thực tế được ghi riêng
trong bàn giao; source đã push không đồng nghĩa runtime/CI đã đạt.
