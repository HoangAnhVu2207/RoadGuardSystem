# RoadGuard Warranty Schedule Validation Design

## Mục tiêu

Kiểm tra lịch khảo sát và lượt bay theo hồ sơ bảo hành áp dụng cho tuyến. Lịch tuần và lịch tháng dừng ở ngày hết hạn và được trả về qua API. Gửi lại cùng yêu cầu giữ nguyên kết quả.

## Phạm vi

- Tạo và cập nhật dự án, hồ sơ bảo hành và phạm vi tuyến.
- Tạo kế hoạch khảo sát gốc, định kỳ và phát sinh.
- Kiểm tra hạn được nhập khi tạo hoặc đổi Operator cho task gắn với kế hoạch có hồ sơ bảo hành.
- Đổi Crew cho task đo đạc `MEASURE_ONLY`. `DueAt` của Crew là hạn công việc và task hiện tại không nhận trường lịch thực hiện hoặc hồ sơ bảo hành.
- Kiểm tra thời điểm ghi nhận chuyến bay theo bảo hành của kế hoạch khi nộp dataset.
- Problem Details và mã lỗi ổn định cho FE.

## Quy tắc nghiệp vụ

1. `WarrantyStartDate` không được sau `WarrantyEndDate`.
2. Một lịch có khoảng thời gian `[plannedStartAt, plannedEndAt)` hợp lệ khi thời điểm bắt đầu không trước ngày bắt đầu bảo hành và thời điểm kết thúc không sau cuối ngày hết hạn theo giờ Việt Nam.
3. Lịch tuần giữ thứ và giờ của lần đầu. Lịch tháng giữ ngày và giờ của lần đầu. Nếu tháng thiếu ngày đó thì dùng ngày cuối tháng và quay lại ngày gốc ở tháng sau.
4. Khi lịch định kỳ vượt hạn bảo hành, BE chỉ sinh các lượt còn hợp lệ và trả ngày cuối đã sinh. Nếu lần đầu đã ngoài hạn thì từ chối toàn bộ yêu cầu.
5. Tạo lịch, dời lịch có ngày mới và giao task theo kế hoạch dùng cùng một quy tắc kiểm tra. Việc tạo hoặc đổi Operator kiểm tra `dueAt` khi người dùng gửi giá trị và task có hồ sơ bảo hành gắn với kế hoạch.
6. Thời điểm quay hoặc ghi nhận thực tế của chuyến bay được kiểm tra theo hồ sơ bảo hành. Thời điểm upload hoặc đồng bộ sau đó không làm mất bằng chứng hợp lệ được tạo trong thời hạn.
7. Khi thiếu hồ sơ áp dụng, BE trả lỗi. Khi các hồ sơ cùng áp dụng có khoảng ngày khác nhau, FE gửi `warrantyId` do PM chọn. Hồ sơ có cùng khoảng ngày được coi như cùng giới hạn lịch.
8. Idempotency và optimistic concurrency giữ nguyên. Gửi lại cùng yêu cầu trả cùng kết quả, còn cùng khóa với nội dung khác trả lỗi xung đột.

## Hợp đồng lỗi

`application/problem+json` giữ các trường `status`, `title`, `detail`, `instance`, `code` và `correlationId`. Các lỗi thời gian bổ sung `field`, `warrantyId`, `warrantyStartDate`, `warrantyEndDate`, `requestedStartAt`, `requestedEndAt` khi có thể.

| Code | HTTP | Ý nghĩa |
| --- | --- | --- |
| `project_dates_invalid` | 422 | Ngày kết thúc dự án trước ngày bắt đầu |
| `warranty_dates_invalid` | 422 | Ngày kết thúc trước ngày bắt đầu |
| `survey_warranty_required` | 422 | Không có hồ sơ bảo hành áp dụng |
| `survey_warranty_selection_required` | 422 | Có nhiều hồ sơ áp dụng và cần chọn một |
| `survey_outside_warranty` | 422 | Lịch hoặc thời điểm thực tế nằm ngoài hạn |
| `survey_time_range_invalid` | 422 | Kết thúc không sau bắt đầu |
| `survey_recurrence_invalid` | 422 | Chu kỳ hoặc ngày bắt đầu không hợp lệ |
| `survey_recurrence_empty` | 422 | Không có lượt nào trong hạn bảo hành |
| `survey_schedule_conflict` | 409 | Lịch hiện tại xung đột với kế hoạch đang hoạt động |

## Hợp đồng chu kỳ

Các request V2 nhận thêm trường tùy chọn sau. Request cũ không có trường này vẫn là một lượt đơn.

```json
{
  "recurrence": {
    "frequency": "WEEKLY",
    "untilWarrantyEnd": true
  }
}
```

`frequency` nhận `NONE`, `WEEKLY` hoặc `MONTHLY`. Với `untilWarrantyEnd=true`, BE tính các lượt từ `plannedAt` đến hết hạn bảo hành. Response trả `occurrenceCount`, `firstPlannedAt`, `lastPlannedAt` và danh sách `occurrences` gồm mã lượt, thời điểm bắt đầu, thời điểm kết thúc và trạng thái. Lưu các lượt bằng khóa duy nhất theo kế hoạch, thời điểm bắt đầu và tuyến.

## Quyền và Mobile

PM thấy ngày bảo hành và ngày cuối có thể lập lịch trước khi xác nhận. FE không tự tính ngày cuối để quyết định ghi dữ liệu. Khi nhận mã lỗi, FE hiển thị lỗi theo `code` và dùng các trường ngày trong response để sửa form.

PM đổi Crew đo đạc trực tiếp bằng ID Crew mới và lý do. Crew cũ nhận thông báo `field.task.assignment_ended.v1` và FE hiển thị **Công việc đã được luân chuyển** khi GET chi tiết trả `field_assignment_reassigned`. FE khóa nộp và có thể xóa cache của lượt phân công cũ. Crew mới nhận cùng task và phụ trách bằng chứng tiếp theo. PM cần xử lý bản nộp đang chờ duyệt trước khi đổi.

## Ngoài phạm vi

- Không thay đổi quy tắc retention.
- Không tự gia hạn bảo hành.
- Không tạo lịch từ reminder hoặc notification.
- Không thay đổi quyền đọc dự án.
