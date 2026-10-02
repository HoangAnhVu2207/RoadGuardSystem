# User stories and acceptance criteria (RF-04 historical transfer)

These are source contents, not newly Accepted rules. The per-item source status is retained as evidence; current authority and implementation readiness are separate. Owner review is required before each named module uses unconfirmed detail. Accepted 32-44 are in [requirements](requirements.md).

<a id="us-01"></a>

### US-01 - Đăng nhập, hồ sơ, quyền và thông báo theo vai trò

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:112`; primary `RF-10-01` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-36A/37`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-01.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là một người dùng nội bộ (Supervisor, PM, Drone Operator hoặc Repair Crew), tôi muốn đăng nhập bằng tài khoản được cấp, xem/cập nhật hồ sơ cá nhân, xem đúng dự án được phân công và nhận thông báo phù hợp để làm việc đúng quyền.</pre>

**US-01.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện**</pre>

**US-01.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- Tài khoản đã được Admin tạo và đang hoạt động.
- Người dùng có thông tin xác thực hợp lệ.</pre>

**US-01.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-01.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Đăng nhập và phiên làm việc**
   - **Given** tài khoản đang hoạt động và mật khẩu đúng
   - **When** người dùng đăng nhập
   - **Then** hệ thống tạo phiên, nhận diện đúng vai trò hiện tại từ server và chỉ tải các dự án/công việc thuộc membership active, còn hiệu lực và đúng vai trò.
   - Nếu tài khoản không tồn tại, bị ngừng sử dụng, mật khẩu sai hoặc phiên hết hạn, hệ thống từ chối truy cập và không tiết lộ thông tin nhạy cảm.</pre>

**US-01.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Đăng xuất và hết hạn phiên**
   - **When** người dùng đăng xuất hoặc phiên hết hạn
   - **Then** token/phiên hiện tại không thể gọi dữ liệu nghiệp vụ; người dùng phải đăng nhập lại.</pre>

**US-01.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Hồ sơ cá nhân**
   - **Given** người dùng đã đăng nhập
   - **When** người dùng sửa thông tin được phép
   - **Then** hệ thống lưu thay đổi và nhật ký; người dùng không thể tự đổi vai trò, quyền dự án hoặc tài khoản người khác.</pre>

**US-01.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Phạm vi dữ liệu**
   - **Given** PM, Drone Operator hoặc Repair Crew truy cập danh sách
   - **Then** chỉ các dự án, nhiệm vụ, lỗi và hồ sơ được phân công được hiển thị.
   - **Given** Supervisor truy cập danh sách
   - **Then** có thể xem toàn bộ dữ liệu thuộc danh mục được cấp quyền Admin.</pre>

**US-01.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Thông báo và nhắc việc**
   - **When** có sự kiện khảo sát, kết quả xử lý, yêu cầu duyệt, trả sửa, từ chối/hủy nhiệm vụ, phân công lại hoặc sắp hết hạn bảo hành
   - **Then** hệ thống tạo thông báo cho đúng vai trò, có liên kết tới đối tượng và trạng thái đã đọc/chưa đọc.</pre>

**US-01.C10** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>6. **Đặt lại mật khẩu**
   - **Given** người dùng gửi yêu cầu khôi phục
   - **When** Supervisor (Admin) thực hiện đặt lại
   - **Then** mật khẩu cũ không bị hiển thị, tài khoản buộc đổi mật khẩu ở lần đăng nhập kế tiếp và nhật ký chỉ lưu người/thời điểm, không lưu mật khẩu.
   - Tài khoản đã ngừng sử dụng không được đặt lại mật khẩu.</pre>

**US-01.C11** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ngoại lệ và kiểm tra**</pre>

**US-01.C12** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- Không trả về dữ liệu dự án trước khi kiểm tra quyền.
- Không cho phép dùng phiên cũ sau khi mật khẩu bị Admin đặt lại.</pre>

**US-01.C13** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `CN01-CN04`, `CN10`, `QT02`, `QT09`.</pre>

Review gate: Review US-01 details against PR-36A/37 before RF-10-01 implements this claim. Checkpoint: RF-10-01 START for unconfirmed details; RF-11 retirement.

<a id="us-27"></a>

### US-27 - Reporter tự đăng ký và xác minh email bằng OTP

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:157`; primary `RF-10-01` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-37`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-27.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là người dân hoặc đại diện chủ đầu tư chưa có tài khoản, tôi muốn tự đăng ký bằng email và xác minh mã OTP để có tài khoản Reporter gửi phản ánh mà không cần Admin tạo hộ.</pre>

**US-27.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-27.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Tạo đăng ký pending**
   - **When** người dùng gửi email hợp lệ, display name, `ReporterType`, mật khẩu, confirm password và idempotency key
   - **Then** hệ thống tạo hoặc tiếp tục một registration intent với `User.status = PENDING`, `role_code = REPORTER`, `email_confirmed = false`; không cấp access/refresh token.</pre>

**US-27.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Gửi OTP**
   - Hệ thống tạo OTP bằng nguồn ngẫu nhiên bảo mật, chỉ lưu hash/HMAC, hạn dùng ngắn, số lần thử tối đa và cooldown resend; adapter Gmail trả provider correlation ID nhưng không lưu code plaintext.
   - Response public không tiết lộ email đã tồn tại, trạng thái account hoặc provider detail.</pre>

**US-27.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Xác minh OTP**
   - **Given** challenge chưa hết hạn, chưa consume và còn lượt thử
   - **When** Reporter gửi đúng OTP
   - **Then** hệ thống consume challenge một lần trong transaction, đặt `email_confirmed = true`, `email_confirmed_at`, chuyển User thành `ACTIVE` và có thể trả token pair chuẩn.</pre>

**US-27.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Từ chối an toàn**
   - OTP sai, hết hạn, đã dùng, sai purpose hoặc vượt giới hạn trả error ổn định, không làm account thành Active và không tiết lộ thông tin tài khoản khác.</pre>

**US-27.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Resend và retry**
   - Resend trước cooldown bị chặn; resend hợp lệ vô hiệu hóa challenge cũ và tạo challenge mới. Retry cùng idempotency key trả cùng registration outcome; payload khác cùng key bị từ chối.</pre>

**US-27.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>6. **Bảo mật và phân quyền**
   - Reporter tự đăng ký chỉ tạo role `REPORTER`; không tạo ProjectMember, không được chọn PM/Supervisor/DroneOperator/RepairCrew và không được gửi report trước khi verify.
   - Không log password, OTP, refresh token, Gmail provider secret hoặc nội dung email; audit chỉ lưu intent, thời điểm, kết quả và correlation ID.</pre>

**US-27.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `CN11`, `CN12`, `QT09`, `US-01`.</pre>

Review gate: Review US-27 details against PR-37 before RF-10-01 implements this claim. Checkpoint: RF-10-01 START for unconfirmed details; RF-11 retirement.

<a id="us-02"></a>

### US-02 - Làm việc ngoại tuyến và đồng bộ

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:184`; primary `RF-10-08` / proposed B; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-42A/43A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-02.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THAY THẾ UC-D23: giữ AC cũ về toàn vẹn/dọn cục bộ; AC-03 mở lại được tự tiếp tục khi OS cho phép. THÊM quyền Fast Track offline vô thời hạn và snapshot; BỎ giả định mọi sửa cần đánh giá server trước.]**</pre>

**US-02.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là Crew hoặc Operator, tôi muốn lưu nhiệm vụ, policy và bằng chứng để làm việc khi mất mạng rồi tự đồng bộ.</pre>

**US-02.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-02.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-02.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-02-AC-01:** **Given** nhiệm vụ có quyền đã tải; **When** thiết bị mất mạng hoặc khởi động lại; **Then** dữ liệu đã lưu/policy/ảnh còn đọc được, hiển thị version và chưa đồng bộ.</pre>

**US-02.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-02-AC-02:** **Given** Fast Track có quyền theo nhiệm vụ/policy; **When** mất mạng lâu; **Then** không tự hết quyền vì thời gian; token server hết hạn không xóa nháp và khi sync có thể cần xác thực lại.</pre>

**US-02.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-02-AC-03:** **Given** báo cáo đã gửi/xếp hàng; **When** có mạng và app được phép chạy; **Then** tự tiếp tục, retry không tạo bản ghi trùng; nháp chưa gửi không tự nộp.</pre>

**US-02.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-02-AC-04:** **Given** thiếu tệp hoặc checksum sai; **When** server kiểm toàn vẹn; **Then** không đánh dấu an toàn/đủ nghiệm thu, không cho dọn tệp chưa an toàn.</pre>

**US-02.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-02-AC-05:** **Given** PM đã đổi nhiệm vụ nhưng máy chưa nhận; **When** sync bản cũ; **Then** theo D05 giữ snapshot/bằng chứng, đưa vào conflict cho PM, không last-write-wins hoặc tự nghiệm thu.</pre>

**US-02.C10** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** CN05–CN09; BR-15/16/19/20; FR-22.</pre>

Review gate: Review US-02 details against PR-42A/43A before RF-10-08 implements this claim. Checkpoint: RF-10-08 START for unconfirmed details; RF-11 retirement.

<a id="us-03"></a>

### US-03 - Dự án, bàn giao và phân công

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:202`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-03.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THAY THẾ UC-D26: AC cũ 2–3 “Supervisor nhập/sửa hình học” → PM nhập/chỉnh nháp, Supervisor xác nhận theo US-31. Giữ tạo dự án, bảo hành, nhân sự và lưu trữ.]**</pre>

