namespace FoodDelivery.Customer.Api.DTOs
{
    public class CheckoutRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
    }
}
