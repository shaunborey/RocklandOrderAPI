using System.ComponentModel.DataAnnotations.Schema;

namespace RocklandOrderAPI.Data
{
    public class UserOrder
    {
        public int Id { get; set; }        
        public ApplicationUser User { get; set; }
        public IEnumerable<OrderDetail> Details { get; set; } = new List<OrderDetail>();
        public byte[] PurchaseOrderPDF { get; set; } = new byte[0];
        public decimal OrderTotal { get; set; }
        public required string ShippingAddress1 { get; set; }
        public string ShippingAddress2 { get; set; } = string.Empty;
        public required string ShippingCity { get; set; }
        public required string ShippingState { get; set; }
        public required string ShippingPostalCode { get; set; }
        public required ShippingOption ShippingOption { get; set; }
        public DateTime OrderDate { get; set; }
        public required OrderStatus Status { get; set; }
    }

    public enum OrderStatus
    {
        New,
        Processing,
        Shipped,
        Hold,
        Cancelled
    }
}
