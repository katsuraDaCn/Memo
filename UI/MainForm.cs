namespace Memo.UI;

public class MainForm : Form
{
    readonly Store _store;
    readonly ReminderEngine _engine;
    readonly List<StickyForm> _stickies = new();
    readonly List<MemoItem> _view = new();

    ListBox _list = null!;
    TextBox _search = null!;
    ComboBox _catFilter = null!;
    ComboBox _statusFilter = null!;
    Button _btnNew = null!, _btnSettings = null!, _btnSound = null!;
    Label _status = null!, _empty = null!;
    NotifyIcon _tray = null!;
    bool _reallyExit;

    readonly Font _fTitle = new(Theme.FontName, 10f, FontStyle.Bold);
    readonly Font _fTitleDone = new(Theme.FontName, 10f, FontStyle.Bold | FontStyle.Strikeout);
    readonly Font _fSub = new(Theme.FontName, 8.75f);
    readonly Font _fChip = new(Theme.FontName, 8f);
    readonly Font _fCheck = new(Theme.FontName, 9.5f, FontStyle.Bold);

    string _lastCatKey = "";

    public MainForm(Store store, ReminderEngine engine)
    {
        _store = store;
        _engine = engine;

        Text = "备忘录";
        Font = new Font(Theme.FontName, 9f);
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(Dpi(680), Dpi(560));
        MinimumSize = new Size(Dpi(540), Dpi(430));
        BackColor = Theme.Bg;
        Icon = AppIcon.Get();
        KeyPreview = true;
        RestorePosition();

        BuildUi();
        BuildTray();

        _store.Changed += OnDataChanged;
        _engine.OnFire = ShowFirePopup;

        Load += (_, _) =>
        {
            RefreshList();
            RefreshStickies();
            _engine.Start();
        };

        var showTimer = new System.Windows.Forms.Timer { Interval = 400 };
        showTimer.Tick += (_, _) =>
        {
            if (Program.ShowRequested)
            {
                Program.ShowRequested = false;
                ShowFromTray();
            }
        };
        showTimer.Start();
    }

    int Dpi(int px) => px * DeviceDpi / 96;

    // ---------- UI 构建 ----------

    void BuildUi()
    {
        var top = new Panel { Dock = DockStyle.Top, Height = Dpi(96), BackColor = Theme.Bg };

        _btnNew = new Button
        {
            Text = "＋ 新建备忘",
            Location = new Point(Dpi(14), Dpi(12)),
            Size = new Size(Dpi(116), Dpi(34)),
            Font = new Font(Theme.FontName, 9.5f, FontStyle.Bold),
        };
        Theme.Style(_btnNew, Theme.Accent, Color.White, Theme.AccentDark);
        _btnNew.Click += (_, _) => OpenEditor(null);

        _search = new TextBox
        {
            PlaceholderText = "🔍  搜索备忘…",
            Font = new Font(Theme.FontName, 10f),
            Location = new Point(Dpi(142), Dpi(14)),
            Size = new Size(top.ClientSize.Width - Dpi(142) - Dpi(14), Dpi(31)),
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
        };
        _search.TextChanged += (_, _) => RefreshList();

        _catFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(Dpi(14), Dpi(54)),
            Size = new Size(Dpi(150), Dpi(30)),
        };
        _catFilter.SelectedIndexChanged += (_, _) => RefreshList();

