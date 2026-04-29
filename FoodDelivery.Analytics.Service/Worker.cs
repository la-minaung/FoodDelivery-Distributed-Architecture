using Confluent.Kafka;
using System.Text;

namespace FoodDelivery.Analytics.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //  Get the URL from appsettings.json dynamically
            var bootstrapServers = _configuration["Kafka:BootstrapServers"];
            var groupId = _configuration["Kafka:GroupId"];
            var topicName = _configuration["Kafka:TopicName"];

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,

                // Disable auto-commit for production-grade manual acknowledgment
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();

            consumer.Subscribe(topicName);

            _logger.LogInformation($"Analytics Service is actively listening to Kafka Topic: {topicName}");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    var key = consumeResult.Message.Key;
                    var value = consumeResult.Message.Value;
                    var eventTypeHeader = consumeResult.Message.Headers.FirstOrDefault(h => h.Key == "Event-Type");
                    var eventType = eventTypeHeader != null ? Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes()) : "Unknown";

                    _logger.LogInformation($"[RECEIVED] Event: {eventType} | Key: {key} | Value: {value}");

                    _logger.LogInformation($"[Audit Log Recorded]: {consumeResult.Message.Value} | Offset: {consumeResult.Offset}");

                    consumer.Commit(consumeResult);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Kafka Consumer is stopping...");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
