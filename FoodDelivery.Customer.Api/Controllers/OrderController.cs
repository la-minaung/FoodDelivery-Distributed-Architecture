using FoodDelivery.Customer.Api.DTOs;
using FoodDelivery.Shared.Contracts.Events;
using FoodDelivery.Shared.Contracts.gRPC;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace FoodDelivery.Customer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly RestaurantMenu.RestaurantMenuClient _grpcClient;

        public OrderController(IPublishEndpoint publishEndpoint, RestaurantMenu.RestaurantMenuClient grpcClient)
        {
            _publishEndpoint = publishEndpoint;
            _grpcClient = grpcClient;
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] CheckoutRequest request)
        {

            var grpcRequest = new MenuRequest { ItemName = request.ItemName };
            var grpcResponse = await _grpcClient.CheckMenuAvailabilityAsync(grpcRequest);

            if (!grpcResponse.IsAvailable)
            {
                return BadRequest(new { Message = $"Sorry, '{request.ItemName}' is currently out of stock." });
            }

            var orderEvent = new OrderPlacedEvent
            {
                OrderId = Guid.NewGuid().ToString(),
                CustomerName = request.CustomerName,
                ItemName = request.ItemName,
                Price = (decimal)grpcResponse.Price,
                CreatedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(orderEvent);

            return Accepted(new { Message = "Order received successfully!", OrderDetails = orderEvent });
        }
    }
}