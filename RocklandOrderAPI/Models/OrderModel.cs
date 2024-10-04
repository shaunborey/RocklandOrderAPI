using RocklandOrderAPI.Data;

namespace RocklandOrderAPI.Models
{
    public class OrderModel
    {
        public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
        public byte[] PurchaseOrderPDF { get; set; } = new byte[0];
        public decimal OrderTotal { get; set; }
        public string ShippingAddress1 { get; set; }
        public string ShippingAddress2 { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingPostalCode { get; set; }
        public int ShippingOptionId { get; set; }
    }
}
