namespace Memo.UI;

public class MemoEditForm : Form
{
    readonly Store _store;
    readonly MemoItem _item;
    readonly bool _isNew;
    readonly DateTime? _origDue;

    TextBox _txtTitle = null!, _txtContent = null!;
    ComboBox _cmbCat = null!, _cmbAdvance = null!, _cmbRepeat = null!;
    CheckBox _chkDue = null!, _chkPin = null!;
    DateTimePicker _dtpDate = null!, _dtpTime = null!;

    static readonly int[] AdvanceValues = { 0, 5, 10, 30, 60 };

    /// <summary>点了“删除”并确认，主窗口据此跳过 Upsert。</summary>
    public bool Deleted { get; private set; }

    public MemoEditForm(Store store, MemoItem item, bool isNew)
    {
        _store = store;
        _item = item;
        _isNew = isNew;
        _origDue = item.DueAt;

        Text = isNew ? "新建备忘" : "编辑备忘";
        Font = new Font(Theme.FontName, 9f);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = Color.White;
        ClientSize = new Size(Dpi(440), Dpi(430));

        Label AddLabel(string text, int y)
        {
            var l = new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Theme.Sub,
                Location = new Point(Dpi(18), y),
            };
            Controls.Add(l);
            return l;
        }

        int y = Dpi(18);
        AddLabel("标题", y);
        y += Dpi(22);
        _txtTitle = new TextBox
        {
            Font = new Font(Theme.FontName, 10.5f),
            Location = new Point(Dpi(18), y),
            Size = new Size(Dpi(404), Dpi(30)),
            PlaceholderText = "要记什么事？",
        };
        _txtTitle.Text = _item.Title;
        y += Dpi(42);

