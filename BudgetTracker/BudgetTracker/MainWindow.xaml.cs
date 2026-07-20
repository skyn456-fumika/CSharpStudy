using System.Windows;
using BudgetTracker.ViewModels;

namespace BudgetTracker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel();
    }
}