**US-03.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là Supervisor và PM, tôi muốn tách tạo dự án khỏi nhập tuyến và giữ lịch sử bảo hành.</pre>

**US-03.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-03.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-03.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-03-AC-01:** **Given** Supervisor có quyền; **When** tạo dự án và giao PM; **Then** mã duy nhất, đúng một PM chính; PM không tự có quyền tạo dự án Supervisor.</pre>

**US-03.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-03-AC-02:** **Given** PM được giao dự án; **When** nhập tim/bề rộng theo đoạn; **Then** lưu bản nháp để preview; người ngoài scope bị chặn.</pre>

**US-03.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-03-AC-03:** **Given** hình học đã dùng; **When** PM chỉnh; **Then** tạo bản nháp/version mới, không đổi liên kết lịch sử; xác nhận theo US-31.</pre>

**US-03.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-03-AC-04:** **Given** hồ sơ bàn giao/bảo hành hợp lệ; **When** lưu hoặc chuyển PM; **Then** giữ tài liệu, thời hạn và lịch sử phân công; không tự xóa khi ngừng dự án.</pre>

**US-03.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** DA01–DA05/DA12; BR-01/35/45; FR-04.</pre>

Review gate: Review US-03 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="us-04"></a>

### US-04 - Kế hoạch khảo sát và baseline theo band

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:219`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-04.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THAY THẾ UC-D29: baseline tổng trên Survey → nguồn baseline theo segment/band; cờ tổng nếu giữ chỉ dẫn xuất.]**</pre>

**US-04.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn lập khảo sát và xác nhận phần đủ điều kiện.</pre>

**US-04.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-04.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-04.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-04-AC-01:** **Given** tuyến/segment đã công bố; **When** lập kế hoạch gốc/định kỳ/phát sinh; **Then** scope và band rõ, nhắc việc không tự thành lệnh bay.</pre>

**US-04.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-04-AC-02:** **Given** mặt đường đủ nhưng mép phải thiếu; **When** xác nhận baseline; **Then** chỉ phần đủ được xác nhận, giữ phần thiếu để PM quyết định bổ sung.</pre>

**US-04.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-04-AC-03:** **Given** có kỳ sau; **When** đối sánh; **Then** đúng version/phạm vi tương thích, không tự đổi baseline lịch sử.</pre>

**US-04.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** DA06–DA11; BR-40/43; FR-26, FR-30.</pre>

Review gate: Review US-04 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="us-05"></a>

### US-05 - Điều phối, tiếp nhận, từ chối và hủy yêu cầu khảo sát

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:235`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-05.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là PM, tôi muốn giao đích danh Drone Operator và điều chỉnh phân công; là Drone Operator, tôi muốn tiếp nhận hoặc từ chối nhiệm vụ chưa bắt đầu với lý do rõ ràng.</pre>

**US-05.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-05.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Phân công**
   - **Given** yêu cầu khảo sát còn hiệu lực
   - **When** PM chọn một Drone Operator và lịch thực hiện
   - **Then** hệ thống chuyển yêu cầu sang `Mới giao`, gửi thông báo và lưu người giao/thời điểm.</pre>

**US-05.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Tiếp nhận**
   - **When** Drone Operator xác nhận
   - **Then** yêu cầu chuyển `Đã nhận`, hiển thị phạm vi, thời hạn, hướng dẫn và cho phép nhập dữ liệu.</pre>

**US-05.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Từ chối**
   - **Given** trạng thái là `Mới giao`
   - **When** Drone Operator từ chối
   - **Then** phải nhập lý do, yêu cầu trả về PM để phân công lại và không được coi là hoàn tất.
   - Nếu đã tiếp nhận, Drone Operator không được tự từ chối; PM phải điều chỉnh phân công.</pre>

**US-05.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Phân công lại**
   - **When** PM đổi người hoặc lịch
   - **Then** hệ thống lưu người cũ, người mới, lý do, lịch mới và thông báo các bên liên quan.</pre>

**US-05.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Hủy/thu hồi**
   - **Given** chưa có bộ dữ liệu máy chủ xác nhận toàn vẹn
   - **When** PM hủy yêu cầu và nhập lý do
   - **Then** yêu cầu chuyển `Đã hủy`, thông báo Drone Operator nếu đã giao/đã nhận và giữ lịch sử.
   - **Given** đã có dữ liệu nộp thành công
   - **Then** hệ thống chặn hủy; PM chỉ được điều chỉnh phân công hoặc yêu cầu bay bổ sung.</pre>

**US-05.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>6. **Phân biệt hoãn và hủy**
   - Hoãn kế hoạch không tạo lệnh bay; hủy yêu cầu là trạng thái của lệnh đã tạo; đổi người/lịch không làm mất yêu cầu.</pre>

**US-05.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `KS01-KS04`, `KS14`.</pre>

Review gate: Review US-05 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="us-06"></a>

### US-06 - Nhập, kiểm tra và tải dữ liệu bay

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:268`; primary `RF-10-04` / proposed B; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-38`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-06.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là Drone Operator, tôi muốn sao chép video vào thiết bị, bổ sung SRT khi cần, kiểm tra chất lượng và tải nhiều tệp theo hàng đợi để Backend có bộ dữ liệu khảo sát toàn vẹn cho xử lý AI.</pre>

**US-06.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-06.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Ghi nhận chuyến bay**
   - **When** Drone Operator nhập thiết bị, thời gian, phạm vi đã bay, ghi chú và tài liệu
   - **Then** thông tin được liên kết với đúng yêu cầu khảo sát.</pre>

**US-06.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Sao chép từ thẻ nhớ**
   - **When** chọn video
   - **Then** ứng dụng sao chép nội dung thật vào bộ nhớ thiết bị, kiểm tra bản sao và không chỉ lưu đường dẫn thẻ nhớ.
   - Thiếu bộ nhớ phải được báo rõ và không đánh dấu nhập thành công.</pre>

**US-06.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Phụ đề định vị**
   - **Given** MP4 không có luồng phụ đề định vị trích xuất được
   - **When** Drone Operator bổ sung SRT
   - **Then** hệ thống kiểm tra ghép đúng video và khoảng thời gian; SRT không được coi mặc định là nhật ký bay đầy đủ.</pre>

**US-06.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Kiểm tra chất lượng**
   - **When** chạy kiểm tra
   - **Then** hệ thống kiểm tra định dạng, định vị, đồng bộ thời gian, độ rõ, ánh sáng, vùng phủ và chồng lấn; vùng không đạt có lý do cụ thể.</pre>

**US-06.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Nộp nhiều video**
   - **Given** các tệp thuộc cùng lần khảo sát
   - **When** Drone Operator nộp
   - **Then** hệ thống gom đúng lần khảo sát, lưu trạng thái từng tệp và xếp hàng tải khi ngoại tuyến.</pre>

**US-06.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>6. **Xác nhận máy chủ và xử lý**
   - **When** máy chủ nhận đủ và kiểm tra toàn vẹn thành công
   - **Then** dữ liệu chuyển sang xử lý; Backend lưu manifest/job bền vững theo dataset + segment + TargetBand + model/config, trả 202 + JobId; worker gọi AI ngoài hoặc mock có nhãn nguồn, retry/dedup theo fingerprint.</pre>

**US-06.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>7. **Theo dõi**
   - Drone Operator và PM xem được trạng thái `Tiếp nhận`, `Đang xử lý`, `Hoàn tất`, `Lỗi` hoặc `Cần bổ sung`, cùng thông báo lỗi có thể hành động.</pre>

**US-06.C10** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `KS05-KS10`, `CN05-CN09`.</pre>

