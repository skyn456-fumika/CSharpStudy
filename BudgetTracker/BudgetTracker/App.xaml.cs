using System.Windows;
using BudgetTracker.Data;

namespace BudgetTracker;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        TransactionRepository repository = new();
        repository.InitializeDatabase();
    }
}