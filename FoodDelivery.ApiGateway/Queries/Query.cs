using FoodDelivery.ApiGateway.Models;
using FoodDelivery.ApiGateway.Services;

namespace FoodDelivery.ApiGateway.Queries
{
    public class Query
    {
        public async Task<Restaurant> GetRestaurantDetails([Service] RestaurantGrpcClientService restaurantService)
        {
            return await restaurantService.GetRestaurantDetailsAsync();
        }

        public async Task<MenuItem> GetMenuItemById(int id, [Service] RestaurantGrpcClientService grpcService)
        {
            return await grpcService.GetMenuItemByIdAsync(id);
        }
    }
}
