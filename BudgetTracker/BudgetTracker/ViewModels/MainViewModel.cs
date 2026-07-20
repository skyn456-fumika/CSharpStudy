using System.Collections.ObjectModel;
using BudgetTracker.Data;
using BudgetTracker.Models;

namespace BudgetTracker.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly TransactionRepository repository;

    private DateTime transactionDate = DateTime.Today;
    private string transactionType = "지출";
    private string category = "";
    private string amountText = "";
    private string memo = "";
    private TransactionItem? selectedTransaction;
    private bool isEditing;
    private long editingId;
    private DateTime selectedMonth = DateTime.Today;
    private string selectedTypeFilter = "전체";

    public MainViewModel()
    {
        repository = new TransactionRepository();

        AddCommand = new RelayCommand(
            AddTransaction,
            CanAddTransaction);

        StartEditCommand = new RelayCommand(
            StartEdit,
            CanStartEdit);

        SaveEditCommand = new RelayCommand(
            SaveEdit,
            CanSaveEdit);

        CancelEditCommand = new RelayCommand(
            CancelEdit,
            () => IsEditing);

        DeleteCommand = new RelayCommand(
            DeleteTransaction,
            () => SelectedTransaction != null);

        LoadTransactions();
    }

    public ObservableCollection<TransactionItem> Transactions { get; } = new();

    public string[] TransactionTypes { get; } =
    {
        "수입",
        "지출"
    };

    public DateTime TransactionDate
    {
        get => transactionDate;
        set => SetProperty(ref transactionDate, value);
    }

    public string TransactionType
    {
        get => transactionType;

        set
        {
            if (SetProperty(ref transactionType, value))
            {
                AddCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Category
    {
        get => category;

        set
        {
            if (SetProperty(ref category, value))
            {
                AddCommand.RaiseCanExecuteChanged();
                SaveEditCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string AmountText
    {
        get => amountText;

        set
        {
            if (SetProperty(ref amountText, value))
            {
                AddCommand.RaiseCanExecuteChanged();
                SaveEditCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Memo
    {
        get => memo;
        set => SetProperty(ref memo, value);
    }

    public TransactionItem? SelectedTransaction
    {
        get => selectedTransaction;

        set
        {
            if (SetProperty(ref selectedTransaction, value))
            {
                StartEditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsEditing
    {
        get => isEditing;

        set
        {
            if (SetProperty(ref isEditing, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public string[] TypeFilters { get; } =
    {
        "전체",
        "수입",
        "지출"
    };

    public DateTime SelectedMonth
    {
        get => selectedMonth;
        set
        {
            if (SetProperty(ref selectedMonth, value))
            {
                LoadTransactions();
            }
        }
    }

    public string SelectedTypeFilter
    {
        get => selectedTypeFilter;
        set
        {
            if (SetProperty(ref selectedTypeFilter, value))
            {
                LoadTransactions();
            }
        }
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand StartEditCommand { get; }
    public RelayCommand SaveEditCommand { get; }
    public RelayCommand CancelEditCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public decimal TotalIncome =>
        Transactions
            .Where(item => item.TransactionType == "수입")
            .Sum(item => item.Amount);

    public decimal TotalExpense =>
        Transactions
            .Where(item => item.TransactionType == "지출")
            .Sum(item => item.Amount);

    public decimal Balance => TotalIncome - TotalExpense;

    private bool CanAddTransaction()
    {
        return !IsEditing
            && !string.IsNullOrWhiteSpace(Category)
            && decimal.TryParse(AmountText, out decimal amount)
            && amount > 0;
    }

    private void RefreshCommandStates()
    {
        AddCommand.RaiseCanExecuteChanged();
        StartEditCommand.RaiseCanExecuteChanged();
        SaveEditCommand.RaiseCanExecuteChanged();
        CancelEditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }

    private void AddTransaction()
    {
        if (!decimal.TryParse(AmountText, out decimal amount))
        {
            return;
        }

        TransactionItem item = new()
        {
            TransactionDate = TransactionDate,
            TransactionType = TransactionType,
            Category = Category.Trim(),
            Amount = amount,
            Memo = Memo.Trim(),
            CreatedAt = DateTime.Now
        };

        repository.Add(item);

        Category = "";
        AmountText = "";
        Memo = "";

        LoadTransactions();
    }

    private void LoadTransactions()
    {
        SelectedTransaction = null;
        Transactions.Clear();

        IEnumerable<TransactionItem> items = repository.GetAll()
            .Where(item =>
                item.TransactionDate.Year == SelectedMonth.Year &&
                item.TransactionDate.Month == SelectedMonth.Month);

        if (SelectedTypeFilter != "전체")
        {
            items = items.Where(item =>
                item.TransactionType == SelectedTypeFilter);
        }

        foreach (TransactionItem item in items)
        {
            Transactions.Add(item);
        }

        OnPropertyChanged(nameof(TotalIncome));
        OnPropertyChanged(nameof(TotalExpense));
        OnPropertyChanged(nameof(Balance));
    }

    private bool CanStartEdit()
    {
        return SelectedTransaction != null
            && !IsEditing;
    }

    private void StartEdit()
    {
        if (SelectedTransaction == null)
        {
            return;
        }

        editingId = SelectedTransaction.Id;

        TransactionDate = SelectedTransaction.TransactionDate;
        TransactionType = SelectedTransaction.TransactionType;
        Category = SelectedTransaction.Category;
        AmountText = SelectedTransaction.Amount.ToString();
        Memo = SelectedTransaction.Memo;

        IsEditing = true;
    }

    private bool CanSaveEdit()
    {
        return IsEditing
            && !string.IsNullOrWhiteSpace(Category)
            && decimal.TryParse(AmountText, out decimal amount)
            && amount > 0;
    }

    private void SaveEdit()
    {
        if (!decimal.TryParse(AmountText, out decimal amount))
        {
            return;
        }

        TransactionItem item = new()
        {
            Id = editingId,
            TransactionDate = TransactionDate,
            TransactionType = TransactionType,
            Category = Category.Trim(),
            Amount = amount,
            Memo = Memo.Trim()
        };

        repository.Update(item);

        ClearInput();
        LoadTransactions();
    }

    private void CancelEdit()
    {
        ClearInput();
    }

    private void DeleteTransaction()
    {
        if (SelectedTransaction == null)
        {
            return;
        }

        repository.Delete(SelectedTransaction.Id);

        ClearInput();
        LoadTransactions();
    }

    private void ClearInput()
    {
        editingId = 0;
        TransactionDate = DateTime.Today;
        TransactionType = "지출";
        Category = "";
        AmountText = "";
        Memo = "";
        IsEditing = false;
    }
}