Review gate: Review US-06 details against PR-38 before RF-10-04 implements this claim. Checkpoint: RF-10-04 START for unconfirmed details; RF-11 retirement.

<a id="us-07"></a>

### US-07 - Bay bổ sung và thử lại xử lý

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:301`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-07.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là PM, tôi muốn xác nhận vùng dữ liệu chưa đạt và yêu cầu bay bổ sung hoặc thử lại tác vụ để hoàn thiện khảo sát mà không ghi đè dữ liệu gốc.</pre>

**US-07.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-07.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Xác nhận nhu cầu bổ sung**
   - **Given** kết quả chất lượng chỉ ra vùng thiếu/không đạt hoặc tác vụ cần thêm dữ liệu
   - **When** PM chỉ rõ vùng, lý do và người thực hiện
   - **Then** hệ thống tạo yêu cầu bổ sung, liên kết cùng lần khảo sát và lưu người xác nhận/thời điểm.</pre>

**US-07.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Không giới hạn lượt**
   - PM có thể tạo nhiều lượt bổ sung; mỗi lượt có phạm vi, lý do, người và nguồn gốc riêng.</pre>

**US-07.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Nộp bổ sung**
   - **When** Drone Operator nộp dữ liệu
   - **Then** hệ thống giữ dữ liệu cũ, kiểm tra vùng phủ/đối sánh và ghi rõ tệp thuộc lượt bổ sung nào.</pre>

**US-07.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Thử lại tác vụ**
   - **Given** lỗi máy chủ trên dữ liệu đã lưu nguyên vẹn
   - **When** PM hoặc Supervisor chọn thử lại
   - **Then** hệ thống tạo lần xử lý mới trên cùng dữ liệu, giữ lịch sử lỗi/lần thử và không yêu cầu bay lại tự động.</pre>

**US-07.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Lỗi dữ liệu**
   - **Given** lỗi do định dạng, thiếu định vị hoặc chất lượng không đạt
   - **Then** hệ thống không cho coi thử lại máy chủ là giải pháp; PM phải quyết định bay bổ sung hoặc xử lý theo ngoại lệ.</pre>

**US-07.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `KS11-KS13`.</pre>

Review gate: Review US-07 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="us-08"></a>

### US-08 - Rà soát và xác minh phát hiện AI

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:327`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-08.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[BỎ UC-D29: AC cũ 7 bắt mọi phát hiện phải đo thực địa và câu “phải đo” trong story. THAY AC 2/7 bằng kiểm chứng theo nhu cầu; research vẫn bắt buộc.]**</pre>

**US-08.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn giữ/sửa/loại phát hiện và chọn cách kiểm chứng phù hợp.</pre>

**US-08.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-08.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-08.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-08-AC-01:** **Given** AI trả detection; **When** mở xem; **Then** hiển thị loại/confidence/bbox/time/source/model, phân biệt ước lượng với số đo thật.</pre>

**US-08.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-08-AC-02:** **Given** PM giữ ứng viên; **When** lưu; **Then** Defect OPEN có nguồn, chưa là xác minh chính thức; không tự tạo Survey/task đo giả.</pre>

**US-08.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-08-AC-03:** **Given** quyết định cần số đo vật lý; **When** xác minh; **Then** phải có số đo được chấp nhận; khi không cần và bằng chứng đủ thì không buộc đo.</pre>

**US-08.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-08-AC-04:** **Given** AI không có detection hoặc PM chưa đủ căn cứ; **When** xem kết quả; **Then** không tự NO_DEFECT/đóng lỗi; loại sai cần lý do và giữ nguồn.</pre>

**US-08.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** AI01/AI04–AI07; BR-07/39/44; FR-13.</pre>

Review gate: Review US-08 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="us-09"></a>

### US-09 - Gộp, đối sánh theo kỳ và theo dõi diễn biến lỗi

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:344`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-09.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là PM hoặc Supervisor, tôi muốn đối chiếu các phát hiện trùng và cùng một hư hỏng qua nhiều kỳ để tránh đếm trùng, theo dõi lỗi mới/ổn định/phát triển và ưu tiên xử lý dựa trên bằng chứng.</pre>

**US-09.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-09.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Gợi ý trùng**
   - **When** hệ thống phát hiện các kết quả có khả năng cùng một lỗi
   - **Then** hệ thống chỉ đề xuất gộp, không tự gộp.</pre>

**US-09.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Quyết định gộp/giữ riêng**
   - **When** PM xác nhận gộp hoặc giữ riêng
   - **Then** hệ thống cập nhật định danh lỗi, giữ liên kết các phát hiện nguồn và lưu quyết định/audit.</pre>

**US-09.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Đối sánh qua khảo sát**
   - **Given** có baseline và các kỳ tương thích
   - **Then** PM có thể xác nhận hoặc sửa liên kết cùng hư hỏng; dữ liệu không đủ tương thích phải báo rõ.</pre>

**US-09.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Phân loại diễn biến**
   - Hệ thống hiển thị lỗi mới, ổn định hoặc đang phát triển theo dữ liệu đã có; nếu thiếu kỳ/thiếu chất lượng thì hiển thị cảnh báo thiếu căn cứ.</pre>

**US-09.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Cảnh báo ưu tiên**
   - **When** lỗi có mức độ cao, thay đổi nhanh hoặc thuộc nhóm cần theo dõi
   - **Then** PM/Supervisor nhận cảnh báo kèm căn cứ và độ tin cậy; cảnh báo không cam kết dự báo thời điểm hỏng.</pre>

**US-09.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `AI08-AI12`, `DA10-DA11`.</pre>

Review gate: Review US-09 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="us-10"></a>

### US-10 - Duyệt nhãn hư hỏng cho dữ liệu huấn luyện

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:368`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-34A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-10.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là PM, tôi muốn duyệt loại và vùng nhãn đã hiệu chỉnh trước khi xuất dữ liệu huấn luyện để chỉ dữ liệu có nguồn gốc và chất lượng được kiểm soát mới được sử dụng.</pre>

**US-10.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-10.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. Chỉ phát hiện đã được PM xác nhận hoặc hiệu chỉnh mới xuất hiện trong danh sách chờ duyệt nhãn.</pre>

**US-10.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **When** PM duyệt nhãn
   **Then** hệ thống lưu người, thời gian, phiên bản nhãn, nguồn AI và ảnh/video gốc.
3. Nhãn bị từ chối phải có lý do và không được đưa vào tập huấn luyện được duyệt.
4. Supervisor (Admin) chỉ xuất tập đã duyệt, kèm phiên bản và quyền truy cập.</pre>

**US-10.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `AI05-AI07`, `AI14`, `QT06-QT07`.</pre>

Review gate: Review US-10 details against PR-34A before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="us-11"></a>

### US-11 - Lập và duyệt từng công việc sửa

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:383`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-11.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[BỎ/THAY THẾ UC-D29: AC cũ 4 duyệt cả đợt, 5 không triển khai phần đạt, 6 duyệt lại toàn bộ → quyết định từng item và trình lại phần thay đổi. AC 1 đo mọi lỗi → căn cứ/đo theo nhu cầu.]**</pre>

**US-11.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Supervisor, tôi muốn quyết định từng item mà không khóa cả gói.</pre>

**US-11.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-11.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-11.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-11-AC-01:** **Given** lỗi nhánh thường đủ xác minh và số đo cần thiết; **When** PM lập phương án; **Then** tạo item và bản trình có scope/bằng chứng, chặn sửa trùng.</pre>

**US-11.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-11-AC-02:** **Given** gói có A/B/C; **When** Supervisor APPROVE A, REQUEST_EVIDENCE B, REJECT C; **Then** A được giao riêng, B chờ bằng chứng, đề xuất C kết thúc nhưng lỗi vẫn mở.</pre>

**US-11.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-11-AC-03:** **Given** phương án cần xem lại; **When** chọn REQUEST_RECONSIDER; **Then** ghi lý do riêng, không gộp với yêu cầu ảnh.</pre>

**US-11.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-11-AC-04:** **Given** PM sửa phần bị trả; **When** trình lại; **Then** version mới chỉ phần cần xét, giữ quyết định phần không đổi.</pre>

**US-11.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** SC01–SC09/SC12; BR-21–23; FR-19.</pre>

Review gate: Review US-11 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-12"></a>

### US-12 - Giao Crew và bàn giao

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:400`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-12.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THAY THẾ UC-D29: AC cũ chỉ giao cả đợt đã duyệt/đội trưởng đích danh → Crew và item/scope; giữ lịch sử. Fast Track có quyền qua nhiệm vụ, không qua gate duyệt cả đợt.]**</pre>

