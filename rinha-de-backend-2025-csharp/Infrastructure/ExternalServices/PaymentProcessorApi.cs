using rinha_de_backend_2025.Domain.Events;

namespace rinha_de_backend_2025.Infrastructure.ExternalServices
{
    public sealed class PaymentProcessorApi : IPaymentProcessorApi
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentProcessorApi(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string?> TryProcessAsync(PaymentEvent paymentDto, CancellationToken cancellationToken)
        {
            var defaultClient = _httpClientFactory.CreateClient("default");
            var fallbackClient = _httpClientFactory.CreateClient("fallback");

            if (await TrySendAsync(defaultClient, paymentDto, cancellationToken))
                return "Default";

            if (await TrySendAsync(fallbackClient, paymentDto, cancellationToken))
                return "Fallback";

            return null;

        }

        private static async Task<bool> TrySendAsync(HttpClient client, PaymentEvent payment, CancellationToken cancellationToken)
        {
            try
            {
                var response = await client.PostAsJsonAsync("/payments", payment, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
