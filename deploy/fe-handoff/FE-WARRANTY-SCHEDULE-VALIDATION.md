# Lịch khảo sát theo bảo hành và đổi Crew đo đạc

## Lập lịch khảo sát

PM gửi `POST /api/v1/projects/{projectId}/survey-plans` với `Idempotency-Key`. Bổ sung các trường dưới đây vào body tạo kế hoạch đang dùng. Giữ `scope` theo các tuyến và đoạn đã chọn.

```json
{
  "plannedAt": "2027-01-31T08:00:00+07:00",
  "surveyType": "PERIODIC",
  "warrantyId": "00000000-0000-4000-8000-000000000001",
  "recurrence": {
    "frequency": "MONTHLY",
    "untilWarrantyEnd": true
  }
}
```

- Bỏ `recurrence` hoặc dùng `frequency: "NONE"` để tạo một lượt. `WEEKLY` và `MONTHLY` yêu cầu `untilWarrantyEnd: true`.
- Ngày đầu và ngày cuối bảo hành tính theo UTC+07:00. Toàn bộ lượt V2 dài một giờ phải nằm trong khoảng bảo hành. Một lượt có thể hoàn tất đúng 00:00 ngày sau ngày hết hạn.
- Lịch tuần giữ thứ và giờ. Lịch tháng giữ ngày và giờ gốc, dùng ngày cuối tháng khi tháng ngắn hơn. FE hiển thị `occurrenceCount`, `firstPlannedAt`, `lastPlannedAt` và `occurrences` từ BE.
- Nếu các hồ sơ áp dụng có khoảng ngày khác nhau, FE gửi lại `warrantyId` người dùng chọn. Các hồ sơ có cùng khoảng ngày cho cùng một phạm vi thời gian có thể dùng cùng quy tắc lập lịch. BE trả ID đã dùng trong `warrantyId`.
- Mỗi lượt trong `occurrences` có `id`, `plannedStartAt`, `plannedEndAt`, `status` và `surveyPlanId`. Lượt chưa kích hoạt có status `SCHEDULED` và `surveyPlanId: null`. Đọc lại danh sách bằng `GET /api/v1/survey-plans/{planId}`.
- Để giao việc, PM gửi `planId` của chuỗi và `occurrenceId` của lượt cùng body hiện có cho `POST /api/v1/projects/{projectId}/survey-tasks`. Mỗi tuyến và loại khảo sát có tối đa một kế hoạch hoạt động. Hoàn tất kế hoạch đang hoạt động trước khi kích hoạt lượt kế tiếp.
- Để dời lịch, gửi `plannedAt` và `reason` qua `POST /api/v1/survey-plans/{planId}/postpone` với `If-Match` và `Idempotency-Key`. BE áp dụng thay đổi cho lượt đã có `surveyPlanId`. Các mốc khác đã sinh giữ ngày ban đầu.
- Với task gắn bảo hành, khi giao hoặc đổi Operator, `dueAt` được gửi tường minh phải nằm trong bảo hành. Khi nộp dataset, BE kiểm tra `recordedAt` theo bảo hành gắn với kế hoạch, kể cả khi nộp file sau ngày hết hạn.

## Lỗi BE để hiển thị trên FE

Dùng `code` để chọn thông điệp và `field` để gắn lỗi vào form. Response có `correlationId` và có thể bổ sung `warrantyId`, `warrantyStartDate`, `warrantyEndDate`, `requestedStartAt`, `requestedEndAt`. Ngày trả về có dạng `YYYY-MM-DD` và mốc thời gian là ISO 8601 có offset.

| HTTP | Code | Field thường gặp | Thông điệp gợi ý |
| --- | --- | --- | --- |
| 422 | `project_dates_invalid` | `endDate` | Ngày kết thúc dự án phải từ ngày bắt đầu trở đi. |
| 422 | `warranty_dates_invalid` | `warrantyEndDate`, `endDate` | Ngày hết bảo hành phải từ ngày bắt đầu bảo hành hoặc ngày bàn giao trở đi. |
| 422 | `survey_time_range_invalid` | `plannedAt`, `plannedEndAt`, `newPlannedStartAt` | Kiểm tra lại thời điểm bắt đầu và kết thúc khảo sát. |
| 422 | `survey_warranty_required` | `warrantyId` | Tuyến cần một hồ sơ bảo hành áp dụng được. |
| 422 | `survey_warranty_selection_required` | `warrantyId` | Chọn hồ sơ bảo hành cho lịch khảo sát. |
| 422 | `survey_outside_warranty` | `plannedAt`, `plannedStartAt`, `newPlannedStartAt`, `dueAt`, `recordedAt` | Thời điểm đã chọn nằm ngoài thời hạn bảo hành. |
| 422 | `survey_recurrence_invalid` | `recurrence.frequency`, `recurrence.untilWarrantyEnd`, `planId`, `warrantyId` | Kiểm tra chu kỳ, kế hoạch hoặc hồ sơ bảo hành đã chọn. |
| 422 | `survey_recurrence_empty` | `recurrence` | Chu kỳ này không có lượt hợp lệ trong thời hạn bảo hành. |
| 409 | `survey_schedule_conflict` | `plannedAt`, `newPlannedStartAt`, `occurrenceId` | Lượt khảo sát bị trùng hoặc vẫn còn một kế hoạch đang hoạt động. |

## Đổi Crew cho task đo đạc

- PM gửi `POST /api/v1/field-inspection-tasks/{taskId}/reassign` với `assignedToUserId`, `reason`, `If-Match` và `Idempotency-Key`. Crew mới nhận cùng task, xác nhận nhận việc và nộp bằng chứng tiếp theo. BE giữ bằng chứng đã ghi và mốc bắt đầu của task.
- Nếu task đã nộp và đang chờ PM xem xét, BE trả 409 `field_submission_review_required`. PM xem xét bản nộp trước khi đổi Crew.
- Khi có mạng, Mobile đồng bộ danh sách và notification. Crew cũ nhận `field.task.assignment_ended.v1`. Khi mở lại chi tiết, 403 `field_assignment_reassigned` được hiển thị là **Công việc đã được luân chuyển**. Với mã này, Mobile vô hiệu hóa gửi lại và cho phép Crew xóa bản nháp hoặc cache đã gắn với lượt phân công cũ.
- Mobile dùng kết quả đồng bộ mới nhất để khóa màn hình. BE tiếp tục kiểm tra quyền ghi của Crew khi nhận yêu cầu từ một bản cache cũ.
