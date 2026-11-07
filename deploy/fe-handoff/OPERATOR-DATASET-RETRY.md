# Operator nộp dataset và gửi lại sau mất mạng

Ngày đối chiếu 07/11/2026. Branch thực hiện `huy-review`, base/HEAD `76121621dd1a01a9acefa5d89d4d6ffdc6635346`. Checkout riêng `D:/RG-dataset` giữ nguyên checkout gốc đang dirty. Nguồn nghiệp vụ là yêu cầu trong phiên làm việc này và HUY-FINAL-INTEGRATION. Các kết luận dưới đây là CURRENT_VERIFIED từ source. Kết quả chạy test được ghi ở cuối.

## Chính sách drone đã chốt

POST dataset yêu cầu `deviceId` là GUID khác rỗng. DTO dùng `Guid?` để bind JSON, service từ chối null/rỗng. OpenAPI đang dùng cũng đánh dấu field này required. Lần nộp mới kiểm `DroneDevices.Status == Active`, rồi ghi `SurveyDataVersions.DeviceId`. Database cho phép nullable cho dữ liệu lịch sử, trong khi luồng nộp hiện tại bắt buộc có device.

Nguồn

- [DTO](../../RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs), [service SubmitDatasetAsync](../../RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs), [repository dataset](../../RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs).
- [DroneDevice](../../RoadGuardSystem.BusinessObjects/Devices/DroneDevice.cs), [mapping drone](../../RoadGuardSystem.Repositories/Configurations/DroneDeviceConfiguration.cs), [mapping dataset](../../RoadGuardSystem.Repositories/Configurations/SurveyDataVersionConfiguration.cs), migration `20261008171739_BaselineCurrentSchema` có cột `SurveyDataVersions.DeviceId` và trigger immutable.

Task và work package trả assignment/scope/geometry, chưa gán drone cố định. `DroneDevices` có ID, serial, model, status, checklist version. API chọn theo task ở phần dưới đọc registry chung và dùng assignment/project để giới hạn người gọi.

**TARGET_CONFIRMED theo quyết định owner ngày 07/11/2026.** Registry là kho dùng chung. Drone Operator đang được giao task và còn quyền trong project được chọn bất kỳ drone Active trong registry. Lựa chọn ghi nhận máy đã thu dữ liệu, không đặt chỗ hoặc giữ độc quyền. Lần nộp mới kiểm lại assignment/project và Active. Replay thành công đã lưu tiếp tục trả kết quả cũ khi actor còn quyền, kể cả khi drone chuyển Maintenance.

## API chọn drone theo task

`GET /api/v1/survey-tasks/{taskId}/eligible-drones?limit=25&cursor=...` nhận Bearer như luồng nộp dataset Operator. GET không dùng Idempotency-Key, If-Match hoặc CSRF. Chỉ DroneOperator đang có assignment hiệu lực trên đúng task và membership hợp lệ theo role/ngày trong project được đọc. PM, Supervisor và Operator khác nhận 403. Session không hợp lệ nhận 401. Kiểm quyền chạy trước truy vấn danh sách registry.

Response `200` có `Cache-Control: private, no-store`, cùng envelope danh sách hiện hành

```json
{
  "items": [
    { "id": "22222222-2222-2222-2222-222222222222", "serialNo": "DRONE-001" }
  ],
  "nextCursor": null
}
```

Chỉ có ID và serialNo. App hiển thị serialNo và gửi ID thật trong deviceId. Giới hạn mặc định 25, hợp lệ 1–100. Thứ tự ID tăng dần theo SQL uniqueidentifier, cursor dùng Data Protection gắn actor/task. Giữ cursor opaque, encode bằng URLSearchParams và lấy tiếp đến null. Đổi actor hoặc task cần bắt đầu lại. limit có thể đổi giữa các trang. Query lạ, query lặp hoặc cursor sai nhận `400 validation_error`. Task không tồn tại nhận `404 survey_request_not_found`; mất quyền nhận `403 access_forbidden`.