**US-12.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn giao đúng đội và thứ tự theo nhánh.</pre>

**US-12.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-12.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-12.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-12-AC-01:** **Given** item APPROVAL_TRACK chưa APPROVED; **When** giao thi công; **Then** bị chặn; item đã duyệt có thể giao không chờ item khác.</pre>

**US-12.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-12-AC-02:** **Given** Fast Track hoặc chỉ-đo được giao; **When** Crew mở; **Then** thấy loại quyền rõ, không dùng số lỗi để tự quyết được sửa.</pre>

**US-12.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-12-AC-03:** **Given** PM đổi đội/thứ tự; **When** lưu; **Then** giữ lịch sử, báo bên liên quan; [ĐỀ XUẤT] hiển thị bản đã nhận.</pre>

**US-12.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-12-AC-04:** **Given** đội cũ ngoại tuyến; **When** định giao cùng phạm vi; **Then** D05 yêu cầu xác nhận dừng/bàn giao trước khi đội mới start, không tự coi lệnh thu hồi đã nhận.</pre>

**US-12.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** SC10–SC11; BR-03/23/24; FR-20.</pre>

Review gate: Review US-12 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-13"></a>

### US-13 - Bằng chứng và tiến độ từng lần sửa

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:417`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-13.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THAY THẾ UC-D24: BEFORE không luôn cần ảnh Crew mới chụp; Fast Track được ảnh dân/drone, chuyến sửa sau dùng ảnh đo. AC gửi cũ chỉ online → cho xếp hàng, server đủ mới nộp chính thức.]**</pre>

**US-13.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là Crew, tôi muốn ghi đủ bằng chứng và gửi báo cáo dù mất mạng.</pre>

**US-13.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-13.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-13.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-13-AC-01:** **Given** nhiệm vụ được giao; **When** nhận hoặc từ chối trước nhận; **Then** ghi người/đội và lý do từ chối, không tự hủy phương án; đã nhận cần PM điều phối.</pre>

**US-13.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-13-AC-02:** **Given** Fast Track có ảnh dân/drone; **When** dùng làm BEFORE; **Then** giữ source/time/lỗi; [ĐỀ XUẤT] ảnh không phù hợp hiện trường phải chụp mới.</pre>

**US-13.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-13-AC-03:** **Given** thiếu BEFORE hợp lệ lưu trên máy; **When** bắt đầu sửa theo app; **Then** bị chặn; không yêu cầu phải upload xong để làm ngoại tuyến.</pre>

**US-13.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-13-AC-04:** **Given** sửa đã thực hiện; **When** ghi AFTER và gửi; **Then** gắn đúng attempt/lỗi, giữ thời điểm thực, xếp hàng nếu offline; không tự đóng.</pre>

**US-13.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-13-AC-05:** **Given** có lỗi mới ngoài nhiệm vụ; **When** ghi nhận; **Then** báo riêng PM, không tự sửa hoặc thêm vào scope đã duyệt.</pre>

**US-13.C10** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** HT01–HT08/HT14–HT15; BR-06/17–20; FR-21.</pre>

Review gate: Review US-13 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-14"></a>

### US-14 - Kiểm tra, sửa lại và đóng theo nhánh

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:435`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-14.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THAY THẾ UC-D25: AC cũ 3/4/6 buộc mọi lỗi trình/xác nhận Supervisor → PM đóng Fast Track và báo Supervisor; nhánh duyệt giữ Supervisor; hỗn hợp đóng tổng sau đủ nhánh.]**</pre>

**US-14.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Supervisor, tôi muốn đóng đúng thẩm quyền và chỉ sửa lại phần chưa đạt.</pre>

**US-14.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-14.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-14.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-14-AC-01:** **Given** server đã nhận đủ báo cáo; **When** PM kiểm; **Then** đối chiếu số đo/policy hoặc phương án, BEFORE/AFTER; thiếu căn cứ khác chất lượng chưa đạt.</pre>

**US-14.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-14-AC-02:** **Given** Fast Track đạt; **When** PM xác nhận; **Then** đóng lỗi và báo Supervisor, không tạo vòng duyệt sửa Fast Track.</pre>

**US-14.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-14-AC-03:** **Given** APPROVAL_TRACK đạt qua PM; **When** Supervisor xác nhận; **Then** đóng phần đủ điều kiện; hồ sơ hỗn hợp còn lỗi bắt buộc chưa đạt không đóng tổng.</pre>

**US-14.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-14-AC-04:** **Given** một lỗi bị trả; **When** Crew sửa tiếp; **Then** giữ lần sửa cũ, thêm attempt/bằng chứng; lỗi khác đạt không bị kéo lùi.</pre>

**US-14.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** HT09–HT13; BR-25–28; FR-23, FR-24.</pre>

Review gate: Review US-14 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-20"></a>

### US-20 - Đo thực địa và xác minh theo nhu cầu

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:452`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-20.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THAY THẾ UC-D21/24: bỏ gate VERIFIED trước mọi sửa tại AC cũ 5, giữ cho APPROVAL_TRACK; thêm đợt chỉ-đo, thiếu ảnh/số đo đo lại, cấm Crew vượt PM. GIỮ TN12 (từ chối nhiệm vụ trước khi tiếp nhận); bổ sung TN07 vào trace đo theo đợt, không đổi mã chức năng cũ.]**</pre>

**US-20.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Crew, tôi muốn đo đủ căn cứ trong phạm vi nhiệm vụ.</pre>

**US-20.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-20.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-20.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-20-AC-01:** **Given** PM giao kiểm tra IncidentCase/Defect; **When** tạo task; **Then** ghi nguồn thật, scope, loại đo và chỉ-đo/đo-và-sửa; không cần Survey giả.</pre>

**US-20.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-20-AC-02:** **Given** đo ngoài Fast Track thiếu ảnh/số đo bắt buộc; **When** nộp; **Then** không chấp nhận; đo lại; [TBD Q05] toàn đợt hay phần thiếu.</pre>

**US-20.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-20-AC-03:** **Given** PM xác định nghiêm trọng nhưng số đo nhỏ; **When** Crew hoàn tất đo; **Then** chỉ gửi PM, không tự sửa.</pre>

**US-20.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-20-AC-04:** **Given** quyết định nhánh thường cần số đo; **When** PM xác minh; **Then** chỉ VERIFIED khi đủ căn cứ/đo cần thiết; Fast Track được giao có ngoại lệ không chờ gate này.</pre>

**US-20.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-20-AC-05:** **Given** đo nghiên cứu; **When** nhập dữ liệu; **Then** giữ purpose và sample IDs theo RS01–RS06, không tự biến đo nghiệp vụ thành nghiên cứu đã đạt.</pre>

**US-20.C10** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** AI13/TN01–TN07; BR-05/07/09/17/44; FR-13, FR-17, FR-31.</pre>

Review gate: Review US-20 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-15"></a>

### US-15 - Dashboard quản lý dự án, tiến độ sửa chữa và rủi ro

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:470`; primary `RF-10-09-B` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-15.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là Supervisor hoặc PM, tôi muốn xem dashboard theo phạm vi quyền để theo dõi tình trạng bảo hành, lỗi còn mở, tiến độ sửa chữa, khảo sát và rủi ro cần ưu tiên.</pre>

**US-15.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-15.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. Supervisor xem tổng quan toàn danh mục; PM chỉ xem các dự án được giao.
2. Dashboard hiển thị trạng thái dự án, khảo sát, lỗi còn mở, dự án sắp hết hạn bảo hành và các việc cần xử lý.
3. Tiến độ sửa chữa hiển thị theo đợt, lỗi, trạng thái bằng chứng và việc cần PM/Supervisor xử lý.
4. Chỉ báo rủi ro cao/hư hỏng phát triển nhanh hiển thị kèm nguồn dữ liệu, kỳ khảo sát và độ tin cậy; không trình bày như dự báo chắc chắn.
5. Supervisor có thể so sánh dự án/kỳ khảo sát theo phạm vi và loại mặt đường tương thích; dữ liệu thiếu tương thích phải được cảnh báo.
6. Mọi con số trên dashboard có liên kết tới hồ sơ nguồn hoặc bộ lọc đã áp dụng.</pre>

**US-15.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `BC01-BC05`.</pre>

Review gate: Review US-15 details against owner source before RF-10-09-B implements this claim. Checkpoint: RF-10-09-B START for unconfirmed details; RF-11 retirement.

<a id="us-16"></a>

