using FoodDelivery.ApiGateway.Models;
using FoodDelivery.Shared.Contracts.gRPC;

namespace FoodDelivery.ApiGateway.Services
{
    public class RestaurantGrpcClientService
    {
        private readonly RestaurantMenu.RestaurantMenuClient _grpcClient;

        public RestaurantGrpcClientService(RestaurantMenu.RestaurantMenuClient grpcClient)
        {
            _grpcClient = grpcClient;
        }

        public async Task<Restaurant> GetRestaurantDetailsAsync()
        {
            var request1 = new MenuRequest { ItemName = "Tom Yum Goong" };
            var response1 = await _grpcClient.CheckMenuAvailabilityAsync(request1);

            var request2 = new MenuRequest { ItemName = "Beef Burger" };
            var response2 = await _grpcClient.CheckMenuAvailabilityAsync(request2);


            return new Restaurant
            {
                Id = 1,
                Name = "Spicy Bangkok Kitchen (Live from Original gRPC)",
                Address = "Sukhumvit Soi 11",
                Menu = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Id = 101,
                        Name = request1.ItemName,
                        Price = response1.Price,
                        Description = "Authentic Thai spicy soup",
                        IsAvailable = response1.IsAvailable
                    },
                    new MenuItem
                    {
                        Id = 102,
                        Name = request2.ItemName,
                        Price = response2.Price,
                        Description = "Stir-fried rice noodles",
                        IsAvailable = response2.IsAvailable
                    }
                }
            };
        }

        public async Task<MenuItem> GetMenuItemByIdAsync(int id)
        {
            var restaurant = await GetRestaurantDetailsAsync();
            return restaurant.Menu.FirstOrDefault(m => m.Id == id);
        }
    }
}
