using System.Runtime.InteropServices;

namespace Memo.UI;

/// <summary>
/// 桌面便签：把重要备忘“钉”在屏幕上常驻置顶显示，可拖动、可编辑、可取消钉住。
/// </summary>
public class StickyForm : Form
{
    [DllImport("user32.dll")]
    static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    const int WM_NCLBUTTONDOWN = 0xA1;
    const int HTCAPTION = 0x2;

    readonly Store _store;
    readonly Action<MemoItem> _onEdit;
    readonly Panel _strip = new();
    readonly Label _lblTitle = new();
    readonly Label _lblContent = new();
    readonly Panel _bottom = new();
    readonly Label _lblDue = new();
    readonly ToolTip _tip = new();

    public Guid ItemId { get; private set; }
    MemoItem _item;

    public StickyForm(Store store, MemoItem item, int cascadeIndex, Action<MemoItem> onEdit)
    {
        _store = store;
        _onEdit = onEdit;
        _item = item;
        ItemId = item.Id;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.White;
        Font = new Font(Theme.FontName, 9f);
        AutoScaleMode = AutoScaleMode.None;
        Size = new Size(Dpi(300), Dpi(206));
        Opacity = 0.97;
        Text = item.Title;

        var wa = Screen.PrimaryScreen!.WorkingArea;
        var pos = ParsePos(item.StickyPos)
                  ?? new Point(wa.Right - Width - Dpi(24) - cascadeIndex * Dpi(28), wa.Top + Dpi(60) + cascadeIndex * Dpi(30));
        pos = new Point(
            Math.Clamp(pos.X, wa.Left, Math.Max(wa.Left, wa.Right - Width)),
            Math.Clamp(pos.Y, wa.Top, Math.Max(wa.Top, wa.Bottom - Height)));
        Location = pos;

        _strip.BackColor = CategoryColor.Get(item.Category).fg;
        _strip.Location = Point.Empty;
        _strip.Size = new Size(Width, Dpi(6));

        _lblTitle.Font = new Font(Theme.FontName, 10f, FontStyle.Bold);
        _lblTitle.ForeColor = Theme.Text;
        _lblTitle.Location = new Point(Dpi(12), Dpi(12));
        _lblTitle.Size = new Size(Width - Dpi(24), Dpi(24));
        _lblTitle.AutoEllipsis = true;
        _lblTitle.Cursor = Cursors.SizeAll;

        _lblContent.Font = new Font(Theme.FontName, 9f);
        _lblContent.ForeColor = Color.FromArgb(58, 65, 80);
        _lblContent.Location = new Point(Dpi(12), Dpi(42));
        _lblContent.Size = new Size(Width - Dpi(24), Height - Dpi(42) - Dpi(40));
        _lblContent.AutoEllipsis = false;

        _bottom.BackColor = Color.FromArgb(249, 250, 253);
        _bottom.Location = new Point(0, Height - Dpi(34));
        _bottom.Size = new Size(Width, Dpi(34));

        _lblDue.AutoSize = false;
        _lblDue.Size = new Size(Dpi(150), Dpi(34));
        _lblDue.Location = new Point(Dpi(12), 0);
        _lblDue.TextAlign = ContentAlignment.MiddleLeft;

        var btnEdit = new Button
        {
            Text = "编辑",
            Size = new Size(Dpi(44), Dpi(24)),
            Location = new Point(_bottom.Width - Dpi(44) - Dpi(70) - Dpi(18), Dpi(5)),
        };
        Theme.Style(btnEdit, Color.FromArgb(238, 240, 246), Theme.Text, Color.FromArgb(226, 230, 240));
        btnEdit.Font = new Font(Theme.FontName, 8.5f);
        var btnClose = new Button
        {
            Text = "取消钉住",
            Size = new Size(Dpi(64), Dpi(24)),
            Location = new Point(_bottom.Width - Dpi(64) - Dpi(12), Dpi(5)),
        };
        Theme.Style(btnClose, Color.FromArgb(253, 235, 236), Theme.Danger, Color.FromArgb(250, 219, 222));
        btnClose.Font = new Font(Theme.FontName, 8.5f);
        btnEdit.Click += (_, _) => _onEdit(_item);
        btnClose.Click += (_, _) => { _item.Pinned = false; _store.Upsert(_item); };

        _bottom.Controls.Add(_lblDue);
        _bottom.Controls.Add(btnEdit);
        _bottom.Controls.Add(btnClose);

        Controls.Add(_strip);
        Controls.Add(_lblTitle);
        Controls.Add(_lblContent);
        Controls.Add(_bottom);

        foreach (var c in new Control[] { this, _strip, _lblTitle, _lblContent, _bottom })
            c.MouseDown += StartDrag;

        UpdateItem(item);
    }

    int Dpi(int px) => px * DeviceDpi / 96;

    public void UpdateItem(MemoItem item)
    {
        _item = item;
        ItemId = item.Id;
        _lblTitle.Text = item.Title;
        _lblContent.Text = string.IsNullOrWhiteSpace(item.Content) ? "（无内容）" : item.Content;
        _tip.SetToolTip(_lblContent, item.Content);
        _strip.BackColor = CategoryColor.Get(item.Category).fg;
        if (item.DueAt is DateTime d)
        {
            _lblDue.Text = $"{d:MM-dd HH:mm} · {TimeText.RelText(d, DateTime.Now)}";
            _lblDue.ForeColor = d < DateTime.Now ? Theme.Danger : Theme.Sub;
        }
        else
        {
            _lblDue.Text = "无提醒";
            _lblDue.ForeColor = Theme.Sub;
        }
    }

    void StartDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        _ = SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DwmRounded.Apply(this, 10);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 10);
        using var pen = new Pen(Theme.Border, 1f);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _item.StickyPos = $"{Location.X},{Location.Y}";
        _store.UpsertQuiet(_item);
        base.OnFormClosed(e);
    }

    static Point? ParsePos(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var parts = s.Split(',');
        if (parts.Length != 2) return null;
        try { return new Point(int.Parse(parts[0]), int.Parse(parts[1])); }
        catch (Exception) { return null; }
    }
}
