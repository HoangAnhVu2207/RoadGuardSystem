# Functional and business rules (RF-04 historical transfer)

These are source contents, not newly Accepted rules. The per-item source status is retained as evidence; current authority and implementation readiness are separate. Owner review is required before each named module uses unconfirmed detail. Accepted 32-44 are in [requirements](requirements.md).

<a id="fr-01"></a>

### FR-01 - Xác thực và quyền

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:24`; primary `RF-10-01` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-36A/37`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-01.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA. **Trace:** US-01, US-17; BR-02.</pre>

**FR-01.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Tài khoản, phiên, role, membership/ownership.</pre>

**FR-01.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Phiên hợp lệ hoặc từ chối theo phạm vi.</pre>

**FR-01.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given người ngoài phạm vi; When đọc/sửa hoặc tải tệp qua ID/URL; Then bị chặn và không lộ dữ liệu. Đổi quyền trên server áp dụng request kế tiếp; việc chưa sync xét riêng xung đột.</pre>

Review gate: Review FR-01 details against PR-36A/37 before RF-10-01 implements this claim. Checkpoint: RF-10-01 START for unconfirmed details; RF-11 retirement.

<a id="fr-02"></a>

### FR-02 - Mời nhân sự

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:32`; primary `RF-10-01` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-02.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-S1. **Căn cứ:** KẾ THỪA SPRINT. **Trace:** US-28; BR-02.</pre>

**FR-02.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Người mời có quyền, email, role, scope.</pre>

**FR-02.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Lời mời có trạng thái và người dùng sau nhận.</pre>

**FR-02.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given lời mời đã dùng/hết hạn; When nhận lại; Then không kích hoạt thêm tài khoản hoặc quyền; không log token/mật khẩu.</pre>

Review gate: Review FR-02 details against owner source before RF-10-01 implements this claim. Checkpoint: RF-10-01 START for unconfirmed details; RF-11 retirement.

<a id="fr-03"></a>

### FR-03 - Reporter đăng ký

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:40`; primary `RF-10-01` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-03.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA. **Trace:** US-27; BR-02, BR-29.</pre>

**FR-03.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Email hợp lệ, thông tin Reporter, mật khẩu và OTP.</pre>

**FR-03.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Reporter đã xác minh; không có ProjectMember.</pre>

**FR-03.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given OTP sai/hết hạn hoặc client chọn PM; When xác minh/đăng ký; Then không cấp quyền nội bộ và không cho gửi phản ánh chưa xác minh.</pre>

Review gate: Review FR-03 details against owner source before RF-10-01 implements this claim. Checkpoint: RF-10-01 START for unconfirmed details; RF-11 retirement.

<a id="fr-04"></a>

### FR-04 - Khởi tạo và bảo hành

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:48`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-04.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-S1. **Căn cứ:** CHỐT + KẾ THỪA. **Trace:** US-03; BR-01, BR-45.</pre>

**FR-04.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Thông tin dự án, bàn giao/bảo hành, PM.</pre>

**FR-04.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Dự án có đúng PM chính và phạm vi quyền.</pre>

**FR-04.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given Supervisor tạo dự án; When giao PM và lưu; Then PM được nhập tuyến, người không thuộc dự án không được chỉnh. Chuyển PM giữ lịch sử.</pre>

Review gate: Review FR-04 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-05"></a>

### FR-05 - CRS và tính mét

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:56`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-05.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-S1. **Căn cứ:** KẾ THỪA + ĐỀ XUẤT. **Trace:** US-24, US-30; BR-35.</pre>

**FR-05.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Tọa độ nguồn, CRS, tuyến, station origin.</pre>

**FR-05.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Hình học mét và GeoJSON WGS84 có nguồn.</pre>

**FR-05.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given CRS thiếu hoặc không tương thích; When đo/buffer; Then không trả kết quả giả. Với dữ liệu phẳng chênh 30/40 m thì khoảng cách 50 m trong dung sai test; tuyến cong tính theo polyline.</pre>

Review gate: Review FR-05 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-06"></a>

### FR-06 - Nhập/chỉnh tim và bề rộng

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:64`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-06.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-S1. **Căn cứ:** CHỐT. **Trace:** US-30; BR-34, BR-35, BR-37.</pre>

**FR-06.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** GPX/chuỗi tọa độ, bề rộng theo đoạn, vùng khảo sát.</pre>

**FR-06.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Bản nháp có nguồn, bản lọc và bản chỉnh.</pre>

**FR-06.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given đoạn rộng 8 m và 10 m với vùng tổng 12 m; When preview; Then mặt đường khác nhau, biên vùng cách tim 6 m trên đoạn thẳng. GPX nhiều track phải chọn; waypoint-only không tự thành tuyến.</pre>

Review gate: Review FR-06 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-07"></a>

### FR-07 - Xác nhận phiên bản tuyến

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:72`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-07.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-S1. **Căn cứ:** KẾ THỪA. **Trace:** US-31; BR-01, BR-35.</pre>

**FR-07.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Bản nháp hợp lệ, quyền Supervisor.</pre>

**FR-07.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** RoadSectionVersion và lịch sử nguồn.</pre>

**FR-07.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given bản đã xác nhận; When retry cùng yêu cầu; Then không sinh version trùng. Sửa hình học đã dùng không ghi đè dữ liệu cũ. Contract retry cụ thể đồng bộ Sprint T4.</pre>

Review gate: Review FR-07 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-08"></a>

### FR-08 - Chia và công bố segment

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:80`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-39A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-08.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-S1. **Căn cứ:** KẾ THỪA. **Trace:** US-24, US-32; BR-35.</pre>

**FR-08.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Phiên bản tuyến, chiều dài/ranh segment.</pre>

**FR-08.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Bộ segment có version và lý trình.</pre>

**FR-08.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given tuyến 4.500 m, target 1.000 m; When chia; Then có bốn đoạn 1.000 m và đoạn 500 m. Không hở/chồng; đổi bộ không đổi job cũ. Phần dư quá nhỏ theo rule cấu hình còn chờ chốt.</pre>

Review gate: Review FR-08 details against PR-39A before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-09"></a>

### FR-09 - Mạng nhiều nhánh

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:88`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-09.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-TBD. **Căn cứ:** NHU CẦU CHỐT / THIẾT KẾ ĐỀ XUẤT. **Trace:** US-38; BR-34, BR-36.</pre>

**FR-09.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Nút giao, polyline từng nhánh, chiều tuyến.</pre>

**FR-09.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Mạng có mã nhánh và phạm vi chọn khảo sát.</pre>

