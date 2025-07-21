using rinha_de_backend_2025.Domain.Entities;

namespace rinha_de_backend_2025.Infrastructure.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment, CancellationToken cancellationToken);
        Task<object> GetPaymentsSummaryAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken);
    }
}
