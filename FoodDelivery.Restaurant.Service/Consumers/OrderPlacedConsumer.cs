using FoodDelivery.Restaurant.Service.Data;
using FoodDelivery.Restaurant.Service.Entities;
using FoodDelivery.Shared.Contracts.Events;
using MassTransit;

namespace FoodDelivery.Restaurant.Service.Consumers
{
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {

        private readonly ILogger<OrderPlacedConsumer> _logger;
        private readonly RestaurantDbContext _dbContext;

        public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger, RestaurantDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var data = context.Message;
            var messageId = context.MessageId;

            // Idempotency Check
            // Ensure we don't process the same order twice if RabbitMQ delivers it again
            bool isAlreadyProcessed = _dbContext.Orders.Any(x => x.OrderId == data.OrderId);
            if (isAlreadyProcessed)
            {
                _logger.LogWarning("[DUPLICATE] Order {OrderId} was already processed. Skipping...", data.OrderId);
                return;
            }

            _logger.LogInformation(
                 "[MessageId: {MessageId}] [KITCHEN ALERT] Order #{OrderId} Received! Start cooking '{ItemName}' for {CustomerName}.",
                 messageId,
                 data.OrderId,
                 data.ItemName,
                 data.CustomerName);

            var newOrder = new Order
            {
                Id = Guid.NewGuid(),
                OrderId = data.OrderId,
                CustomerName = data.CustomerName,
                ItemName = data.ItemName,
                ReceivedAt = DateTime.UtcNow,
                Status = "ReadyForDelivery"
            };

            _dbContext.Orders.Add(newOrder);

            var deliveryEvent = new OrderReadyForDeliveryEvent
            {
                OrderId = data.OrderId,
                RiderNote = "Fragile. Handle with care."
            };

            await context.Publish(deliveryEvent);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "[MessageId: {MessageId}] [KITCHEN STATUS] Order #{OrderId} ('{ItemName}') is ready for delivery and event published via Outbox!",
                messageId,
                data.OrderId,
                data.ItemName);
        }
    }
}
