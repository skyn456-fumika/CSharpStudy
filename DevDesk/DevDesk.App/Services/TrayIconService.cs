using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace DevDesk.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _showMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;
    private bool _disposed;

    public TrayIconService()
    {
        _showMenuItem = new ToolStripMenuItem("DevDesk 열기");
        _exitMenuItem = new ToolStripMenuItem("종료");

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_showMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Text = "DevDesk",
            Icon = Icon.ExtractAssociatedIcon(
                Environment.ProcessPath!)
                ?? SystemIcons.Application,
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
        _showMenuItem.Click += OnShowMenuItemClick;
        _exitMenuItem.Click += OnExitMenuItemClick;
    }

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public void ShowBalloonTip(
        string title,
        string message,
        ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(
            timeout: 3000,
            tipTitle: title,
            tipText: message,
            tipIcon: icon);
    }

    private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
    {
        ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnShowMenuItemClick(object? sender, EventArgs e)
    {
        ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnExitMenuItemClick(object? sender, EventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _notifyIcon.DoubleClick -= OnNotifyIconDoubleClick;
        _showMenuItem.Click -= OnShowMenuItemClick;
        _exitMenuItem.Click -= OnExitMenuItemClick;

        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();

        GC.SuppressFinalize(this);
    }
}