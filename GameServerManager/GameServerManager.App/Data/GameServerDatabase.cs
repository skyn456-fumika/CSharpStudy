using GameServerManager.App.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;

namespace GameServerManager.App.Data;

public class GameServerDatabase
{
    private readonly string _connectionString;

    public GameServerDatabase()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "GameServerManager");

        Directory.CreateDirectory(directory);

        var databasePath = Path.Combine(
            directory,
            "gameservermanager.db");

        _connectionString =
            new SqliteConnectionStringBuilder
            {
                DataSource = databasePath
            }.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS game_servers
            (
                id                   TEXT PRIMARY KEY,
                server_name          TEXT NOT NULL,
                executable_path      TEXT NOT NULL,
                arguments            TEXT NOT NULL,
                working_directory    TEXT NOT NULL,
                host                 TEXT NOT NULL,
                port                 INTEGER NOT NULL,
                auto_restart         INTEGER NOT NULL,
                start_order          INTEGER NOT NULL DEFAULT 0,
                dependency_server_id TEXT NULL,
                cpu_warning_threshold REAL NOT NULL DEFAULT 80,
                memory_warning_threshold_mb REAL NOT NULL DEFAULT 500
            );

            CREATE TABLE IF NOT EXISTS server_histories
            (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                server_id   TEXT NOT NULL,
                server_name TEXT NOT NULL,
                event_type  TEXT NOT NULL,
                is_success  INTEGER NOT NULL,
                message     TEXT NOT NULL,
                created_at  TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS
                idx_server_histories_created_at
            ON server_histories(created_at DESC);

            CREATE INDEX IF NOT EXISTS
                idx_server_histories_server_id
            ON server_histories(server_id);
            """;

        await command.ExecuteNonQueryAsync();

        try
        {
            var alterCommand = connection.CreateCommand();

            alterCommand.CommandText =
                """
                ALTER TABLE game_servers
                ADD COLUMN dependency_server_id TEXT NULL;
                """;

            await alterCommand.ExecuteNonQueryAsync();
        }
        catch (SqliteException ex)
            when (ex.SqliteErrorCode == 1 &&
                  ex.Message.Contains(
                      "duplicate column name",
                      StringComparison.OrdinalIgnoreCase))
        {
            // 이미 컬럼이 있으면 무시한다.
        }

        try
        {
            var alterCommand = connection.CreateCommand();

            alterCommand.CommandText =
                """
                ALTER TABLE game_servers
                ADD COLUMN cpu_warning_threshold
                REAL NOT NULL DEFAULT 80;
                """;

            await alterCommand.ExecuteNonQueryAsync();
        }
        catch (SqliteException ex)
            when (ex.SqliteErrorCode == 1 &&
                  ex.Message.Contains(
                      "duplicate column name",
                      StringComparison.OrdinalIgnoreCase))
        {
        }

        try
        {
            var alterCommand = connection.CreateCommand();

            alterCommand.CommandText =
                """
                ALTER TABLE game_servers
                ADD COLUMN memory_warning_threshold_mb
                REAL NOT NULL DEFAULT 500;
                """;

            await alterCommand.ExecuteNonQueryAsync();
        }
        catch (SqliteException ex)
            when (ex.SqliteErrorCode == 1 &&
                  ex.Message.Contains(
                      "duplicate column name",
                      StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    public async Task AddHistoryAsync(
        ServerHistoryEntry history)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO server_histories
            (
                server_id,
                server_name,
                event_type,
                is_success,
                message,
                created_at
            )
            VALUES
            (
                $serverId,
                $serverName,
                $eventType,
                $isSuccess,
                $message,
                $createdAt
            );
            """;

        command.Parameters.AddWithValue(
            "$serverId",
            history.ServerId.ToString());

        command.Parameters.AddWithValue(
            "$serverName",
            history.ServerName);

        command.Parameters.AddWithValue(
            "$eventType",
            history.EventType);

        command.Parameters.AddWithValue(
            "$isSuccess",
            history.IsSuccess ? 1 : 0);

        command.Parameters.AddWithValue(
            "$message",
            history.Message);

        command.Parameters.AddWithValue(
            "$createdAt",
            history.CreatedAt.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<ServerHistoryEntry>>
        GetHistoriesAsync(
            Guid? serverId = null,
            int limit = 500)
    {
        var histories = new List<ServerHistoryEntry>();

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        if (serverId is null)
        {
            command.CommandText =
                """
                SELECT
                    id,
                    server_id,
                    server_name,
                    event_type,
                    is_success,
                    message,
                    created_at
                FROM server_histories
                ORDER BY created_at DESC
                LIMIT $limit;
                """;
        }
        else
        {
            command.CommandText =
                """
                SELECT
                    id,
                    server_id,
                    server_name,
                    event_type,
                    is_success,
                    message,
                    created_at
                FROM server_histories
                WHERE server_id = $serverId
                ORDER BY created_at DESC
                LIMIT $limit;
                """;

            command.Parameters.AddWithValue(
                "$serverId",
                serverId.Value.ToString());
        }

        command.Parameters.AddWithValue("$limit", limit);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            histories.Add(
                new ServerHistoryEntry
                {
                    Id = reader.GetInt64(0),
                    ServerId = Guid.Parse(
                        reader.GetString(1)),
                    ServerName = reader.GetString(2),
                    EventType = reader.GetString(3),
                    IsSuccess = reader.GetInt64(4) == 1,
                    Message = reader.GetString(5),
                    CreatedAt = DateTime.Parse(
                        reader.GetString(6),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind)
                });
        }

        return histories;
    }

    public async Task DeleteAllHistoriesAsync()
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
            "DELETE FROM server_histories;";

        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveServerAsync(GameServerModel server)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO game_servers
            (
                id,
                server_name,
                executable_path,
                arguments,
                working_directory,
                host,
                port,
                auto_restart,
                start_order,
                dependency_server_id,
                cpu_warning_threshold,
                memory_warning_threshold_mb
            )
            VALUES
            (
                $id,
                $serverName,
                $executablePath,
                $arguments,
                $workingDirectory,
                $host,
                $port,
                $autoRestart,
                $startOrder,
                $dependencyServerId,
                $cpuWarningThreshold,
                $memoryWarningThresholdMb
            )
            ON CONFLICT(id) DO UPDATE SET
                server_name = excluded.server_name,
                executable_path = excluded.executable_path,
                arguments = excluded.arguments,
                working_directory = excluded.working_directory,
                host = excluded.host,
                port = excluded.port,
                auto_restart = excluded.auto_restart,
                start_order = excluded.start_order,
                dependency_server_id = excluded.dependency_server_id,
                cpu_warning_threshold = excluded.cpu_warning_threshold,
                memory_warning_threshold_mb = excluded.memory_warning_threshold_mb;
            """;

        command.Parameters.AddWithValue(
            "$id",
            server.Id.ToString());

        command.Parameters.AddWithValue(
            "$serverName",
            server.ServerName);

        command.Parameters.AddWithValue(
            "$executablePath",
            server.ExecutablePath);

        command.Parameters.AddWithValue(
            "$arguments",
            server.Arguments);

        command.Parameters.AddWithValue(
            "$workingDirectory",
            server.WorkingDirectory);

        command.Parameters.AddWithValue(
            "$host",
            server.Host);

        command.Parameters.AddWithValue(
            "$port",
            server.Port);

        command.Parameters.AddWithValue(
            "$autoRestart",
            server.AutoRestart ? 1 : 0);

        command.Parameters.AddWithValue(
            "$startOrder",
            server.StartOrder);

        command.Parameters.AddWithValue(
            "$dependencyServerId",
            server.DependencyServerId?.ToString()
                ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "$cpuWarningThreshold",
            server.CpuWarningThreshold);

        command.Parameters.AddWithValue(
            "$memoryWarningThresholdMb",
            server.MemoryWarningThresholdMb);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<GameServerModel>> GetServersAsync()
    {
        var servers = new List<GameServerModel>();

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                id,
                server_name,
                executable_path,
                arguments,
                working_directory,
                host,
                port,
                auto_restart,
                start_order,
                dependency_server_id,
                cpu_warning_threshold,
                memory_warning_threshold_mb
            FROM game_servers
            ORDER BY start_order, server_name;
            """;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            servers.Add(
                new GameServerModel
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ServerName = reader.GetString(1),
                    ExecutablePath = reader.GetString(2),
                    Arguments = reader.GetString(3),
                    WorkingDirectory = reader.GetString(4),
                    Host = reader.GetString(5),
                    Port = reader.GetInt32(6),
                    AutoRestart = reader.GetInt64(7) == 1,
                    StartOrder = reader.GetInt32(8),
                    DependencyServerId =
                        reader.IsDBNull(9)
                            ? null
                            : Guid.Parse(reader.GetString(9)),
                    CpuWarningThreshold = reader.GetDouble(10),
                    MemoryWarningThresholdMb = reader.GetDouble(11),
                    Status = ServerStatus.Stopped,
                    TcpStatus = TcpConnectionStatus.NotChecked,
                    OverallStatus = ServerOverallStatus.Stopped
                });
        }

        foreach (var server in servers)
        {
            if (server.DependencyServerId is null)
            {
                server.DependencyServerName = "없음";
                continue;
            }

            var dependency = servers.FirstOrDefault(
                item => item.Id == server.DependencyServerId);

            server.DependencyServerName =
                dependency?.ServerName ?? "삭제된 서버";
        }

        return servers;
    }

    public async Task DeleteServerAsync(Guid serverId)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            DELETE FROM game_servers
            WHERE id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            serverId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<ServerHistoryEntry>> GetFilteredHistoriesAsync(
        Guid? serverId,
        string? eventType,
        bool? isSuccess,
        int limit = 500)
    {
        var histories =
            new List<ServerHistoryEntry>();

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        var conditions =
            new List<string>();

        if (serverId is not null)
        {
            conditions.Add(
                "server_id = $serverId");

            command.Parameters.AddWithValue(
                "$serverId",
                serverId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            conditions.Add(
                "event_type = $eventType");

            command.Parameters.AddWithValue(
                "$eventType",
                eventType);
        }

        if (isSuccess is not null)
        {
            conditions.Add(
                "is_success = $isSuccess");

            command.Parameters.AddWithValue(
                "$isSuccess",
                isSuccess.Value ? 1 : 0);
        }

        var whereClause =
            conditions.Count == 0
                ? string.Empty
                : "WHERE " +
                  string.Join(
                      " AND ",
                      conditions);

        command.CommandText =
            $"""
        SELECT
            id,
            server_id,
            server_name,
            event_type,
            is_success,
            message,
            created_at
        FROM server_histories
        {whereClause}
        ORDER BY created_at DESC
        LIMIT $limit;
        """;

        command.Parameters.AddWithValue(
            "$limit",
            limit);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            histories.Add(
                new ServerHistoryEntry
                {
                    Id = reader.GetInt64(0),
                    ServerId =
                        Guid.Parse(reader.GetString(1)),
                    ServerName =
                        reader.GetString(2),
                    EventType =
                        reader.GetString(3),
                    IsSuccess =
                        reader.GetInt64(4) == 1,
                    Message =
                        reader.GetString(5),
                    CreatedAt =
                        DateTime.Parse(reader.GetString(6))
                });
        }

        return histories;
    }

    public async Task<int> DeleteOldHistoriesAsync(
        int retentionDays)
    {
        if (retentionDays <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retentionDays));
        }

        var cutoff =
            DateTime.Now.AddDays(-retentionDays);

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
            """
        DELETE FROM server_histories
        WHERE created_at < $cutoff;
        """;

        command.Parameters.AddWithValue(
            "$cutoff",
            cutoff.ToString("O"));

        return await command.ExecuteNonQueryAsync();
    }
}