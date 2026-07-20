using Microsoft.Data.Sqlite;
using TaskManager.Models;

namespace TaskManager.Data;

public class TaskRepository
{
    private const string ConnectionString =
        "Data Source=taskmanager.db";

    public void InitializeDatabase()
    {
        using SqliteConnection connection =
            new(ConnectionString);

        connection.Open();

        string sql = """
            CREATE TABLE IF NOT EXISTS tasks
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT NOT NULL,
                is_completed INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL
            );
            """;

        using SqliteCommand command =
            new(sql, connection);

        command.ExecuteNonQuery();
    }

    public void Add(TaskItem taskItem)
    {
        using SqliteConnection connection =
            new(ConnectionString);

        connection.Open();

        string sql = """
        INSERT INTO tasks
        (
            title,
            description,
            is_completed,
            created_at
        )
        VALUES
        (
            $title,
            $description,
            $isCompleted,
            $createdAt
        );
        """;

        using SqliteCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "$title",
            taskItem.Title);

        command.Parameters.AddWithValue(
            "$description",
            taskItem.Description);

        command.Parameters.AddWithValue(
            "$isCompleted",
            taskItem.IsCompleted ? 1 : 0);

        command.Parameters.AddWithValue(
            "$createdAt",
            taskItem.CreatedAt.ToString("O"));

        command.ExecuteNonQuery();
    }

    public List<TaskItem> GetAll()
    {
        List<TaskItem> taskItems = new();

        using SqliteConnection connection =
            new(ConnectionString);

        connection.Open();

        string sql = """
        SELECT
            id,
            title,
            description,
            is_completed,
            created_at
        FROM tasks
        ORDER BY id DESC;
        """;

        using SqliteCommand command =
            new(sql, connection);

        using SqliteDataReader reader =
            command.ExecuteReader();

        while (reader.Read())
        {
            TaskItem taskItem = new()
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                IsCompleted = reader.GetInt64(3) == 1,
                CreatedAt = DateTime.Parse(
                    reader.GetString(4))
            };

            taskItems.Add(taskItem);
        }

        return taskItems;
    }

    public void UpdateCompletion(
    long id,
    bool isCompleted)
    {
        using SqliteConnection connection =
            new(ConnectionString);

        connection.Open();

        string sql = """
        UPDATE tasks
        SET is_completed = $isCompleted
        WHERE id = $id;
        """;

        using SqliteCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "$isCompleted",
            isCompleted ? 1 : 0);

        command.Parameters.AddWithValue(
            "$id",
            id);

        command.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using SqliteConnection connection =
            new(ConnectionString);

        connection.Open();

        string sql = """
        DELETE FROM tasks
        WHERE id = $id;
        """;

        using SqliteCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "$id",
            id);

        command.ExecuteNonQuery();
    }

    public void Update(TaskItem taskItem)
    {
        using SqliteConnection connection =
            new(ConnectionString);

        connection.Open();

        string sql = """
        UPDATE tasks
        SET
            title = $title,
            description = $description
        WHERE id = $id;
        """;

        using SqliteCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "$title",
            taskItem.Title);

        command.Parameters.AddWithValue(
            "$description",
            taskItem.Description);

        command.Parameters.AddWithValue(
            "$id",
            taskItem.Id);

        command.ExecuteNonQuery();
    }
}