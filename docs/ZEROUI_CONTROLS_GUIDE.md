# ZeroUI — Comprehensive Control Catalog & Demo Tour

Tài liệu giới thiệu kiến trúc, tính năng kỹ thuật và hướng dẫn vận hành toàn bộ hệ sinh thái Control của **ZeroUI** (`ZeroPlatform`), đi kèm video animation và hình ảnh trực quan từ ứng dụng Benchmark Demo.

---

## 🎬 Video Demo & Ghi Hình Chuyển Động Thực Tế

### 1. Video Ghi Hình Chuyển Động Trực Tiếp Toàn Diện (In-Process Live Recording - 30s)
Video ghi lại trực tiếp toàn bộ quá trình ứng dụng đang chạy ở tốc độ 15 FPS, tự hành đi qua **toàn bộ 8 nhóm Tab và tất cả các Subtab nghiệp vụ (21 phân cảnh)**:

![ZeroUI In-Process Live Motion Recording](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/zeroui_live_recording.gif)

- **File MP4 quay trực tiếp (1366x850, 15 FPS, 30s):** [zeroui_live_recording.mp4](file:///e:/15.%20Other/dotnet/libs/ZeroPlatform/ZeroUI/docs/images/zeroui_live_recording.mp4)
- **File GIF chuyển động trực tiếp:** [zeroui_live_recording.gif](file:///e:/15.%20Other/dotnet/libs/ZeroPlatform/ZeroUI/docs/images/zeroui_live_recording.gif)

#### ⏱️ Chi tiết 21 phân cảnh diễn ra trong video:
1. `0.0s`: **ZeroGrid** — Cuộn mượt mà tốc độ cao qua 100,000 dòng dữ liệu không sinh rác GC.
2. `1.7s`: **Standard DataGridView** — Chế độ Virtual Mode chuẩn của Windows Forms.
3. `2.7s`: **Industrial Verticals** — Trạm xử lý nước & nước thải (Water Treatment Plant).
4. `4.0s`: **Industrial Verticals** — Phân hệ lọc hóa dầu (Petrochemical Refining).
5. `5.3s`: **Industrial Verticals** — Nồi lên men y sinh & dược phẩm (Bioreactor Life Sciences).
6. `6.7s`: **Industrial Verticals** — Hệ thống quản lý tòa nhà thông minh (Smart Building BMS).
7. `8.0s`: **Industrial Verticals** — Năng lượng tái tạo & lưu trữ pin BESS (Renewable Energy).
8. `9.3s`: **SCADA Closed-Loop** — Quy trình phản ứng mẻ vòng lặp kín (Bơm quay 2950 RPM, van, ống dẫn lưu chất, bồn 3D, gia nhiệt).
9. `10.7s`: **SCADA Closed-Loop** — Kích hoạt xung áp suất đột biến và báo động động học thời gian thực.
10. `11.7s`: **SCADA P&ID** — Sơ đồ dòng quy trình công nghệ P&ID tương tác.
11. `13.0s`: **SCADA ISA-18.2** — Lưới quản lý báo động chuẩn ISA-18.2 & Faceplate chỉnh thông số PID.
12. `14.3s`: **Plant Overview** — Giám sát toàn cảnh nhà máy & giao diện HMI Mimics.
13. `15.7s`: **Network & IT/OT** — Bản đồ sơ đồ mạng, kiểm tra ping viễn thông & băng thông.
14. `17.0s`: **MES Production** — Bảng điều khiển nhà máy thông minh, đồng hồ OEE và nhịp sản xuất Takt Timer.
15. `18.3s`: **MES Kanban** — Thẻ quy trình sản xuất điện tử (Process Flow Cards).
16. `19.7s`: **WMS Workstation** — Trạm nhận hàng, quét mã vạch và phân bổ lô hàng FIFO/FEFO.
17. `21.0s`: **WMS Storage Racks** — Mô phỏng trực quan hệ thống giá kệ kho nhiều tầng và ô vị trí pallet.
18. `22.3s`: **Analytics & Charts** — Doanh thu theo quý, Donut phân bổ chi phí có KPI tâm, và biểu đồ SPC Box-and-Whisker.
19. `23.7s`: **UI Components: Core** — DatePicker zoom 3 cấp (Ngày ↔ Tháng ↔ Năm), ô nhập chip TokenEdit, ColorPicker, Range Slider.
20. `25.0s`: **UI Components: Enterprise** — Hộp chọn GridLookup gắn virtual grid và Wizard hướng dẫn cài đặt.
21. `26.3s`: **UI Components: Data BOM** — Cây phân cấp đa mức BOM TreeList với checkbox 3 trạng thái.
22. `27.7s`: **Obsidian Dark Mode** — Toàn cảnh trung tâm điều khiển SCADA chuyển sang giao diện tối bóng đêm sang trọng.

### 2. Video Trình Chiếu Toàn Cảnh Hệ Sinh Thái (Full Ecosystem Showcase)
Video và animation trình chiếu trọn vẹn từng màn hình nghiệp vụ (MES, WMS, SCADA, Charts, Grid, Editors):

![ZeroUI Full Ecosystem Demo Showcase](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/zeroui_demo_showcase.gif)

- **File MP4 độ phân giải cao (1080p, 60 FPS):** [zeroui_demo_showcase.mp4](file:///e:/15.%20Other/dotnet/libs/ZeroPlatform/ZeroUI/docs/images/zeroui_demo_showcase.mp4)
- **File GIF trình chiếu:** [zeroui_demo_showcase.gif](file:///e:/15.%20Other/dotnet/libs/ZeroPlatform/ZeroUI/docs/images/zeroui_demo_showcase.gif)

---

## 🚀 Cách Chạy Demo Trực Tiếp Trên Máy Tính

Để trực tiếp tương tác, kiểm tra tốc độ cuộn 10 triệu dòng, thử nghiệm các control P&ID và đổi 9 skin giao diện, bạn chạy một trong các lệnh sau trong Terminal (PowerShell):

```powershell
# 1. Chạy qua script điều hướng tự động
.\run-zeroui-demo.ps1 -Demo winforms

# 2. Hoặc khởi chạy trực tiếp qua dotnet CLI
dotnet run --project "ZeroUI\src\ZeroUI.Samples.BenchmarkDemo\ZeroUI.Samples.BenchmarkDemo.csproj" -c Debug

# 3. Chạy chế độ Headless Benchmark đo đạc hiệu năng CPU / RAM / GC
dotnet run --project "ZeroUI\src\ZeroUI.Samples.BenchmarkDemo\ZeroUI.Samples.BenchmarkDemo.csproj" -c Debug -- --benchmark
```

---

## 🏛️ Danh Mục & Giới Thiệu Chi Tiết Các Control

ZeroUI được thiết kế theo triết lý **Zero-Allocation**, kiến trúc **Single-HWND** (chỉ 1 handle Win32 duy nhất cho toàn bộ grid, triệt tiêu lỗi rò rỉ GDI handle), và tối ưu render bằng bộ đệm kép Win32 Memory DC DIBSection.

---

### 1. Phân Hệ Big Data Virtual Grid & OLAP Matrix

![01. ZeroGrid Virtual DataGrid Benchmark](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/01_zerogrid_benchmark.png)

#### Các Control tiêu biểu:
- **`ZeroGridControl` (`ZeroUI.WinForms.DataGrid`):**
  - **Khả năng tải:** Render mượt mà **10,000,000+ dòng dữ liệu** ở tốc độ 60–120 FPS.
  - **Zero-Allocation:** Vòng lặp tính toán viewport (`VirtualViewport2D`) và bộ đệm cell (`CellValueBuffer`) hoàn toàn không sinh rác GC Gen0.
  - **Sắp xếp & Lọc cực nhanh:** Thuật toán tráo con trỏ index (`RowIndexMap`) cho phép sort 1 triệu dòng trong ~80ms; bộ lọc tìm kiếm tức thì debounced 150ms.
  - **Mật độ hiển thị (`RowDensity`):** Hỗ trợ chuyển đổi linh hoạt giữa `Compact` (24px), `Normal` (28px) và `Comfortable` (36px).
  - **Streaming Export:** Xuất file CSV tốc độ >1,100,000 dòng/giây trực tiếp từ bộ đệm bộ nhớ mà không gây lag giao diện.
- **`PivotGridControl` / `ZeroPivotGrid` (`ZeroUI.WinForms.PivotGrid`):**
  - Ma trận tổng hợp đa chiều OLAP (Cross-tab Matrix) nhóm dữ liệu theo các chiều Hàng (Row) và Cột (Column).
  - Tự động tính toán tổng con (Sub-totals), tổng lớn (Grand totals) với các hàm `Sum`, `Count`, `Average`, `Min`, `Max`.
  - Hỗ trợ cây phân cấp thu gọn/mở rộng drill-down (`▶`/`▼`).
- **`ZeroFilterControl` & `FilterCriteria`:**
  - Trình dựng câu truy vấn điều kiện trực quan dạng cây logic (`AND`, `OR`, `NOT AND`, `NOT OR`), tự động sinh biểu thức SQL WHERE.

---

### 2. Phân Hệ Thiết Bị Công Nghiệp P&ID, SCADA & Chế Tạo (MES)

![09. Industrial, SCADA & MES Hardware Control Suite](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/09_industrial_hero_showcase.png)

![13. SCADA Field Actuators, Sensors & Safety Controls](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/13_scada_actuators_composite.png)

#### Các Control tiêu biểu:
- **`ZeroTank3D`:** Bồn chứa chất lỏng 3D hiển thị thể tích thực tế (`CurrentLevelLiters`), dung tích thiết kế (`CapacityLiters`), hiệu ứng sóng chất lỏng và hiển thị tên dung môi (IPA, Axit, Nước cất).
- **`ZeroLedTower`:** Đèn tháp Andon chuẩn nhà máy thông minh với 4 tầng LED độc lập (`Red`, `Amber`, `Green`, `Blue`) hỗ trợ các trạng thái `On`, `Off`, `BlinkFast`, `BlinkSlow`.
- **`ZeroSevenSegment`:** Đồng hồ hiển thị LED 7 đoạn điện tử mô phỏng hiển thị cân kỹ thuật số, nhiệt độ lò nung với màu sắc neon tùy biến.
- **`ZeroLinearGauge` & `ZeroGauge`:**
  - Đồng hồ đo áp suất / lưu lượng dạng thanh tuyến tính hoặc mặt cung tròn xuyên tâm (Radial Dial Gauge).
  - Hiển thị kim đo mượt, dải vạch chia màu cảnh báo (Green/Yellow/Red) và tính toán tỷ lệ OEE (%).
- **`ZeroTaktTimer`:** Đồng hồ đếm ngược nhịp sản xuất (Takt Time) với vòng tiến độ trực quan, cảnh báo quá nhịp cho dây chuyền lắp ráp.
- **`ZeroIndustrialMotor`, `ZeroIndustrialPump`, `ZeroIndustrialFan`:**
  - Động cơ không đồng bộ, bơm ly tâm, quạt thông gió công nghiệp với hiệu ứng cánh quạt / rotor quay mượt mà theo tốc độ RPM thực tế.
- **`ZeroIndustrialHeater` & `ZeroIndustrialValve`:**
  - Lò gia nhiệt hiển thị thanh nhiệt đỏ và nhiệt độ setpoint.
  - Van điều khiển (Control Valve, Solenoid Valve) hiển thị góc mở thực tế từ 0% đến 100%.
- **`ZeroPneumaticCylinder` & `ZeroConveyorBelt`:**
  - Xy lanh khí nén 2 chiều hiển thị hành trình co duỗi piston theo phần trăm (`ExtensionPercent`).
  - Băng tải con lăn công nghiệp hiển thị hướng chạy, tốc độ mét/phút và dòng sản phẩm di chuyển.
- **`ZeroIndustrialSensor`:** Cảm biến quang học, điện cảm, điện dung với đèn LED trạng thái kích hoạt.
- **`ZeroProductionCounter`:** Bảng điện tử hiển thị kế hoạch sản lượng (Plan, Actual, NG, Chênh lệch).
- **`ZeroInterlockIndicator` & `ZeroCommandButton`:**
  - Đèn tín hiệu liên động an toàn (Interlock Permissive).
  - Nút bấm lệnh điều khiển công nghiệp có cơ chế chống bấm nhầm (yêu cầu nhấn giữ `PressAndHoldSeconds`).

---

### 3. Phân Hệ Giám Sát Quy Trình SCADA & Vòng Lặp Kín (Closed-Loop)

![12. SCADA Closed-Loop Simulation](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/12_scada_closed_loop_simulation.png)

![08. Obsidian Dark SCADA Process](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/08_dark_theme_scada.png)

#### Các Control tiêu biểu:
- **`ZeroAlarmGrid`:** Lưới quản lý cảnh báo thời gian thực chuẩn ISA-18.2 với các mức độ nghiêm trọng (`Critical`, `High`, `Medium`, `Low`), hỗ trợ xác nhận cảnh báo (`Acknowledge`) và tạm hoãn (`Shelve`).
- **`ZeroTrendChart`:** Bộ ghi đồ thị dao động ký / xu hướng thời gian thực 60 FPS, sử dụng cấu trúc bộ đệm tròn (`RingBuffer`), vẽ cùng lúc nhiều kênh đo với ngưỡng giới hạn trên/dưới (USL/LSL) và con trỏ đo crosshair.
- **`ZeroPipeFlow`:** Đường ống dẫn động học với các hạt dòng chảy hạt lưu chất (Liquid/Gas) chuyển động theo tốc độ và áp suất thực tế.
- **`ZeroTreeList`:** Cây danh mục phân cấp ảo nhiều cấp cột, tích hợp checkbox 3 trạng thái (`Unchecked`, `Checked`, `Indeterminate`) phù hợp cho cây thiết bị nhà máy hoặc BOM sản phẩm.

---

### 4. Phân Hệ Quản Lý Kho Vận (WMS) & Vị Trí Kệ Hàng (Racks)

![05. WMS Warehouse Rack Center](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/05_wms_warehouse_rack.png)

#### Các Control tiêu biểu:
- **`ZeroWarehouseRack` (`ZeroUI.WinForms.Warehouse`):**
  - Mô phỏng trực quan hệ thống giá kệ kho hàng (Dãy - Khung - Tầng - Ô vị trí pallet).
  - Hiển thị mã vạch vị trí (Bin Location Code), loại hàng hóa, tải trọng tối đa.
  - Phân chia màu sắc cảnh báo tỷ lệ chiếm dụng (Trống, Đang chứa, Đầy, Khóa bảo trì).
  - Tương tác nhấp chuột để chọn ô hàng, xem chi tiết lô (Lot No, Ngày nhập, Trọng lượng).

---

### 5. Phân Hệ Biểu Đồ Thống Kê & Báo Cáo Doanh Nghiệp (BI Charts)

![10. Business Charts Dashboard](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/10_business_charts_dashboard.png)

![11. Charts Hero Showcase](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/11_charts_hero_showcase.png)

#### Các Control tiêu biểu:
- **`ZeroBarChart`:** Biểu đồ cột nhóm (Grouped Column), cột xếp chồng (Stacked Bar) hiển thị doanh thu so với mục tiêu ngân sách.
- **`ZeroPieChart`:** Biểu đồ tròn và bánh Donut với tiêu đề và giá trị KPI tổng hợp nổi bật ở tâm.
- **`ZeroLineChart`:** Biểu đồ đường mượt (Spline Area) với dải màu chuyển sắc gradient thể hiện xu hướng biến thiên.
- **`ZeroBoxPlotChart`:** Biểu đồ hộp và râu (Box-and-Whisker) chuyên sâu cho quản trị chất lượng thống kê (SPC), hiển thị giá trị Min, Max, Tứ phân vị Q1, Trung vị Median, Q3, điểm dị biệt Outliers và các đường ngưỡng dung sai USL / LSL.
- **Candlestick, Radar, Funnel, Waterfall Charts:** Bộ biểu đồ nến tài chính, biểu đồ mạng nhện đa tiêu chí, biểu đồ phễu chuyển đổi và biểu đồ thác nước phân tích biến động.

---

### 6. Phân Hệ Editors Hiện Đại & Điều Hướng Nâng Cao

![02. UI Components & Editors Showcase](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/02_components_showcase.png)

![07. Multi-Tier Zoom DatePicker](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/07_datepicker_multitier_zoom.png)

#### Các Control tiêu biểu:
- **`ZeroDatePicker` & `ZeroDateRangePicker`:**
  - Lịch chọn ngày thông minh hỗ trợ thu phóng 3 cấp độ mượt mà không nhấp nháy: **Ngày (Days) ↔ Tháng (Months) ↔ Năm (Years)** khi nhấp chuột vào header.
  - Hỗ trợ các phím tắt chọn nhanh dải ngày (Hôm nay, Tuần này, Tháng này, Quý này).
- **`ZeroGridLookup`:** Hộp chọn dropdown nhiều cột tích hợp sẵn lưới ảo `ZeroGridControl`, hỗ trợ gõ tìm kiếm lọc dữ liệu tức thì trên hàng trăm ngàn dòng danh mục vật tư/khách hàng.
- **`ZeroCheckedComboBox`:** Dropdown chọn nhiều mục với checkbox, tóm tắt số lượng mục đã chọn.
- **`ZeroTokenEdit`:** Ô nhập liệu dạng thẻ chip (tags/badges) có thể xóa bằng phím Backspace hoặc nút `x`.
- **`ZeroColorPicker`:** Bảng chọn màu ma trận mẫu cùng bộ điều khiển mã HEX/RGB chuyên nghiệp.
- **`RangeControl` / `DateTimeRangeSlider`:** Thanh trượt khoảng 2 đầu thumb tương tác trực tiếp trên biểu đồ tần suất (histogram) hoặc sparkline nền.
- **`ZeroValidationProvider` & `ZeroErrorProvider`:**
  - Bộ kiểm tra hợp lệ dữ liệu biểu mẫu hỗ trợ cú pháp Fluent (`NotEmpty`, `Range`, `Regex`, `Email`, `Custom`).
  - Hiển thị badge cảnh báo nhấp nháy mượt mà, viền đỏ ô lỗi và tự động cuộn đến control lỗi đầu tiên.

---

### 7. Phân Hệ Layout, Docking Đa Màn Hình & Hệ Thống Theme

![04. SCADA & Smart Factory Hub](C:/Users/phong.vo/.gemini/antigravity-ide/brain/c427bc97-8da4-42a7-b275-0decc5babb11/images/04_scada_smart_factory.png)

#### Các Control tiêu biểu:
- **`ZeroDockManager` & `ZeroFloatingWindow`:**
  - Hệ thống docking đa vùng (Trái, Phải, Trên, Dưới, Document tabs giữa), hỗ trợ kéo thả tách cửa sổ trôi nổi (`Floating`) ra các màn hình phụ.
  - Tự động lưu và khôi phục bố cục qua JSON thuần túy (`ZeroWorkspaceSerializer`).
- **`ZeroOptimizedPanel`:** Panel thẻ bo tròn thông minh sử dụng kỹ thuật 9-slice shadow atlas, triệt tiêu độ trễ làm bóng GDI+ mà vẫn giữ độ sắc nét chữ tối đa.
- **`ZeroSideNav` & `ZeroToolbar`:** Thanh điều hướng dọc thu gọn thông minh và thanh công cụ ribbon chống đè nút (collision guard).
- **9 Bộ Theme / Skin tích hợp sẵn:**
  - `obsidian_dark` (Mặc định cho phòng điều khiển SCADA)
  - `clean_light` (Giao diện phẳng sáng hiện đại)
  - `nordic_slate` (Tông xám Bắc Âu thanh lịch)
  - `cyberpunk_neon` (Tông Neon công nghệ cao)
  - `emerald_industrial` (Tông xanh lục công nghiệp)
  - `solar_amber` (Tông vàng hổ phách năng lượng)
  - `amethyst_violet`, `crimson_ruby`, `oled_midnight`.
