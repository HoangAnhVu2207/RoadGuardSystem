# RoadGuard trên Ubuntu Server

Bộ cài dành cho một server LAN riêng, triển khai BE + SQL Server + MinIO thật. Chạy ASP.NET Core ở `Production`, không bật Sandbox/Handoff/AI mock, không nhập dữ liệu demo. SQL Server Developer chỉ dùng cho phát triển, kiểm thử và demo dự án; cần giấy phép phù hợp trước khi dùng thương mại.

## Cây thư mục

```text
/opt/roadguard/                         Mã nguồn và bản phát hành
├── repository.git/                    Git bare, theo nhánh deploy
├── releases/<40-character-SHA>/        Bản mã nguồn cố định theo commit
├── current -> releases/<SHA>/          Bản kích hoạt thành công gần nhất
└── roadguard-lan-ca.crt                Chứng chỉ CA công khai cho máy khách
/etc/roadguard/                         Cấu hình riêng, quyền root
├── application.json                   Tài khoản SQL chạy ứng dụng, khóa ứng dụng
├── maintenance.json                   Tài khoản quản trị dùng lúc bảo trì
├── sql.env / storage.env / server.env
├── mssql.conf / active-release
└── tls/                               CA riêng và chứng chỉ server
/srv/roadguard/                         Dữ liệu bền vững
├── sql/                               Database SQL Server
├── objects/                           Object storage riêng tư
├── files/                             File theo cấu hình BE hiện có
├── keys/                              Data Protection, giữ khi cập nhật
└── backups/<UTC-time>/                 SQL + object/file/key/config + checksum
```

Release chỉ giải nén năm thư mục BE cùng file build, fixture cần để biên dịch và `deploy/server`. Không giải nén báo cáo, lịch sử tài liệu, tests hay các bản sao dự án vào release. Git objects, Docker images và build cache nằm trong vùng quản lý của Git/Docker. Không tự xóa bản cũ hoặc dữ liệu. Log container giới hạn 3 file × 10 MB/container.

## Cài lần đầu

Điều kiện: Docker daemon và Docker Compose đang hoạt động; có Git, Python 3, OpenSSL, curl, tar, flock. Server có IPv4 LAN ổn định; nên đặt DHCP reservation trên router. Không mở port router ra Internet trong bước này.

Chọn **commit đủ 40 ký tự đã kiểm tra trên nhánh deploy**, tải `deploy/server/bootstrap.sh` tại đúng commit vào `/tmp/roadguard-bootstrap-<SHA>.sh`, rồi chạy trong SSH có terminal:

```bash
sudo bash /tmp/roadguard-bootstrap-<SHA>.sh <SHA> 192.168.1.11
```

Thay `<SHA>` bằng commit thật, không gõ nguyên dấu ngoặc. Wizard hỏi email, tên và mật khẩu Supervisor đầu tiên. Nhập mật khẩu trực tiếp trên server, tối thiểu 16 ký tự có chữ hoa/thường/số/ký tự đặc biệt. Không gửi mật khẩu qua chat. Các secret khác được sinh ngẫu nhiên tại server, không đưa vào Git. Lần build đầu phải tải SQL/.NET/Go và biên dịch MinIO nên có thể mất nhiều phút.

Bootstrap từ chối khi đã có thư mục cài đặt. Nếu mất kết nối hoặc build lỗi sau khi wizard đã tạo cấu hình, **không chạy lại wizard, không xóa dữ liệu**. Dùng script trong release đã tải để xem log, sửa nguyên nhân và chạy lại `initialize` khi chưa có `active-release`:

```bash
sudo bash /opt/roadguard/releases/<SHA>/deploy/server/manage.sh logs
sudo bash /opt/roadguard/releases/<SHA>/deploy/server/manage.sh initialize
```

Nếu lỗi trước khi tạo đủ cấu hình/REVISION, cần kiểm tra trạng thái từng thư mục trước khi sửa; bộ cài không tự dọn một cài đặt dở dang.

Maintenance chỉ nhận DB `RoadGuard_Server` trên service `sql`; tạo dấu sở hữu và từ chối DB cũ không khớp. Áp dụng migrations có sẵn, tạo canonical roles và một Supervisor nếu chưa có người dùng. Không thay đổi tài khoản đã tồn tại. Mật khẩu Supervisor dạng rõ được bỏ khỏi cấu hình maintenance sau khi bước này thành công. Ứng dụng thông thường dùng SQL login `roadguard_app`, không dùng `sa`, không tự migrate/seed khi khởi động.

## Địa chỉ và chứng chỉ

| Thành phần | Địa chỉ LAN | Ghi chú |
|---|---|---|
| BE | `https://192.168.1.11:8443` | HTTPS, giữ nguyên secure cookies/CSRF |
| Health | `https://192.168.1.11:8443/health` | Kiểm tra kết nối DB và private bucket |
| Object API | `https://192.168.1.11:9443` | Dùng signed URL; bucket không public |
| SQL / MinIO console | Không publish | Chỉ network Docker nội bộ |

Lấy **CA công khai** từ server bằng PowerShell, sau khi cài xong:

```powershell
scp dev@192.168.1.11:/opt/roadguard/roadguard-lan-ca.crt "$env:USERPROFILE/Downloads/roadguard-lan-ca.crt"
curl.exe --cacert "$env:USERPROFILE/Downloads/roadguard-lan-ca.crt" https://192.168.1.11:8443/health
```

Đối chiếu fingerprint chứng chỉ qua phiên SSH đã tin cậy trước khi cài CA vào kho tin cậy máy khách. Với Windows, có thể import CA vào `Current User > Trusted Root Certification Authorities` để trình duyệt tin cậy. Chỉ phân phối `roadguard-lan-ca.crt`, không phân phối `ca.key`, `server.key`, PFX hay JSON cấu hình. Không bỏ kiểm tra TLS để chữa lỗi kết nối. Chứng chỉ server có hạn 365 ngày; cần lập lịch gia hạn trước hạn, bộ cài chưa tự gia hạn. Đổi IP cần cấp lại SAN và cập nhật endpoints/cấu hình, không chỉ đổi URL FE.

## Vận hành và cập nhật

```bash
sudo bash /opt/roadguard/current/deploy/server/manage.sh status
sudo bash /opt/roadguard/current/deploy/server/manage.sh logs
sudo bash /opt/roadguard/current/deploy/server/manage.sh backup
sudo bash /opt/roadguard/current/deploy/server/manage.sh stop
sudo bash /opt/roadguard/current/deploy/server/manage.sh start
sudo bash /opt/roadguard/current/deploy/server/manage.sh update
df -h / /srv/roadguard
```

Sau khi code đã được kiểm tra và push vào `origin/deploy`, `update` lấy commit mới, chỉ chấp nhận tiến tới commit hậu duệ, build trước khi dừng dịch vụ. Sau đó dừng API/MinIO, backup, migrate bằng maintenance và kiểm tra health trước khi đổi `current`. Có khóa chống hai lệnh bảo trì đồng thời. Không dùng `git pull` đè vào release đang chạy. Docker restart policy khởi động lại container cùng Docker sau reboot; `stop` chủ động giữ dịch vụ dừng cho đến khi `start`.

Backup SQL dùng `COPY_ONLY`, checksum và `RESTORE VERIFYONLY`; object/file/key/config được chụp khi API và MinIO đã dừng. Điều này gây downtime, cần chọn thời điểm phù hợp. `VERIFYONLY` không thay thế thử phục hồi toàn bộ trên máy khác. Nếu backup/update lỗi, API/MinIO có thể giữ trạng thái dừng: xem log trước, không chạy `start` bản cũ sau migration khi chưa đánh giá tương thích. Backup thành công riêng lẻ sẽ tự khởi động dịch vụ lại.

Sao chép backup mã hóa sang ổ/máy khác: bản sao cùng SSD không chống hỏng ổ. Backup chứa secret và dữ liệu, chỉ root đọc. Chưa có tự động xóa hoặc lịch backup; theo dõi dung lượng, quản lý thời gian giữ backup/images/releases theo nhu cầu, tránh `docker system prune --volumes`. Không có tự động hạ schema/rollback. Khi cần phục hồi, giữ nguyên hiện trạng, thử SQL restore và giải nén file/key/config vào môi trường cách ly, dùng đúng release trong file `RELEASE`, xác minh rồi mới chuyển dịch vụ.

## Phạm vi chưa hoàn thành

- Đây là nền BE + DB + storage; chưa chứng minh E2E FE/mobile/AI.
- Supervisor đầu tiên phải đổi mật khẩu bằng luồng có sẵn. Cần kiểm thử đăng nhập/đổi mật khẩu thực tế sau cài đặt, không coi health xanh là đạt toàn bộ nghiệp vụ.
- SMTP chưa được cấu hình; mời nhân viên, OTP và khôi phục mật khẩu chưa sử dụng được. Cấu hình `IdentityOnboarding` trong **cả hai** JSON qua editor riêng trên server trước khi dùng; không ghi secret vào command line. Đặt `FrontendBaseUrl` về FE thật sau khi có FE.
- Khi nối FE, thêm đúng HTTPS origin vào `ServerDeployment:AllowedOrigins` trong cả hai JSON và `MINIO_API_CORS_ALLOW_ORIGIN` trong `storage.env`, rồi restart. Dùng FE cùng site với BE để đáp ứng SameSite cookies. Không bật wildcard hoặc hạ secure cookie.
- AI service token đang tắt. Chỉ cấu hình AI thật sau khi đã kiểm tra contract và khóa dịch vụ riêng; không bật mock để tuyên bố AI đã chạy.
- HTTPS LAN dùng CA riêng; domain công khai, Internet access, chứng chỉ công cộng và pipeline tự động triển khai là bước riêng.

## Kiểm tra bộ cài trước khi phát hành

```bash
python3 -m unittest discover -s deploy/server -p test_setup.py -v
python3 deploy/server/verify_compose.py
for script in deploy/server/*.sh; do bash -n "$script"; done
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter FullyQualifiedName~ServerDeploymentTests
```

Các kiểm tra này không cần DB chung, không tạo secret server và không thay thế build/run container, kiểm tra migration, SQL permissions và upload thật trên server.