**FR-09.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given hai nhánh gần nhau hoặc giao khác cao độ; When gán vị trí; Then không tự nối/gán chắc khi thiếu căn cứ; PM xác nhận khi nhiều ứng viên.</pre>

Review gate: Review FR-09 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-10"></a>

### FR-10 - Quản lý tấm

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:96`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-10.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-TBD. **Căn cứ:** NHU CẦU CHỐT / THIẾT KẾ ĐỀ XUẤT. **Trace:** US-36; BR-31, BR-32, BR-33.</pre>

**FR-10.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Mốc khe, dải tấm, hoàn công hoặc chiều dài dự kiến.</pre>

**FR-10.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Tấm dự kiến/đã xác nhận; liên kết lỗi.</pre>

**FR-10.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given vỡ mép và ổ gà cùng tấm; When nhóm; Then giữ hai Defect riêng. Ranh segment không cắt giả tấm; chưa đo khe không gọi lưới là tấm thật.</pre>

Review gate: Review FR-10 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-11"></a>

### FR-11 - Gửi phản ánh

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:104`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-11.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT + KẾ THỪA. **Trace:** US-21; BR-29, BR-47.</pre>

**FR-11.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Reporter, ảnh, vị trí/nguồn từng ảnh, mô tả.</pre>

**FR-11.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Report, hồ sơ tiếp nhận và thông báo.</pre>

**FR-11.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given ảnh cũ được upload ở nơi khác; When gửi; Then không dùng GPS upload làm vị trí chụp. Chưa rõ dự án giữ hàng điều phối, không mất dữ liệu.</pre>

Review gate: Review FR-11 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="fr-12"></a>

### FR-12 - Liên kết báo trùng

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:112`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-33A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-12.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-TBD. **Căn cứ:** ĐỀ XUẤT HOÀN THIỆN. **Trace:** US-22; BR-30, BR-31.</pre>

**FR-12.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Report/detection và ứng viên gần vị trí.</pre>

**FR-12.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Quyết định liên kết/tách và hồ sơ chính.</pre>

**FR-12.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given năm report cùng lỗi; When PM xác nhận liên kết; Then giữ năm nguồn, một phạm vi xử lý chính và không lộ danh tính. Khoảng 1–2 m không tự hợp khác loại lỗi.</pre>

Review gate: Review FR-12 details against PR-33A before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="fr-13"></a>

### FR-13 - Kiểm chứng phản ánh/AI

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:120`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-13.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT. **Trace:** US-08, US-20, US-22; BR-07, BR-39.</pre>

**FR-13.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Report/Defect sơ bộ và căn cứ.</pre>

**FR-13.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Lựa chọn kiểm chứng, kết luận PM.</pre>

**FR-13.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given chưa cần số đo vật lý và bằng chứng drone đủ; When PM xác minh; Then không bắt nhiệm vụ đo giả. Khi cần số đo mà thiếu thì chưa đủ điều kiện.</pre>

Review gate: Review FR-13 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="fr-14"></a>

### FR-14 - Phân cấp và ưu tiên

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:128`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-14.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT. **Trace:** US-34; BR-03, BR-04, BR-13, BR-14.</pre>

**FR-14.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Bằng chứng, severity/urgency, danh sách kế hoạch.</pre>

**FR-14.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Phân cấp có lịch sử và thứ tự PM.</pre>

**FR-14.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given gợi ý hệ thống đổi; When refresh; Then thứ tự PM đã giao giữ nguyên. Reporter/Crew không tự ghi đè. Lỗi chờ gom vẫn giữ mức khẩn cấp.</pre>

Review gate: Review FR-14 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="fr-15"></a>

### FR-15 - Lập policy Fast Track

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:136`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-15.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT / CHI TIẾT TBD. **Trace:** US-33; BR-11, BR-12.</pre>

**FR-15.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** PM, phiên bản, điều kiện, loại lỗi, biện pháp.</pre>

**FR-15.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Policy hiển thị Crew và bản áp dụng nhiệm vụ.</pre>

**FR-15.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given Crew xem ngoại tuyến; When mở nhiệm vụ đã tải; Then thấy đúng policy đã nhận. Không đủ cấu hình bắt buộc không tự kết luận đủ điều kiện. D03: Supervisor ban hành khung, PM kích hoạt trong khung; vượt khung cần phê duyệt.</pre>

**FR-15.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Readiness / acceptance gate: CONDITIONAL.** Q02 business authority đã chốt bởi D03. Q03 technical basis/ngưỡng/hạn mức và framework/version/exception contract vẫn chưa freeze; production activation/evaluation fail closed khi thiếu hồ sơ.</pre>

Review gate: Review FR-15 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-16"></a>

### FR-16 - Gom đợt đo

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:146`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-32A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-16.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT D02/32A. **Trace:** US-35; BR-09/10.</pre>

**FR-16.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Gate:** batch luôn chỉ đo; reminder hằng tuần chỉ nhắc PM rà soát, không tự tạo/giao task hoặc cấp quyền. Cần contract/config/test cho reminder và link task sửa sau đo; không hỏi lại quyết định 32A.</pre>

**FR-16.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** PM chọn lỗi lớn/nhỏ, đội, nhiệm vụ chỉ-đo.</pre>

**FR-16.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Đợt đo, kết quả từng lỗi; kế hoạch sửa sau.</pre>

**FR-16.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given 10 lỗi có 5 lỗi nhỏ; When Crew đo thấy năm lỗi đạt policy; Then chỉ gửi kết quả, không tự sửa. PM phân công sửa bằng hành động riêng.</pre>

Review gate: Review FR-16 details against PR-32A before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-17"></a>

### FR-17 - Phiên đo và xác nhận

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:156`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-17.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT. **Trace:** US-20; BR-05, BR-07, BR-17.</pre>

**FR-17.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Task, người/đội, số đo/đơn vị, dụng cụ, vị trí, ảnh.</pre>

**FR-17.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Phiên đo có phiên bản và kết quả kiểm tra.</pre>

**FR-17.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given phiên đo ngoài Fast Track thiếu ảnh hoặc số đo; When nộp; Then không được chấp nhận và phải đo lại theo Q05. Số đo nhỏ hơn kết luận PM không cấp quyền sửa.</pre>

Review gate: Review FR-17 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-18"></a>

