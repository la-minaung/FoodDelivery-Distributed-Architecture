using FoodDelivery.ApiGateway.Models;
using FoodDelivery.ApiGateway.Services;

namespace FoodDelivery.ApiGateway.Queries
{
    public class Query
    {
        public Restaurant GetRestaurantDetails([Service] RestaurantMockService restaurantService)
        {
            return restaurantService.GetMockRestaurant();
        }

        public MenuItem GetMenuItemById(int id, [Service] RestaurantMockService restaurantService)
        {
            return restaurantService.GetMenuItemById(id);
        }
    }
}
