using Npgsql;
using System.Data;

namespace rinha_de_backend_2025.Infrastructure.Postgres
{
    public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public NpgsqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("postgres-connection-string") ?? "Host=postgres;Port=5432;Username=rinha_user;Password=rinha_pass;Database=rinha_db";

        }

        public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
