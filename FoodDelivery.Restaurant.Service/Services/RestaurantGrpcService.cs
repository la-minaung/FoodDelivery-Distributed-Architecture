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
        private readonly ILogger<RestaurantGrpcService> _logger;

        public RestaurantGrpcService(ILogger<RestaurantGrpcService> logger)
        {
            _logger = logger;
        }

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

        public override async Task SubscribeOrderStatus(OrderStatusRequest request, IServerStreamWriter<OrderStatusResponse> responseStream, ServerCallContext context)
        {

            var statuses = new[] { "Order Received", "Preparing Ingredients", "Cooking in Progress", "Packing", "Ready for Delivery" };

            foreach (var status in statuses)
            {
                // Stop processing if the client disconnects or cancels the request
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                // Push the current status update to the client stream
                await responseStream.WriteAsync(new OrderStatusResponse { Status = status });

                await Task.Delay(2000);
            }
        }

        public override async Task<LocationSummaryResponse> SendRiderLocations(IAsyncStreamReader<LocationRequest> requestStream, ServerCallContext context)
        {
            int pointCount = 0;
            string currentRiderId = string.Empty;

            // Read locations continuously until the client completes the stream
            await foreach (var location in requestStream.ReadAllAsync())
            {
                pointCount++;
                currentRiderId = location.RiderId;

                // Log the received coordinates silently
                _logger.LogInformation($"Received location from {location.RiderId}: Lat {location.Latitude}, Lng {location.Longitude}");
            }

            // Return a single summary response after the stream ends
            return new LocationSummaryResponse
            {
                Message = $"Tracking finished for rider {currentRiderId}.",
                TotalPointsReceived = pointCount
            };
        }

        public override async Task LiveChat(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
        {
            // Read incoming messages from the client continuously
            await foreach (var message in requestStream.ReadAllAsync())
            {
                // Stop processing if connection drops
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                _logger.LogInformation($"[INCOMING CHAT] {message.Sender}: {message.Text}");

                // Simulate server processing time before replying
                await Task.Delay(500);

                var reply = new ChatMessage
                {
                    Sender = "Restaurant Bot",
                    Text = $"We received your message: '{message.Text}'. Our team is checking."
                };

                // Push the reply back to the client immediately
                await responseStream.WriteAsync(reply);
            }
        }
    }
}