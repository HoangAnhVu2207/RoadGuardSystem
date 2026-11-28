# Kiểm chứng lịch khảo sát theo bảo hành và luồng đổi Crew

Base `c01dc1d7e1a7d6aa10c1355e15aca941e55a011e`. Thay đổi thuộc gói này gồm cập nhật API khảo sát, kiểm tra ngày dự án và bảo hành, migration lịch định kỳ, đổi Crew đo đạc và thông báo. Worktree có sẵn thay đổi kiểm thử AI local trong `tests/local-ai`, tài liệu triển khai ngày 27/11 và bản ghi E2E Kênh Đông 2.

## Tự review

1. Đối chiếu ngày đầu và ngày cuối bảo hành UTC+07:00, ngày gốc của lịch tháng, hành vi của endpoint đời trước và phạm vi tuyến. Ngày dự án, thời điểm bàn giao và thời hạn bảo hành giữ ý nghĩa riêng. `dueAt` kiểm tra cho Operator khi gửi tường minh và kế hoạch đã gắn bảo hành. `DueAt` của Crew là hạn công việc đo đạc.
2. Đối chiếu giao lại task, xung đột ghi, gửi lại cùng yêu cầu, Problem Details và migration từ schema cũ. Crew cũ nhận thông báo dành riêng cho lượt đã kết thúc và nhận `field_assignment_reassigned` khi mở lại task. Crew mới tiếp tục cùng task. Migration giữ các mốc cũ và chỉ tạo lượt cho các lịch định kỳ được yêu cầu sau nâng cấp.

## Kết quả tại máy phát triển

| Kiểm tra | Kết quả |
| --- | --- |
| Build solution và `dotnet format --verify-no-changes --no-restore` | PASS. Dời một comment trong `RoutingMapRepository.Sources.cs` để sửa lỗi whitespace có sẵn mà gate toàn solution phát hiện. |
| Toàn bộ unit test | PASS 1.324/1.324 trên Docker Linux. Lượt Windows có 7 ca AI worker chạm ngưỡng dung lượng trống mặc định của fixture. Cùng bộ test qua khi chạy trên Docker Linux với font Unicode của fixture báo cáo. |
| API lịch V2 và khảo sát | PASS 6/6 trong lượt xác minh cuối, gồm lịch tuần, lịch tháng, chọn bảo hành, đổi Operator và đồng bộ muộn. |
| Project và Warranty API | PASS 11/11 trong lượt chạy tập trung, gồm ngày đảo ngược, bảo hành kết thúc trước bàn giao và xử lý gửi lại. |
| Endpoint khảo sát đời trước | PASS nhóm tập trung 5 API và 2 SQL. |
| Đổi Crew | PASS kiểm tra HTTP bằng phiên thực tế của fixture và các nhóm Field/H6 SQL, H5 Offline SQL. |
| Migration lịch định kỳ và kiểm tra SQL liên quan | PASS 4/4 ở lượt xác minh migration và Crew. PASS 12/12 hồi quy P230 và phạm vi bảo hành LD07. |
| Swagger và kiểm kê nguồn | PASS 37/37. |
| Phân bổ lane CI | PASS 243 lớp API, 120 lớp SQL và 51 phương thức SQL phân bổ riêng. Workflow, gate TRX và 12 fixture gate lane đều qua. |

CI GitHub trên SHA mới, review độc lập bên ngoài, luồng Mobile trên APK, triển khai Ubuntu và Gmail thuộc `NOT RUN`. API và SQL kiểm tra hành vi BE. Ca AI worker trong unit test dùng fixture, nên kết quả này chỉ xác minh logic xử lý của worker trong bài test.
