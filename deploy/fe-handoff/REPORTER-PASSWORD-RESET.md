# Reporter quên mật khẩu

Dùng HTTPS. Gửi `Content-Type: application/json` cho cả hai POST. Hai endpoint dùng trước đăng nhập, không gửi Bearer, cookie, CSRF hay Idempotency-Key.

## Yêu cầu gửi mã

`POST /api/v1/auth/reporter/password-reset/request`

```json
{"email":"reporter@example.test"}
```

Email hợp lệ về định dạng luôn nhận `202 Accepted` với body rỗng. Hiển thị “Nếu tài khoản đủ điều kiện, mã sẽ được gửi về email đã đăng ký.” Điều kiện gửi là Reporter đang hoạt động, role đang hiệu lực và email đã xác minh. BE gửi về email lưu trên tài khoản. Với cấu hình mặc định, chờ 60 giây giữa hai lần yêu cầu, tối đa 4 lần gửi trong một cửa sổ 15 phút. `202` cũng áp dụng khi chạm giới hạn hoặc hệ thống mail trả lỗi giao nhận.

## Xác nhận

`POST /api/v1/auth/reporter/password-reset/confirm`

```json
{"email":"reporter@example.test","otp":"012345","newPassword":"<mật khẩu mới>"}
```

- `204 No Content`: mật khẩu đã đổi và các phiên cũ bị thu hồi. Xóa thông tin đăng nhập cũ trên app rồi chuyển về màn đăng nhập.
- `400 validation_error`: dữ liệu thiếu hoặc sai định dạng. Mã là chuỗi 6 chữ số, giữ số 0 đầu.
- `400 invalid_reset_otp`: mã sai, hết hạn, đã dùng, chạm giới hạn thử hoặc tài khoản không đủ điều kiện. Mã mặc định có hiệu lực 10 phút, tối đa 5 lần thử sai mỗi mã. Sau khi bị chặn, dùng mã mới nếu còn lượt gửi.
- `422 password_policy_failed`: mã hợp lệ nhưng mật khẩu mới bị bộ kiểm tra mật khẩu hiện hành từ chối. Cho nhập lại mật khẩu trước khi mã hết hạn.

Mỗi lần phát mã hợp lệ thay mã trước cho cùng tài khoản. OTP đặt lại tách riêng với OTP đăng ký. Giữ luồng đổi mật khẩu sau đăng nhập và luồng Supervisor đặt lại mật khẩu cho nhân sự.

## Backend và kiểm chứng

Áp migration `20261117063514_ReporterPasswordReset` qua quy trình EF hiện hành. Thêm bảng `ReporterPasswordResetChallenges`. Dùng cấu hình hiện có `IdentityOnboarding:Secret` (Base64 ít nhất 32 byte), `SmtpHost`, `SmtpPort`, `SmtpUsername` và `SmtpPassword` trong section `IdentityOnboarding`. Không thêm khóa cấu hình mail. Khi chạy test, dùng test mail sink. Chưa xác minh SMTP thật hoặc app Reporter.

[OpenAPI](openapi.json) · [Luồng Operator mở lại video](OPERATOR-DATASET-RETRY.md)
