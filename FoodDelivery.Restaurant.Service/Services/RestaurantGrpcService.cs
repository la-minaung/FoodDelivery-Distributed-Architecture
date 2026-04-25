using Grpc.Core;
using FoodDelivery.Shared.Contracts.gRPC;

namespace FoodDelivery.Restaurant.Service.Services
{
    public class RestaurantGrpcService : RestaurantMenu.RestaurantMenuBase
    {
        private readonly Dictionary<string, double> _menu = new()
        {
            { "Spicy Fried Chicken", 15.99 },
            { "Beef Burger", 12.50 },
            { "Coca Cola", 2.00 }
        };

        public override Task<MenuResponse> CheckMenuAvailability(MenuRequest request, ServerCallContext context)
        {
            var response = new MenuResponse();  

            if (_menu.TryGetValue(request.ItemName, out var price))
            {
                response.IsAvailable = true;
                response.Price = price;
            }
            else
            {
                response.IsAvailable = false;
                response.Price = 0;
            }

            return Task.FromResult(response);
        }
    }
}