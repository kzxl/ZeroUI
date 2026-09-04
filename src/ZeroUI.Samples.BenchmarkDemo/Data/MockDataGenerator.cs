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
            new ItemTemplate("IC-MCU", "ARM Cortex-M4 STM32F407VGT6 LQFP100 MCU", 145000, 100, 2500),
            new ItemTemplate("IC-PWR", "TI TPS54302DDCR Buck Converter 28V 3A Power IC", 18500, 500, 10000),
            new ItemTemplate("IC-MEM", "Winbond W25Q128JVS 128Mb SPI Flash SOIC-8", 24000, 300, 5000),
            new ItemTemplate("IC-IOT", "ESP32-WROOM-32E Wi-Fi/BLE Module PCB Ant", 78000, 200, 4000),
            new ItemTemplate("IC-MPU", "NXP i.MX RT1062 Crossover Processor 600MHz", 260000, 50, 1200),
            new ItemTemplate("IC-DRV", "TMC2209 Ultra-Silent Stepper Motor Driver", 45000, 150, 3000),
            new ItemTemplate("IC-ADC", "TI ADS1256 24-Bit High Precision ADC IC", 195000, 40, 800),
            new ItemTemplate("IC-PHY", "Microchip LAN8720A 10/100 Ethernet PHY QFN-24", 32000, 200, 3500),

            // Passive SMT Components
            new ItemTemplate("SMD-CAP", "Murata 0805 10uF 25V X7R SMD Ceramic Capacitor", 1200, 5000, 50000),
            new ItemTemplate("SMD-RES", "Yageo 0603 10kΩ 1% Precision Thin Film Resistor", 450, 10000, 80000),
            new ItemTemplate("SMD-IND", "Coilcraft 10uH 3.5A Shielded Power Inductor SMD", 8500, 1000, 15000),
            new ItemTemplate("SMD-XTAL", "TXC 3225 16.000MHz 10ppm SMD Crystal Oscillator", 6200, 500, 8000),
            new ItemTemplate("SMD-DIO", "Panjit SS34 40V 3A SMC Schottky Rectifier Diode", 2800, 2000, 20000),
            new ItemTemplate("SMD-POL", "Panasonic 100uF 35V Low ESR Polymer Capacitor SMD", 14500, 400, 6000),
            new ItemTemplate("SMD-TVS", "Semtech SM712 ESD/TVS RS-485 Protection Diode", 9800, 800, 12000),

            // Connectors & Electromechanical
            new ItemTemplate("CONN-HDR", "Pin Header 2x20P 2.54mm Right-Angle Gold Plated", 6500, 300, 5000),
            new ItemTemplate("CONN-USBC", "USB Type-C 16-Pin SMT IPX7 Waterproof Receptacle", 12000, 500, 8000),
            new ItemTemplate("CONN-TERM", "Phoenix Contact 5.08mm 4-Pin Terminal Block", 16500, 200, 4000),
            new ItemTemplate("CONN-REL", "Omron G3MB-202P 5VDC Solid State Relay (SSR)", 42000, 80, 1500),
            new ItemTemplate("CONN-FFC", "FFC Signal Cable 0.5mm 30-Pin L=150mm Gold Plated", 8200, 400, 6000),
            new ItemTemplate("SENS-TEMP", "PT100 3-Wire Class A Industrial RTD Sensor", 185000, 30, 500),

            // Pneumatics & Automation
            new ItemTemplate("PNEU-VAL", "Airtac 4V210-08 24VDC 5/2 Pneumatic Solenoid Valve", 245000, 20, 350),
            new ItemTemplate("PNEU-CYL", "SMC MGPM25-50Z Guided Compact Air Cylinder", 1450000, 10, 120),
            new ItemTemplate("PNEU-VAC", "SMC ZP2-20UM Conductive Vacuum Suction Cup", 85000, 50, 800),
            new ItemTemplate("PNEU-FRL", "Festo MS4-LFR-1/4-D7 Filter Regulator Unit", 1850000, 8, 80),
            new ItemTemplate("SENS-PROX", "Keyence PZ-G41N Optical Through-Beam Sensor", 1150000, 15, 200),
            new ItemTemplate("SENS-PRS", "SMC ISE30A-01-N Digital Pressure Sensor", 1680000, 12, 150),

            // Motion & Mechanical
            new ItemTemplate("MEC-STEP", "Nema 23 2.8Nm High-Torque Hybrid Stepper Motor", 485000, 15, 250),
            new ItemTemplate("MEC-RAIL", "HIWIN HGH20CA-1000 Linear Guide Rail & Block", 1250000, 10, 100),
            new ItemTemplate("MEC-SCREW", "TBI Motion SFU1605-600mm Precision Ball Screw", 1950000, 6, 80),
            new ItemTemplate("MEC-BRG", "SKF 6205-2RSH/C3 Rubber-Sealed Deep Groove Bearing", 95000, 40, 600),
            new ItemTemplate("MEC-ALU", "AL6063 40x80 Anodized Industrial Aluminum Extrusion", 280000, 20, 300),
            new ItemTemplate("MEC-ENCL", "CNC Milled Aluminum Enclosure IP67 Waterproof", 680000, 25, 400),

            // Chemicals, Packaging & SMT Consumables
            new ItemTemplate("CHEM-SOLD", "Senju M705-GRN360 Lead-Free Solder Paste 500g", 1250000, 10, 150),
            new ItemTemplate("CHEM-GLUE", "ShinEtsu 7783 High-Thermal Compound Paste 100g", 380000, 20, 300),
            new ItemTemplate("CONS-TAPE", "Kapton Polyimide High-Temp Tape 25mm x 33m", 115000, 30, 450),
            new ItemTemplate("PKG-ESD", "ESD Conductive Molded Component Tray 400x300mm", 65000, 100, 1500),
            new ItemTemplate("CONS-WIPE", "Cleanroom Lint-Free Wiper 9x9 Inch (Class 100)", 145000, 40, 600)
        };

        private static readonly string[] SampleStatuses = new[]
        {
            "Passed OQC",
            "Pending IQC",
            "SMT Feeding",
            "QC Quarantine",
            "Low Stock Warning"
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
