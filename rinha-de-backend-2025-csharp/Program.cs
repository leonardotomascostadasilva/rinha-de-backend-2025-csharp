using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using rinha_de_backend_2025.Contracts;
using rinha_de_backend_2025.Domain.Events;
using rinha_de_backend_2025.Infrastructure.BackgroundServices;
using rinha_de_backend_2025.Infrastructure.ExternalServices;
using rinha_de_backend_2025.Infrastructure.Postgres;
using rinha_de_backend_2025.Infrastructure.Repositories;
using Scalar.AspNetCore;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1", options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

builder.Services.AddHttpClient("default", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("payment-processor-default") ?? "http://payment-processor-default:8080");
    client.Timeout = TimeSpan.FromSeconds(1);
});

builder.Services.AddHttpClient("fallback", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("payment-processor-fallback") ?? "http://payment-processor-fallback:8080");
    client.Timeout = TimeSpan.FromSeconds(1);
});

builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

builder.Services.AddSingleton<ConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("redis-connection-string") ?? "redis:6379"));

builder.Services.AddHostedService<PaymentQueueProcessor>();
builder.Services.AddHostedService<PaymentQueueProcessorRetry>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentProcessorApi, PaymentProcessorApi>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();


app.MapPost("/payments", (
    [FromBody] PaymentRequest request,
    [FromServices] ConnectionMultiplexer redis,
    CancellationToken cancellationToken) =>
{
    if (!Validator.TryValidateObject(request, new ValidationContext(request), null, true))
        return Results.UnprocessableEntity();

    var db = redis.GetDatabase();
    var paymentDto = new PaymentEvent(request.CorrelationId, request.Amount, DateTime.UtcNow);
    _ = db.ListRightPushAsync("payments-processor-queue", JsonSerializer.Serialize(paymentDto));

    return Results.Accepted();

}).DisableRequestTimeout();

app.MapGet("/payments-summary", async (
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    [FromServices] IPaymentRepository paymentRepository,
    CancellationToken cancellationToken) =>
{
    var summary = await paymentRepository.GetPaymentsSummaryAsync(from, to, cancellationToken);

    return Results.Ok(summary);

}).DisableRequestTimeout();

app.Run();