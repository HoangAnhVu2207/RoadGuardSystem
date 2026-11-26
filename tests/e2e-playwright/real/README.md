# Test RoadGuard trên server

Playwright đăng nhập qua API thật, kiểm tra danh tính và vai trò, refresh, logout và xác nhận token của phiên đã logout bị từ chối. Chế độ video dùng đúng upload multipart mà FE gọi, nộp dataset với vai Drone Operator và theo dõi job REAL do BE tự tạo. Báo cáo lưu HTTP status, correlation ID và mã lỗi. Báo cáo chỉ lưu tiền tố của ID nghiệp vụ.

## 1. Kiểm tra release trên Ubuntu

Chạy trong PowerShell. Nhập sudo trong terminal. Lệnh đọc release, trạng thái container, đĩa, RAM và khóa bảo trì.

```powershell
ssh.exe -t -i C:/Users/HoangAnhVu/.ssh/roadguard-server-codex -o HostKeyAlias=192.168.1.11 -o IdentitiesOnly=yes -o StrictHostKeyChecking=yes dev@100.81.253.113 'hostname; echo ACTIVE_RELEASE; sudo cat /etc/roadguard/active-release; sudo docker ps -a --filter name=roadguard-server; df -h /srv/roadguard; free -h; sudo lslocks -rn -o PATH | grep -Fx /run/lock/roadguard-server.lock || true'
```

Chỉ chạy video khi đã xác nhận release BE có REAL worker, container AI hoạt động và host đang rảnh cho một đợt xử lý. Phiên bản `7e21430886049a98c0df33a6989acbb4fdc42479` là mốc BE đã biết từ trước khi tích hợp REAL.

## 2. Kiểm tra đăng nhập thực tế

Chạy từ mạng ngoài LAN để kết quả phản ánh đường truy cập của đội FE. Nhập thông tin tài khoản trong hộp thoại PowerShell. Có thể bỏ qua tài khoản chưa có bằng cách hủy hộp thoại tương ứng. Drone Operator là tài khoản cần có ở bước video.

```powershell
Set-Location -LiteralPath 'C:\Users\HoangAnhVu\.codex\worktrees\be-ai-integration\RoadGuardSystem\tests\e2e-playwright'
npm ci
$env:E2E_BASE_URL = 'https://roadguardsystem.tail79aea8.ts.net'
$env:E2E_ALLOW_MUTATION = 'false'
$env:E2E_REAL_MODE = 'auth'
foreach ($role in 'SUPERVISOR', 'PM', 'REPORTER', 'CREW', 'OPERATOR') {
    $cred = Get-Credential -Message "RoadGuard E2E - $role"
    if ($cred) {
        Set-Item -LiteralPath "Env:E2E_${role}_EMAIL" -Value $cred.UserName
        Set-Item -LiteralPath "Env:E2E_${role}_PASSWORD" -Value $cred.GetNetworkCredential().Password
    }
}
Remove-Variable cred -ErrorAction SilentlyContinue
npm exec -- playwright test --config=real/playwright.config.ts server.spec.ts
```

## 3. Kiểm tra survey task

Dùng ID từ hồ sơ bàn giao FE hoặc danh sách survey task của Operator. Mỗi clip cần một task riêng ở trạng thái `NEW_ASSIGNED`, `ACCEPTED` hoặc `IN_PROGRESS`, được giao cho Operator đã nhập. Nhiều task có thể thuộc cùng dự án DEMO. Điền thời điểm thực tế ghi video theo ISO 8601 có múi giờ. Scope và ETag được đọc từ BE.

`E2E_CLIPS` nhận danh sách khóa phân cách bằng dấu phẩy. Ví dụ, đặt `E2E_CLIPS=KENH_DONG` để chạy một clip. Với khóa riêng, cấu hình các biến cùng mẫu `E2E_<KEY>_PROJECT_ID`, `TASK_ID`, `DEVICE_ID`, `RECORDED_AT`, `VIDEO` và `SRT`. Mặc định dùng `KENH_DONG,DUONG_SO_2`.