Không có drone Active trả `{"items":[],"nextCursor":null}`. App hiển thị “Chưa có máy khả dụng” và nút tải lại. Registry và quyền được đọc lại mỗi request, nên các trang không phải snapshot bất biến. Không suy tình trạng Active hiện tại từ danh sách đã tải trước đó.

Drone chuyển Maintenance sau khi được chọn sẽ biến mất khỏi danh sách mới. Nộp mới bằng drone đó nhận `422 survey_validation_failed` khi các điều kiện nộp khác hợp lệ. App giữ bản nháp để người dùng kiểm tra, tải lại lựa chọn cho thao tác mới đã rõ kết quả. Nếu lần nộp trước đang pending vì timeout, tiếp tục giữ nguyên request cũ để retry/đối chiếu, không tự thay deviceId hay key.

Nguồn [action](../../RoadGuardSystem.API/Controllers/SurveyV2Controller.cs), [service](../../RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs), [query registry](../../RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs), [DTO](../../RoadGuardSystem.DTOs/Surveys/EligibleDroneDto.cs).

Chưa xác định được source app Operator. Checkout FE tìm thấy tại `D:/DO_AN/roadguard-frontend` là app CAD, không có caller nộp dataset trong phần source đã tìm. Phần lưu bền/retry dưới đây là handoff để tích hợp vào đúng app.

## Contract HTTP hiện tại

`POST /api/v1/survey-tasks/{taskId}/datasets` nhận Bearer. Actor phải là Drone Operator đang có assignment hiệu lực trên task và membership hiện hành đúng role/project. Lần nộp mới còn kiểm task Accepted/InProgress, scope BAND_V1, scope/file đúng task, file đã VERIFIED, đúng purpose và giới hạn kích thước. Telemetry có thể vắng và trả trạng thái MISSING.

Header bắt buộc là `Idempotency-Key`, `If-Match` và `Content-Type: application/json`. Lấy version từ GET task sau accept hoặc command cập nhật task tương ứng. Ví dụ body minh họa, các ID phải lấy từ nguồn hợp lệ thực tế

```json
{
  "videoFileIds": ["11111111-1111-1111-1111-111111111111"],
  "telemetryFileIds": [],
  "recordedAt": "2026-11-07T08:00:00Z",
  "deviceId": "22222222-2222-2222-2222-222222222222",
  "scope": [{
    "routeVersionId": "33333333-3333-3333-3333-333333333333",
    "segmentSetId": "44444444-4444-4444-4444-444444444444",
    "segmentIds": ["55555555-5555-5555-5555-555555555555"],
    "targetBand": "SURFACE"
  }],
  "pairs": [{
    "videoFileId": "11111111-1111-1111-1111-111111111111",
    "telemetryFileId": null,
    "timeOffsetMilliseconds": 0
  }]
}
```

Thành công và replay cùng trả `201`, body `DatasetResponseDto`, `Location: /api/v1/datasets/{id}` và ETag của dataset. Body gồm `id`, `surveyTaskId`, `dataVersionId`, `integrityStatus`, `telemetryStatus`, `version`. `id` và `dataVersionId` là cùng ID phiên bản dataset. Giữ ETag dataset riêng với If-Match task đã dùng để nộp.

| HTTP/code | Cách xử lý |
| --- | --- |
| 401 | Xử lý phiên đăng nhập theo auth hiện có, giữ pending request để đối chiếu. |
| 403 access_forbidden | Mất quyền hoặc assignment. Dừng retry tự động, giữ dữ liệu cục bộ. |
| 404 survey_request_not_found | Task không có. Dừng retry tự động, giữ dữ liệu cục bộ. |
| 409 duplicate_request | Key đã dùng trong scope với fingerprint khác, có thể do đổi If-Match. Đối chiếu request đã lưu. |
| 409 survey_invalid_state_transition | Lần nộp mới không hợp lệ với trạng thái task hiện tại. Đọc lại task để đối chiếu. |
| 409 survey_scope_incompatible | Task không dùng scope BAND_V1. |
| 412 concurrency_conflict | Lần nộp mới dùng version task cũ hoặc cạnh tranh ghi. Đối chiếu kết quả trước khi tạo thao tác mới. |
| 422 survey_validation_failed | Device, scope, file, cặp video/telemetry hoặc dữ liệu đầu vào không hợp lệ. |
| 428 validation_error | Thiếu Idempotency-Key hoặc If-Match. |

