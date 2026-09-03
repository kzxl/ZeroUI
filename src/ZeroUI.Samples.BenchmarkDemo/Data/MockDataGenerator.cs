using System;

namespace ZeroUI.Samples.BenchmarkDemo.Data
{
    public static class MockDataGenerator
    {
        private struct ItemTemplate
        {
            public string CodePrefix;
            public string Name;
            public double BasePrice;
            public int MinQty;
            public int MaxQty;

            public ItemTemplate(string prefix, string name, double basePrice, int minQty, int maxQty)
            {
                CodePrefix = prefix;
                Name = name;
                BasePrice = basePrice;
                MinQty = minQty;
                MaxQty = maxQty;
            }
        }

        private static readonly ItemTemplate[] RealCatalog = new[]
        {
            // Active Semiconductors & ICs
            new ItemTemplate("IC-MCU", "Vi điều khiển ARM Cortex-M4 STM32F407VGT6 LQFP100", 145000, 100, 2500),
            new ItemTemplate("IC-PWR", "IC Quản lý nguồn Buck Converter TI TPS54302DDCR", 18500, 500, 10000),
            new ItemTemplate("IC-MEM", "Chip nhớ SPI Flash Winbond W25Q128JVS 128Mb SOIC-8", 24000, 300, 5000),
            new ItemTemplate("IC-IOT", "Module Wi-Fi/Bluetooth ESP32-WROOM-32E PCB Ant", 78000, 200, 4000),
            new ItemTemplate("IC-MPU", "Bộ vi xử lý NXP i.MX RT1062 Crossover MCU 600MHz", 260000, 50, 1200),
            new ItemTemplate("IC-DRV", "Driver điều khiển động cơ bước TMC2209 Ultra-Silent", 45000, 150, 3000),
            new ItemTemplate("IC-ADC", "IC Chuyển đổi ADC 24-Bit TI ADS1256 High Precision", 195000, 40, 800),
            new ItemTemplate("IC-PHY", "Bộ thu phát Ethernet PHY Microchip LAN8720A QFN-24", 32000, 200, 3500),

            // Passive SMT Components
            new ItemTemplate("SMD-CAP", "Tụ gốm nhiều lớp SMD 0805 10uF 25V X7R Murata", 1200, 5000, 50000),
            new ItemTemplate("SMD-RES", "Điện trở màng mỏng chính xác 0603 10kΩ 1% Yageo", 450, 10000, 80000),
            new ItemTemplate("SMD-IND", "Cuộn cảm công suất Shielded SMD 10uH 3.5A Coilcraft", 8500, 1000, 15000),
            new ItemTemplate("SMD-XTAL", "Thạch anh dao động SMD 3225 16.000MHz 10ppm TXC", 6200, 500, 8000),
            new ItemTemplate("SMD-DIO", "Diode Schottky chỉnh lưu SS34 40V 3A SMC Panjit", 2800, 2000, 20000),
            new ItemTemplate("SMD-POL", "Tụ nhôm rắn Polymer SMD 100uF 35V Low ESR Panasonic", 14500, 400, 6000),
            new ItemTemplate("SMD-TVS", "Diode triệt áp ESD/TVS Semtech SM712 bảo vệ RS-485", 9800, 800, 12000),

            // Connectors & Electromechanical
            new ItemTemplate("CONN-HDR", "Đầu nối Pin Header 2x20P 2.54mm Mạ vàng chân cong", 6500, 300, 5000),
            new ItemTemplate("CONN-USBC", "Cổng kết nối USB Type-C 16-Pin SMT Chống nước IPX7", 12000, 500, 8000),
            new ItemTemplate("CONN-TERM", "Khối cầu đấu Terminal Block Phoenix 5.08mm 4-Pin", 16500, 200, 4000),
            new ItemTemplate("CONN-REL", "Rơ-le bán dẫn thể rắn SSR Omron G3MB-202P 5VDC", 42000, 80, 1500),
            new ItemTemplate("CONN-FFC", "Cáp dẹt tín hiệu FFC 0.5mm 30-Pin L=150mm mạ vàng", 8200, 400, 6000),
            new ItemTemplate("SENS-TEMP", "Cảm biến nhiệt độ công nghiệp PT100 3 dây Class A", 185000, 30, 500),

            // Pneumatics & Automation
            new ItemTemplate("PNEU-VAL", "Van điện từ khí nén 5/2 Airtac 4V210-08 24VDC", 245000, 20, 350),
            new ItemTemplate("PNEU-CYL", "Xy lanh khí nén compact 2 ty dẫn hướng SMC MGPM25-50Z", 1450000, 10, 120),
            new ItemTemplate("PNEU-VAC", "Đầu núm hút chân không cao su dẫn điện SMC ZP2-20UM", 85000, 50, 800),
            new ItemTemplate("PNEU-FRL", "Cụm lọc điều áp khí nén đôi Festo MS4-LFR-1/4-D7", 1850000, 8, 80),
            new ItemTemplate("SENS-PROX", "Cảm biến tiệm cận quang học thu phát Keyence PZ-G41N", 1150000, 15, 200),
            new ItemTemplate("SENS-PRS", "Cảm biến áp suất khí nén kỹ thuật số SMC ISE30A-01-N", 1680000, 12, 150),

            // Motion & Mechanical
            new ItemTemplate("MEC-STEP", "Động cơ bước Hybrid Nema 23 2.8Nm Moment xoắn cao", 485000, 15, 250),
            new ItemTemplate("MEC-RAIL", "Thanh trượt vuông dẫn hướng tuyến tính HIWIN HGH20CA-1000", 1250000, 10, 100),
            new ItemTemplate("MEC-SCREW", "Trục vít me bi chính xác TBI Motion SFU1605-600mm", 1950000, 6, 80),
            new ItemTemplate("MEC-BRG", "Vòng bi đũa cầu nắp chắn cao su SKF 6205-2RSH/C3", 95000, 40, 600),
            new ItemTemplate("MEC-ALU", "Khung nhôm định hình kỹ thuật AL6063 40x80 Anodized", 280000, 20, 300),
            new ItemTemplate("MEC-ENCL", "Vỏ hợp kim nhôm đúc nguyên khối CNC Milling IP67", 680000, 25, 400),

            // Chemicals, Packaging & SMT Consumables
            new ItemTemplate("CHEM-SOLD", "Kem hàn không chì SMT Senju M705-GRN360 Hộp 500g", 1250000, 10, 150),
            new ItemTemplate("CHEM-GLUE", "Keo tản nhiệt dẫn nhiệt cao cấp ShinEtsu 7783 Tuýp 100g", 380000, 20, 300),
            new ItemTemplate("CONS-TAPE", "Cuộn băng dính chịu nhiệt Polyimide Kapton 25mm x 33m", 115000, 30, 450),
            new ItemTemplate("PKG-ESD", "Khay xốp chống tĩnh điện định hình ESD Tray 400x300mm", 65000, 100, 1500),
            new ItemTemplate("CONS-WIPE", "Giấy lau phòng sạch không bụi Cleanroom Wiper 9x9 inch", 145000, 40, 600)
        };

