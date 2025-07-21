namespace rinha_de_backend_2025.Domain.Events
{
    public sealed record PaymentEvent(Guid CorrelationId, decimal Amount, DateTime RequestedAt);
}