Các response lỗi dùng ProblemDetails có `code` và `correlationId`. JSON/DTO lỗi có thể bị middleware từ chối trước service.

Các GET đọc hiện có

- `GET /api/v1/survey-tasks/{taskId}` trả `SurveyTaskV2ResponseDto`, có ETag. PM/Sup theo project hoặc Operator có assignment hiệu lực. Cookie và Bearer đều được hỗ trợ theo bản sửa trước.
- `GET /api/v1/survey-tasks/{id}/work-package` trả `SurveyTaskWorkPackage`, có ETag, scope/geometry/access point, cùng guard task. Hiện dùng Bearer.
- `GET /api/v1/datasets/{id}` trả `DatasetDetailView`, có ETag, deviceId đã ghi cùng metadata/sourceFiles/pairs. PM/Sup theo project hoặc Operator đang được giao task và còn quyền project. Hiện dùng Bearer.

Nguồn [controller](../../RoadGuardSystem.API/Controllers/SurveyV2Controller.cs), [DTO response](../../RoadGuardSystem.DTOs/Surveys/DatasetResponseDto.cs), [DTO đọc](../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs), [service đọc/ghi](../../RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs). OpenAPI runtime Development là `/swagger/v1/swagger.json`. Snapshot BE `openapi.json` đã có required deviceId; API mới lấy contract từ controller/DTO và `EligibleSurveyDroneOperationFilter`. Đợt này thêm GET bộ chọn, giữ contract POST dataset hiện tại.

## Retry sau timeout hoặc app bị đóng

Trước lần gửi đầu tiên, app cần lưu bền taskId, body đầy đủ, Idempotency-Key và chuỗi If-Match. Khóa thao tác theo pending submission để các trigger không gửi song song. Khi timeout/mất mạng, giữ pending. Khi app mở lại, gửi lại cùng body và đúng hai header đã lưu. Giữ file ID, thứ tự mảng, recordedAt và biểu diễn If-Match kể cả dấu nháy. Cùng request sau khi BE commit sẽ nhận lại cùng dataset ID và headers.

Fingerprint hiện có chứa taskId, danh sách video/telemetry đã sắp ID, recordedAt, deviceId, scope JSON, **If-Match nguyên đầu vào** và pairs JSON. Scope/pairs giữ thứ tự. Repository chỉ trim If-Match sau lúc service tạo fingerprint. FE nên lưu chính xác request ban đầu thay vì tái dựng body/version.

Receipt key được trim và có scope `actorUserId + projectId + SurveyDatasetSubmitted + Idempotency-Key`. Khác key có thể tạo thao tác mới và còn chịu guard trạng thái/version. Khác actor có scope riêng và phải qua kiểm quyền.

Service kiểm lại role, assignment hiệu lực và membership hiện hành trước khi vào repository kể cả replay. Repository tìm receipt trước handler ghi. Replay thành công dùng kết quả đã lưu, bỏ qua kiểm trạng thái/version/drone của lần ghi mới. Vì vậy drone chuyển Maintenance sau commit vẫn cho replay đã thành công khi actor còn quyền. Cùng key nhưng đổi payload hoặc If-Match trả 409, sau kiểm quyền hiện tại.

Khi nhận thành công, lưu dataset ID rồi hoàn tất pending submission. Với 403/404/409/412/422, dừng retry tự động và giữ dữ liệu. Đọc task hoặc dataset nếu đã biết ID để đối chiếu. Kết quả chưa rõ cần trạng thái “Cần đối chiếu”. Không tự đổi key hay If-Match cho pending chưa rõ kết quả. Khi retry nhận lại một lỗi đã được lưu trong receipt, thay đổi điều kiện thực tế không tự biến receipt đó thành thành công.

## Transaction và retention

