namespace Memo.UI;

public class SettingsForm : Form
{
    readonly Store _store;
    CheckBox _chkStart = null!, _chkSound = null!, _chkDnd = null!;
    DateTimePicker _dtpStart = null!, _dtpEnd = null!;

    public SettingsForm(Store store)
    {
        _store = store;
        var s = _store.Settings;

        Text = "设置";
        Font = new Font(Theme.FontName, 9f);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = Color.White;
        ClientSize = new Size(Dpi(420), Dpi(300));

        _chkStart = new CheckBox
        {
            Text = "开机自动启动（随 Windows 登录启动）",
            AutoSize = true,
            Location = new Point(Dpi(20), Dpi(24)),
            Checked = StartupService.IsEnabled(),
        };
        _chkSound = new CheckBox
        {
            Text = "提醒声音（弹窗时响铃）",
            AutoSize = true,
            Location = new Point(Dpi(20), Dpi(60)),
            Checked = s.SoundEnabled,
        };
        var btnTest = new Button
        {
            Text = "试听",
            Size = new Size(Dpi(60), Dpi(26)),
            Location = new Point(ClientSize.Width - Dpi(60) - Dpi(20), Dpi(56)),
        };
        Theme.Style(btnTest, Color.FromArgb(240, 242, 248), Theme.Text, Color.FromArgb(226, 230, 240));
        btnTest.Click += (_, _) => SoundService.PlayAsync();

        _chkDnd = new CheckBox
        {
            Text = "免打扰（时段内到点只弹窗，不响铃）",
            AutoSize = true,
            Location = new Point(Dpi(20), Dpi(104)),
            Checked = s.DndEnabled,
        };

        var lblPeriod = new Label { Text = "时段", AutoSize = true, ForeColor = Theme.Sub, Location = new Point(Dpi(28), Dpi(146)) };
        _dtpStart = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(Dpi(80), Dpi(140)),
            Size = new Size(Dpi(96), Dpi(26)),
        };
        var lblTo = new Label { Text = "到", AutoSize = true, ForeColor = Theme.Sub, Location = new Point(Dpi(184), Dpi(146)) };
        _dtpEnd = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(Dpi(214), Dpi(140)),
            Size = new Size(Dpi(96), Dpi(26)),
        };
        SetTime(_dtpStart, s.DndStart, 23, 0);
        SetTime(_dtpEnd, s.DndEnd, 8, 0);
        _dtpStart.Enabled = s.DndEnabled;
        _dtpEnd.Enabled = s.DndEnabled;
        _chkDnd.CheckedChanged += (_, _) => { _dtpStart.Enabled = _dtpEnd.Enabled = _chkDnd.Checked; };

        var hint = new Label
        {
            Text = "· 数据保存在本机：" + _store.DataDirectory + "\n· 关闭主窗口只是最小化到托盘，提醒不会停止。",
            ForeColor = Theme.Sub,
            Font = new Font(Theme.FontName, 8.5f),
            Location = new Point(Dpi(20), Dpi(186)),
            Size = new Size(ClientSize.Width - Dpi(40), Dpi(44)),
        };

        var btnSave = new Button
        {
            Text = "保存",
            Size = new Size(Dpi(88), Dpi(34)),
            Location = new Point(ClientSize.Width - Dpi(88) - Dpi(20), Dpi(244)),
            Font = new Font(Theme.FontName, 9.5f, FontStyle.Bold),
        };
        Theme.Style(btnSave, Theme.Accent, Color.White, Theme.AccentDark);
        var btnCancel = new Button
        {
            Text = "取消",
            Size = new Size(Dpi(72), Dpi(34)),
            Location = new Point(ClientSize.Width - Dpi(88) - Dpi(72) - Dpi(30), Dpi(244)),
        };
        Theme.Style(btnCancel, Color.White, Theme.Text, Color.FromArgb(236, 239, 247));
        btnCancel.FlatAppearance.BorderColor = Theme.Border;
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.Click += (_, _) => Close();
        btnSave.Click += OnSave;
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[]
        {
            _chkStart, _chkSound, btnTest, _chkDnd, lblPeriod, _dtpStart, lblTo, _dtpEnd, hint, btnSave, btnCancel,
        });
    }

    int Dpi(int px) => px * DeviceDpi / 96;

    static void SetTime(DateTimePicker dtp, string hm, int defH, int defM)
    {
        if (!TimeSpan.TryParseExact(hm, @"hh\:mm", null, out var t))
            t = new TimeSpan(defH, defM, 0);
        dtp.Value = DateTime.Today + t;
    }

    void OnSave(object? sender, EventArgs e)
    {
        try
        {
            StartupService.SetEnabled(_chkStart.Checked);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "设置开机启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var s = _store.Settings;
        s.StartWithWindows = _chkStart.Checked;
        s.SoundEnabled = _chkSound.Checked;
        s.DndEnabled = _chkDnd.Checked;
        s.DndStart = _dtpStart.Value.ToString("HH:mm");
        s.DndEnd = _dtpEnd.Value.ToString("HH:mm");
        _store.Save();
        Close();
    }
}
