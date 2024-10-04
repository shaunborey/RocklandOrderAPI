namespace RocklandOrderAPI.Data
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Product Item { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
