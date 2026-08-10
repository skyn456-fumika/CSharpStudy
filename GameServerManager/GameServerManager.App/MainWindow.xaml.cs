using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using GameServerManager.App.ViewModels;

namespace GameServerManager.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    private bool _trayMessageShown;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();

        _viewModel.ExitRequested +=
            OnExitRequested;

        DataContext = _viewModel;

        _viewModel.SelectedServerLogs.CollectionChanged +=
            OnSelectedServerLogsChanged;
    }

    private void OnSelectedServerLogsChanged(
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

            if (ServerLogDataGrid.Items.Contains(lastItem))
            {
                ServerLogDataGrid.ScrollIntoView(lastItem);
            }
        });
    }

    private void Window_StateChanged(
        object? sender,
        EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            return;
        }

        Hide();

        if (_trayMessageShown)
        {
            return;
        }

        _trayMessageShown = true;

        _viewModel.ShowTrayMinimizedMessage();
    }

    private void Window_Closing(
        object? sender,
        CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();

        if (!_trayMessageShown)
        {
            _trayMessageShown = true;

            _viewModel.ShowTrayMinimizedMessage();
        }
    }

    private void OnExitRequested(
        object? sender,
        EventArgs e)
    {
        if (_viewModel.HasRunningServers)
        {
            var result =
                System.Windows.MessageBox.Show(
                    "현재 실행 중인 서버가 있습니다.\n" +
                    "GameServerManager를 종료하시겠습니까?\n\n" +
                    "실행 중인 서버도 함께 종료됩니다.",
                    "프로그램 종료",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _allowClose = true;
        Close();
    }
}