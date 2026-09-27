# Nghiên cứu công nghệ C# và nền tảng cho RoadGuard

Ngày: 22/09/2026. Trạng thái: đề xuất kỹ thuật cho hotfix; chưa tự động thêm package, migration hay hạ tầng.

## Bối cảnh checkout

Các project đang target `net8.0`. Backend đã có ASP.NET Core, EF Core SQL Server 8.0.x, SQL Server Spatial/NetTopologySuite, Quartz 3.14 và Testcontainers cho SQL Server. Quyết định ưu tiên là tận dụng các ranh giới hiện có (`Controller -> IService -> IRepository`, outbox/lease/retry) trước khi thêm thư viện.

## Khuyến nghị

| Công nghệ/cách dùng | Giá trị cho RoadGuard | Quyết định đề xuất |
|---|---|---|
| EF Core SQL Server + NetTopologySuite | Lưu polyline, SRID, spatial index và truy vấn route/segment | Giữ stack hiện tại; dùng `LengthIndexedLine`/station trên service domain và kiểm chứng bằng SQL Server spatial. Không dùng SQLite để kết luận spatial/migration. |
| `ProjNet` hoặc pipeline transform tương đương | Chuyển hệ tọa độ khi import GeoJSON/GPX/GPS | Chỉ thêm sau khi chốt danh sách SRID và sai số; mọi transform phải lưu source/target SRID và method. |
| `BackgroundService` + Quartz + outbox lease | Dispatch AI async, retry, polling và scheduled survey | Giữ Quartz và persistence worker hiện có; không giữ HTTP request chờ GPU. Job phải idempotent theo manifest fingerprint. |
| `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience` | Timeout, retry có backoff, circuit breaker và bulkhead cho AI adapter | Đề xuất cho adapter HTTP; không retry lỗi schema/permission; package chỉ thêm trong slice được duyệt. Xem [HTTP resilience của .NET](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience). |
| OpenTelemetry (`OpenTelemetry.Extensions.Hosting` và instrumentation phù hợp) | Trace `ReportId/CaseId/SegmentSetId/JobId`, đo queue delay, coverage và retry | Đề xuất ở hotfix observability; không ghi GPS/ảnh nhạy cảm vào span. Chốt exporter theo môi trường trước khi thêm package. |
| `System.Text.Json` source generation/contract validation | Manifest/result version, enum/unknown-field policy và payload nhỏ hơn | Ưu tiên built-in; raw AI payload lưu bất biến, DTO chỉ nhận schema version đã biết. |
| FFmpeg/ffprobe CLI qua process boundary | Đọc metadata/video và ánh xạ clip time với video gốc; tách SRT khi cần | Dùng tool version pin + checksum + sandboxed process. Không coi wrapper .NET là nguồn sự thật; lưu command/version/provenance. Tham khảo [FFmpeg documentation](https://ffmpeg.org/documentation.html). |
| Azure Blob/S3-compatible `IFileStorage` | Video lớn, signed URL ngắn hạn, range read cho AI | Giữ interface storage hiện có; chọn provider sau khi chốt deployment. Không đưa URL tùy ý vào AI manifest. |
| MapLibre/Terra Draw/Turf/OSRM | FE vẽ route, preview station/segment, routing tham khảo | Đây là công nghệ FE/dịch vụ ngoài; BE vẫn xác nhận geometry, lý trình và phiên bản. Không thêm vào C# backend. |

## Mẫu C# nên dùng

- `CancellationToken` chạy xuyên controller, service, repository và worker; timeout adapter phải khác timeout request của FE.
- `DateTimeOffset` UTC cho event, video timestamp và audit; không trộn local time vào manifest.
- `IAsyncEnumerable`/streaming cho upload và đọc video metadata; không nạp toàn bộ video vào memory.
- `record` DTO bất biến, một public type mỗi file; domain entity giữ invariant/state transition.
- `ProblemDetails` với error code ổn định; lỗi AI tạm thời, lỗi dữ liệu và lỗi quyền phải phân loại riêng.
- Fingerprint canonical JSON của manifest + scope + model/config để deduplicate; idempotency key không dựa vào URL tạm.
- `TimeProvider` được inject ở service/worker khi cần kiểm thử deadline, lease và retry; không gọi clock tĩnh trong domain.
- Log structured với correlation ID, không log token, GPS Reporter ngoài scope, ảnh hoặc raw payload đầy đủ.

## Không nên thêm lúc này

- MediatR, AutoMapper, repository framework mới hoặc một message broker thứ hai: làm trùng ranh giới hiện tại.
- FluentValidation nếu chỉ lặp lại DataAnnotations/contract checks đã có; chỉ dùng khi có yêu cầu validation phức tạp được owner duyệt.
- Wrapper FFmpeg chạy tự do từ request thread; xử lý media phải là worker có giới hạn tài nguyên và provenance.
- Tự xây routing hoặc tự suy diễn tim đường từ hai điểm; dùng dữ liệu tuyến đã xác nhận và spatial operations.

## Lộ trình nghiên cứu/áp dụng

1. Đo baseline: kích thước video, số segment, queue delay, thời gian manifest/result và tỷ lệ retry.
2. Chốt contract AI bằng video/SRT mẫu, timeout, idempotency, storage access và model/config version.
3. Làm hotfix contract/persistence trước; sau đó mới thêm resilience/observability package nếu số đo chứng minh cần.
4. Kiểm thử SQL Server spatial, worker restart, late result, duplicate result, coverage thiếu và route version mới.
5. Chỉ pin package version sau khi kiểm tra tương thích `net8.0`, license, security scan và build/test CI.

## Nguồn chính

- [ASP.NET Core hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [EF Core spatial data](https://learn.microsoft.com/en-us/ef/core/modeling/spatial)
- [ASP.NET Core metrics](https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Quartz.NET documentation](https://www.quartz-scheduler.net/documentation/)
- [NetTopologySuite documentation](https://nettopologysuite.github.io/NetTopologySuite/)
