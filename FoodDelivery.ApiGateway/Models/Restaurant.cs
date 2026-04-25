namespace FoodDelivery.ApiGateway.Models
{
    public class Restaurant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public List<MenuItem> Menu { get; set; } = new();
    }
}