### FR-18 - Fast Track cùng chuyến

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:164`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-18.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT. **Trace:** US-33; BR-05, BR-06, BR-08, BR-14, BR-15, BR-25.</pre>

**FR-18.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Nhiệm vụ đo-và-sửa, policy, BEFORE, số đo.</pre>

**FR-18.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Lần sửa, AFTER, báo cáo PM.</pre>

**FR-18.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given một lỗi nhỏ được giao đo-và-sửa, offline và đạt policy; When Crew thực hiện; Then lưu kết quả/sync sau; không chờ PM duyệt từng số đo, không tự đóng.</pre>

**FR-18.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Readiness / acceptance gate: CONDITIONAL.** Nhánh nghiệp vụ Fast Track đã chốt; nghiệm thu end-to-end phụ thuộc Q02/Q03 và các case Q04/Q06 liên quan. Không tự thêm gate Supervisor duyệt sửa trước thi công.</pre>

Review gate: Review FR-18 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-19"></a>

### FR-19 - Duyệt từng công việc

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:174`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-19.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT + KẾ THỪA. **Trace:** US-11; BR-21, BR-22, BR-23.</pre>

**FR-19.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Phương án/bằng chứng từng item, bản trình.</pre>

**FR-19.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Quyết định từng item và lịch sử.</pre>

**FR-19.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given A được duyệt, B cần ảnh, C bị từ chối; When lưu; Then A đủ điều kiện giao, B chờ ảnh, C kết thúc đề xuất nhưng Defect mở.</pre>

Review gate: Review FR-19 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-20"></a>

### FR-20 - Phân công Crew

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:182`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-20.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT / NGOẠI LỆ ĐỀ XUẤT. **Trace:** US-12; BR-03, BR-23, BR-24.</pre>

**FR-20.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** PM, item/phạm vi, Crew, thứ tự, phiên bản.</pre>

**FR-20.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Assignment và dấu nhận/bàn giao.</pre>

**FR-20.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given item APPROVAL_TRACK chưa duyệt; When giao thi công; Then bị chặn. Đổi đội giữ lịch sử; xung đột đội cũ offline không giải quyết bằng ghi đè.</pre>

Review gate: Review FR-20 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-21"></a>

### FR-21 - Ảnh trước/sau và tiến độ

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:190`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-21.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT / ĐIỀU KIỆN ĐỀ XUẤT. **Trace:** US-13; BR-17, BR-18, BR-20.</pre>

**FR-21.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Ảnh gốc/ảnh đo, thời điểm, lần sửa, số đo.</pre>

**FR-21.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Evidence đúng nguồn và lần xử lý.</pre>

**FR-21.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given Fast Track có ảnh Reporter phù hợp; When ghi BEFORE; Then giữ nguồn, không giả ảnh Crew mới chụp. Thiếu BEFORE hợp lệ chặn bắt đầu theo app.</pre>

Review gate: Review FR-21 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-22"></a>

### FR-22 - Hàng đợi ngoại tuyến

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:198`; primary `RF-10-08` / proposed B; coordination RF-10-07. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-22.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT + KẾ THỪA. **Trace:** US-02; BR-15, BR-16, BR-19, BR-20.</pre>

**FR-22.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Dữ liệu cục bộ, snapshot, thao tác đã gửi.</pre>

**FR-22.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Sync idempotent, toàn vẹn hoặc hàng xung đột.</pre>

**FR-22.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given app bị dừng/mất mạng khi upload; When được chạy lại có mạng; Then tiếp tục không tạo bản ghi trùng. Mất mạng không tự hết quyền Fast Track.</pre>

**FR-22.C05** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Readiness / acceptance gate: CONDITIONAL.** D05/D06/42A đã chốt business authority cho handover/conflict/rescue. End-to-end vẫn chờ acknowledgement/intake/rescue wire schema, security, atomic race and device/key tests. Core durability/dedup có thể kiểm riêng; không ghi FR-22 toàn bộ PASS trước các gate kỹ thuật.</pre>

Review gate: Review FR-22 details against owner source before RF-10-08 implements this claim. Checkpoint: RF-10-08 START for unconfirmed details; RF-11 retirement.

<a id="fr-23"></a>

### FR-23 - Kiểm tra và đóng

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:208`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-23.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT. **Trace:** US-14, US-23; BR-25, BR-26, BR-27.</pre>

**FR-23.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Báo cáo đủ tệp, kết quả theo lỗi.</pre>

**FR-23.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Đạt/chưa đạt, đóng theo thẩm quyền.</pre>

**FR-23.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given Fast Track đạt; When PM xác nhận; Then đóng và báo Supervisor. Hồ sơ hỗn hợp còn một lỗi chưa đạt không được đóng tổng.</pre>

Review gate: Review FR-23 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-24"></a>

### FR-24 - Sửa lại/tái phát

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:216`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-24.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** CHỐT / QUYỀN MỞ LẠI TBD. **Trace:** US-14, US-37; BR-27, BR-28.</pre>

**FR-24.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Phản ánh sau sửa, hồ sơ trước và bằng chứng.</pre>

**FR-24.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Kết luận chưa đạt/tái phát và lịch sử.</pre>

**FR-24.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given cùng vị trí sau nghiệm thu; When report mới; Then PM phân biệt, không auto merge hoặc auto reopen. Quyền mở lại hồ sơ Supervisor theo Q07.</pre>

Review gate: Review FR-24 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-25"></a>

### FR-25 - Công bố kết quả

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:224`; primary `RF-10-07` / proposed A; coordination RF-10-06. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-25.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA / TỪNG PHẦN TBD. **Trace:** US-23; BR-29, BR-48.</pre>

**FR-25.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Quyết định hợp lệ, ảnh được PM chọn.</pre>

**FR-25.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Timeline công khai theo người báo.</pre>

**FR-25.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given chưa được nghiệm thu; When Crew upload; Then không tự công bố REPAIRED. Công bố từng phần chờ Q08; không lộ bằng chứng nội bộ.</pre>

Review gate: Review FR-25 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="fr-26"></a>

