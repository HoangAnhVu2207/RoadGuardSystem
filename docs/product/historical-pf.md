# Process flows (RF-04 historical transfer)

These are source contents, not newly Accepted rules. The per-item source status is retained as evidence; current authority and implementation readiness are separate. Owner review is required before each named module uses unconfirmed detail. Accepted 32-44 are in [requirements](requirements.md).

<a id="pf-01"></a>

### PF-01 - Khởi tạo dự án và công bố tuyến

Source: `docs/diagram/V2/02_Requirements/03_To_Be_Process.md:20`; primary `RF-10-02` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**PF-01.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BR-01, BR-32–37; FR-04–10; US-03, US-24, US-30–32, US-36, US-38.  
**Đầu vào:** dự án Supervisor tạo, PM được giao, tọa độ/tim đường và bề rộng có nguồn. **Đầu ra:** route version xác nhận, segment set và lớp bản đồ có phiên bản.</pre>

**PF-01.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>```mermaid
flowchart TD
 A[&quot;Supervisor: tạo dự án, giao PM&quot;] --&gt; B[&quot;PM: nhập GPX hoặc các điểm tim đường&quot;]
 B --&gt; C[&quot;PM: khai báo bề rộng từng khoảng, nhánh và nút giao&quot;]
 C --&gt; D[&quot;Backend: kiểm CRS, geometry và dựng lớp đường&quot;]
 D --&gt; E{&quot;Dữ liệu đủ và hợp lệ?&quot;}
 E --&gt;|Không| C
 E --&gt;|Có| F[&quot;PM: kiểm tra bản đồ, hoàn thiện nháp&quot;]
 F --&gt; G[&quot;Supervisor: xác nhận tuyến theo luồng kế thừa&quot;]
 G --&gt; H{&quot;Được xác nhận?&quot;}
 H --&gt;|Chưa| F
 H --&gt;|Có| I[&quot;Backend: tạo phiên bản bất biến&quot;]
 I --&gt; J[&quot;PM: chia segment, kiểm tra và công bố&quot;]
```</pre>

**PF-01.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>- GPX Sprint 1 là nháp; lọc điểm không biến GPS thành khảo sát chính xác. GPS drone lấy tuyến thuộc Sprint 2; ghi GPS điện thoại chưa chốt.
- Mỗi nhánh có hướng lý trình; network topology và panel mapping là thiết kế đề xuất. Không nối giả qua ngã tư/cầu chỉ vì tọa độ gần nhau.
- Mặt đường 8 m rồi 10 m giữ hai bề rộng thật; hành lang tổng 12 m tương ứng 6 m mỗi bên tim, khác road polygon. Nhập nhiều điểm đo đúng tại đường cong; nội suy không tăng độ chính xác nguồn.
- Đổi tuyến sau tác nghiệp tạo version mới; survey/repair cũ giữ version cũ. Tránh segment dư vài mm bằng quy tắc gộp phần dư dưới minimum — ngưỡng và ngoại lệ tuyến ngắn phải được chốt.</pre>

Review gate: Review PF-01 details against owner source before RF-10-02 implements this claim. Checkpoint: RF-10-02 START for unconfirmed details; RF-11 retirement.

<a id="pf-02"></a>

### PF-02 - Tiếp nhận, đối chiếu trùng và chọn kiểm chứng

Source: `docs/diagram/V2/02_Requirements/03_To_Be_Process.md:45`; primary `RF-10-06` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**PF-02.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BR-04, BR-07, BR-29–31, BR-39, BR-47; FR-11–14; US-08, US-21–23, US-34.  
**Đầu vào:** phản ánh người dân, phát hiện drone hoặc ghi nhận Crew. **Đầu ra:** nguồn được giữ, PM xác định phạm vi và cách xử lý.</pre>

