using FoodDelivery.ApiGateway.Queries;
using FoodDelivery.ApiGateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RestaurantMockService>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();
