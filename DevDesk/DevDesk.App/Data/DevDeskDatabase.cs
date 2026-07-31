using System.IO;
using DevDesk.App.Models;
using Microsoft.Data.Sqlite;

namespace DevDesk.App.Data;

public class DevDeskDatabase
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _databaseLock = new(1, 1);

    public DevDeskDatabase()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevDesk");

        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "devdesk.db");
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task InitializeAsync()
    {
        await _databaseLock.WaitAsync();

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS CheckHistories
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CheckType TEXT NOT NULL,
                    Target TEXT NOT NULL,
                    IsSuccess INTEGER NOT NULL,
                    StatusCode INTEGER NULL,
                    ResponseTimeMs INTEGER NOT NULL,
                    Message TEXT NOT NULL,
                    CheckedAt TEXT NOT NULL
                );
                """;

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    public async Task AddHistoryAsync(CheckHistoryModel history)
    {
        await _databaseLock.WaitAsync();

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO CheckHistories
                (
                    CheckType,
                    Target,
                    IsSuccess,
                    StatusCode,
                    ResponseTimeMs,
                    Message,
                    CheckedAt
                )
                VALUES
                (
                    $checkType,
                    $target,
                    $isSuccess,
                    $statusCode,
                    $responseTimeMs,
                    $message,
                    $checkedAt
                );
                """;

            command.Parameters.AddWithValue("$checkType", history.CheckType);
            command.Parameters.AddWithValue("$target", history.Target);
            command.Parameters.AddWithValue("$isSuccess", history.IsSuccess ? 1 : 0);

            command.Parameters.AddWithValue(
                "$statusCode",
                history.StatusCode is null
                    ? DBNull.Value
                    : history.StatusCode.Value);

            command.Parameters.AddWithValue(
                "$responseTimeMs",
                history.ResponseTimeMs);

            command.Parameters.AddWithValue("$message", history.Message);
            command.Parameters.AddWithValue(
                "$checkedAt",
                history.CheckedAt.ToString("O"));

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    public async Task<List<CheckHistoryModel>> GetHistoriesAsync(
        int limit = 1000)
    {
        await _databaseLock.WaitAsync();

        try
        {
            var histories = new List<CheckHistoryModel>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                """
            SELECT
                Id,
                CheckType,
                Target,
                IsSuccess,
                StatusCode,
                ResponseTimeMs,
                Message,
                CheckedAt
            FROM CheckHistories
            ORDER BY CheckedAt DESC
            LIMIT $limit;
            """;

            command.Parameters.AddWithValue("$limit", limit);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                histories.Add(new CheckHistoryModel
                {
                    Id = reader.GetInt64(0),
                    CheckType = reader.GetString(1),
                    Target = reader.GetString(2),
                    IsSuccess = reader.GetInt64(3) == 1,
                    StatusCode = reader.IsDBNull(4)
                        ? null
                        : reader.GetInt32(4),
                    ResponseTimeMs = reader.GetInt64(5),
                    Message = reader.GetString(6),
                    CheckedAt = DateTime.Parse(reader.GetString(7))
                });
            }

            return histories;
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    public async Task DeleteAllHistoriesAsync()
    {
        await _databaseLock.WaitAsync();

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CheckHistories;";

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _databaseLock.Release();
        }
    }
}