namespace Memo.UI;

/// <summary>
/// 到点提醒弹窗：屏幕右下角置顶弹出，支持逐条“稍后提醒/完成”，多条合并为一个窗口。
/// </summary>
public class ReminderPopupForm : Form
{
    static readonly List<ReminderPopupForm> Open = new();

    const int HeaderH = 54, RowH = 78, PopupW = 360;

    readonly Store _store;
    readonly Action<MemoItem> _onEdit;
    Panel _rowsHost = null!;
    readonly List<Panel> _rows = new();

    public ReminderPopupForm(Store store, List<Fire> fires, Action<MemoItem> onEdit)
    {
        _store = store;
        _onEdit = onEdit;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.White;
        Font = new Font(Theme.FontName, 9f);
        AutoScaleMode = AutoScaleMode.None;

        int visibleRows = Math.Min(fires.Count, 6);
        int bodyH = visibleRows * Dpi(RowH) + Dpi(10);
        Size = new Size(Dpi(PopupW), Dpi(HeaderH) + bodyH + Dpi(4));

        var header = new Panel { Location = new Point(0, 0), Size = new Size(Width, Dpi(HeaderH)), BackColor = Theme.Accent };
        var hTitle = new Label
        {
            Text = fires.Count > 1 ? $"🔔 备忘提醒（{fires.Count} 条）" : "🔔 备忘提醒",
            ForeColor = Color.White,
            Font = new Font(Theme.FontName, 11f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(Dpi(16), Dpi(14)),
        };
        var btnClose = new Button
        {
            Text = "✕",
            Size = new Size(Dpi(30), Dpi(30)),
            Location = new Point(Width - Dpi(42), Dpi(12)),
        };
        Theme.Style(btnClose, Theme.Accent, Color.White, Theme.AccentDark);
        btnClose.Font = new Font(Theme.FontName, 11f);
        btnClose.Click += (_, _) => Close();
        header.Controls.Add(hTitle);
        header.Controls.Add(btnClose);
        Controls.Add(header);

        _rowsHost = new Panel
        {
            Location = new Point(0, Dpi(HeaderH)),
            Size = new Size(Width, bodyH),
            AutoScroll = fires.Count > 6,
            BackColor = Color.White,
        };
        _rowsHost.AutoScrollMargin = new Size(0, Dpi(10));
        Controls.Add(_rowsHost);

        int y = 0;
        foreach (var f in fires)
        {
            var row = BuildRow(f);
            row.Location = new Point(0, y);
            y += Dpi(RowH);
            _rowsHost.Controls.Add(row);
            _rows.Add(row);
        }

        Shown += (_, _) => { Open.Add(this); RepositionSelf(); };
        FormClosed += (_, _) =>
        {
            Open.Remove(this);
            foreach (var p in Open.ToArray()) p.RepositionSelf();
        };
    }

    int Dpi(int px) => px * DeviceDpi / 96;

    void RepositionSelf()
    {
        var wa = Screen.PrimaryScreen!.WorkingArea;
        int offY = Open.Where(p => p != this).Sum(p => p.Height + Dpi(10));
        Location = new Point(wa.Right - Width - Dpi(14), wa.Bottom - Height - Dpi(14) - offY);
    }

    Panel BuildRow(Fire f)
    {
        var m = f.Item;
        int w = Width;
        bool adv = f.Kind == FireKind.Advance;

        var row = new Panel { Size = new Size(w, Dpi(RowH)), BackColor = Color.White };
        row.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(242, 243, 248), 1f);
            e.Graphics.DrawLine(pen, Dpi(14), row.Height - 1, row.Width - Dpi(14), row.Height - 1);
        };

        var tag = new Label
        {
            Text = adv ? "提前提醒" : "已到期",
            BackColor = adv ? Theme.Warn : Theme.Danger,
            ForeColor = Color.White,
            Font = new Font(Theme.FontName, 8f),
            Size = new Size(Dpi(58), Dpi(19)),
            Location = new Point(Dpi(14), Dpi(11)),
            TextAlign = ContentAlignment.MiddleCenter,
        };
        var title = new Label
        {
            Text = m.Title,
            Font = new Font(Theme.FontName, 10f, FontStyle.Bold),
            ForeColor = Theme.Text,
            Location = new Point(Dpi(14), Dpi(33)),
            Size = new Size(w - Dpi(28) - Dpi(150), Dpi(22)),
            AutoEllipsis = true,
        };
        var time = new Label
        {
            Text = m.DueAt is DateTime d ? $"{d:MM-dd HH:mm} · {TimeText.RelText(d, DateTime.Now)}" : "",
            Font = new Font(Theme.FontName, 8.5f),
            ForeColor = adv ? Theme.Warn : Theme.Danger,
            Location = new Point(Dpi(14), Dpi(55)),
            Size = new Size(w - Dpi(28) - Dpi(150), Dpi(18)),
            AutoEllipsis = true,
        };

        var btnLater = new Button
        {
            Text = "稍后 ▾",
            Size = new Size(Dpi(62), Dpi(27)),
            Location = new Point(w - Dpi(62) - Dpi(12) - Dpi(58), Dpi(25)),
        };
        Theme.Style(btnLater, Color.FromArgb(240, 242, 248), Theme.Text, Color.FromArgb(226, 230, 240));
        var btnDone = new Button
        {
            Text = "完成",
            Size = new Size(Dpi(52), Dpi(27)),
            Location = new Point(w - Dpi(52) - Dpi(12), Dpi(25)),
        };
        Theme.Style(btnDone, Theme.Accent, Color.White, Theme.AccentDark);

        var menu = new ContextMenuStrip();
        (string label, int minutes)[] options = { ("10分钟后", 10), ("30分钟后", 30), ("1小时后", 60), ("明天此时", 24 * 60) };
        foreach (var (label, minutes) in options)
        {
            var it = new ToolStripMenuItem(label);
            it.Click += (_, _) =>
            {
                ReminderEvaluator.ApplySnooze(m, minutes);
                _store.Upsert(m);
                RemoveRow(row);
            };
            menu.Items.Add(it);
        }
        btnLater.Click += (_, _) => menu.Show(btnLater, new Point(0, btnLater.Height));

        btnDone.Click += (_, _) =>
        {
            m.Done = true;
            m.Pinned = false;
            m.SnoozeUntil = null;
            _store.Upsert(m);
            RemoveRow(row);
        };

        title.DoubleClick += (_, _) => _onEdit(m);
        var tip = new ToolTip();
        tip.SetToolTip(title, "双击查看 / 编辑");
        tip.SetToolTip(time, m.Content);

        row.Controls.AddRange(new Control[] { tag, title, time, btnLater, btnDone });
        return row;
    }

    void RemoveRow(Panel row)
    {
        _rows.Remove(row);
        _rowsHost.Controls.Remove(row);
        row.Dispose();

        int y = 0;
        foreach (var r in _rows) { r.Location = new Point(0, y); y += Dpi(RowH); }

        int bodyH = Math.Min(_rows.Count, 6) * Dpi(RowH) + Dpi(10);
        _rowsHost.Height = bodyH;
        Height = Dpi(HeaderH) + bodyH + Dpi(4);
        RepositionSelf();

        if (_rows.Count == 0) Close();
    }
}