        AddLabel("分类", y);
        y += Dpi(22);
        _cmbCat = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(Dpi(18), y),
            Size = new Size(Dpi(190), Dpi(28)),
        };
        _cmbCat.Items.AddRange(Categories.All);
        int catIdx = Array.IndexOf(Categories.All, _item.Category);
        _cmbCat.SelectedIndex = catIdx >= 0 ? catIdx : 0;
        _chkPin = new CheckBox
        {
            Text = "钉为桌面便签（常驻显示）",
            AutoSize = true,
            Location = new Point(Dpi(226), y + Dpi(5)),
            Checked = _item.Pinned,
        };
        y += Dpi(42);

        AddLabel("内容", y);
        y += Dpi(22);
        _txtContent = new TextBox
        {
            Location = new Point(Dpi(18), y),
            Size = new Size(Dpi(404), Dpi(92)),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(Theme.FontName, 9.5f),
        };
        _txtContent.Text = _item.Content;
        y += Dpi(104);

        _chkDue = new CheckBox
        {
            Text = "到期提醒",
            Font = new Font(Theme.FontName, 9.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(Dpi(18), y),
            Checked = _item.DueAt != null,
        };
        y += Dpi(28);

        var due = _item.DueAt ?? DateTime.Now.AddHours(1);
        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(Dpi(18), y),
            Size = new Size(Dpi(130), Dpi(28)),
            Value = due.Date,
        };
        _dtpTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(Dpi(156), y),
            Size = new Size(Dpi(100), Dpi(28)),
            Value = due,
        };
        var lblAdv = new Label { Text = "提前提醒", AutoSize = true, ForeColor = Theme.Sub, Location = new Point(Dpi(272), y + Dpi(7)) };
        _cmbAdvance = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(Dpi(332), y), Size = new Size(Dpi(90), Dpi(28)) };
        _cmbAdvance.Items.AddRange(new object[] { "不提前", "5分钟", "10分钟", "30分钟", "1小时" });
        int advIdx = Array.IndexOf(AdvanceValues, _item.AdvanceRemindMinutes);
        _cmbAdvance.SelectedIndex = advIdx >= 0 ? advIdx : 0;
        y += Dpi(38);

        var lblRep = new Label { Text = "重复", AutoSize = true, ForeColor = Theme.Sub, Location = new Point(Dpi(18), y + Dpi(7)) };
        _cmbRepeat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(Dpi(70), y), Size = new Size(Dpi(120), Dpi(28)) };
        _cmbRepeat.Items.AddRange(new object[] { "不重复", "每天", "每周", "每月" });
        int repIdx = (int)_item.Repeat;
        _cmbRepeat.SelectedIndex = repIdx is >= 0 and <= 3 ? repIdx : 0;
        y += Dpi(40);

        var btnSave = new Button
        {
            Text = "保存",
            Size = new Size(Dpi(92), Dpi(34)),
            Location = new Point(ClientSize.Width - Dpi(92) - Dpi(18), y),
            Font = new Font(Theme.FontName, 9.5f, FontStyle.Bold),
        };
        Theme.Style(btnSave, Theme.Accent, Color.White, Theme.AccentDark);
        var btnCancel = new Button
        {
            Text = "取消",
            Size = new Size(Dpi(76), Dpi(34)),
            Location = new Point(ClientSize.Width - Dpi(92) - Dpi(76) - Dpi(28), y),
        };
        Theme.Style(btnCancel, Color.White, Theme.Text, Color.FromArgb(236, 239, 247));
        btnCancel.FlatAppearance.BorderColor = Theme.Border;
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.Click += (_, _) => Close();
        btnSave.Click += OnSave;
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        if (!_isNew)
        {
            var btnDelete = new Button
            {
                Text = "删除",
                Size = new Size(Dpi(76), Dpi(34)),
                Location = new Point(Dpi(18), y),
            };
            Theme.Style(btnDelete, Color.FromArgb(253, 235, 236), Theme.Danger, Color.FromArgb(250, 219, 222));
            btnDelete.Click += (_, _) =>
            {
                if (MessageBox.Show(this, $"确定删除备忘「{_item.Title}」？", "删除备忘",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                Deleted = true;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btnDelete);
        }

        Controls.AddRange(new Control[]
        {
            _txtTitle, _cmbCat, _chkPin, _txtContent, _chkDue,
            _dtpDate, _dtpTime, lblAdv, _cmbAdvance, lblRep, _cmbRepeat,
            btnSave, btnCancel,
        });

        ApplyDueEnabled();
        _chkDue.CheckedChanged += (_, _) => ApplyDueEnabled();

        void ApplyDueEnabled()
        {
            bool on = _chkDue.Checked;
            foreach (var c in new Control[] { _dtpDate, _dtpTime, _cmbAdvance, _cmbRepeat, lblAdv, lblRep })
                c.Enabled = on;
        }
    }

    int Dpi(int px) => px * DeviceDpi / 96;

    void OnSave(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtTitle.Text))
        {
            MessageBox.Show(this, "请先填写标题。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtTitle.Focus();
            return;
        }

        _item.Title = _txtTitle.Text.Trim();
        _item.Content = _txtContent.Text;
        _item.Category = _cmbCat.SelectedItem as string ?? Categories.All[0];
        _item.Pinned = _chkPin.Checked;
        _item.DueAt = _chkDue.Checked ? _dtpDate.Value.Date + _dtpTime.Value.TimeOfDay : null;
        int advIdx = _cmbAdvance.SelectedIndex;
        _item.AdvanceRemindMinutes = advIdx >= 0 && advIdx < AdvanceValues.Length ? AdvanceValues[advIdx] : 0;
        int repIdx = _cmbRepeat.SelectedIndex;
        _item.Repeat = repIdx is >= 0 and <= 3 ? (RepeatMode)repIdx : RepeatMode.None;

        // 到期时间改成未来的：重置提醒状态，让它正常走“提前/到期”流程
        if (_item.DueAt != _origDue && _item.DueAt is DateTime d && d > DateTime.Now)
        {
            _item.RemindedDue = false;
            _item.RemindedAdvance = false;
            _item.SnoozeUntil = null;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
