using FoodDelivery.Shared.Contracts.Events;
using MassTransit;

namespace FoodDelivery.Restaurant.Service.Consumers
{
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {

        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {

            var data = context.Message;

            _logger.LogInformation(
                "[KITCHEN ALERT] Order #{OrderId} Received! Start cooking '{ItemName}' for {CustomerName}.",
                data.OrderId,
                data.ItemName,
                data.CustomerName);

            await Task.Delay(2000);

            _logger.LogInformation(
                "[KITCHEN STATUS] Order #{OrderId} ('{ItemName}') is ready for delivery!",
                data.OrderId,
                data.ItemName);
        }
    }
}
