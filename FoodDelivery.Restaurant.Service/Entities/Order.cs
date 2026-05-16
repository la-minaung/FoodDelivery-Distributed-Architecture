namespace FoodDelivery.Restaurant.Service.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
