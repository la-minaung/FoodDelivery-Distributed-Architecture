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

        [HttpPost("simulate-rider/{riderId}")]
        public async Task<IActionResult> SimulateRiderRoute(string riderId)
        {
            // Open a client stream connection to the gRPC server
            using var call = _grpcClient.SendRiderLocations();

            _logger.LogInformation("Started location streaming for rider {RiderId}", riderId);

            // Simulate sending 5 GPS locations sequentially
            for (int i = 0; i < 5; i++)
            {
                var location = new LocationRequest
                {
                    RiderId = riderId,
                    Latitude = 13.7563 + (i * 0.001),
                    Longitude = 100.5018 + (i * 0.001)
                };

                await call.RequestStream.WriteAsync(location);
                _logger.LogInformation("Sent point {Point} to server.", i + 1);

                await Task.Delay(1000); // 1 second delay between points
            }

            // Notify the server that the stream is complete
            await call.RequestStream.CompleteAsync();

            // Await the final summary response from the server
            var response = await call.ResponseAsync;

            _logger.LogInformation("Streaming completed. Server response: {Message}", response.Message);

            return Ok(new
            {
                response.Message,
                response.TotalPointsReceived
            });
        }
    }
}