```powershell
$env:E2E_KENH_DONG_PROJECT_ID = Read-Host 'Project ID Kenh Dong'
$env:E2E_KENH_DONG_TASK_ID = Read-Host 'Survey task ID Kenh Dong'
$env:E2E_KENH_DONG_DEVICE_ID = Read-Host 'Drone device ID Kenh Dong'
$env:E2E_KENH_DONG_RECORDED_AT = Read-Host 'Thoi diem ghi clip Kenh Dong (ISO 8601)'
$env:E2E_DUONG_SO_2_PROJECT_ID = Read-Host 'Project ID Duong so 2'
$env:E2E_DUONG_SO_2_TASK_ID = Read-Host 'Survey task ID Duong so 2'
$env:E2E_DUONG_SO_2_DEVICE_ID = Read-Host 'Drone device ID Duong so 2'
$env:E2E_DUONG_SO_2_RECORDED_AT = Read-Host 'Thoi diem ghi clip Duong so 2 (ISO 8601)'
$env:E2E_REAL_MODE = 'preflight'
npm exec -- playwright test --config=real/playwright.config.ts server.spec.ts
```

`preflight` chỉ đọc BE. Nếu thiếu target, clip hoặc endpoint thì báo `NOT_RUN` trong receipt và không tự chọn project khác.

## 4. Upload và theo dõi xử lý AI

Sau khi preflight thành công, nhập origin HTTPS đã được xác nhận cho signed upload từ mạng thử nghiệm. Origin gồm scheme, hostname và port nếu có. Lấy giá trị từ cấu hình triển khai được duyệt. Nếu server chỉ ký URL MinIO nội bộ, test ngoài LAN dừng ở điều kiện truy cập storage và cần báo lại trước khi chạy video.

```powershell
$env:E2E_STORAGE_ORIGIN = Read-Host 'Origin HTTPS cua signed upload (khong nhap signed URL)'
$env:E2E_KENH_DONG_VIDEO = Read-Host 'Duong dan tuyet doi MP4 Kenh Dong'
$env:E2E_DUONG_SO_2_VIDEO = Read-Host 'Duong dan tuyet doi MP4 Duong so 2'
$env:E2E_PROTECTED_PROJECT_IDS_FILE = Read-Host 'File ngoai repo chua ID cac project cu, moi dong mot GUID'
$env:E2E_REAL_MODE = 'real'
$env:E2E_ALLOW_MUTATION = 'true'
try {
    npm exec -- playwright test --config=real/playwright.config.ts server.spec.ts
} finally {
    $env:E2E_ALLOW_MUTATION = 'false'
}
```

Đặt `E2E_<KEY>_SRT` bằng đường dẫn tuyệt đối ngoài repo nếu có SRT đã cắt theo thời gian của MP4. Chế độ `real` nhận task mới trước lúc upload. Mỗi dataset thành công chuyển task sang `SUBMITTED`. Nếu lỗi giữa chừng, kiểm tra receipt và trạng thái task trước khi chạy lại. File bảo vệ nằm ngoài repo và liệt kê ID của mọi project cũ.

Đọc báo cáo mới nhất và xóa biến chứa thông tin đăng nhập khỏi PowerShell sau khi chạy xong.

```powershell
$result = Get-ChildItem .\results\real-*.json | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-Content -LiteralPath $result.FullName
foreach ($role in 'SUPERVISOR', 'PM', 'OTHER_PM', 'REPORTER', 'CREW', 'OPERATOR') {
    Remove-Item -LiteralPath "Env:E2E_${role}_EMAIL", "Env:E2E_${role}_PASSWORD" -ErrorAction SilentlyContinue
}
```

## 5. Kiểm tra offline và stack dùng một lần

Đặt `E2E_REAL_MODE=dry-run` rồi chạy `npm run test:public` để kiểm tra hash MP4/SRT và các tham số task đã cấu hình. Đọc `status` trong receipt. `NOT_RUN` cho biết còn thiếu dữ liệu đầu vào.

Với stack dùng một lần, đặt `E2E_DISPOSABLE=true` và dùng hai origin `http://127.0.0.1` của API và MinIO local. Chế độ `real` cần đủ sáu phiên, gồm `E2E_OTHER_PM_EMAIL` và `E2E_OTHER_PM_PASSWORD` cho PM ngoài dự án. Dùng tài khoản fixture cho lượt chạy này.
