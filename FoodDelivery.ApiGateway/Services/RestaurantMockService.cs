using FoodDelivery.ApiGateway.Models;

namespace FoodDelivery.ApiGateway.Services
{
    public class RestaurantMockService
    {
        public Restaurant GetMockRestaurant()
        {
            return new Restaurant
            {
                Id = 1,
                Name = "Spicy Bangkok Kitchen",
                Address = "Sukhumvit Soi 11",
                Menu = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Id = 101,
                        Name = "Tom Yum Goong",
                        Price = 15.5,
                        Description = "Spicy and sour shrimp soup",
                        IsAvailable = true
                    },
                    new MenuItem
                    {
                        Id = 102,
                        Name = "Pad Thai",
                        Price = 10.0,
                        Description = "Stir-fried rice noodles with egg and peanuts",
                        IsAvailable = true
                    }
                }
            };
        }

        public MenuItem GetMenuItemById(int id)
        {
            var restaurant = GetMockRestaurant();
            return restaurant.Menu.FirstOrDefault(m => m.Id == id);
        }
    }
}
