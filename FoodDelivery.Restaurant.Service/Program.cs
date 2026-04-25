using FoodDelivery.Restaurant.Service.Consumers;
using FoodDelivery.Restaurant.Service.Services;
using FoodDelivery.Shared.Contracts.Events;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Message<OrderPlacedEvent>(m => m.SetEntityName("order.created"));

        var rabbitConfig = builder.Configuration.GetSection("RabbitMq");

        cfg.Host(rabbitConfig["Host"], rabbitConfig["VirtualHost"], h =>
        {
            h.Username(rabbitConfig["Username"]);
            h.Password(rabbitConfig["Password"]);
        });

        cfg.UseMessageRetry(r =>
        {
            r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromSeconds(2)
            );

        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<RestaurantGrpcService>();

app.MapGet("/", () => "Restaurant Worker Service is running and listening to RabbitMQ...");

app.Run();