### FR-26 - Nhiệm vụ khảo sát

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:232`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-26.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA. **Trace:** US-04, US-05, US-07; BR-07, BR-40, BR-43.</pre>

**FR-26.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** PM chọn version/segment/band, Operator, điểm tiếp cận.</pre>

**FR-26.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Task, trạng thái nhận/từ chối/đổi/bổ sung.</pre>

**FR-26.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given thiếu dữ liệu mép phải; When PM yêu cầu bổ sung; Then giữ mép trái đã đạt và lịch sử, có thể đổi Operator.</pre>

Review gate: Review FR-26 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="fr-27"></a>

### FR-27 - Tiếp nhận video/telemetry

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:240`; primary `RF-10-04` / proposed B; coordination RF-10-03, RF-10-05. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-38`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-27.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA. **Trace:** US-06; BR-20, BR-42.</pre>

**FR-27.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Video, SRT/phụ đề, metadata chuyến.</pre>

**FR-27.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Dataset toàn vẹn, quality và job riêng.</pre>

**FR-27.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given thiếu telemetry; When upload video hợp lệ; Then không giả đủ định vị/coverage; giữ trạng thái thiếu theo contract. Không mất bản gốc khi retry.</pre>

Review gate: Review FR-27 details against PR-38 before RF-10-04 implements this claim. Checkpoint: RF-10-04 START for unconfirmed details; RF-11 retirement.

<a id="fr-28"></a>

### FR-28 - Đánh giá SRT và coverage

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:248`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-28.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-TBD. **Căn cứ:** KẾ THỪA / TIÊU CHÍ TBD. **Trace:** US-25, US-39; BR-35, BR-41.</pre>

**FR-28.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Video/telemetry đồng bộ, phạm vi nhiệm vụ.</pre>

**FR-28.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Kết quả vị trí, chất lượng và coverage riêng.</pre>

**FR-28.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given GPS trong vùng nhưng không nhìn được mép; When đánh giá; Then không tự đạt coverage. Tách thời gian chuyển nhánh/cất-hạ cánh khi đủ dữ liệu; thiếu thì UNKNOWN.</pre>

Review gate: Review FR-28 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="fr-29"></a>

### FR-29 - AI bất đồng bộ

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:256`; primary `RF-10-05` / proposed B; coordination RF-10-03. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-29.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA. **Trace:** US-26; BR-39, BR-42.</pre>

**FR-29.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Dataset, manifest, model/config có version.</pre>

**FR-29.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** JobId, raw result và detections có nguồn.</pre>

**FR-29.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given worker chết/retry; When chạy lại; Then không mất manifest hoặc tạo kết quả nghiệp vụ trùng. Mock có nhãn; result muộn không ghi đè bản hiện hành.</pre>

Review gate: Review FR-29 details against owner source before RF-10-05 implements this claim. Checkpoint: RF-10-05 START for unconfirmed details; RF-11 retirement.

<a id="fr-30"></a>

### FR-30 - Baseline và theo kỳ

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:264`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-30.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA. **Trace:** US-04, US-09, US-25; BR-39, BR-40.</pre>

**FR-30.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Dữ liệu/band đủ điều kiện, kết luận PM.</pre>

**FR-30.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Baseline theo segment/band.</pre>

**FR-30.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given mặt đường đủ nhưng mép thiếu; When xác nhận; Then chỉ phần đủ được baseline; không đổi lịch sử khi model/tuyến thay.</pre>

Review gate: Review FR-30 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="fr-31"></a>

### FR-31 - Research validation

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:272`; primary `RF-10-05` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-31.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-RESEARCH. **Căn cứ:** KẾ THỪA BẮT BUỘC NGHIÊN CỨU. **Trace:** US-20, US-26; BR-44.</pre>

**FR-31.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Ground truth, derived measurement, sample IDs.</pre>

**FR-31.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Ghép cặp, bias, MAE/RMSE và báo cáo.</pre>

**FR-31.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given mẫu thiếu hoặc không ghép được; When tính; Then nêu số mẫu dùng/loại và lý do; không dùng mock làm bằng chứng độ chính xác.</pre>

Review gate: Review FR-31 details against owner source before RF-10-05 implements this claim. Checkpoint: RF-10-05 START for unconfirmed details; RF-11 retirement.

<a id="fr-32"></a>

### FR-32 - Google Maps

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:280`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-32.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-S1. **Căn cứ:** CHỐT. **Trace:** US-40; BR-38.</pre>

**FR-32.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Task có quyền và đích WGS84.</pre>

**FR-32.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Chuyển Maps hoặc hiển thị tọa độ.</pre>

**FR-32.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given Operator chưa có điểm tập kết; When bấm; Then yêu cầu bổ sung, không lấy trung điểm segment. Mở Maps không tự đổi trạng thái việc.</pre>

Review gate: Review FR-32 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-33"></a>

### FR-33 - Lập phạm vi bay nhiều nhánh

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:288`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-33.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-TBD. **Căn cứ:** NHU CẦU CHỐT / GIẢI PHÁP ĐỀ XUẤT. **Trace:** US-39; BR-36, BR-41, BR-43.</pre>

**FR-33.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Nhánh/band, điểm cất-hạ cánh, nhóm mission.</pre>

**FR-33.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Phạm vi/tệp export có mã và version.</pre>

**FR-33.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given nhiều nhánh; When lập kế hoạch; Then không bắt thứ tự trục-chính-trước cho mọi trường hợp; Operator kiểm tra mission ngoài RoadGuard, không gửi lệnh bay từ hệ thống.</pre>

Review gate: Review FR-33 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="fr-34"></a>

### FR-34 - Dashboard và timeline

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:296`; primary `RF-10-09-B` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-34.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA. **Trace:** US-15, US-29; BR-03, BR-29, BR-45.</pre>

**FR-34.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Sự kiện bền vững và trạng thái hồ sơ.</pre>

**FR-34.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Dashboard theo phạm vi, drilldown nguồn.</pre>

**FR-34.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given sự kiện retry; When đọc timeline; Then không nhân đôi; số thống kê phân biệt báo cáo/lỗi/tấm/việc sửa.</pre>

Review gate: Review FR-34 details against owner source before RF-10-09-B implements this claim. Checkpoint: RF-10-09-B START for unconfirmed details; RF-11 retirement.

<a id="fr-35"></a>

### FR-35 - Xuất và lưu trữ

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:304`; primary `RF-10-09-B` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-35.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA. **Trace:** US-16, US-19; BR-20, BR-45.</pre>

**FR-35.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Scope, bộ lọc, quyền, trạng thái giữ.</pre>

**FR-35.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Hồ sơ xuất có nguồn, tác vụ xóa được duyệt.</pre>

**FR-35.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given tranh chấp/thiếu thời hạn; When xóa; Then bị chặn. Tệp xuất ghi phần thiếu thay vì giả đầy đủ.</pre>

Review gate: Review FR-35 details against owner source before RF-10-09-B implements this claim. Checkpoint: RF-10-09-B START for unconfirmed details; RF-11 retirement.

<a id="fr-36"></a>

### FR-36 - Quản trị cấu hình/mô hình

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:312`; primary `RF-10-05` / proposed B; coordination RF-10-09-B. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-36.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M. **Căn cứ:** KẾ THỪA + 34A. **Trace:** US-10, US-17, US-18; BR-02, BR-39, BR-42, BR-45.</pre>