**PF-02.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>```mermaid
flowchart TD
 A[&quot;Reporter: report và ảnh&quot;] --&gt; D[&quot;Backend: lưu nguồn, thông báo PM&quot;]
 B[&quot;AI: phát hiện từ dữ liệu drone&quot;] --&gt; D
 C[&quot;Crew: ghi nhận ngoài nhiệm vụ&quot;] --&gt; D
 D --&gt; E[&quot;PM: xác định dự án, đối chiếu vị trí và bằng chứng&quot;]
 E --&gt; F{&quot;Cùng hư hỏng đã có?&quot;}
 F --&gt;|Có| G[&quot;PM: liên kết nguồn vào hồ sơ phù hợp&quot;]
 F --&gt;|Chưa rõ hoặc khác| H[&quot;PM: giữ riêng hoặc yêu cầu kiểm chứng&quot;]
 G --&gt; I[&quot;PM: đánh giá severity và urgency&quot;]
 H --&gt; I
 I --&gt; J{&quot;Cần kiểm chứng bằng cách nào?&quot;}
 J --&gt;|Thực địa| K[&quot;PM: giao đo hoặc kiểm tra&quot;]
 J --&gt;|Drone| L[&quot;PM: giao khảo sát&quot;]
 J --&gt;|Đủ bằng chứng| M[&quot;PM: xác nhận kết luận có căn cứ&quot;]
```</pre>

**PF-02.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**5 người cùng báo:** giữ 5 report, ảnh và quyền chủ sở hữu; đề xuất liên kết một case/Defect khi PM xác nhận cùng hư hỏng. Không tạo 5 công việc sửa. Khoảng cách 1–2 m chỉ là gợi ý cần chốt, không đủ để hợp nhất ổ gà và vỡ mép hai bên đường. Drone không cần report công dân giả. Không tìm thấy lỗi cần căn cứ đủ, không suy từ AI không phát hiện.</pre>

Review gate: Review PF-02 details against owner source before RF-10-06 implements this claim. Checkpoint: RF-10-06 START for unconfirmed details; RF-11 retirement.

<a id="pf-03"></a>

### PF-03 - PM chọn đo+sửa hay gom đợt chỉ đo

Source: `docs/diagram/V2/02_Requirements/03_To_Be_Process.md:69`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**PF-03.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BR-03, BR-05–14; FR-14–18; US-20, US-33–35.  
**Đầu vào:** lỗi/phản ánh trong dự án, mức chính thức/gợi ý, policy. **Đầu ra:** nhiệm vụ có phạm vi và quyền rõ ràng.</pre>

**PF-03.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>```mermaid
flowchart TD
 A[&quot;PM: xem lỗi, mức độ và gợi ý&quot;] --&gt; B{&quot;PM đã xác định nghiêm trọng?&quot;}
 B --&gt;|Có| C[&quot;Giao đo, không cho tự sửa&quot;]
 B --&gt;|Không| D{&quot;PM chọn cách tổ chức?&quot;}
 D --&gt;|Gom nhiều lỗi| E[&quot;Tạo đợt MEASURE_ONLY&quot;]
 D --&gt;|Lỗi nhỏ riêng lẻ| F[&quot;Giao INSPECT_AND_REPAIR kèm policy&quot;]
 C --&gt; G[&quot;Crew: đo, chụp ảnh, nộp kết quả&quot;]
 E --&gt; G
 G --&gt; H[&quot;PM: xem kết quả, lập kế hoạch sửa sau&quot;]
 F --&gt; I[&quot;Crew: chuẩn bị dụng cụ, đo và đánh giá policy&quot;]
 I --&gt; J{&quot;Đủ quyền và đúng policy?&quot;}
 J --&gt;|Có| K[&quot;Thực hiện PF-04 Fast Track&quot;]
 J --&gt;|Không| L[&quot;Báo PM, chờ quyết định và hàng chờ&quot;]
```</pre>

**PF-03.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Quy tắc ưu tiên:** 10 lỗi trong đợt có 5 lỗi nhỏ thì cả đợt chỉ đo, không tự sửa 5 lỗi. Policy ELIGIBLE không ghi đè nhiệm vụ MEASURE_ONLY. “Một tuần” là ví dụ PM gom công việc để tiết kiệm, không buộc chờ đủ tuần hoặc hệ thống tự chọn nhánh. Báo cáo mới xuất hiện không tự hủy quyền nhiệm vụ đã giao.</pre>

