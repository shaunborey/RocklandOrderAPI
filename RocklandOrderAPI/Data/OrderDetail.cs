namespace RocklandOrderAPI.Data
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int UserOrderId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