`IdempotencyOperationService.ExecuteAttemptAsync` mở transaction, gọi handler rồi lưu receipt và commit. Handler dataset lưu Survey, SurveyDataVersion, SurveyFiles, task.MarkSubmitted và audit `survey_dataset_submitted` trong transaction đó. Lỗi trước commit rollback các ghi này và receipt. Upload/verify file có transaction và vòng đời riêng, file đã upload tiếp tục tồn tại nếu submit thất bại.

Nguồn [idempotency](../../RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs), [dataset persistence](../../RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs).

CURRENT_VERIFIED trong phạm vi source, migrations, hosted registration, scripts/tools và cấu hình triển khai có trong checkout này

- `IdempotencyRecord` có CreatedAtUtc, không có field expiry.
- Lookup receipt dùng đúng actor/project/operation/key, không có filter tuổi bản ghi.
- Chưa tìm thấy job/script xóa hoặc TTL cho receipt submit trong phần source/deployment đã kiểm.
- UploadSession có ExpiresAt và MultipartDeadline, các giá trị đó thuộc upload và không đặt hạn cho receipt submit dataset.

**UNKNOWN phần runtime thật.** Chưa kiểm SQL Agent jobs, tác vụ bên ngoài repo, cấu hình môi trường hoặc thao tác DBA của hệ thống đang chạy. Vì vậy chưa thể cam kết thời gian lưu receipt trên runtime. Nếu receipt bị dọn ngoài code, backend mất kết quả replay đã lưu, request sẽ đi qua nhánh nộp mới với guard trạng thái/version hiện tại. Không bảo đảm vẫn trả cùng ID khi đó. Đợt này không đặt TTL mới.

## Kiểm thử và giới hạn

Bổ sung vào `Anh01SurveyFlowTests.ManualAssessmentPartialBaselineSupplementAndRevocationPersistThroughHttp` hai biến thể access point. Luồng tạo task, accept, upload/verify và submit đi qua HTTP. Storage là mock video. Các thay đổi membership/assignment/drone trong SQL fixture được dùng để cô lập điều kiện kiểm quyền và hiệu lực.

Ca kiểm gồm drone Maintenance bị từ chối trước lần nộp mới, replay sau commit vẫn trả cùng ID/body/Location/ETag dù drone đã chuyển Maintenance, đúng một dataset/audit/receipt theo thao tác thành công, đổi payload hoặc If-Match cùng key trả duplicate_request, mất membership hoặc đổi assignment khiến replay bị từ chối. Đây là mô phỏng client mất phản hồi sau commit bằng cách gửi lại request đã commit, không phải test mạng/app thực.

Kết quả thực thi local

| Lệnh | Kết quả |
| --- | --- |
| `dotnet test tests/RoadGuardSystem.ApiTests --filter FullyQualifiedName~Anh01SurveyFlowTests -p:RunAnalyzers=false` | Restore/compile đạt. Lượt đầu 2 ca lỗi khởi tạo fixture vì Docker đang tắt, chưa vào nghiệp vụ. |
| `dotnet test tests/RoadGuardSystem.ApiTests --no-build --filter FullyQualifiedName~Anh01SurveyFlowTests` | Sau khi khởi động Docker, 2 PASS / 0 FAIL / 0 SKIP. TRX `TestResults/DatasetRetry/dataset-retry-final.trx`. |
| `dotnet test tests/RoadGuardSystem.ApiTests --no-build --filter "FullyQualifiedName~ManualSwaggerContractTests.SourceValidatedRequestPropertiesAndQueryConstraintsAreExposed\|FullyQualifiedName~ManualSwaggerContractTests.RequiredSourceValidatedFieldsAreNonNullableThroughoutTheRequestGraph"` | 2 PASS / 0 FAIL / 0 SKIP. TRX `TestResults/DatasetRetry/dataset-openapi.trx`. Khi dùng lệnh, bỏ dấu escape Markdown trước ký tự phân cách OR. |
| `dotnet format whitespace --verify-no-changes --no-restore --include tests/RoadGuardSystem.ApiTests/Surveys/Anh01SurveyFlowTests.cs` | PASS. |
| `git diff --check` và kiểm link/JSON ví dụ của trang này | PASS. |