**PF-03.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**D02:** sau khi batch đo hoàn tất, PM tạo/giao task sửa riêng; task mới có thể Fast Track nếu nằm trong policy và đủ quyền/bằng chứng. Không đổi hồi tố MEASURE_ONLY và bước H không tự chọn track. Cấp urgency cao không tự kích hoạt quyền khẩn cấp; lỗi ngoài nhiệm vụ chỉ ghi nhận.</pre>

Review gate: Review PF-03 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="pf-04"></a>

### PF-04 - Fast Track tại hiện trường, gồm offline

Source: `docs/diagram/V2/02_Requirements/03_To_Be_Process.md:94`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**PF-04.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BR-05, BR-08, BR-11–20, BR-25; FR-15, FR-17–18, FR-21–23; US-02, US-13–14, US-20, US-33.  
**Đầu vào:** nhiệm vụ PM cho đo+sửa đã tải, policy version và phạm vi. **Đầu ra:** lỗi được PM kiểm/đóng hoặc quay lại bổ sung/sửa; Supervisor nhận báo cáo.</pre>

**PF-04.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>```mermaid
flowchart TD
 A[&quot;Crew: mở nhiệm vụ và policy đã tải&quot;] --&gt; B[&quot;Crew: đo, ghi số liệu và bằng chứng&quot;]
 B --&gt; C{&quot;Đủ quyền, không PM block, policy đạt?&quot;}
 C --&gt;|Không| D[&quot;Ghi kết quả, báo PM; chưa sửa&quot;]
 C --&gt;|Có| E{&quot;Đã gắn ảnh BEFORE phù hợp?&quot;}
 E --&gt;|Chưa| F[&quot;Bổ sung BEFORE trước khi sửa&quot;]
 F --&gt; E
 E --&gt;|Có| G[&quot;Crew: sửa, chụp AFTER, lưu báo cáo&quot;]
 G --&gt; H[&quot;App: giữ dữ liệu và tự sync khi có mạng&quot;]
 H --&gt; I[&quot;PM: kiểm kết quả và bằng chứng&quot;]
 I --&gt; J{&quot;Đạt yêu cầu?&quot;}
 J --&gt;|Thiếu bằng chứng| K[&quot;Yêu cầu bổ sung có căn cứ&quot;]
 J --&gt;|Sửa chưa đạt| L[&quot;Giao sửa lại, giữ lịch sử lần trước&quot;]
 J --&gt;|Đạt| M[&quot;PM: đóng lỗi Fast Track và báo Supervisor&quot;]
```</pre>

**PF-04.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Ảnh BEFORE Fast Track được lấy từ người dân/drone nếu phù hợp; không tạo nguồn giả. Ảnh đo và số liệu vẫn cần theo nhiệm vụ/policy; reuse BEFORE không miễn đo. Ảnh sau sửa không được đổi nhãn BEFORE. Nếu đã sửa ngoài app nhưng thiếu BEFORE không thể phục hồi, giữ ngoại lệ chưa đạt, cách xử lý Q06 cần chốt — không bịa ảnh để đóng hồ sơ.</pre>

**PF-04.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Không giới hạn thời gian làm offline. Snapshot nhiệm vụ/policy cho phép thực hiện offline nhưng không phải access token vĩnh viễn. D05 yêu cầu acknowledgement trước khi đội mới start cùng scope; khi sync vẫn kiểm quyền hiện tại và đưa late evidence vào conflict. PM nghiêm trọng đã biết thì Crew không được tự hạ mức rồi sửa.</pre>

Review gate: Review PF-04 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="pf-05"></a>

### PF-05 - Đo theo đợt, lập phương án và giao sửa

