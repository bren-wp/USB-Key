using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace Brendigo.USBKey.Services;

/// <summary>
/// Keeps a visible Windows notification-area entry while the WPF USB monitor runs.
/// This is convenience only, not a privileged system service.
/// </summary>
public sealed class TrayController : IDisposable
{
    private readonly Forms.NotifyIcon _notify;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Icon? _ownedIcon;
    private bool _disposed;

    public TrayController(Action open, Action lockNow, Action exit)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(lockNow);
        ArgumentNullException.ThrowIfNull(exit);

        _ownedIcon = Environment.ProcessPath is string path && File.Exists(path)
            ? Icon.ExtractAssociatedIcon(path)
            : null;

        _menu = new Forms.ContextMenuStrip();
        _menu.Items.Add("Otvori Brendigo USB Key", null,
            (_, _) => Dispatch(open));
        _menu.Items.Add("Zaključaj Windows", null,
            (_, _) => Dispatch(lockNow));
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("Zatvori aplikaciju", null,
            (_, _) => Dispatch(exit));

        _notify = new Forms.NotifyIcon
        {
            Icon = _ownedIcon ?? SystemIcons.Shield,
            Text = "Brendigo USB Key",
            ContextMenuStrip = _menu,
            Visible = true
        };
        _notify.DoubleClick += (_, _) => Dispatch(open);
    }

    public void ShowBackgroundHint()
    {
        if (_disposed) return;
        _notify.BalloonTipTitle = "Brendigo USB Key";
        _notify.BalloonTipText =
            "Program nastavlja lokalni nadzor USB ključeva. Desnim klikom na ikonu možeš ga potpuno zatvoriti.";
        _notify.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notify.ShowBalloonTip(2000);
    }

    private static void Dispatch(Action action)
    {
        var dispatcher = Application.Current.Dispatcher;
        if (!dispatcher.HasShutdownStarted)
            dispatcher.BeginInvoke(action);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notify.Visible = false;
        _notify.Dispose();
        _menu.Dispose();
        _ownedIcon?.Dispose();
    }
}
