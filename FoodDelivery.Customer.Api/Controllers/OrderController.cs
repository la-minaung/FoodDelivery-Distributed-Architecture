using FoodDelivery.Customer.Api.DTOs;
using FoodDelivery.Shared.Contracts.Events;
using FoodDelivery.Shared.Contracts.gRPC;
using Grpc.Core;
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
        private readonly ILogger<OrderController> _logger;

        public OrderController(IPublishEndpoint publishEndpoint, RestaurantMenu.RestaurantMenuClient grpcClient, ILogger<OrderController> logger)
        {
            _publishEndpoint = publishEndpoint;
            _grpcClient = grpcClient;
            _logger = logger;
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


        [HttpGet("track-order/{orderId}")]
        public async IAsyncEnumerable<string> TrackOrder(string orderId)
        {
            var request = new OrderStatusRequest { OrderId = orderId };

            using var call = _grpcClient.SubscribeOrderStatus(request);

            _logger.LogInformation("--- Tracking Order: {OrderId} ---", orderId);

            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                _logger.LogInformation("[LIVE UPDATE] Order #{OrderId} : {Status}", orderId, response.Status);
                yield return $"Order {orderId} Status: {response.Status}";
            }

            _logger.LogInformation("--- Tracking Completed for Order: {OrderId} ---", orderId);
        }
    }
}