Source: `docs/diagram/V2/02_Requirements/03_To_Be_Process.md:120`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**PF-05.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BR-03, BR-09, BR-17, BR-21–24; FR-16–21; US-11–13, US-20, US-34–35.  
**Đầu vào:** đợt MEASURE_ONLY. **Đầu ra:** kết quả đo hợp lệ, thứ tự PM chọn, các item đủ quyền được giao Crew.</pre>

**PF-05.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>```mermaid
flowchart TD
 A[&quot;Crew: đo từng lỗi và chụp ảnh đo&quot;] --&gt; B{&quot;Đủ ảnh và số liệu?&quot;}
 B --&gt;|Không| C[&quot;Không nhận kết quả đo; phải đo lại&quot;]
 C --&gt; A
 B --&gt;|Có| D[&quot;PM: đánh giá kết quả và chọn lỗi sửa&quot;]
 D --&gt; E[&quot;PM: sắp thứ tự, chọn Crew và phương án&quot;]
 E --&gt; F[&quot;Supervisor: xét từng item nhánh thường&quot;]
 F --&gt; G{&quot;Quyết định theo item&quot;}
 G --&gt;|Duyệt| H[&quot;PM: giao phần đã duyệt&quot;]
 G --&gt;|Cần bằng chứng| I[&quot;PM: bổ sung căn cứ&quot;]
 G --&gt;|Xem lại phương án| J[&quot;PM: chỉnh phương án và trình lại&quot;]
 G --&gt;|Từ chối| K[&quot;Kết thúc đề xuất; lỗi vẫn chưa xử lý&quot;]
 I --&gt; F
 J --&gt; F
 H --&gt; L[&quot;Crew: sửa theo phân công, dùng ảnh đo làm BEFORE&quot;]
```</pre>

**PF-05.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Sơ đồ bước F áp dụng **APPROVAL_TRACK**. Theo D02, item nhỏ sau đợt đo có thể đi Fast Track bằng task sửa mới nếu đủ policy; không hồi tố task đo. Phần được duyệt được giao riêng, không chờ toàn batch. Theo D07, giữ phần đo đạt và đo lại phần thiếu; PM mở rộng scope đo lại nếu sai dụng cụ/phương pháp ảnh hưởng rộng. REJECT khác REQUEST_RECONSIDER và NO_DEFECT; giữ lịch sử version/item.</pre>

**PF-05.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Gợi ý severity/urgency không tự thay sequence của PM. Chuẩn bị vật tư theo policy không mở thêm module kho/BOM/chi phí chi tiết.</pre>

Review gate: Review PF-05 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="pf-06"></a>

### PF-06 - Nghiệm thu, sửa lại và đóng hồ sơ hỗn hợp

Source: `docs/diagram/V2/02_Requirements/03_To_Be_Process.md:147`; primary `RF-10-07` / proposed A; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**PF-06.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BR-21–28, BR-48; FR-23–25; US-14, US-23, US-37.  
**Đầu vào:** báo cáo sửa từng item/attempt. **Đầu ra:** quyết định từng lỗi và đóng case đúng nhánh.</pre>

**PF-06.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>```mermaid
flowchart TD
 A[&quot;Crew: nộp kết quả từng lần sửa&quot;] --&gt; B[&quot;PM: kiểm ảnh, số liệu và kết quả&quot;]
 B --&gt; C{&quot;Kết quả review&quot;}
 C --&gt;|Thiếu bằng chứng| D[&quot;Bổ sung; chưa kết luận phải thi công lại&quot;]
 C --&gt;|Sửa chưa đạt| E[&quot;PM: giao lần sửa tiếp, giữ evidence cũ&quot;]
 C --&gt;|Đạt| F{&quot;Nhánh sửa?&quot;}
 F --&gt;|Fast Track| G[&quot;PM: chấp nhận lỗi và báo Supervisor&quot;]
 F --&gt;|Nhánh thường| H[&quot;Supervisor: nghiệm thu từng lỗi&quot;]
 H --&gt; I{&quot;Đạt?&quot;}
 I --&gt;|Không| B
 I --&gt;|Có| J[&quot;Ghi kết quả đạt của nhánh&quot;]
 G --&gt; J
 J --&gt; K{&quot;Case hỗn hợp còn lỗi bắt buộc chưa đạt?&quot;}
 K --&gt;|Có| L[&quot;Giữ hồ sơ tổng mở&quot;]
 K --&gt;|Không, case hỗn hợp| M[&quot;Supervisor: đóng hồ sơ tổng&quot;]
 K --&gt;|Không, chỉ Fast Track| N[&quot;PM: đóng hồ sơ Fast Track&quot;]
```</pre>

