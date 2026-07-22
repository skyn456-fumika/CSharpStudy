namespace MemoApi.Models;

public class Memo
{
    public long Id { get; set; }

    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    public string Category { get; set; } = "일반";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}