**FR-36.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** Admin, rule/model versions, tài khoản.</pre>

**FR-36.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Cấu hình phát hành có audit; PM duyệt/từ chối nhãn trong project và chỉ nhãn đã duyệt được export cho training; AI không tự duyệt.</pre>

**FR-36.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given đổi model/rule; When xem kết quả cũ; Then giữ phiên bản đã dùng; không tái tính ngược âm thầm.</pre>

Review gate: Review FR-36 details against owner source before RF-10-05 implements this claim. Checkpoint: RF-10-05 START for unconfirmed details; RF-11 retirement.

<a id="fr-37"></a>

### FR-37 - Emergency tạm

Source: `docs/diagram/V2/02_Requirements/01_FRD_SRS.md:320`; primary `RF-10-07` / proposed A; coordination RF-10-09-A. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-35A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**FR-37.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Ưu tiên:** M-TBD. **Căn cứ:** KẾ THỪA. **Trace:** US-41; BR-14, BR-46.</pre>

**FR-37.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Đầu vào/tiền điều kiện:** PM kích hoạt, lý do và Crew đủ điều kiện.</pre>

**FR-37.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Hành vi/đầu ra bắt buộc:** Nhiệm vụ tạm, báo Supervisor và hậu kiểm.</pre>

**FR-37.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- **Kiểm chứng:** Given rào chắn xong nhưng hư hỏng còn; When đóng nhiệm vụ tạm; Then Defect không tự RESOLVED; sửa chính thức tiếp tục theo nhánh phù hợp.</pre>

Review gate: Review FR-37 details against PR-35A before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-01"></a>

### BR-01 - Khởi tạo và người phụ trách

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:15`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-01.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-03, US-30, US-31.</pre>

**BR-01.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Supervisor tạo dự án và giao đúng một PM chính; PM nhập/chỉnh tọa độ và bề rộng.</pre>

**BR-01.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** PM không tự tạo dự án theo quyền Supervisor; thay PM giữ lịch sử.</pre>

Review gate: Review BR-01 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="br-02"></a>

### BR-02 - Phạm vi quyền

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:23`; primary `RF-10-01` / proposed A; coordination RF-10-04, RF-10-06. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-02.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-01, US-17, US-21.</pre>

**BR-02.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Mọi request kiểm tra role, membership/nhiệm vụ hoặc ownership Reporter ở server; quyền tệp theo cùng phạm vi.</pre>

**BR-02.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Người ngoài dự án không đọc/sửa hoặc tải tệp qua URL còn hiệu lực ngoài scope.</pre>

Review gate: Review BR-02 details against owner source before RF-10-01 implements this claim. Checkpoint: RF-10-01 START for unconfirmed details; RF-11 retirement.

<a id="br-03"></a>

### BR-03 - PM quyết định ưu tiên

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:31`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-03.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-34.</pre>

**BR-03.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Hệ thống chỉ gợi ý dựa severity/urgency; PM chọn lỗi, thứ tự và Crew. Không tự thay kế hoạch đã giao.</pre>

**BR-03.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Cập nhật gợi ý không làm đổi thứ tự đã lưu của PM.</pre>

Review gate: Review BR-03 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-04"></a>

### BR-04 - Hai chiều phân cấp

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:39`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-04.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-34.</pre>

**BR-04.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Lưu riêng nghiêm trọng và khẩn cấp. Ba mức khẩn cấp: Bình thường, Cần xử lý sớm, Khẩn cấp; PM quyết định.</pre>

**BR-04.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Reporter/Crew cảnh báo nhưng không tự ghi đè phân cấp chính thức.</pre>

Review gate: Review BR-04 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="br-05"></a>

### BR-05 - Không vượt kết luận PM

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:47`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-05.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-20, US-33.</pre>

**BR-05.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Khi PM xác định nghiêm trọng, Crew chỉ đo/báo; dù số đo nhỏ hơn cũng không tự sửa.</pre>

**BR-05.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Thử sửa khi PM có chỉ đạo nghiêm trọng bị chặn theo bản nhiệm vụ đã nhận.</pre>

Review gate: Review BR-05 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-06"></a>

### BR-06 - Lỗi mới ngoài nhiệm vụ

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:55`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-06.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-13, US-33.</pre>

**BR-06.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Crew chỉ ghi nhận lỗi mới ngoài phạm vi công việc, không tự sửa.</pre>

**BR-06.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Lỗi mới có báo cáo riêng cho PM, không tự thêm vào phần đã được giao sửa.</pre>

Review gate: Review BR-06 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-07"></a>

### BR-07 - Chọn kiểm chứng

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:63`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-07.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-08, US-20, US-22.</pre>

**BR-07.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM có thể chọn kiểm tra trực tiếp hoặc drone cho một hay nhiều phản ánh; đo vật lý khi quyết định cần số đo.</pre>

**BR-07.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không có gate field-first/drone-first cứng; thiếu số đo bắt buộc không được kết luận cần số đo đó.</pre>

Review gate: Review BR-07 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="br-08"></a>

### BR-08 - Một lỗi nhỏ riêng lẻ

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:71`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-08.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-33.</pre>

**BR-08.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM có thể giao đo-và-sửa; Crew đo, đạt policy và đủ bằng chứng thì Fast Track cùng chuyến.</pre>

**BR-08.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không cần PM duyệt số đo trước sửa; quyền phát sinh từ nhiệm vụ và policy.</pre>

Review gate: Review BR-08 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-09"></a>

### BR-09 - Đợt gom chỉ đo trước

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:79`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-09.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-35.</pre>

**BR-09.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Trong đợt gom nhiều lỗi, Crew đo/chụp rồi báo PM, không tự sửa ngay kể cả lỗi nhỏ đạt policy. PM phân công sửa sau.</pre>

**BR-09.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Ví dụ 10 lỗi có 5 LOW: không lỗi nào tự được sửa trong chuyến chỉ-đo.</pre>

Review gate: Review BR-09 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-10"></a>

### BR-10 - Một tuần không tự cấp quyền

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:87`; primary `RF-10-07` / proposed A; coordination RF-10-09-A. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-32A`; source label: `CHỐT 32A`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-10.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT 32A. **Truy vết:** US-35; D02/32A.</pre>

**BR-10.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM tự gom đợt đo; hệ thống nhắc PM rà soát hằng tuần. Reminder không tự tạo/giao task, không tự cấp quyền sửa và không đổi mode nhiệm vụ đang giao.</pre>

**BR-10.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Reminder xuất hiện đúng cấu hình nhưng không tạo batch/task; việc đã giao không bị client tự đổi chỉ vì thêm report.</pre>

Review gate: Review BR-10 details against PR-32A before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-11"></a>

### BR-11 - Người lập policy

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:95`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-11.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-33.</pre>

