using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace GameServerManager.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    private bool _disposed;

    public event EventHandler? ExitRequested;

    public TrayIconService()
    {
        var icon = GetApplicationIcon();

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "GameServerManager",
            Icon = icon,
            Visible = true
        };

        var contextMenu =
            new Forms.ContextMenuStrip();

        var openMenuItem =
            new Forms.ToolStripMenuItem("열기");

        openMenuItem.Click += (_, _) =>
            ShowMainWindow();

        var exitMenuItem =
            new Forms.ToolStripMenuItem("종료");

        exitMenuItem.Click += (_, _) =>
        {
            ExitRequested?.Invoke(
                this,
                EventArgs.Empty);
        };

        contextMenu.Items.Add(openMenuItem);
        contextMenu.Items.Add(
            new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon.ContextMenuStrip =
            contextMenu;

        _notifyIcon.DoubleClick += (_, _) =>
            ShowMainWindow();
    }

    public void ShowInformation(
        string title,
        string message)
    {
        ShowNotification(
            title,
            message,
            Forms.ToolTipIcon.Info);
    }

    public void ShowWarning(
        string title,
        string message)
    {
        ShowNotification(
            title,
            message,
            Forms.ToolTipIcon.Warning);
    }

    public void ShowError(
        string title,
        string message)
    {
        ShowNotification(
            title,
            message,
            Forms.ToolTipIcon.Error);
    }

    private void ShowNotification(
        string title,
        string message,
        Forms.ToolTipIcon icon)
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = icon;

        _notifyIcon.ShowBalloonTip(5000);
    }

    public void ShowMainWindow()
    {
        var window =
            System.Windows.Application.Current.MainWindow;

        if (window is null)
        {
            return;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState ==
            WindowState.Minimized)
        {
            window.WindowState =
                WindowState.Normal;
        }

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
    }

    private static Icon GetApplicationIcon()
    {
        var processPath =
            Environment.ProcessPath;

        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var icon =
                Icon.ExtractAssociatedIcon(
                    processPath);

            if (icon is not null)
            {
                return icon;
            }
        }

        return SystemIcons.Application;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}