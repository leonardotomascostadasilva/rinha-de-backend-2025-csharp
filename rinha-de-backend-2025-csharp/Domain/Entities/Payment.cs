namespace rinha_de_backend_2025.Domain.Entities
{
    public sealed class Payment
    {
        public Guid CorrelationId { get; set; }
        public decimal Amount { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Processor { get; set; }
        public Payment()
        {

        }
        public Payment(Guid correlationId, decimal amount, string providerName, DateTime requestedAt)
        {
            Amount = amount;
            RequestedAt = requestedAt;
            Processor = providerName;
            CorrelationId = correlationId;
        }
    }
}