**BR-11.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM lập policy Fast Track và Crew xem policy trên app; policy chỉ định lỗi/điều kiện được tự sửa.</pre>

**BR-11.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Crew không tự sửa nội dung policy; lịch sử giữ người lập và phiên bản áp dụng.</pre>

Review gate: Review BR-11 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-12"></a>

### BR-12 - Nội dung policy

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:103`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-12.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-33.</pre>

**BR-12.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Policy ghi loại lỗi, điều kiện đo, biện pháp, bằng chứng, phạm vi, dụng cụ/vật tư tham khảo và năng lực đội; ngưỡng thực tế phải được cung cấp.</pre>

**BR-12.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Thiếu cấu hình điều kiện bắt buộc thì không kết luận ELIGIBLE; không hard-code ngưỡng suy đoán.</pre>

Review gate: Review BR-12 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-13"></a>

### BR-13 - Ngoài policy

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:111`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-13.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-20, US-33, US-34.</pre>

**BR-13.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Crew gửi kết quả cho PM; PM đánh giá phân cấp và đưa vào hàng chờ/quyết định tiếp. Không đạt policy không tự có nghĩa nghiêm trọng hơn.</pre>

**BR-13.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Lỗi còn mở; không tự giảm khẩn cấp hoặc tự chuyển severity.</pre>

Review gate: Review BR-13 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-14"></a>

### BR-14 - Khẩn cấp và Fast Track

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:119`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-14.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-33, US-34, US-41.</pre>

**BR-14.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>LOW có urgency Khẩn cấp vẫn có thể Fast Track nếu policy và nhiệm vụ cho phép; urgency không tự kích hoạt EMERGENCY.</pre>

**BR-14.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không đổi nhánh chỉ do một trường urgency.</pre>

Review gate: Review BR-14 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-15"></a>

### BR-15 - Sửa ngoại tuyến

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:127`; primary `RF-10-08` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-15.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-02, US-33.</pre>

**BR-15.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Cho Fast Track ngoại tuyến không giới hạn thời gian mất mạng, trong nhiệm vụ/policy đã được giao.</pre>

**BR-15.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Mất mạng lâu không tự hết quyền; không thêm gate online trước mỗi lần sửa.</pre>

Review gate: Review BR-15 details against owner source before RF-10-08 implements this claim. Checkpoint: RF-10-08 START for unconfirmed details; RF-11 retirement.

<a id="br-16"></a>

### BR-16 - Xung đột ngoại tuyến

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:135`; primary `RF-10-08` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-16.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-02, US-12.</pre>

**BR-16.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Lưu snapshot nhiệm vụ/policy; lệnh PM chưa được máy nhận không thể có hiệu lực tức thời trên máy đó. Khi sync giữ bằng chứng và chuyển xung đột cho PM.</pre>

**BR-16.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không dùng last-write-wins để xóa số đo, không tự chấp nhận kết quả xung đột.</pre>

Review gate: Review BR-16 details against owner source before RF-10-08 implements this claim. Checkpoint: RF-10-08 START for unconfirmed details; RF-11 retirement.

<a id="br-17"></a>

### BR-17 - Bằng chứng trước

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:143`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-17.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-13, US-20, US-33.</pre>

**BR-17.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Fast Track có thể dùng ảnh Reporter/drone làm BEFORE; đo ngoài Fast Track bắt buộc ảnh và số đo.</pre>

**BR-17.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không chấp nhận phiên đo thiếu trường/ảnh bắt buộc; phải đo lại phần được quyết định.</pre>

Review gate: Review BR-17 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-18"></a>

### BR-18 - Tính phù hợp ảnh cũ

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:151`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-18.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-13, US-33.</pre>

**BR-18.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Ảnh tái sử dụng phải đúng lỗi/nguồn/thời điểm và còn phản ánh hiện trường; nếu không phù hợp thì chụp mới trước sửa.</pre>

**BR-18.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không đổi ảnh AFTER thành BEFORE; app chặn bắt đầu sửa khi không có bằng chứng trước hợp lệ cục bộ.</pre>

Review gate: Review BR-18 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-19"></a>

### BR-19 - Tự đồng bộ

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:159`; primary `RF-10-08` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-19.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-02, US-13.</pre>

**BR-19.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Ảnh/số đo/báo cáo đã xác nhận gửi hoặc xếp hàng tự tiếp tục khi có mạng và app được phép thực thi.</pre>

**BR-19.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Nháp chưa gửi không tự trở thành báo cáo chính thức; retry không tạo lần sửa trùng.</pre>

Review gate: Review BR-19 details against owner source before RF-10-08 implements this claim. Checkpoint: RF-10-08 START for unconfirmed details; RF-11 retirement.

<a id="br-20"></a>

### BR-20 - Toàn vẹn tệp

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:167`; primary `RF-10-04` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-20.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-02, US-06.</pre>

**BR-20.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Chỉ báo lưu an toàn sau server xác nhận tệp/metadata đầy đủ và toàn vẹn; dọn bản cục bộ theo lựa chọn người dùng.</pre>

**BR-20.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Tệp thiếu/checksum sai không đủ điều kiện nghiệm thu và không bị dọn tự động.</pre>

Review gate: Review BR-20 details against owner source before RF-10-04 implements this claim. Checkpoint: RF-10-04 START for unconfirmed details; RF-11 retirement.

<a id="br-21"></a>

### BR-21 - Quyết định từng công việc

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:175`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-21.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-11.</pre>

**BR-21.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Nhánh duyệt có APPROVE, REQUEST_EVIDENCE, REQUEST_RECONSIDER, REJECT riêng từng RepairItem.</pre>

**BR-21.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Một item được duyệt không phải chờ các item khác trong gói.</pre>

Review gate: Review BR-21 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-22"></a>

### BR-22 - Từ chối phương án

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:183`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-22.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-11.</pre>

**BR-22.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>REJECT kết thúc đề xuất; Defect vẫn chưa xử lý để PM lập phương án khác.</pre>

**BR-22.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không chuyển NO_DEFECT/RESOLVED vì từ chối phương án.</pre>

Review gate: Review BR-22 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-23"></a>

### BR-23 - Điều kiện giao sửa

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:191`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-23.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-11, US-12.</pre>