**PF-06.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Lỗi cùng vị trí phát hiện sau nghiệm thu cần PM phân biệt “lần trước chưa đạt” và “tái phát”. **Đề xuất:** chưa đạt → tiếp tục hồ sơ và thêm attempt; tái phát thật → lỗi/case mới liên kết lịch sử. Ai mở lại case Supervisor đã đóng và quy trình thu hồi nghiệm thu là Q07; không tự mở chỉ vì gần GPS. Từng lỗi trên cùng tấm có nghiệm thu độc lập.</pre>

**PF-06.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Công bố Reporter là bước riêng của PM, chọn ảnh AFTER đủ phạm vi; không gửi toàn bộ hồ sơ nội bộ. Công bố từng phần khi case còn mở là Q08, chưa tự bật. PM Fast Track review là quyết định kết quả, không thêm một vòng Supervisor duyệt lại có được sửa hay không.</pre>

Review gate: Review PF-06 details against owner source before RF-10-07 implements this claim. Checkpoint: RF-10-07 START for unconfirmed details; RF-11 retirement.

<a id="pf-07"></a>

### PF-07 - Bay theo mạng tuyến, kiểm dữ liệu và baseline

Source: `docs/diagram/V2/02_Requirements/03_To_Be_Process.md:175`; primary `RF-10-03` / proposed B; coordination none. Historical authority: `HISTORICAL_SOURCE_ONLY`; current authority: `HISTORICAL_SOURCE_ONLY`; confirmed overlap `none`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**PF-07.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BR-36–37, BR-39–44; FR-26–31, FR-33; US-04–08, US-25–26, US-38–39.  
**Đầu vào:** phạm vi segment/band/hành lang và yêu cầu PM. **Đầu ra:** dataset đủ điều kiện, phát hiện để PM review, baseline từng vùng khi phù hợp.</pre>

**PF-07.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>```mermaid
flowchart TD
 A[&quot;PM: chọn nhánh, segment và band cần khảo sát&quot;] --&gt; B[&quot;PM/Operator: lập các chặng và điểm tiếp cận&quot;]
 B --&gt; C[&quot;Operator: thực hiện bay, nộp video và telemetry&quot;]
 C --&gt; D[&quot;Backend: checksum, thời gian và dữ liệu GPS&quot;]
 D --&gt; E{&quot;Dataset hợp lệ?&quot;}
 E --&gt;|Không| F[&quot;Trả lỗi kỹ thuật để bổ sung&quot;]
 F --&gt; C
 E --&gt;|Có| G[&quot;Đánh giá vị trí hành lang và coverage riêng&quot;]
 G --&gt; H{&quot;Đủ vùng và chất lượng theo tiêu chí?&quot;}
 H --&gt;|Thiếu hoặc unknown| I[&quot;PM: quyết định bay bổ sung&quot;]
 I --&gt; B
 H --&gt;|Đủ| J[&quot;AI: xử lý async, giữ nguồn và model version&quot;]
 J --&gt; K[&quot;PM: review phát hiện hoặc xác nhận baseline từng band&quot;]
```</pre>

**PF-07.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Không bắt buộc trục chính trước hay mọi nhánh trước; chia theo phạm vi cần kiểm, mức ưu tiên PM, pin, điểm cất/hạ và chất lượng quan sát. Quyền chỉnh kế hoạch PM/Operator cần contract cụ thể; kế hoạch là hỗ trợ, không tự điều khiển drone. Dronelink là khả năng xuất/tích hợp cần PoC với định dạng thực tế, không khẳng định API tích hợp đã có.</pre>

