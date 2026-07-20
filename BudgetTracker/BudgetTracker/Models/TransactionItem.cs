namespace BudgetTracker.Models;

public class TransactionItem
{
    public long Id { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.Today;

    public string TransactionType { get; set; } = "지출";

    public string Category { get; set; } = "";

    public decimal Amount { get; set; }

    public string Memo { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}