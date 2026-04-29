using Confluent.Kafka;
using FoodDelivery.Customer.Api.DTOs;
using FoodDelivery.Shared.Contracts.Events;
using FoodDelivery.Shared.Contracts.gRPC;
using Grpc.Core;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace FoodDelivery.Customer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly RestaurantMenu.RestaurantMenuClient _grpcClient;
        private readonly ILogger<OrderController> _logger;
        private readonly IProducer<string, string> _kafkaProducer;
        private readonly IConfiguration _configuration;

        public OrderController(IPublishEndpoint publishEndpoint, RestaurantMenu.RestaurantMenuClient grpcClient, ILogger<OrderController> logger, IProducer<string, string> kafkaProducer, IConfiguration configuration)
        {
            _publishEndpoint = publishEndpoint;
            _grpcClient = grpcClient;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _configuration = configuration;
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

            // Send to RabbitMQ
            await _publishEndpoint.Publish(orderEvent);

            // Send to Kafka
            var auditMessage = $"[AUDIT] Order Placed | OrderId: {orderEvent.OrderId} | Customer: {request.CustomerName} | Item: {request.ItemName} | Time: {DateTime.UtcNow}";

            // Prepare custom headers for metadata
            var headers = new Confluent.Kafka.Headers();
            headers.Add("Source-Service", Encoding.UTF8.GetBytes("FoodDelivery.Customer.Api"));
            headers.Add("Event-Type", Encoding.UTF8.GetBytes("OrderCreatedEvent"));
            headers.Add("Correlation-Id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));

            var topicName = _configuration["Kafka:TopicName"];

            await _kafkaProducer.ProduceAsync(topicName, new Message<string, string>
            {
                Key = orderEvent.OrderId,
                Value = auditMessage,
                Headers = headers
            });

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

        [HttpPost("live-chat/{customerName}")]
        public async Task<IActionResult> StartLiveChat(string customerName, [FromBody] List<string> messages)
        {
            using var call = _grpcClient.LiveChat();
            var chatLog = new List<string>();

            _logger.LogInformation("Starting live chat session for {CustomerName}", customerName);

            // Task 1: Background thread to send messages to the server
            var sendTask = Task.Run(async () =>
            {
                foreach (var msg in messages)
                {
                    var chatMessage = new ChatMessage { Sender = customerName, Text = msg };
                    await call.RequestStream.WriteAsync(chatMessage);

                    // Simulate user typing delay
                    await Task.Delay(1000);
                }

                // Notify the server that we have finished sending
                await call.RequestStream.CompleteAsync();
            });

            // Task 2: Background thread to read responses from the server
            var readTask = Task.Run(async () =>
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    chatLog.Add($"{response.Sender}: {response.Text}");
                }
            });

            // Wait for both sending and reading streams to complete
            await Task.WhenAll(sendTask, readTask);

            _logger.LogInformation("Live chat session completed for {CustomerName}", customerName);

            return Ok(new { SessionEnd = true, ChatLog = chatLog });
        }
    }
}