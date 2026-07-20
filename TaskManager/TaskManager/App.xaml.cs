using System.Windows;
using TaskManager.Data;

namespace TaskManager;

public partial class App : Application
{
    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        TaskRepository repository = new();

        repository.InitializeDatabase();
    }
}