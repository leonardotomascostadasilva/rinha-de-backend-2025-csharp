using System.Data;

namespace rinha_de_backend_2025.Infrastructure.Postgres
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
    }
}