### US-16 - Xuất hồ sơ, nguồn gốc và tra cứu lưu trữ

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:486`; primary `RF-10-09-B` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-16.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là Supervisor hoặc PM, tôi muốn xuất báo cáo và hồ sơ bằng chứng theo dự án/đoạn/lỗi/khoảng thời gian để tái hiện được nội dung, nguồn gốc và tính toàn vẹn tại thời điểm xuất.</pre>

**US-16.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-16.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. Người dùng chỉ chọn được dự án, đoạn, lỗi và khoảng thời gian trong phạm vi quyền.</pre>

**US-16.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **When** xuất báo cáo
   **Then** hệ thống lưu bộ lọc, người xuất, thời điểm, trạng thái và phiên bản dữ liệu được dùng.
3. Hồ sơ tổng hợp gồm, khi có: bàn giao, khảo sát/baseline, ảnh gốc, loại và số đo lỗi, độ không chắc chắn, quyết định xác minh, phương án sửa, bằng chứng sau sửa và lịch sử duyệt.
4. Tệp xuất kèm nguồn gốc: mã tệp, checksum/dấu kiểm tra toàn vẹn, thời gian, tác giả, phiên bản mô hình và lịch sử sửa đổi.
5. Dữ liệu thiếu hoặc bằng chứng chưa có phải được ghi rõ trong báo cáo; hệ thống không tạo cảm giác hồ sơ đầy đủ.
6. Phương án xuất MVP hỗ trợ PDF tổng hợp và ZIP dữ liệu gốc/bảng kê khi cấu hình cho phép; lỗi tạo tệp phải báo rõ và không làm mất dữ liệu nguồn.
7. Sau khi dự án đóng, Supervisor/PM vẫn tra cứu được hồ sơ trong phạm vi quyền cho tới hết thời hạn lưu trữ hoặc lâu hơn nếu đang giữ tranh chấp.</pre>

**US-16.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `BC06-BC10`, `QT09`, `QT11-QT14`.</pre>

Review gate: Review US-16 details against owner source before RF-10-09-B implements this claim. Checkpoint: RF-10-09-B START for unconfirmed details; RF-11 retirement.

<a id="us-17"></a>

### US-17 - Quản lý tài khoản, quyền, danh mục và nhắc việc

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:504`; primary `RF-10-01` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-17.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là Supervisor (Admin), tôi muốn quản lý vòng đời tài khoản, quyền dự án, danh mục lỗi, quy tắc phân mức và cấu hình nhắc để hệ thống vận hành nhất quán.</pre>

**US-17.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-17.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Tài khoản**
   - Admin tạo/cập nhật/ngừng sử dụng tài khoản, gán một trong năm vai trò và ghi nhật ký thay đổi.</pre>

**US-17.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Ngừng tài khoản có việc mở**
   - **When** tài khoản bị ngừng sử dụng
   - **Then** hệ thống thu hồi phiên, chặn đăng nhập mới, giữ lịch sử, lập danh sách việc cần bàn giao và thông báo người có quyền phân công lại.
   - Không tự hủy, tự hoàn tất hoặc xóa bản nháp/công việc đang mở.</pre>

**US-17.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Phân quyền**
   - Admin cấp/sửa quyền dự án theo vai trò; thay đổi được audit và có hiệu lực với request server tiếp theo; **[THÊM UC-D23]** máy offline chưa nhận lệnh cần xử lý Q04, không hứa thu hồi tức thì. PM/Drone Operator/Repair Crew không xem được dữ liệu ngoài membership active, còn hiệu lực và đúng vai trò.
   - Khi Admin đổi role toàn hệ thống, hệ thống thu hồi toàn bộ phiên và refresh token trong cùng transaction; JWT role cũ không tiếp tục cấp quyền.
   - Khi quyền project bị đổi, hết hạn hoặc kết thúc, request kế tiếp phải bị kiểm tra theo membership hiện tại phía server; client claim không được dùng thay thế.</pre>

**US-17.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Danh mục**
   - Admin thêm hoặc ngừng sử dụng loại lỗi; mục cũ không bị xóa khỏi lịch sử.</pre>

**US-17.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Quy tắc phân mức**
   - Admin tạo phiên bản quy tắc theo chuẩn và loại mặt đường, lưu căn cứ; thay phiên bản không làm thay đổi ngược kết quả lịch sử.</pre>

**US-17.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>6. **Nhắc việc**
   - Admin cấu hình nhắc khảo sát và mốc trước hạn bảo hành; cấu hình không tự áp dụng thời hạn pháp lý chưa được xác minh.</pre>

**US-17.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `QT01-QT05`, `CN10`.</pre>

Review gate: Review US-17 details against owner source before RF-10-01 implements this claim. Checkpoint: RF-10-01 START for unconfirmed details; RF-11 retirement.

<a id="us-18"></a>

### US-18 - Quản trị mô hình AI, tác vụ và thiết bị

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:530`; primary `RF-10-05` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-18.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là Supervisor (Admin), tôi muốn quản lý phiên bản mô hình AI, theo dõi hàng đợi/tài nguyên và thông tin thiết bị để biết kết quả được tạo bởi mô hình nào và xử lý lỗi vận hành có kiểm soát.</pre>

**US-18.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-18.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Phiên bản mô hình**
   - Admin tạo, phát hành hoặc ngừng sử dụng phiên bản; lưu chỉ số đánh giá, ngưỡng vận hành, thời điểm và phiên bản áp dụng.</pre>

**US-18.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Nguồn kết quả**
   - Mỗi phát hiện AI giữ tham chiếu tới phiên bản mô hình; đổi mô hình không làm mất nguồn của kết quả cũ.</pre>

**US-18.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Tập huấn luyện**
   - Chỉ nhãn đã được PM duyệt mới được xuất; tệp xuất có nguồn gốc, phiên bản và quyền truy cập.</pre>

**US-18.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Giám sát tác vụ**
   - Admin xem tải, tiến trình, lỗi Backend/hàng đợi AI, dung lượng và trạng thái từng tác vụ.</pre>

**US-18.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Thử lại an toàn**
   - Tác vụ thất bại do hạ tầng có thể thử lại trên dữ liệu còn nguyên; lỗi dữ liệu phải chuyển PM quyết định bổ sung.</pre>

**US-18.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>6. **Thiết bị và quy trình**
   - Admin quản lý thông tin thiết bị bay, checklist và tài liệu chuyến bay; hệ thống không gửi lệnh điều khiển thiết bị bay.</pre>

**US-18.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `QT06-QT10`, `KS10`, `KS13`.</pre>

Review gate: Review US-18 details against owner source before RF-10-05 implements this claim. Checkpoint: RF-10-05 START for unconfirmed details; RF-11 retirement.

<a id="us-19"></a>

### US-19 - Lưu trữ, giữ hồ sơ và xóa dữ liệu có phê duyệt

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:552`; primary `RF-10-09-A` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-41A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-19.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story**  
Là PM, tôi muốn lập yêu cầu xóa hồ sơ đã hết hạn; là Supervisor, tôi muốn kiểm tra điều kiện, trạng thái tranh chấp và phê duyệt để dữ liệu chỉ bị xóa đúng chính sách.</pre>

**US-19.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria**</pre>

**US-19.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>1. **Điều kiện lưu trữ**
   - Hệ thống tính tối thiểu tới hết bảo hành cộng 5 năm và hiển thị căn cứ tính cho từng hồ sơ.</pre>

**US-19.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>2. **Lập yêu cầu**
   - **Given** hồ sơ đủ điều kiện thời hạn
   - **When** PM chọn phạm vi và lập yêu cầu
   - **Then** hệ thống đưa yêu cầu vào trạng thái chờ Supervisor, chưa xóa dữ liệu.</pre>

**US-19.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>3. **Kiểm tra tranh chấp**
   - **Given** hồ sơ đang bị giữ do tranh chấp hoặc phạm vi chưa rõ
   - **Then** hệ thống chặn lập/duyệt xóa và hiển thị lý do.</pre>

**US-19.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>4. **Phê duyệt**
   - **When** Supervisor xem xét và phê duyệt
   - **Then** hệ thống chỉ xóa đúng phạm vi đã duyệt theo chính sách, lưu biên bản, người, thời điểm và kết quả.</pre>

**US-19.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>5. **Từ chối**
   - **When** Supervisor từ chối
   - **Then** phải có lý do; dữ liệu vẫn tra cứu được và yêu cầu chuyển trạng thái bị từ chối.</pre>

