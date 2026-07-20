using BudgetTracker.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace BudgetTracker.Data;

public class TransactionRepository
{
    private const string ConnectionString = "Data Source=budgettracker.db";

    public void InitializeDatabase()
    {
        using SqliteConnection connection = new(ConnectionString);
        connection.Open();

        string sql = """
            CREATE TABLE IF NOT EXISTS transactions
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                transaction_date TEXT NOT NULL,
                transaction_type TEXT NOT NULL,
                category TEXT NOT NULL,
                amount TEXT NOT NULL,
                memo TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            """;

        using SqliteCommand command = new(sql, connection);
        command.ExecuteNonQuery();
    }

    public void Add(TransactionItem item)
    {
        using SqliteConnection connection = new(ConnectionString);
        connection.Open();

        string sql = """
            INSERT INTO transactions
            (
                transaction_date,
                transaction_type,
                category,
                amount,
                memo,
                created_at
            )
            VALUES
            (
                $transactionDate,
                $transactionType,
                $category,
                $amount,
                $memo,
                $createdAt
            );
            """;

        using SqliteCommand command = new(sql, connection);

        command.Parameters.AddWithValue(
            "$transactionDate",
            item.TransactionDate.ToString("yyyy-MM-dd"));

        command.Parameters.AddWithValue(
            "$transactionType",
            item.TransactionType);

        command.Parameters.AddWithValue(
            "$category",
            item.Category);

        command.Parameters.AddWithValue(
            "$amount",
            item.Amount.ToString(CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue(
            "$memo",
            item.Memo);

        command.Parameters.AddWithValue(
            "$createdAt",
            item.CreatedAt.ToString("O"));

        command.ExecuteNonQuery();
    }

    public List<TransactionItem> GetAll()
    {
        List<TransactionItem> items = new();

        using SqliteConnection connection = new(ConnectionString);
        connection.Open();

        string sql = """
            SELECT
                id,
                transaction_date,
                transaction_type,
                category,
                amount,
                memo,
                created_at
            FROM transactions
            ORDER BY transaction_date DESC, id DESC;
            """;

        using SqliteCommand command = new(sql, connection);
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            TransactionItem item = new()
            {
                Id = reader.GetInt64(0),
                TransactionDate = DateTime.Parse(reader.GetString(1)),
                TransactionType = reader.GetString(2),
                Category = reader.GetString(3),
                Amount = decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                Memo = reader.GetString(5),
                CreatedAt = DateTime.Parse(reader.GetString(6))
            };

            items.Add(item);
        }

        return items;
    }

    public void Update(TransactionItem item)
    {
        using SqliteConnection connection = new(ConnectionString);
        connection.Open();

        string sql = """
        UPDATE transactions
        SET
            transaction_date = $transactionDate,
            transaction_type = $transactionType,
            category = $category,
            amount = $amount,
            memo = $memo
        WHERE id = $id;
        """;

        using SqliteCommand command = new(sql, connection);

        command.Parameters.AddWithValue(
            "$transactionDate",
            item.TransactionDate.ToString("yyyy-MM-dd"));

        command.Parameters.AddWithValue(
            "$transactionType",
            item.TransactionType);

        command.Parameters.AddWithValue(
            "$category",
            item.Category);

        command.Parameters.AddWithValue(
            "$amount",
            item.Amount.ToString(
                CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue(
            "$memo",
            item.Memo);

        command.Parameters.AddWithValue(
            "$id",
            item.Id);

        command.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using SqliteConnection connection = new(ConnectionString);
        connection.Open();

        string sql = """
        DELETE FROM transactions
        WHERE id = $id;
        """;

        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("$id", id);

        command.ExecuteNonQuery();
    }
}