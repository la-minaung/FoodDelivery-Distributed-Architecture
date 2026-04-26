using FoodDelivery.ApiGateway.Queries;
using FoodDelivery.ApiGateway.Services;
using FoodDelivery.Shared.Contracts.gRPC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpcClient<RestaurantMenu.RestaurantMenuClient>(options =>
{
    options.Address = new Uri("https://localhost:7139");
});

builder.Services.AddScoped<RestaurantGrpcClientService>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();