**US-19.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>6. **Giữ/gỡ giữ hồ sơ**
   - Admin/Supervisor ghi căn cứ và lý do khi thiết lập hoặc gỡ giữ; gỡ giữ không tự động xóa dữ liệu.</pre>

**US-19.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>7. **Phân biệt dọn thiết bị**
   - Dọn bản sao cục bộ trên điện thoại chỉ là thao tác đồng bộ an toàn, không được coi là xóa hồ sơ máy chủ.</pre>

**US-19.C10** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Mã truy vết:** `QT11-QT14`.</pre>

Review gate: Review US-19 details against PR-41A before RF-10-09-A implements this claim. Checkpoint: RF-10-09-A START for unconfirmed details; RF-11 retirement.

<a id="us-21"></a>

### US-21 - Reporter gửi và bổ sung phản ánh

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:581`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-21.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D19/29: US-21 trước chỉ có trong ma trận trace, chưa có thân/AC.]**</pre>

**US-21.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là Reporter, tôi muốn gửi ảnh có vị trí và theo dõi phần của mình.</pre>

**US-21.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-21.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-21.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-21-AC-01:** **Given** Reporter xác minh tài khoản; **When** gửi ảnh và mô tả; **Then** lưu report/ảnh với nguồn/time/location riêng; nhận hồ sơ tiếp nhận và thông báo.</pre>

**US-21.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-21-AC-02:** **Given** ảnh cũ upload nơi khác; **When** xác nhận vị trí; **Then** không tự lấy GPS upload; cho vị trí được xác nhận có nguồn.</pre>

**US-21.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-21-AC-03:** **Given** Reporter khác; **When** đọc report/tệp bằng ID; **Then** bị chặn; không có quyền project chỉ nhờ gửi report.</pre>

**US-21.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** PA01/PA02; BR-29/47; FR-11.</pre>

Review gate: Review US-21 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="us-22"></a>

### US-22 - PM tiếp nhận, kiểm chứng và liên kết báo trùng

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:597`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-33A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-22.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D19/29: bổ sung thân US-22 đã có ở trace; liên kết/tách chi tiết là đề xuất.]**</pre>

**US-22.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn xử lý phản ánh và tránh giao sửa trùng.</pre>

**US-22.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-22.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-22.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-22-AC-01:** **Given** chưa rõ dự án; **When** tiếp nhận; **Then** [ĐỀ XUẤT] vào hàng điều phối, không mất report.</pre>

**US-22.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-22-AC-02:** **Given** năm report ứng viên cùng lỗi; **When** PM xác nhận liên kết; **Then** giữ năm nguồn và hồ sơ chính; không tạo năm nhiệm vụ sửa.</pre>

**US-22.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-22-AC-03:** **Given** các điểm cách 1–2 m; **When** hệ thống gợi ý; **Then** [ĐỀ XUẤT Q09] không tự gộp khác loại/tấm/thời điểm; PM xác nhận, có tách gộp nhầm.</pre>

**US-22.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-22-AC-04:** **Given** một/nhiều phản ánh; **When** PM chọn kiểm chứng; **Then** cho trực tiếp hoặc drone, lưu căn cứ; ngoài phạm vi/trùng khác NO_DEFECT.</pre>

**US-22.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** PA03–PA05; BR-07/30/31/47; FR-12, FR-13.</pre>

Review gate: Review US-22 details against PR-33A before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="us-23"></a>

### US-23 - Hồ sơ xử lý và kết quả công bố

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:614`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-23.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D25/29: bổ sung thân US-23 ở trace; phần công bố từng lỗi Q08 chưa chốt.]**</pre>

**US-23.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, Supervisor và Reporter, tôi muốn đóng hồ sơ đúng nhánh và công bố đúng người.</pre>

**US-23.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-23.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-23.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-23-AC-01:** **Given** hồ sơ chỉ Fast Track và PM kiểm đủ; **When** đóng; **Then** PM đóng và báo Supervisor.</pre>

**US-23.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-23-AC-02:** **Given** hồ sơ hỗn hợp; **When** đóng tổng; **Then** chỉ Supervisor sau đủ phần bắt buộc theo từng nhánh.</pre>

**US-23.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-23-AC-03:** **Given** ảnh AFTER chưa được công bố hoặc kết quả chưa nghiệm thu; **When** Reporter xem; **Then** không tự hiển thị REPAIRED hoặc ảnh nội bộ.</pre>

**US-23.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-23-AC-04:** **Given** một lỗi đạt trong case còn mở; **When** PM muốn công bố từng phần; **Then** [TBD Q08] chưa tự thay điều kiện Case Verified cũ; không công bố toàn case hoàn thành.</pre>

**US-23.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** PA06/PA07; BR-25/26/29/48; FR-23, FR-25.</pre>

Review gate: Review US-23 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="us-24"></a>

### US-24 - Tuyến và bộ segment có phiên bản

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:631`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-24.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D29: bổ sung thân US-24 có ở trace; chi tiết GPX/xác nhận/chia tách sang US-30/31/32 đã được Sprint giữ mã.]**</pre>

**US-24.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn giữ lịch sử không gian khi chia lại phạm vi.</pre>

**US-24.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-24.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-24.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-24-AC-01:** **Given** tuyến/segment đã dùng; **When** tạo bản mới; **Then** giữ IDs/geometry lịch sử; không gán dữ liệu cũ vào bản mới tự động.</pre>

**US-24.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-24-AC-02:** **Given** ánh xạ phiên bản; **When** kết quả thiếu vị trí; **Then** không phân phát một lỗi cho mọi đoạn con; cần kiểm chứng.</pre>

**US-24.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-24-AC-03:** **Given** có station origin; **When** tính lý trình; **Then** origin + offset dọc tuyến, không luôn bắt đầu 0.</pre>

**US-24.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** DA13–DA16; BR-35/37; FR-05, FR-08.</pre>

Review gate: Review US-24 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="us-25"></a>

### US-25 - Phạm vi khảo sát và coverage từng band

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:647`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-25.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D28/29: bổ sung thân US-25, ngưỡng SRT/coverage Q11.]**</pre>

**US-25.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Operator, tôi muốn theo dõi phần dữ liệu thật sự đủ.</pre>

**US-25.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-25.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-25.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-25-AC-01:** **Given** task chọn Surface/LeftEdge/RightEdge; **When** bay ngược chiều tuyến; **Then** trái/phải vẫn theo chiều lý trình đã xác định.</pre>

**US-25.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-25-AC-02:** **Given** SRT trong vùng 12 m nhưng không nhìn thấy mép; **When** đánh giá; **Then** [ĐỀ XUẤT Q11] tách đạt vị trí và thiếu coverage; không đánh SUFFICIENT từ point-in-polygon.</pre>

**US-25.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-25-AC-03:** **Given** thiếu telemetry/camera; **When** tính coverage; **Then** UNKNOWN/chưa đủ căn cứ thay vì tự lấp GPS thiếu.</pre>

**US-25.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** KS15/KS16; BR-40/41; FR-28, FR-30.</pre>

Review gate: Review US-25 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="us-26"></a>

### US-26 - AI ngoài qua job bền vững và validation

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:663`; primary `RF-10-05` / proposed B; coordination RF-10-03. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-44`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-26.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D29: bổ sung thân US-26 đã có ở trace; giữ RS01–RS06.]**</pre>

**US-26.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và hệ thống, tôi muốn phân tích có nguồn và thử lại an toàn.</pre>

**US-26.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-26.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-26.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-26-AC-01:** **Given** dataset hợp lệ; **When** nhận yêu cầu phân tích; **Then** lưu manifest/job trước trả nhận, khóa version model/config/scope.</pre>

**US-26.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-26-AC-02:** **Given** worker lỗi rồi retry; **When** nhận kết quả; **Then** không nhân đôi detection, kết quả muộn không ghi đè bản hiện hành.</pre>

**US-26.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-26-AC-03:** **Given** adapter mock; **When** hiển thị/xuất kết quả; **Then** gắn nguồn mock và không ghi như độ chính xác thực nghiệm.</pre>

**US-26.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-26-AC-04:** **Given** ground truth và derived samples; **When** tính sai số; **Then** ghép bằng ID, nêu mẫu thiếu/outlier; không dùng frame gần trùng để giả test độc lập.</pre>

**US-26.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** AI15–AI17/RS01–RS06; BR-39/42/44; FR-29, FR-31.</pre>

Review gate: Review US-26 details against PR-44 before RF-10-05 implements this claim. Checkpoint: RF-10-05 START for unconfirmed details; RF-11 retirement.

<a id="us-28"></a>

### US-28 - Mời nhân sự nội bộ

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:680`; primary `RF-10-01` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-28.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D29: giữ ý nghĩa US-28 trong Sprint 1, không tái dùng mã cho Fast Track.]**</pre>