        private static readonly string[] SampleStatuses = new[]
        {
            "Đạt tiêu chuẩn OQC",
            "Chờ kiểm định IQC",
            "Đang cấp chuyền SMT",
            "Tạm giữ cách ly QC",
            "Cảnh báo sắp hết tồn"
        };

        public static InventoryItem[] Generate(int count)
        {
            var items = new InventoryItem[count];
            var rand = new Random(42); // Deterministic seed for reproducible benchmarks

            int catLen = RealCatalog.Length;
            int statusLen = SampleStatuses.Length;

            for (int i = 0; i < count; i++)
            {
                int id = i + 1;
                ref readonly var tpl = ref RealCatalog[i % catLen];

                string code = $"{tpl.CodePrefix}-{id:D6}";
                string name = tpl.Name;

                // Varied quantity around template range
                int qty = rand.Next(tpl.MinQty, tpl.MaxQty + 1);

                // Slight price fluctuation (+/- 5%) for authenticity
                double priceFluctuation = 1.0 + ((rand.NextDouble() * 0.10) - 0.05);
                double price = Math.Round(tpl.BasePrice * priceFluctuation, 0);

                // Realistic manufacturing lots
                int day = (i % 28) + 1;
                int month = ((i / 28) % 12) + 1;
                string line = (i % 4) switch
                {
                    0 => "SMT1",
                    1 => "SMT2",
                    2 => "ASSY",
                    _ => "IMP"
                };
                string lot = $"LOT-2026{month:D2}{day:D2}-{line}";
                string status = SampleStatuses[i % statusLen];

                items[i] = new InventoryItem(id, code, name, qty, price, lot, status);
            }

            return items;
        }
    }
}
