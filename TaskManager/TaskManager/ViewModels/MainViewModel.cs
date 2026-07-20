using System.Collections.ObjectModel;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly TaskRepository repository;

    private string title = "";
    private string description = "";
    private TaskItem? selectedTaskItem;
    private bool isEditing;
    private long editingTaskId;
    private string selectedFilter = "전체";

    public MainViewModel()
    {
        repository = new TaskRepository();

        AddCommand = new RelayCommand(
            AddTask,
            CanAddTask);

        ToggleCompletionCommand =
    new RelayCommand(
        ToggleCompletion,
        HasSelectedTask);

        DeleteCommand =
            new RelayCommand(
                DeleteTask,
                HasSelectedTask);

        StartEditCommand =
    new RelayCommand(
        StartEdit,
        HasSelectedTask);

        SaveEditCommand =
            new RelayCommand(
                SaveEdit,
                CanSaveEdit);

        CancelEditCommand =
            new RelayCommand(
                CancelEdit,
                () => IsEditing);

        StartEditCommand =
    new RelayCommand(
        StartEdit,
        CanStartEdit);

        LoadTasks();
    }

    public ObservableCollection<TaskItem> TaskItems
    {
        get;
    } = new();

    public string Title
    {
        get => title;

        set
        {
            if (SetProperty(ref title, value))
            {
                AddCommand.RaiseCanExecuteChanged();
                SaveEditCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    public RelayCommand StartEditCommand { get; }

    public RelayCommand SaveEditCommand { get; }

    public RelayCommand CancelEditCommand { get; }

    public RelayCommand AddCommand { get; }

    public RelayCommand ToggleCompletionCommand
    {
        get;
    }

    public RelayCommand DeleteCommand
    {
        get;
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

    public string[] Filters { get; } = { "전체", "진행 중", "완료" };

    private void RefreshCommandStates()
    {
        AddCommand.RaiseCanExecuteChanged();
        StartEditCommand.RaiseCanExecuteChanged();
        SaveEditCommand.RaiseCanExecuteChanged();
        CancelEditCommand.RaiseCanExecuteChanged();
        ToggleCompletionCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }

    private bool CanAddTask()
    {
        return !IsEditing
            && !string.IsNullOrWhiteSpace(Title);
    }

    private void AddTask()
    {
        TaskItem taskItem = new()
        {
            Title = Title.Trim(),
            Description = Description.Trim(),
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };

        repository.Add(taskItem);

        Title = "";
        Description = "";

        LoadTasks();
    }

    private void LoadTasks()
    {
        SelectedTaskItem = null;
        TaskItems.Clear();

        IEnumerable<TaskItem> taskItems = repository.GetAll();

        if (SelectedFilter == "진행 중")
        {
            taskItems = taskItems.Where(task => !task.IsCompleted);
        }
        else if (SelectedFilter == "완료")
        {
            taskItems = taskItems.Where(task => task.IsCompleted);
        }

        foreach (TaskItem taskItem in taskItems)
        {
            TaskItems.Add(taskItem);
        }
    }

    public TaskItem? SelectedTaskItem
    {
        get => selectedTaskItem;

        set
        {
            if (SetProperty(
                ref selectedTaskItem,
                value))
            {
                StartEditCommand.RaiseCanExecuteChanged();

                ToggleCompletionCommand
                    .RaiseCanExecuteChanged();

                DeleteCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    private bool HasSelectedTask()
    {
        return SelectedTaskItem != null;
    }

    private void ToggleCompletion()
    {
        if (SelectedTaskItem == null)
        {
            return;
        }

        bool newCompletion =
            !SelectedTaskItem.IsCompleted;

        repository.UpdateCompletion(
            SelectedTaskItem.Id,
            newCompletion);

        LoadTasks();
    }

    private void DeleteTask()
    {
        if (SelectedTaskItem == null)
        {
            return;
        }

        repository.Delete(
            SelectedTaskItem.Id);

        LoadTasks();
    }

    private bool CanStartEdit()
    {
        return SelectedTaskItem != null
            && !IsEditing;
    }

    private void StartEdit()
    {
        if (SelectedTaskItem == null)
        {
            return;
        }

        editingTaskId = SelectedTaskItem.Id;

        Title = SelectedTaskItem.Title;
        Description = SelectedTaskItem.Description;

        IsEditing = true;

        RefreshCommandStates();
    }

    private bool CanSaveEdit()
    {
        return IsEditing
            && !string.IsNullOrWhiteSpace(Title);
    }

    private void SaveEdit()
    {
        if (!CanSaveEdit())
        {
            return;
        }

        TaskItem taskItem = new()
        {
            Id = editingTaskId,
            Title = Title.Trim(),
            Description = Description.Trim()
        };

        repository.Update(taskItem);

        ClearEditState();
        LoadTasks();
    }

    private void CancelEdit()
    {
        ClearEditState();
    }

    private void ClearEditState()
    {
        editingTaskId = 0;

        Title = "";
        Description = "";

        IsEditing = false;

        RefreshCommandStates();
    }

    public string SelectedFilter
    {
        get => selectedFilter;
        set
        {
            if (SetProperty(ref selectedFilter, value))
            {
                LoadTasks();
            }
        }
    }

}