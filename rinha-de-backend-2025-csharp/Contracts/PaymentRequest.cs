namespace rinha_de_backend_2025.Contracts
{
    public sealed record PaymentRequest(Guid CorrelationId, decimal Amount);
}