Lượt triển khai bộ chọn bổ sung kiểm HTTP cho Active registry dùng chung, Maintenance bị loại, chỉ có hai field ID/serialNo, phân trang không trùng, cursor rỗng/sai/khác actor-task, query lạ/lặp, danh sách rỗng, sai role/assignment/membership và drone mất hiệu lực sau chọn. Kiểm Swagger xác nhận Bearer, schema hai field, limit và response status. Các test retry ở lượt trước được giữ và chạy lại.

| Kiểm tra bổ sung trong lượt triển khai API | Kết quả thực tế |
| --- | --- |
| `dotnet test tests/RoadGuardSystem.ApiTests --no-restore -p:RunAnalyzers=false` với filter Anh01SurveyFlowTests và ManualSwaggerContractTests | 21 PASS / 1 FAIL / 0 SKIP. Lỗi ở kiểm chứng cứ source vì source service/repository đã thêm method. |
| Chạy lại ProductionInventoryAndEvidenceExcerptsMatchCurrentSource và EligibleSurveyDronesExposeOnlySelectionFieldsAndBoundedBearerRead sau đồng bộ chứng cứ | 2 PASS / 0 FAIL / 0 SKIP, `eligible-drones-openapi-final.trx`. |
| Chạy lại Anh01SurveyFlowTests và EligibleSurveyDronesExposeOnlySelectionFieldsAndBoundedBearerRead sau thêm kiểm cursor rỗng | 3 PASS / 0 FAIL / 0 SKIP, `eligible-drones-http-final.trx`. |
| `dotnet test tests/RoadGuardSystem.UnitTests --filter FullyQualifiedName~P2V2SurveyServiceTests -p:RunAnalyzers=false` | 5 PASS / 0 FAIL / 0 SKIP, `eligible-drones-unit.trx`. |
| `dotnet build tests/RoadGuardSystem.ApiTests --no-restore -clp:ErrorsOnly` | PASS, 0 errors / 591 warnings. |
| Scoped whitespace format, Verify-P102Docs.ps1, Verify-AgentSetup.ps1, git diff check và link/JSON mẫu | PASS. |
| SandboxOfflineRouteCatalogTests sau bổ sung GET eligible-drones vào danh mục route mới | 1 PASS / 0 FAIL / 0 SKIP, `eligible-drone-route-catalog-final.trx`. Trước bổ sung, test tái hiện FAIL do đúng route mới chưa có trong danh mục. |

Các TRX nằm trong `TestResults/DatasetRetry` của checkout D:/RG-dataset. Những ca đã qua và không bị thay đổi tiếp được giữ làm bằng chứng, không cộng các lượt chạy lặp thành số test độc lập. Database chỉ là SQL fixture cô lập; video storage là mock. Không có source app hoặc runtime FE/Operator được xác minh ở đợt này.

Self-review lượt 1 đối chiếu quyền trước truy vấn, không đặt chỗ drone, ràng buộc cursor, raw If-Match trong fingerprint, Active chỉ kiểm ở lần ghi mới và transaction receipt/dataset/audit. Lượt 2 kiểm scope file, nhánh huy-review, DI/interface/fake unit fixture, schema/OpenAPI và giới hạn app/retention. Đã sửa ca cursor rỗng bị model binding đổi thành null. Đồng bộ các bản lưu chứng cứ source trong Swagger schema/test sau khi thêm method service/repository. Review ngoài phiên này chưa thực hiện.

Quyết định eligibility drone đã được chốt và triển khai. Source app Operator và runtime DB retention vẫn cần nguồn riêng. Đã tìm trong workspace D:/DO_AN các manifest Flutter/Android/Expo và caller survey-tasks trong checkout FE, đồng thời đối chiếu tài liệu môi trường/integration của repo. Chưa xác định được repo/branch app Operator. Vì vậy bộ chọn UI và hàng đợi retry bền trên app còn giao FE triển khai theo contract trên.