**BR-23.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>APPROVAL_TRACK chỉ giao phần APPROVED; Fast Track theo quyền nhiệm vụ; đổi biện pháp/phạm vi phải xét lại phần thay đổi.</pre>

**BR-23.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** PM xếp ưu tiên cao không thay thế phê duyệt bắt buộc.</pre>

Review gate: Review BR-23 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-24"></a>

### BR-24 - Không sửa trùng

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:199`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-24.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-12.</pre>

**BR-24.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Không giao hai việc sửa chồng phạm vi hiệu lực; chuyển đội cần giữ lịch sử và xác nhận bàn giao khi đội cũ ngoại tuyến.</pre>

**BR-24.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Đổi Crew không tự đổi bằng chứng hoặc quyền đã duyệt.</pre>

Review gate: Review BR-24 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-25"></a>

### BR-25 - Đóng Fast Track

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:207`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-25.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-14, US-33.</pre>

**BR-25.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM kiểm tra đủ số đo/policy/bằng chứng và kết quả, đóng lỗi Fast Track và gửi báo cáo Supervisor; không thêm gate Supervisor duyệt sửa.</pre>

**BR-25.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Crew báo xong hoặc upload thành công không tự đóng.</pre>

Review gate: Review BR-25 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-26"></a>

### BR-26 - Đóng hồ sơ hỗn hợp

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:215`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-26.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-14, US-23.</pre>

**BR-26.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Hồ sơ hỗn hợp chờ đủ xác nhận theo từng nhánh rồi Supervisor đóng tổng.</pre>

**BR-26.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Fast Track đạt không làm toàn hồ sơ đóng khi còn lỗi bắt buộc chưa đạt.</pre>

Review gate: Review BR-26 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-27"></a>

### BR-27 - Sửa chưa đạt

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:223`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-27.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-14.</pre>

**BR-27.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Lỗi sửa chưa đạt tiếp tục xử lý; giữ lần sửa/bằng chứng trước và kết quả đạt của lỗi khác.</pre>

**BR-27.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không tạo ticket mới chỉ vì một lần sửa bị trả; phân biệt thiếu ảnh với cần thi công lại.</pre>

Review gate: Review BR-27 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-28"></a>

### BR-28 - Phân biệt tái phát

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:231`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-28.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-37.</pre>

**BR-28.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM phân biệt lần sửa trước chưa đạt với hư hỏng tái phát sau nghiệm thu.</pre>

**BR-28.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Cùng GPS không tự quyết định mở lại hoặc tạo mới; quyền mở lại hồ sơ Supervisor còn TBD.</pre>

Review gate: Review BR-28 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-29"></a>

### BR-29 - Report và riêng tư

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:239`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-29.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-21, US-23.</pre>

**BR-29.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Report giữ người gửi/bằng chứng gốc; Reporter chỉ xem phần công bố thuộc sở hữu, không xem danh tính người khác.</pre>

**BR-29.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Năm người báo không lộ ảnh nội bộ/danh tính lẫn nhau.</pre>

Review gate: Review BR-29 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="br-30"></a>

### BR-30 - Báo trùng và gộp sai

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:247`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-33A`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-30.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-22.</pre>

**BR-30.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Hệ thống gợi ý trùng; PM liên kết nhiều report về hồ sơ chính, giữ nguồn; tách lại khi gộp nhầm.</pre>

**BR-30.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không tạo năm lệnh sửa cùng một lỗi; không tăng severity chỉ vì năm report.</pre>

Review gate: Review BR-30 details against PR-33A before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="br-31"></a>

### BR-31 - Gom 1–2 m

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:255`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-31.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-09, US-36.</pre>

**BR-31.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Khoảng gần 1–2 m chỉ gợi ý, xét độ chính xác GPS, nhánh, phía đường, ảnh, loại lỗi và thời điểm trước xác nhận.</pre>

**BR-31.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Ổ gà và vỡ mép gần nhau vẫn có thể là hai lỗi.</pre>

Review gate: Review BR-31 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="br-32"></a>

### BR-32 - Các cấp quản lý

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:263`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-32.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-36, US-38.</pre>

**BR-32.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Tuyến/segment để quản lý; tấm để định vị; từng lỗi để đánh giá/nghiệm thu; nhóm công việc để gom sửa.</pre>

**BR-32.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Một tấm có nhiều lỗi, sửa một lỗi không đóng tất cả.</pre>

Review gate: Review BR-32 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="br-33"></a>

### BR-33 - Tấm dự kiến và tấm thật

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:271`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-33.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-36.</pre>

**BR-33.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Tấm sinh theo khoảng 4 m chỉ dự kiến tới khi đối chiếu khe/hoàn công; nhiều dải tăng số tấm; lỗi có thể liên quan nhiều tấm.</pre>

**BR-33.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không khẳng định mọi tấm dài 4 m theo TCVN; ranh segment không cắt giả tài sản vật lý.</pre>

Review gate: Review BR-33 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="br-34"></a>

### BR-34 - Bề rộng từng đoạn

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:279`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-34.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-30, US-38.</pre>

**BR-34.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM nhập tim và bề rộng từng khoảng; ví dụ P1–P2 8 m, P2–P3 10 m, vùng tổng mong muốn 12 m.</pre>

**BR-34.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Mặt đường giữ 8/10 m; biên vùng ví dụ cách tim 6 m, phần dư ngoài mép lần lượt 2/1 m.</pre>

Review gate: Review BR-34 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="br-35"></a>

### BR-35 - Tính mét và phiên bản

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:287`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-35.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-24, US-30, US-31, US-32.</pre>

**BR-35.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Giữ CRS nguồn, chuyển thật sang CRS mét dự án, đo dọc polyline và station origin; xuất WGS84 cho bản đồ.</pre>

**BR-35.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không trộn SRID hoặc chỉ đổi nhãn; dữ liệu cũ không tự gắn vào tuyến/segment mới.</pre>

Review gate: Review BR-35 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="br-36"></a>

### BR-36 - Mạng nhánh

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:295`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-36.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-38.</pre>

**BR-36.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Dùng nút giao và đoạn nối có polyline; giữ mã nhánh/chiều tuyến; gần giao lộ phải xác nhận khi nhiều ứng viên.</pre>

**BR-36.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không nối mọi đường cắt nhau khác cao độ; curve vertex không tự là nút giao.</pre>

Review gate: Review BR-36 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="br-37"></a>