**PF-07.C04** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Vùng tổng12 m là ví dụ corridor, không là giới hạn bay an toàn/pháp lý được hệ thống chứng nhận. SRT ở trong vùng đáp ứng kiểm vị trí theo ngưỡng cần chốt Q11; không chứng minh ảnh nhìn đủ mép đường. Thiếu GPS/pose để UNKNOWN. Không lấy GPS máy bay làm tọa độ lỗi. Baseline cần segment/band đủ coverage và PM xác nhận; AI COMPLETED không tự tạo baseline hoặc NO_DEFECT. Research Validation dùng ground truth độc lập, không thay đổi trạng thái lỗi tác nghiệp.</pre>

Review gate: Review PF-07 details against owner source before RF-10-03 implements this claim. Checkpoint: RF-10-03 START for unconfirmed details; RF-11 retirement.

<a id="pf-08"></a>

### PF-08 - Đồng bộ dữ liệu ngoại tuyến

Source: `docs/diagram/V2/02_Requirements/03_To_Be_Process.md:200`; primary `RF-10-08` / proposed B; coordination RF-10-03, RF-10-07. Historical authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; current authority: `MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL`; confirmed overlap `PR-42A/43A`; source label: `UNSPECIFIED`. Readiness: `BLOCKED`.

Transferred source text and acceptance conditions:

**PF-08.C01** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>**Truy vết:** BR-15–20, BR-42; FR-21–22; US-02, US-06, US-13.  
**Đầu vào:** operation ID, snapshot và ảnh/số đo lưu cục bộ. **Đầu ra:** dữ liệu server được xác nhận hoặc xung đột hiển thị, không mất dữ liệu âm thầm.</pre>

**PF-08.C02** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>```mermaid
flowchart TD
 A[&quot;App: lưu thao tác và file cục bộ&quot;] --&gt; B{&quot;Có kết nối phù hợp?&quot;}
 B --&gt;|Không| C[&quot;Giữ hàng đợi, tiếp tục tác nghiệp được phép&quot;]
 C --&gt; B
 B --&gt;|Có| D[&quot;App: gửi operation ID, payload và tệp&quot;]
 D --&gt; E[&quot;Backend: xác thực, dedup, checksum và version&quot;]
 E --&gt; F{&quot;Kết quả&quot;}
 F --&gt;|Retry cùng payload| G[&quot;Trả kết quả đã ghi&quot;]
 F --&gt;|Thiếu tệp hoặc lỗi mạng| H[&quot;Giữ pending, tiếp tục upload&quot;]
 H --&gt; D
 F --&gt;|Xung đột quyền hoặc phiên bản| I[&quot;Giữ chứng cứ, đưa PM xử lý&quot;]
 F --&gt;|Hợp lệ và đủ tệp| J[&quot;Ghi transaction, xác nhận đồng bộ&quot;]
```</pre>

**PF-08.C03** (origin `HISTORICAL_SOURCE_ONLY`, current `HISTORICAL_SOURCE_ONLY`):

<pre>Đây là thiết kế sync draft. D05/D06/42A đã chốt business rules cho handover/rescue và bảo toàn actor; contract intake/conflict/receipt/security vẫn phải hoàn thiện. Không yêu cầu Crew chờ server trước mỗi lần sửa đã được giao; cũng không dùng offline để tự tạo quyền ngoài nhiệm vụ. Không retry vô hạn lỗi nghiệp vụ theo kiểu tạo bản ghi mới. Client time và server receive time tách biệt. Hạn nhiệm vụ không phải hạn xóa dữ liệu chưa sync.</pre>

Review gate: Review PF-08 details against PR-42A/43A before RF-10-08 implements this claim. Checkpoint: RF-10-08 START for unconfirmed details; RF-11 retirement.
