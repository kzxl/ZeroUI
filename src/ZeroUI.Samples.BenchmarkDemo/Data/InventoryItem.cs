namespace ZeroUI.Samples.BenchmarkDemo.Data
{
    public struct InventoryItem
    {
        public int Id;
        public string ItemCode;
        public string ItemName;
        public int Quantity;
        public double UnitPrice;
        public double TotalAmount;
        public string LotNumber;
        public string Status;

        public InventoryItem(int id, string code, string name, int qty, double price, string lot, string status)
        {
            Id = id;
            ItemCode = code;
            ItemName = name;
            Quantity = qty;
            UnitPrice = price;
            TotalAmount = qty * price;
            LotNumber = lot;
            Status = status;
        }
    }
}
