using System.Drawing;
using System.Windows.Forms;

namespace DiscordStatusUpdater.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    public event EventHandler? ShowWindowRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? UpdateStatusRequested;
    public event EventHandler? ClearStatusRequested;

    public TrayIconService(Icon? icon = null)
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Discord Status Updater",
            Icon = icon ?? SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip { Renderer = new DarkMenuRenderer() };

        var openItem = new ToolStripMenuItem("Discord Status Updater")
        {
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(219, 222, 225)
        };
        openItem.Click += (_, _) => ShowWindowRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(openItem);

        menu.Items.Add(new ToolStripSeparator());

        var updateItem = new ToolStripMenuItem("Update Status")
        { ForeColor = Color.FromArgb(219, 222, 225) };
        updateItem.Click += (_, _) => UpdateStatusRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(updateItem);

        var clearItem = new ToolStripMenuItem("Clear Status")
        { ForeColor = Color.FromArgb(219, 222, 225) };
        clearItem.Click += (_, _) => ClearStatusRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(clearItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit")
        { ForeColor = Color.FromArgb(219, 222, 225) };
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        return menu;
    }

    public void SetTooltip(string text)
        => _notifyIcon.Text = text.Length > 63 ? text[..63] : text;

    public void ShowBalloonTip(string message, int timeoutMs = 2500)
        => _notifyIcon.ShowBalloonTip(timeoutMs, "Discord Status Updater", message, ToolTipIcon.None);

    public void Dispose()
    {
        if (_disposed) return;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _disposed = true;
    }
}

// ── Dark context-menu renderer ────────────────────────────────────────────────
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color Bg      = Color.FromArgb(43,  45,  49);
    private static readonly Color Hover   = Color.FromArgb(64,  66,  72);
    private static readonly Color Sep     = Color.FromArgb(30,  31,  34);
    private static readonly Color Fg      = Color.FromArgb(219, 222, 225);

    public DarkMenuRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var b = new SolidBrush(Bg);
        e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var color = e.Item.Selected ? Hover : Bg;
        using var b = new SolidBrush(color);
        e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Fg;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var p = new Pen(Sep);
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(p, 0, y, e.Item.Width, y);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var p = new Pen(Sep);
        e.Graphics.DrawRectangle(p, new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1));
    }
}

internal sealed class DarkColorTable : ProfessionalColorTable
{
    private static readonly Color Bg  = Color.FromArgb(43, 45, 49);
    private static readonly Color Bdr = Color.FromArgb(30, 31, 34);

    public override Color MenuBorder                        => Bdr;
    public override Color MenuItemBorder                    => Color.Transparent;
    public override Color ToolStripDropDownBackground       => Bg;
    public override Color ImageMarginGradientBegin          => Bg;
    public override Color ImageMarginGradientMiddle         => Bg;
    public override Color ImageMarginGradientEnd            => Bg;
    public override Color MenuItemSelected                  => Color.FromArgb(64, 66, 72);
    public override Color MenuItemSelectedGradientBegin     => Color.FromArgb(64, 66, 72);
    public override Color MenuItemSelectedGradientEnd       => Color.FromArgb(64, 66, 72);
    public override Color MenuItemPressedGradientBegin      => Color.FromArgb(64, 66, 72);
    public override Color MenuItemPressedGradientEnd        => Color.FromArgb(64, 66, 72);
}
