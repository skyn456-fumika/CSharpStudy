using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using DevDesk.App.Services;
using DevDesk.App.ViewModels;
using DevDesk.App.Models;
using Forms = System.Windows.Forms;

namespace DevDesk.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly TrayIconService _trayIconService;
    private bool _isClosing;
    private bool _isExitRequested;
    private bool _hasShownTrayMessage;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        _trayIconService = new TrayIconService();

        DataContext = _viewModel;

        _viewModel.LogEntries.CollectionChanged += OnLogEntriesChanged;
        _viewModel.StatusNotificationRequested +=
            OnStatusNotificationRequested;

        _trayIconService.ShowRequested += OnTrayShowRequested;
        _trayIconService.ExitRequested += OnTrayExitRequested;


        StateChanged += OnWindowStateChanged;
        Closing += OnWindowClosing;
    }

    private void OnLogEntriesChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add ||
            e.NewItems is null ||
            e.NewItems.Count == 0)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            var lastItem = e.NewItems[^1];

            if (LogDataGrid.Items.Contains(lastItem))
            {
                LogDataGrid.ScrollIntoView(lastItem);
            }
        });
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            return;
        }

        Hide();

        if (_hasShownTrayMessage)
        {
            return;
        }

        _hasShownTrayMessage = true;

        _trayIconService.ShowBalloonTip(
            "DevDesk",
            "DevDesk가 트레이에서 계속 실행 중입니다.");
    }

    private void OnTrayShowRequested(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();

        if (Topmost)
        {
            Topmost = false;
        }

        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void OnTrayExitRequested(object? sender, EventArgs e)
    {
        _isExitRequested = true;
        Close();
    }

    private void OnStatusNotificationRequested(
        object? sender,
        StatusNotificationEventArgs e)
    {
        var icon = e.IsError
            ? Forms.ToolTipIcon.Error
            : Forms.ToolTipIcon.Info;

        _trayIconService.ShowBalloonTip(
            e.Title,
            e.Message,
            icon);
    }

    private async void OnWindowClosing(
        object? sender,
        CancelEventArgs e)
    {
        if (!_isExitRequested)
        {
            e.Cancel = true;
            Hide();

            if (!_hasShownTrayMessage)
            {
                _hasShownTrayMessage = true;

                _trayIconService.ShowBalloonTip(
                    "DevDesk",
                    "창을 닫아도 DevDesk는 트레이에서 계속 실행됩니다.");
            }

            return;
        }

        if (_isClosing)
        {
            return;
        }

        e.Cancel = true;
        _isClosing = true;
        IsEnabled = false;

        try
        {
            await _viewModel.ShutdownAsync();
        }
        finally
        {
            _viewModel.LogEntries.CollectionChanged -= OnLogEntriesChanged;
            _viewModel.StatusNotificationRequested -=
                OnStatusNotificationRequested;

            _trayIconService.ShowRequested -= OnTrayShowRequested;
            _trayIconService.ExitRequested -= OnTrayExitRequested;

            StateChanged -= OnWindowStateChanged;
            Closing -= OnWindowClosing;

            _trayIconService.Dispose();

            Close();
        }
    }
}