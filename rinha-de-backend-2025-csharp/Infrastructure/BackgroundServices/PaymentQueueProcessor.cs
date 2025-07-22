using Polly;
using rinha_de_backend_2025.Domain.Entities;
using rinha_de_backend_2025.Domain.Events;
using rinha_de_backend_2025.Infrastructure.ExternalServices;
using rinha_de_backend_2025.Infrastructure.Repositories;
using StackExchange.Redis;
using System.Text.Json;

namespace rinha_de_backend_2025.Infrastructure.BackgroundServices
{
    public class PaymentQueueProcessor : BackgroundService
    {
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly IServiceProvider _serviceProvider;

        public PaymentQueueProcessor(ConnectionMultiplexer connectionMultiplexer, IServiceProvider serviceProvider)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var workers = Enumerable
                 .Range(0, 6)
                 .Select(_ => Task.Run(() => ProcessPaymentItemAsync(stoppingToken), stoppingToken));

            await Task.WhenAll(workers);
        }

        private async Task ProcessPaymentItemAsync(CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                var redis = _connectionMultiplexer.GetDatabase();
                var item = await redis.ListLeftPopAsync("payments-processor-queue");

                if (item.IsNullOrEmpty)
                {
                    await Task.Delay(5, cancellationToken);
                    continue;
                };

                try
                {
                    var paymentRequest = JsonSerializer.Deserialize<PaymentEvent>(item);

                    if (paymentRequest != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                        var paymentProcessorApi = scope.ServiceProvider.GetRequiredService<IPaymentProcessorApi>();

                        var redisKey = $"payment:{paymentRequest.CorrelationId}";
                        var exists = await redis.KeyExistsAsync(redisKey);
                        if (exists) continue;

                        var policy = Policy
                            .Handle<Exception>()
                            .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(100));

                        var providerName = await policy.ExecuteAsync(() =>
                            paymentProcessorApi.TryProcessAsync(paymentRequest, cancellationToken)
                        );

                        if (providerName is null) continue;

                        var payment = new Payment(paymentRequest.CorrelationId, paymentRequest.Amount, providerName, paymentRequest.RequestedAt);
                        await paymentRepository.AddAsync(payment, cancellationToken);
                        await redis.StringSetAsync(redisKey, "1", TimeSpan.FromMinutes(5));
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