**US-28.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là Supervisor, tôi muốn mời tài khoản đúng role và scope.</pre>

**US-28.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-28.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-28.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-28-AC-01:** **Given** người mời có quyền; **When** tạo lời mời; **Then** role/scope được server kiểm; không log token.</pre>

**US-28.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-28-AC-02:** **Given** lời mời hết hạn/đã dùng; **When** nhận; **Then** không cấp tài khoản/quyền lần nữa.</pre>

**US-28.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-28-AC-03:** **Given** retry cùng thao tác; **When** gửi lại; **Then** không sinh user trùng; trạng thái lời mời truy vết được.</pre>

**US-28.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** CN01/QT01/QT02; S1-T1; FR-02.</pre>

Review gate: Review US-28 details against owner source before RF-10-01 implements this claim. Checkpoint: RF-10-01 START for unconfirmed details; RF-11 retirement.

<a id="us-29"></a>

### US-29 - Timeline hoạt động dự án

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:696`; primary `RF-10-09-A` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-29.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D29: giữ ý nghĩa US-29 Sprint 1 ProjectActivity.]**</pre>

**US-29.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Supervisor, tôi muốn xem sự kiện bền vững theo phạm vi.</pre>

**US-29.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-29.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-29.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-29-AC-01:** **Given** sự kiện nghiệp vụ thành công; **When** ghi timeline; **Then** đúng actor/time/object/scope, retry không nhân đôi.</pre>

**US-29.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-29-AC-02:** **Given** người ngoài project; **When** đọc timeline; **Then** không được xem.</pre>

**US-29.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-29-AC-03:** **Given** xem lịch sử; **When** lọc thời gian/đối tượng; **Then** truy lại nguồn; không cho sửa sự kiện để che lịch sử.</pre>

**US-29.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BC01–BC03/QT09; S1-T2; FR-34.</pre>

Review gate: Review US-29 details against owner source before RF-10-09-A implements this claim. Checkpoint: RF-10-09-A START for unconfirmed details; RF-11 retirement.

<a id="us-30"></a>

### US-30 - Nhập GPX và chỉnh tuyến nháp

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:712`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-30.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D26/29: giữ US-30 Sprint T3, mở rộng bề rộng biến thiên và vùng tổng 12 m.]**</pre>

**US-30.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn nhập tim/bề rộng theo đoạn và preview.</pre>

**US-30.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-30.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-30.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-30-AC-01:** **Given** GPX chỉ waypoint hoặc nhiều track chưa chọn; **When** import; **Then** không tự tạo tim; báo định dạng/track cần chọn theo contract.</pre>

**US-30.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-30-AC-02:** **Given** GPX track hợp lệ; **When** lọc/chỉnh; **Then** tính mét, giữ đầu/cuối và bản gốc; chỉnh tay không tự mất khi lọc lại.</pre>

**US-30.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-30-AC-03:** **Given** P1–P2 8 m, P2–P3 10 m, vùng 12 m; **When** preview; **Then** mặt đường giữ hai bề rộng; biên cách tim 6 m trên đoạn thẳng.</pre>

**US-30.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-30-AC-04:** **Given** tuyến cong chỉ có hai đầu; **When** dựng; **Then** không tuyên bố đã biết chính xác đường cong; yêu cầu thêm dữ liệu khi cần.</pre>

**US-30.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** DA02/DA13/DA16; BR-34/35/37; S1-T3; FR-05, FR-06.</pre>

Review gate: Review US-30 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="us-31"></a>

### US-31 - Supervisor xác nhận tuyến

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:729`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-31.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D29: giữ US-31 Sprint T4; contract idempotency cần đồng bộ, không giữ lỗi tự mâu thuẫn.]**</pre>

**US-31.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là Supervisor, tôi muốn công bố version tuyến từ bản PM đã chuẩn bị.</pre>

**US-31.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-31.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-31.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-31-AC-01:** **Given** nháp hợp lệ và CRS cấu hình; **When** Supervisor xác nhận; **Then** tạo đúng một version với station origin/tham số/nguồn.</pre>

**US-31.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-31-AC-02:** **Given** cùng yêu cầu đã thành công; **When** retry; **Then** trả cùng kết quả, không tạo version trùng; payload khác cùng key bị phát hiện.</pre>

**US-31.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-31-AC-03:** **Given** PM không có quyền xác nhận Supervisor; **When** gọi thao tác; **Then** bị chặn.</pre>

**US-31.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** DA13; BR-01/35; S1-T4; FR-07.</pre>

Review gate: Review US-31 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="us-32"></a>

### US-32 - Preview, chỉnh và công bố segment

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:745`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-39A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-32.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D29: giữ US-32 Sprint T5; sửa trace baseline D-08 → D-09 khi đồng bộ spec.]**</pre>

**US-32.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn chia theo mét và giữ tính liên tục.</pre>

**US-32.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-32.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-32.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-32-AC-01:** **Given** tuyến 4.500 m target 1.000 m; **When** preview; **Then** bốn segment 1.000 và một 500 m.</pre>

**US-32.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-32-AC-02:** **Given** split/merge/moveBoundary hợp lệ; **When** công bố; **Then** không hở/chồng, thao tác ngoài range bị chặn; bản công bố bất biến.</pre>

**US-32.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-32-AC-03:** **Given** phần dư dưới minimum; **When** preview; **Then** [ĐỀ XUẤT] gộp phần dư vào đoạn trước theo rule được chốt.</pre>

**US-32.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** DA14/DA15; BR-35; S1-T5; FR-08.</pre>

Review gate: Review US-32 details against PR-39A before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="us-33"></a>

### US-33 - PM lập policy và Crew Fast Track

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:761`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-33.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D22/23/24: story mới; không áp gate server trước sửa hoặc thời hạn mất mạng.]**</pre>

**US-33.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Crew, tôi muốn xử lý một lỗi nhỏ cùng chuyến đúng quyền.</pre>

**US-33.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-33.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-33.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-33-AC-01:** **Given** PM lập policy; **When** Crew tải nhiệm vụ; **Then** hiển thị loại/điều kiện/phương pháp/bằng chứng và version; khung ban hành/hạn mức Q02/Q03.</pre>

**US-33.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-33-AC-02:** **Given** một lỗi nhỏ được giao đo-và-sửa, số đo đạt policy, BEFORE đủ; **When** Crew sửa offline; **Then** cho sửa không cần PM duyệt số đo trước; lưu AFTER/báo cáo, không tự đóng.</pre>

**US-33.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-33-AC-03:** **Given** task chỉ-đo hoặc PM xác định nghiêm trọng; **When** Crew muốn sửa dù số đo nhỏ; **Then** không được sửa; báo PM.</pre>

**US-33.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-33-AC-04:** **Given** ngoài policy hoặc lỗi mới ngoài nhiệm vụ; **When** Crew ghi nhận; **Then** chờ PM, không tự sửa hoặc tự đổi urgency/severity.</pre>

**US-33.C09** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-33-AC-05:** **Given** PM nhận đủ Fast Track; **When** kiểm đạt; **Then** PM đóng và báo Supervisor; không tạo vòng duyệt sửa.</pre>

**US-33.C10** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** SC13/TN03/HT04/HT09/HT12; BR-05/06/08/11–18/25; FR-15, FR-18.</pre>

Review gate: Review US-33 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-34"></a>

### US-34 - PM phân cấp và sắp xếp thứ tự

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:779`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-34.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D20: story mới; độ nghiêm trọng khác độ khẩn cấp và nhánh sửa.]**</pre>

**US-34.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn tự quyết định ưu tiên từ gợi ý.</pre>

**US-34.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-34.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-34.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-34-AC-01:** **Given** Reporter/Crew gửi cảnh báo; **When** PM đánh giá; **Then** lưu hai trường phân cấp và căn cứ; ba mức urgency đã chốt.</pre>

**US-34.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-34-AC-02:** **Given** hệ thống có gợi ý mới; **When** PM mở kế hoạch; **Then** không tự thay thứ tự đã giao; PM tự chọn/sắp lại.</pre>

**US-34.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-34-AC-03:** **Given** LOW có urgency Khẩn cấp và policy cho phép; **When** PM giao Fast Track; **Then** không bị chuyển EMERGENCY chỉ do urgency.</pre>

**US-34.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** SC14/AI05/BC04; BR-03/04/13/14; FR-14.</pre>

Review gate: Review US-34 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-35"></a>

