using Dapper;
using rinha_de_backend_2025.Domain.Entities;
using rinha_de_backend_2025.Infrastructure.Postgres;

namespace rinha_de_backend_2025.Infrastructure.Repositories
{
    public sealed class PaymentRepository : IPaymentRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PaymentRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
        {
            using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);

            var sql = """
                INSERT INTO payments (correlationId, amount, processor, requested_at)
                VALUES (@correlationId, @amount, @processor, @requested_at);
            """;

            var parameters = new
            {
                amount = payment.Amount,
                processor = payment.Processor,
                requested_at = payment.RequestedAt,
                correlationId = payment.CorrelationId
            };

            var command = new CommandDefinition(
                 commandText: sql,
                 parameters: parameters,
                 cancellationToken: cancellationToken
             );

            await conn.ExecuteAsync(command);
        }

        public async Task<object> GetPaymentsSummaryAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
        {
            var sql = """
                SELECT processor,
                       COUNT(*) AS total_requests,
                       COALESCE(SUM(amount), 0) AS total_amount
                FROM payments
                WHERE requested_at BETWEEN @from AND @to
                  AND (processor = 'Default' OR processor = 'Fallback')
                GROUP BY processor
            """;

            using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);

            var results = await conn.QueryAsync<(string processor, long total_requests, decimal total_amount)>(
                new CommandDefinition(sql, new { from, to }, cancellationToken: cancellationToken));

            var defaultSummary = results.FirstOrDefault(r => r.processor == "Default");
            var fallbackSummary = results.FirstOrDefault(r => r.processor == "Fallback");

            var summary = new
            {
                @default = new
                {
                    totalRequests = defaultSummary.total_requests,
                    totalAmount = defaultSummary.total_amount
                },
                fallback = new
                {
                    totalRequests = fallbackSummary.total_requests,
                    totalAmount = fallbackSummary.total_amount
                }
            };


            return summary;
        }
    }
}
