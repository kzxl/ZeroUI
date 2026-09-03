using System;

namespace ZeroUI.Samples.BenchmarkDemo.Data
{
    public static class MockDataGenerator
    {
        private static readonly string[] SampleNames = new[]
        {
            "Khung nhôm định hình 40x40",
            "Động cơ bước Nema 23 Stepper",
            "Cảm biến quang Omron E3Z-D61",
            "Van điện từ khí nén SMC 5/2",
            "Xy lanh khí nén Festo DSNU-25",
            "Băng tải PVC tải trọng 200kg",
            "Bi trượt tuyến tính THK HSR25",
            "Bộ lập trình PLC Mitsubishi FX5U",
            "Biến tần Schneider ATV310 2.2kW",
            "Màn hình cảm ứng HMI Weintek 7 inch"
        };

        private static readonly string[] SampleStatuses = new[]
        {
            "Đã nhập kho",
            "Chờ kiểm tra",
            "Đang gia công",
            "Hoàn thành",
            "Tạm giữ"
        };

        public static InventoryItem[] Generate(int count)
        {
            var items = new InventoryItem[count];
            var rand = new Random(42); // Deterministic seed for fair benchmarks

            for (int i = 0; i < count; i++)
            {
                int id = i + 1;
                string code = $"VT-{id:D7}";
                string name = SampleNames[i % SampleNames.Length];
                int qty = rand.Next(1, 1000);
                double price = rand.Next(50, 5000) * 1000.0;
                string lot = $"LOT-2026-{(i % 12) + 1:D2}";
                string status = SampleStatuses[i % SampleStatuses.Length];

                items[i] = new InventoryItem(id, code, name, qty, price, lot, status);
            }

            return items;
        }
    }
}