### BR-37 - Nguồn tuyến theo sprint

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:303`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-37.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-24, US-30.</pre>

**BR-37.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>GPX ngoài app và nhập/chỉnh tim thuộc Sprint 1; nguồn track drone dựng tuyến Sprint 2; recorder điện thoại chưa chốt.</pre>

**BR-37.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** GPX thô không tự là tim đường đã công bố.</pre>

Review gate: Review BR-37 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="br-38"></a>

### BR-38 - Chỉ đường

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:311`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-38.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-40.</pre>

**BR-38.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Crew từ nhiệm vụ đo/sửa; Operator dùng điểm tiếp cận/tập kết; chuyển Google Maps ở Sprint 1.</pre>

**BR-38.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không dùng trung điểm segment hoặc GPS drone làm đích tùy ý; mở Maps không đổi trạng thái việc.</pre>

Review gate: Review BR-38 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="br-39"></a>

### BR-39 - AI là nguồn hỗ trợ

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:319`; primary `RF-10-05` / proposed B; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-44`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-39.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-08, US-26.</pre>

**BR-39.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Kết quả AI là ứng viên; PM giữ/sửa/loại và xác minh từ căn cứ phù hợp. Không detections không đồng nghĩa không có lỗi.</pre>

**BR-39.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không tự tạo kết luận NO_DEFECT/đóng bảo hành từ job AI.</pre>

Review gate: Review BR-39 details against PR-44 before RF-10-05 implements this claim. Checkpoint: RF-10-05 START for unconfirmed details; RF-11 retirement.

<a id="br-40"></a>

### BR-40 - Baseline theo band

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:327`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-40.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-04, US-25.</pre>

**BR-40.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Baseline xác nhận theo segment/band đủ điều kiện; giữ phần đạt, bổ sung phần thiếu.</pre>

**BR-40.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không dùng cờ tổng của Survey thay nguồn baseline chi tiết.</pre>

Review gate: Review BR-40 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="br-41"></a>

### BR-41 - Vị trí bay khác coverage

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:335`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-41.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-25, US-39.</pre>

**BR-41.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>SRT trong vùng chứng minh tối đa điều kiện vị trí khi dữ liệu đủ; coverage/quality đánh giá riêng.</pre>

**BR-41.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Trong vùng 12 m nhưng camera thiếu mép không tự đạt toàn bộ khảo sát.</pre>

Review gate: Review BR-41 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="br-42"></a>

### BR-42 - Job bền vững

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:343`; primary `RF-10-05` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-42.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-26.</pre>

**BR-42.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Lưu manifest/job trước khi trả nhận xử lý; nguồn mock/real và version rõ; retry giữ danh tính, kết quả muộn không ghi đè bản mới.</pre>

**BR-42.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Crash/retry không nhân đôi detection nghiệp vụ hoặc mất nguồn.</pre>

Review gate: Review BR-42 details against owner source before RF-10-05 implements this claim. Checkpoint: RF-10-05 START for unconfirmed details; RF-11 retirement.

<a id="br-43"></a>

### BR-43 - PM quyết định bay bổ sung

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:351`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `CHỐT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-43.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** CHỐT. **Truy vết:** US-07.</pre>

**BR-43.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM xác nhận phạm vi/lý do bay bổ sung, có thể đổi Operator; không giới hạn số lượt hợp lệ.</pre>

**BR-43.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Giữ dữ liệu cũ, phần đạt; lỗi server không tự tạo chuyến bay.</pre>

Review gate: Review BR-43 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="br-44"></a>

### BR-44 - Research validation

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:359`; primary `RF-10-05` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-44.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-20, US-26.</pre>

**BR-44.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Giữ RS01–RS06: ground truth thực, ghép đúng mẫu và báo sai số/độ không chắc chắn; mock không chứng minh chất lượng AI.</pre>

**BR-44.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Báo cáo nêu mẫu, thiết bị, phương pháp, outlier, dữ liệu thiếu.</pre>

Review gate: Review BR-44 details against owner source before RF-10-05 implements this claim. Checkpoint: RF-10-05 START for unconfirmed details; RF-11 retirement.

<a id="br-45"></a>

### BR-45 - Lưu trữ và audit

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:367`; primary `RF-10-09-A` / proposed A; coordination none. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-41A`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-45.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-16, US-19.</pre>

**BR-45.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Lịch sử không xóa cứng trong tác nghiệp; giữ ít nhất hết bảo hành + 5 năm theo yêu cầu dự án; tranh chấp chặn xóa.</pre>

**BR-45.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Xóa hết hạn cần Supervisor; log không chứa secret; không suy đây là thời hạn luật đã xác minh.</pre>

Review gate: Review BR-45 details against PR-41A before RF-10-09-A implements this claim. Checkpoint: RF-10-09-A START for unconfirmed details; RF-11 retirement.

<a id="br-46"></a>

### BR-46 - EMERGENCY riêng

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:375`; primary `RF-10-07` / proposed A; coordination RF-10-09-A. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-35A`; source label: `KẾ THỪA`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-46.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** KẾ THỪA. **Truy vết:** US-41.</pre>

**BR-46.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>PM kích hoạt biện pháp tạm, thông báo Supervisor và hậu kiểm; xử lý tạm không tự là sửa chính thức.</pre>

**BR-46.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Đóng nhiệm vụ rào chắn không tự đóng Defect còn cần sửa.</pre>

Review gate: Review BR-46 details against PR-35A before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="br-47"></a>

### BR-47 - Thông báo khác hồ sơ

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:383`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-47.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-21, US-22.</pre>

**BR-47.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Ticket nghiệp vụ ánh xạ IncidentCase; Notification chỉ báo sự kiện. Bổ sung evidence không tự tạo thêm ticket.</pre>

**BR-47.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Chưa biết dự án/PM vẫn nhận phản ánh và chờ điều phối.</pre>

Review gate: Review BR-47 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="br-48"></a>

### BR-48 - Công bố từng phần

Source: `docs/diagram/V2/02_Requirements/02_Business_Rules.md:391`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `ĐỀ XUẤT`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**BR-48.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Trạng thái:** ĐỀ XUẤT. **Truy vết:** US-23.</pre>

**BR-48.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Có thể công bố lỗi liên quan đã được xác nhận dù hồ sơ tổng còn mở; cần chủ dự án chốt trước triển khai.</pre>

**BR-48.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Điều kiện kiểm chứng:** Không mô tả toàn hồ sơ hoàn thành khi chỉ một lỗi đã đạt.</pre>

Review gate: Review BR-48 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.