### US-35 - Gom đợt đo rồi phân công sửa

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:795`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-32A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-35.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D21: story mới, thay cách hiểu mọi LOW trong chuyến gom đều được tự sửa.]**</pre>

**US-35.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Crew, tôi muốn giảm chuyến đi và giữ quyền quyết định PM.</pre>

**US-35.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-35.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-35.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-35-AC-01:** **Given** 10 lỗi có 5 lỗi nhỏ trong đợt gom chỉ-đo; **When** Crew xác nhận năm lỗi đạt policy; **Then** chỉ đo/chụp/báo, không sửa ngay.</pre>

**US-35.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-35-AC-02:** **Given** PM có số đo/ảnh đạt; **When** lập và giao sửa; **Then** tạo task sửa riêng sau đo; có thể Fast Track nếu đủ policy/quyền; task MEASURE_ONLY không bị đổi hồi tố.</pre>

**US-35.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-35-AC-03:** **Given** có thêm report giữa tuần; **When** refresh dữ liệu; **Then** [ĐỀ XUẤT] không tự đổi loại nhiệm vụ đã giao; không tự buộc chờ đủ bảy ngày.</pre>

**US-35.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** TN07/TN01/SC14; BR-09/10; FR-16.</pre>

Review gate: Review US-35 details against PR-32A before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-36"></a>

### US-36 - Tấm bê tông và nhiều lỗi trên tấm

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:811`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-36.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D27: story mới; schema tấm và rule gộp là thiết kế đề xuất.]**</pre>

**US-36.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Crew, tôi muốn định vị và nhóm việc mà vẫn giữ từng lỗi.</pre>

**US-36.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-36.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-36.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-36-AC-01:** **Given** chỉ biết khoảng 4 m; **When** sinh lưới; **Then** ghi dự kiến, không tự là tấm hoàn công; nhiều dải có nhiều tấm.</pre>

**US-36.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-36-AC-02:** **Given** vỡ mép và ổ gà cùng tấm; **When** nhóm công việc; **Then** giữ hai lỗi và kết quả riêng; một lỗi đạt không đóng toàn tấm.</pre>

**US-36.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-36-AC-03:** **Given** lỗi trên khe hoặc nhiều tấm; **When** gắn vị trí; **Then** [ĐỀ XUẤT] liên kết nhiều tấm, thiếu độ chính xác thì chờ xác nhận.</pre>

**US-36.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** DA18/AI08; BR-31–33; FR-10.</pre>

Review gate: Review US-36 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="us-37"></a>

### US-37 - Phản ánh sau đóng và sửa tiếp

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:827`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-37.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D25: story mới; quyền mở lại case Supervisor chưa chốt.]**</pre>

**US-37.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn phân biệt lần sửa chưa đạt với tái phát.</pre>

**US-37.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-37.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-37.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-37-AC-01:** **Given** report cùng vùng đã sửa; **When** kiểm chứng; **Then** PM quyết định chưa đạt hay tái phát, không auto merge do GPS.</pre>

**US-37.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-37-AC-02:** **Given** sửa trước chưa đạt; **When** xử lý; **Then** [ĐỀ XUẤT] mở lại case cũ theo quyền được chốt, giữ lịch sử đóng.</pre>

**US-37.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-37-AC-03:** **Given** tái phát sau nghiệm thu hợp lệ; **When** xử lý; **Then** [ĐỀ XUẤT] case mới liên kết case cũ, không xóa nghiệm thu trước.</pre>

**US-37.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** PA08/HT10/HT13; BR-27/28; FR-24.</pre>

Review gate: Review US-37 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="us-38"></a>

### US-38 - Mạng đường nhiều nhánh

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:843`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-38.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D26: nhu cầu chốt, mô hình nút/edge đề xuất.]**</pre>

**US-38.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM, tôi muốn nhập mạng có đường cong và bề rộng biến thiên.</pre>

**US-38.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-38.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-38.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-38-AC-01:** **Given** trục chính và hai nhánh; **When** nhập/chỉnh; **Then** mã nhánh/chiều tuyến riêng, chọn đúng nút nối.</pre>

**US-38.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-38-AC-02:** **Given** hai đường giao khác cao độ hoặc vị trí nhiều ứng viên; **When** kết nối/gán lỗi; **Then** không tự nối hoặc gán chắc chắn; yêu cầu xác nhận.</pre>

**US-38.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-38-AC-03:** **Given** đường cong/bề rộng thay đổi; **When** preview; **Then** mặt đường/vùng khảo sát hợp lệ; nội suy không đổi nguồn thành thực đo.</pre>

**US-38.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** DA17/DA02; BR-34–36; FR-09.</pre>

Review gate: Review US-38 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="us-39"></a>

### US-39 - Khảo sát mạng nhánh và kiểm dữ liệu bay

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:859`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-39.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D28: hướng giải quyết đề xuất, không tích hợp điều khiển drone.]**</pre>

**US-39.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Operator, tôi muốn chọn phạm vi bay hợp lý và biết phần thiếu.</pre>

**US-39.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-39.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-39.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-39-AC-01:** **Given** nhiều nhánh/điểm tập kết; **When** lập kế hoạch; **Then** không bắt mọi chuyến trục chính trước; PM chọn phạm vi, Operator kiểm mission ngoài app.</pre>

**US-39.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-39-AC-02:** **Given** export/import Dronelink; **When** thực hiện thử; **Then** mã/version phạm vi truy vết được, không giả thành chuyến bay đã nghiệm thu.</pre>

**US-39.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-39-AC-03:** **Given** SRT có đoạn chuyển nhánh hoặc thiếu thông số camera; **When** đánh giá; **Then** [ĐỀ XUẤT Q11] tách phạm vi thu, vị trí/quality/coverage; thiếu thì chưa xác định.</pre>

**US-39.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** KS18/KS15/KS16; BR-36/41/43; FR-28, FR-33.</pre>

Review gate: Review US-39 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="us-40"></a>

### US-40 - Chỉ đường từ nhiệm vụ

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:875`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-40.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D29: chức năng đã chốt Sprint 1, nguồn task tối thiểu cần đồng bộ Sprint spec.]**</pre>

**US-40.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là Crew hoặc Operator, tôi muốn xem đích và chuyển Google Maps.</pre>

**US-40.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-40.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-40.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-40-AC-01:** **Given** Crew mở nhiệm vụ có quyền và đích hợp lệ; **When** bấm Chỉ đường; **Then** mở Google Maps với WGS84 đúng thứ tự, không gửi PII không cần.</pre>

**US-40.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-40-AC-02:** **Given** Operator chưa có điểm tiếp cận/tập kết; **When** bấm chỉ đường; **Then** yêu cầu bổ sung, không lấy trung điểm segment/GPS drone.</pre>

**US-40.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-40-AC-03:** **Given** không mở được ứng dụng ngoài; **When** thao tác; **Then** cho xem/sao chép tọa độ; không tự đánh dấu đến nơi/hoàn thành.</pre>

**US-40.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** HT02/KS17; BR-38; FR-32.</pre>

Review gate: Review US-40 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="us-41"></a>

### US-41 - Xử lý tạm EMERGENCY

Source: `docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md:891`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-35A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**US-41.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>&gt; **[THÊM UC-D29: chi tiết kế thừa Design v2, thời hạn/năng lực phải chốt; không đồng nhất urgency với nhánh.]**</pre>

**US-41.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**User Story:** Là PM và Crew đủ điều kiện, tôi muốn xử lý tình huống tức thời và hậu kiểm.</pre>

**US-41.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Tiền điều kiện:** tài khoản/quyền và đối tượng hợp lệ theo story; ngoại tuyến dùng bản đã tải, không giả server đã nhận. **Kết quả:** theo từng AC dưới đây.</pre>

**US-41.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Acceptance Criteria R3**</pre>

**US-41.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-41-AC-01:** **Given** PM kích hoạt có lý do; **When** giao việc; **Then** thông báo Supervisor, scope tạm và đội đủ điều kiện.</pre>

**US-41.C06** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-41-AC-02:** **Given** rào chắn/vá tạm hoàn thành; **When** hậu kiểm; **Then** không tự đóng Defect còn cần sửa chính thức.</pre>

**US-41.C07** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **US-41-AC-03:** **Given** urgency Khẩn cấp nhưng PM chưa kích hoạt; **When** xem lỗi; **Then** không tự tạo nhiệm vụ EMERGENCY.</pre>

**US-41.C08** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** SC10/HT12; BR-14/46; FR-37.</pre>

Review gate: Review US-41 details against PR-35A before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.
