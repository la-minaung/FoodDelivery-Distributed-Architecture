using Confluent.Kafka;
using FoodDelivery.Shared.Contracts.Events;
using FoodDelivery.Shared.Contracts.gRPC;
using MassTransit;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"];

var producerConfig = new ProducerConfig
{
    BootstrapServers = bootstrapServers,

    // 1. Wait for acknowledgment from all in-sync replicas to ensure zero data loss
    Acks = Acks.All,

    // 2. Prevent message duplication in case of network errors or retries
    EnableIdempotence = true,

    // 3. Automatically retry up to 3 times in the background if delivery fails
    MessageSendMaxRetries = 3,

    // 4. Maximum number of unacknowledged requests (Must be <= 5 for Idempotence)
    MaxInFlight = 5
};

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Message<OrderPlacedEvent>(m => m.SetEntityName("order.created"));

        var rabbitConfig = builder.Configuration.GetSection("RabbitMq");

        cfg.Host(rabbitConfig["Host"], rabbitConfig["VirtualHost"], h =>
        {
            h.Username(rabbitConfig["Username"]);
            h.Password(rabbitConfig["Password"]);
        });
    });
});



builder.Services.AddGrpcClient<RestaurantMenu.RestaurantMenuClient>(o =>
{
    o.Address = new Uri("https://localhost:7139");
});



builder.Services.AddSingleton<IProducer<string, string>>(x =>
    new ProducerBuilder<string, string>(producerConfig).Build()
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
