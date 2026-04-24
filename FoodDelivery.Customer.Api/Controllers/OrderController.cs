using FoodDelivery.Customer.Api.DTOs;
using FoodDelivery.Shared.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace FoodDelivery.Customer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly IPublishEndpoint _publishEndpoint;

        private static readonly Dictionary<string, decimal> _fakeMenu = new()
        {
            { "Spicy Fried Chicken", 15.99m },
            { "Beef Burger", 12.50m },
            { "Coca Cola", 2.00m }
        };

        public OrderController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] CheckoutRequest request)
        {

            if (!_fakeMenu.ContainsKey(request.ItemName))
            {
                return BadRequest(new { Message = $"Sorry, we don't have '{request.ItemName}' in our menu." });
            }

            var orderEvent = new OrderPlacedEvent
            {
                OrderId = Guid.NewGuid().ToString(),
                CustomerName = request.CustomerName,
                ItemName = request.ItemName,
                Price = _fakeMenu[request.ItemName],
                CreatedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(orderEvent);

            return Accepted(new { Message = "Order received successfully!", OrderDetails = orderEvent });
        }
    }
}