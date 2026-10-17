using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using DevopsCommander.Shared.Interfaces;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Shared.Infrastructure
{
    /// <summary>
    /// SQLite-backed persistence store for deployment state and logs.
    /// Provides durable storage for tracking deployment progress across processes.
    /// </summary>
    public class SqliteDeploymentStore : IDeploymentStore
    {
        private readonly string _connectionString;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _isInitialized;

        public SqliteDeploymentStore(string connectionString = "Data Source=deployments.db")
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            }

            _connectionString = connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
                ? connectionString
                : $"Data Source={connectionString}";
        }

        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                const string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS Deployments (
                        Id TEXT PRIMARY KEY,
                        Status TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        StartedAt TEXT NULL,
                        FinishedAt TEXT NULL,
                        ErrorMessage TEXT NULL,
                        ErrorDetails TEXT NULL,
                        RequestJson TEXT NOT NULL,
                        ExecutionLogJson TEXT NOT NULL
                    );";

                await using var command = new SqliteCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task SaveAsync(DeploymentState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO Deployments (Id, Status, CreatedAt, StartedAt, FinishedAt, ErrorMessage, ErrorDetails, RequestJson, ExecutionLogJson)
                VALUES (@Id, @Status, @CreatedAt, @StartedAt, @FinishedAt, @ErrorMessage, @ErrorDetails, @RequestJson, @ExecutionLogJson);";

            await using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", state.Id.ToString());
            command.Parameters.AddWithValue("@Status", state.Status.ToString());
            command.Parameters.AddWithValue("@CreatedAt", state.CreatedAt.ToString("o"));
            command.Parameters.AddWithValue("@StartedAt", (object?)state.StartedAt?.ToString("o") ?? DBNull.Value);
            command.Parameters.AddWithValue("@FinishedAt", (object?)state.FinishedAt?.ToString("o") ?? DBNull.Value);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)state.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@ErrorDetails", (object?)state.ErrorDetails ?? DBNull.Value);
            command.Parameters.AddWithValue("@RequestJson", JsonSerializer.Serialize(state.Request));
            command.Parameters.AddWithValue("@ExecutionLogJson", JsonSerializer.Serialize(state.ExecutionLog));

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(DeploymentState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                UPDATE Deployments
                SET Status = @Status,
                    StartedAt = @StartedAt,
                    FinishedAt = @FinishedAt,
                    ErrorMessage = @ErrorMessage,
                    ErrorDetails = @ErrorDetails,
                    ExecutionLogJson = @ExecutionLogJson
                WHERE Id = @Id;";

            await using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", state.Id.ToString());
            command.Parameters.AddWithValue("@Status", state.Status.ToString());
            command.Parameters.AddWithValue("@StartedAt", (object?)state.StartedAt?.ToString("o") ?? DBNull.Value);
            command.Parameters.AddWithValue("@FinishedAt", (object?)state.FinishedAt?.ToString("o") ?? DBNull.Value);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)state.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@ErrorDetails", (object?)state.ErrorDetails ?? DBNull.Value);
            command.Parameters.AddWithValue("@ExecutionLogJson", JsonSerializer.Serialize(state.ExecutionLog));

            await command.ExecuteNonQueryAsync();
        }

        public async Task<DeploymentState?> GetByIdAsync(Guid deploymentId)
        {
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT Id, Status, CreatedAt, StartedAt, FinishedAt, ErrorMessage, ErrorDetails, RequestJson, ExecutionLogJson
                FROM Deployments
                WHERE Id = @Id;";

            await using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", deploymentId.ToString());

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapFromReader(reader);
        }

        public async Task<IReadOnlyList<DeploymentState>> ListAsync()
        {
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT Id, Status, CreatedAt, StartedAt, FinishedAt, ErrorMessage, ErrorDetails, RequestJson, ExecutionLogJson
                FROM Deployments
                ORDER BY CreatedAt DESC;";

            await using var command = new SqliteCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var list = new List<DeploymentState>();
            while (await reader.ReadAsync())
            {
                list.Add(MapFromReader(reader));
            }

            return list;
        }

        private static DeploymentState MapFromReader(SqliteDataReader reader)
        {
            var id = Guid.Parse(reader.GetString(0));
            var status = Enum.Parse<DeployStatus>(reader.GetString(1));
            var createdAt = DateTimeOffset.Parse(reader.GetString(2));
            var startedAt = !reader.IsDBNull(3) ? DateTimeOffset.Parse(reader.GetString(3)) : (DateTimeOffset?)null;
            var finishedAt = !reader.IsDBNull(4) ? DateTimeOffset.Parse(reader.GetString(4)) : (DateTimeOffset?)null;
            var errorMessage = !reader.IsDBNull(5) ? reader.GetString(5) : null;
            var errorDetails = !reader.IsDBNull(6) ? reader.GetString(6) : null;
            var requestJson = reader.GetString(7);
            var logJson = reader.GetString(8);

            var request = JsonSerializer.Deserialize<DeployRequest>(requestJson)
                          ?? throw new InvalidOperationException($"Failed to deserialize request for deployment {id}");
            var logs = JsonSerializer.Deserialize<List<ExecutionLogEntry>>(logJson) ?? [];

            return new DeploymentState
            {
                Id = id,
                Request = request,
                Status = status,
                CreatedAt = createdAt,
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                ErrorMessage = errorMessage,
                ErrorDetails = errorDetails,
                ExecutionLog = logs
            };
        }
    }
}