        _statusFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(Dpi(172), Dpi(54)),
            Size = new Size(Dpi(110), Dpi(30)),
        };
        _statusFilter.Items.AddRange(new object[] { "全部", "未完成", "已完成" });
        _statusFilter.SelectedIndex = 0;
        _statusFilter.SelectedIndexChanged += (_, _) => RefreshList();

        _btnSettings = new Button
        {
            Text = "⚙ 设置",
            Size = new Size(Dpi(84), Dpi(30)),
            Location = new Point(top.ClientSize.Width - Dpi(84) - Dpi(14), Dpi(54)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        Theme.Style(_btnSettings, Color.White, Theme.Text, Color.FromArgb(233, 236, 245));
        _btnSettings.FlatAppearance.BorderColor = Theme.Border;
        _btnSettings.FlatAppearance.BorderSize = 1;
        _btnSettings.Click += (_, _) => ShowSettings();

        top.Controls.AddRange(new Control[] { _btnNew, _search, _catFilter, _statusFilter, _btnSettings });

        var listHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(Dpi(10), Dpi(4), Dpi(10), 0) };

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = Dpi(64),
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Bg,
            IntegralHeight = false,
        };
        _list.DrawItem += DrawItem;
        _list.MouseDown += ListMouseDown;
        _list.MouseDoubleClick += (_, e) => { if (e.Button == MouseButtons.Left && Selected() is MemoItem m) OpenEditor(m); };
        _list.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete && Selected() is MemoItem m) DeleteItem(m); };

        _empty = new Label
        {
            Text = "还没有备忘\n点左上角「＋ 新建备忘」添加一条",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Sub,
            Font = new Font(Theme.FontName, 10.5f),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Theme.Bg,
        };
        listHost.Controls.Add(_list);
        listHost.Controls.Add(_empty);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = Dpi(42), BackColor = Theme.Bg };

        _status = new Label
        {
            AutoSize = true,
            ForeColor = Theme.Sub,
            Location = new Point(Dpi(16), Dpi(12)),
        };
        _btnSound = new Button
        {
            Text = SoundText(),
            Size = new Size(Dpi(124), Dpi(28)),
            Location = new Point(bottom.ClientSize.Width - Dpi(124) - Dpi(14), Dpi(7)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        Theme.Style(_btnSound, Theme.ChipBgGray, Theme.Text, Color.FromArgb(226, 230, 242));
        _btnSound.Click += (_, _) =>
        {
            _store.Settings.SoundEnabled = !_store.Settings.SoundEnabled;
            _store.Save();
        };
        bottom.Controls.Add(_status);
        bottom.Controls.Add(_btnSound);

        // 先加 Fill，再加 Top/Bottom（WinForms 布局顺序）
        Controls.Add(listHost);
        Controls.Add(top);
        Controls.Add(bottom);
    }

    string SoundText() => $"🔔 提醒声音：{(_store.Settings.SoundEnabled ? "开" : "关")}";

    void BuildTray()
    {
        _tray = new NotifyIcon
        {
            Icon = AppIcon.Get(),
            Text = "备忘录 — 到点提醒",
            Visible = true,
        };
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主界面", null, (_, _) => ShowFromTray());
        menu.Items.Add("＋ 新建备忘", null, (_, _) => { ShowFromTray(); OpenEditor(null); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("设置", null, (_, _) => { ShowFromTray(); ShowSettings(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ReallyExit());
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowFromTray();
    }

    // ---------- 列表绘制 ----------

    void DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _view.Count) return;
        var m = _view[e.Index];
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var card = new Rectangle(e.Bounds.X + Dpi(2), e.Bounds.Y + Dpi(3), e.Bounds.Width - Dpi(4), e.Bounds.Height - Dpi(6));
        using (var path = Theme.Rounded(card, Dpi(10)))
        {
            using var bg = new SolidBrush(m.Done ? Theme.CardOff : Theme.Card);
            g.FillPath(bg, path);
            using var pen = (e.State & DrawItemState.Selected) != 0 ? new Pen(Theme.Accent, 1.6f) : new Pen(Theme.Border, 1f);
            g.DrawPath(pen, path);
        }

        var cb = new Rectangle(card.X + Dpi(14), card.Y + card.Height / 2 - Dpi(9), Dpi(18), Dpi(18));
        using (var cbPath = Theme.Rounded(cb, Dpi(5)))
        {
            if (m.Done)
            {
                using var b = new SolidBrush(Theme.Done);
                g.FillPath(b, cbPath);
                TextRenderer.DrawText(g, "✓", _fCheck, cb, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else
            {
                using var b = new SolidBrush(Color.White);
                g.FillPath(b, cbPath);
                using var p = new Pen(Color.FromArgb(198, 206, 222), 1.5f);
                g.DrawPath(p, cbPath);
            }
        }

        int tx = cb.Right + Dpi(12);
        int rightReserve = Dpi(100);
        var titleRect = new Rectangle(tx, card.Y + Dpi(7), card.Width - (tx - card.X) - rightReserve, Dpi(22));
        TextRenderer.DrawText(g, m.Title, m.Done ? _fTitleDone : _fTitle, titleRect,
            m.Done ? Theme.Done : Theme.Text,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        var now = DateTime.Now;
        string line = TimeText.DueText(m, now);
        var rep = TimeText.RepeatText(m);
        if (rep.Length > 0) line += " · " + rep;
        Color lc = m.Done ? Theme.Done
            : m.DueAt is DateTime d
                ? (d < now ? Theme.Danger : d - now < TimeSpan.FromMinutes(60) ? Theme.Warn : Theme.Sub)
                : Theme.Sub;
        var lineRect = new Rectangle(tx, card.Y + Dpi(32), card.Width - (tx - card.X) - rightReserve, Dpi(18));
        TextRenderer.DrawText(g, line, _fSub, lineRect, lc, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        string cat = string.IsNullOrWhiteSpace(m.Category) ? "未分类" : m.Category;
        var (cBg, cFg) = CategoryColor.Get(m.Category);
        var cSz = TextRenderer.MeasureText(g, cat, _fChip);
        var chip = new Rectangle(card.Right - cSz.Width - Dpi(20), card.Y + Dpi(10), cSz.Width + Dpi(12), Dpi(20));
        using (var cp = Theme.Rounded(chip, Dpi(10)))
        using (var b = new SolidBrush(cBg))
        {
            g.FillPath(b, cp);
        }
        TextRenderer.DrawText(g, cat, _fChip, chip, cFg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        if (m.Pinned)
        {
            using var b = new SolidBrush(Theme.Accent);
            g.FillEllipse(b, chip.Left - Dpi(14), chip.Top + Dpi(6), Dpi(7), Dpi(7));
        }
    }

    void ListMouseDown(object? sender, MouseEventArgs e)
    {
        int idx = _list.IndexFromPoint(e.Location);
        if (idx < 0 || idx >= _view.Count)
        {
            _list.SelectedIndex = -1;
            return;
        }
        var m = _view[idx];
        var r = _list.GetItemRectangle(idx);
        var card = new Rectangle(r.X + Dpi(2), r.Y + Dpi(3), r.Width - Dpi(4), r.Height - Dpi(6));
        var cb = new Rectangle(card.X + Dpi(14), card.Y + card.Height / 2 - Dpi(9), Dpi(18), Dpi(18));

        if (cb.Contains(e.Location))
        {
            ToggleDone(m);
            return;
        }
        _list.SelectedIndex = idx;
        if (e.Button == MouseButtons.Right) ShowItemMenu(m);
    }

    MemoItem? Selected()
    {
        int idx = _list.SelectedIndex;
        return idx >= 0 && idx < _view.Count ? _view[idx] : null;
    }

    // ---------- 数据与刷新 ----------

    void OnDataChanged()
    {
        RefreshList();
        RefreshStickies();
        _btnSound.Text = SoundText();
    }

    void RefreshList()
    {
        IEnumerable<MemoItem> q = _store.Memos;

        string kw = _search.Text.Trim();
        if (kw.Length > 0)
            q = q.Where(m => m.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)
                          || m.Content.Contains(kw, StringComparison.OrdinalIgnoreCase));

        if (_catFilter.SelectedItem is string cat && cat != "全部分类")
            q = cat == "（未分类）"
                ? q.Where(m => string.IsNullOrWhiteSpace(m.Category))
                : q.Where(m => m.Category == cat);

        q = _statusFilter.SelectedIndex switch
        {
            1 => q.Where(m => !m.Done),
            2 => q.Where(m => m.Done),
            _ => q,
        };

        var active = new List<MemoItem>();
        var done = new List<MemoItem>();
        foreach (var m in q) (m.Done ? done : active).Add(m);
        active.Sort((a, b) =>
        {
            int ra = a.DueAt.HasValue ? 0 : 1, rb = b.DueAt.HasValue ? 0 : 1;
            if (ra != rb) return ra - rb;
            if (a.DueAt is DateTime da && b.DueAt is DateTime db) return da.CompareTo(db);
            return b.UpdatedAt.CompareTo(a.UpdatedAt);
        });
        done.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));

        _view.Clear();
        _view.AddRange(active);
        _view.AddRange(done);

        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Items.AddRange(_view.ToArray());
        _list.EndUpdate();

        bool empty = _view.Count == 0;
        _empty.Visible = empty;
        _list.Visible = !empty;
        _status.Text = $"共 {_store.Memos.Count} 条 · 未完成 {_store.Memos.Count(m => !m.Done)} 条";

        // 固定分类：全部分类 / 工作 / 生活（数据里若有空分类再补一个入口）
        var cats = new List<string> { "全部分类" };
        cats.AddRange(Categories.All);
        if (_store.Memos.Any(m => string.IsNullOrWhiteSpace(m.Category))) cats.Add("（未分类）");
        var key = string.Join("\n", cats);
        if (key != _lastCatKey)
        {
            _lastCatKey = key;
            var sel = _catFilter.SelectedItem as string ?? "全部分类";
            _catFilter.Items.Clear();
            _catFilter.Items.AddRange(cats.ToArray());
            _catFilter.SelectedItem = cats.Contains(sel) ? sel : "全部分类";
        }
    }

    void ToggleDone(MemoItem m)
    {
        m.Done = !m.Done;
        if (m.Done) { m.SnoozeUntil = null; m.Pinned = false; }
        _store.Upsert(m);
    }

    void DeleteItem(MemoItem m)
    {
        if (MessageBox.Show(this, $"确定删除备忘「{m.Title}」？", "删除备忘",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _store.Delete(m);
    }

    // ---------- 右键菜单 ----------

    void ShowItemMenu(MemoItem m)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("✏ 编辑", null, (_, _) => OpenEditor(m));

        if (m.DueAt != null && !m.Done)
        {
            var later = new ToolStripMenuItem("⏰ 稍后提醒");
            AddSnoozeItems(later.DropDownItems, m);
            menu.Items.Add(later);
        }

        var pin = new ToolStripMenuItem("📌 钉为桌面便签") { Checked = m.Pinned };
        pin.Click += (_, _) => { m.Pinned = !m.Pinned; _store.Upsert(m); };
        menu.Items.Add(pin);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(m.Done ? "↩ 标记未完成" : "✔ 标记完成", null, (_, _) => ToggleDone(m));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("🗑 删除", null, (_, _) => DeleteItem(m));
        menu.Show(Cursor.Position);
    }

    void AddSnoozeItems(ToolStripItemCollection items, MemoItem m)
    {
        (string label, int minutes)[] options =
        {
            ("10分钟后", 10), ("30分钟后", 30), ("1小时后", 60), ("明天此时", 24 * 60),
        };
        foreach (var (label, minutes) in options)
        {
            var it = new ToolStripMenuItem(label);
            it.Click += (_, _) =>
            {
                ReminderEvaluator.ApplySnooze(m, minutes);
                _store.Upsert(m);
            };
            items.Add(it);
        }
    }

    // ---------- 便签 ----------

    void RefreshStickies()
    {
        var pinned = _store.Memos.Where(m => m.Pinned && !m.Done).ToList();

        for (int i = _stickies.Count - 1; i >= 0; i--)
        {
            var s = _stickies[i];
            var live = pinned.FirstOrDefault(p => p.Id == s.ItemId);
            if (live == null)
            {
                _stickies.RemoveAt(i);
                s.Close();
            }
            else
            {
                s.UpdateItem(live);
            }
        }

        int idx = 0;
        foreach (var p in pinned)
        {
            if (_stickies.Any(s => s.ItemId == p.Id)) continue;
            var sf = new StickyForm(_store, p, idx, m => OpenEditor(m));
            sf.FormClosed += (_, _) => _stickies.Remove(sf);
            sf.Show();
            _stickies.Add(sf);
            idx++;
        }
    }

    // ---------- 弹窗 / 编辑 / 设置 ----------

    void ShowFirePopup(List<Fire> fires)
    {
        if (fires.Count == 0) return;
        var popup = new ReminderPopupForm(_store, fires, m => OpenEditor(m));
        popup.Show();
    }

    void OpenEditor(MemoItem? existing)
    {
        var item = existing ?? new MemoItem();
        bool isNew = existing == null;
        using var dlg = new MemoEditForm(_store, item, isNew);
        if (dlg.ShowDialog(this) == DialogResult.OK && !dlg.Deleted)
        {
            _store.Upsert(item);
            if (!item.Done && item.DueAt is DateTime d && d <= DateTime.Now)
                _engine.RunNow();
        }
    }

    void ShowSettings()
    {
        using var dlg = new SettingsForm(_store);
        dlg.ShowDialog(this);
    }

    // ---------- 托盘 / 关闭 ----------

    void ShowFromTray()
    {
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    void ReallyExit()
    {
        _reallyExit = true;
        _tray.Visible = false;
        _tray.Dispose();
        SavePosition();
        Close();
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_reallyExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            if (_store.Settings.FirstRun)
            {
                _tray.ShowBalloonTip(2500, "备忘录仍在运行", "已最小化到托盘，到点会照常弹窗提醒。", ToolTipIcon.Info);
                _store.Settings.FirstRun = false;
                _store.SaveQuiet();
            }
            return;
        }
        SavePosition();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _tray.Visible = false;
        base.OnFormClosed(e);
    }

    void SavePosition()
    {
        _store.Settings.MainPos = $"{Location.X},{Location.Y}";
        _store.SaveQuiet();
    }

    void RestorePosition()
    {
        if (_store.Settings.MainPos is not string pos) { StartPosition = FormStartPosition.CenterScreen; return; }
        var p = ParsePos(pos);
        if (p == null) { StartPosition = FormStartPosition.CenterScreen; return; }
        var wa = Screen.FromPoint(p.Value).Bounds;
        p = new Point(
            Math.Clamp(p.Value.X, wa.Left - Width + 100, wa.Right - 100),
            Math.Clamp(p.Value.Y, wa.Top, wa.Bottom - 100));
        Location = p.Value;
    }

    static Point? ParsePos(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var parts = s.Split(',');
        if (parts.Length != 2) return null;
        try { return new Point(int.Parse(parts[0]), int.Parse(parts[1])); }
        catch (Exception) { return null; }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.N))
        {
            OpenEditor(null);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
