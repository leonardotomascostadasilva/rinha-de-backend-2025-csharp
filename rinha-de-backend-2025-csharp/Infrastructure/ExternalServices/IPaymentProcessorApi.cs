using rinha_de_backend_2025.Domain.Events;

namespace rinha_de_backend_2025.Infrastructure.ExternalServices
{
    public interface IPaymentProcessorApi
    {
        Task<string?> TryProcessAsync(PaymentEvent paymentDto, CancellationToken cancellationToken);
    }
}
