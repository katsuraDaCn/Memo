namespace Memo.UI;

/// <summary>全局扁平配色。</summary>
public static class Theme
{
    public static readonly Color Bg      = Color.FromArgb(245, 246, 250);
    public static readonly Color Card    = Color.White;
    public static readonly Color CardOff = Color.FromArgb(243, 244, 248);
    public static readonly Color Border  = Color.FromArgb(225, 229, 239);
    public static readonly Color Accent  = Color.FromArgb(79, 124, 244);
    public static readonly Color AccentDark = Color.FromArgb(58, 102, 224);
    public static readonly Color Text    = Color.FromArgb(31, 36, 48);
    public static readonly Color Sub     = Color.FromArgb(138, 147, 166);
    public static readonly Color Danger  = Color.FromArgb(224, 70, 76);
    public static readonly Color Warn    = Color.FromArgb(230, 145, 20);
    public static readonly Color Done    = Color.FromArgb(168, 175, 190);
    public static readonly Color ChipBgGray = Color.FromArgb(238, 240, 246);
    public static readonly Color ChipFgGray = Color.FromArgb(110, 118, 135);

    public static readonly string FontName = "Microsoft YaHei UI";

    public static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (r.Width < d || r.Height < d) { path.AddRectangle(r); return path; }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>按钮统一扁平风格。</summary>
    public static void Style(Button b, Color back, Color fore, Color hover)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = hover;
        b.BackColor = back;
        b.ForeColor = fore;
        b.Cursor = Cursors.Hand;
    }
}

/// <summary>固定分类：工作 / 生活（不做自由输入）。</summary>
public static class Categories
{
    public static readonly string[] All = { "工作", "生活" };
}

/// <summary>分类颜色：按分类名稳定散列取一组柔和配色。</summary>
public static class CategoryColor
{
    static readonly (Color bg, Color fg)[] Palette =
    {
        (Color.FromArgb(232, 240, 254), Color.FromArgb(51, 99, 224)),
        (Color.FromArgb(226, 245, 235), Color.FromArgb(22, 124, 88)),
        (Color.FromArgb(253, 233, 235), Color.FromArgb(199, 56, 79)),
        (Color.FromArgb(253, 242, 224), Color.FromArgb(176, 108, 14)),
        (Color.FromArgb(238, 231, 252), Color.FromArgb(107, 63, 193)),
        (Color.FromArgb(224, 243, 246), Color.FromArgb(13, 110, 133)),
    };

    public static (Color bg, Color fg) Get(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return (Theme.ChipBgGray, Theme.ChipFgGray);
        unchecked
        {
            int h = 17;
            foreach (var c in category.Trim()) h = h * 31 + c;
            return Palette[((h % Palette.Length) + Palette.Length) % Palette.Length];
        }
    }
}

/// <summary>运行时生成的应用图标（蓝底圆角 + M），用于窗口/托盘。</summary>
public static class AppIcon
{
    static Icon? _icon;

    public static Icon Get()
    {
        if (_icon != null) return _icon;
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = Theme.Rounded(new Rectangle(0, 0, 32, 32), 8);
            using var bg = new SolidBrush(Theme.Accent);
            g.FillPath(bg, path);
            using var f = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            var sz = g.MeasureString("M", f);
            using var fg = new SolidBrush(Color.White);
            g.DrawString("M", f, fg, (32 - sz.Width) / 2f, (32 - sz.Height) / 2f + 1);
        }
        _icon = Icon.FromHandle(bmp.GetHicon());
        return _icon;
    }
}

/// <summary>无边框窗口的 Win11 圆角（DWM），Win10 退化为 Region 圆角。</summary>
public static class DwmRounded
{
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void Apply(Form form, int radius = 12)
    {
        int pref = 2; // DWMWCP_ROUND
        try
        {
            if (DwmSetWindowAttribute(form.Handle, 33, ref pref, 4) != 0) Fallback(form, radius);
        }
        catch (Exception) { Fallback(form, radius); }
    }

    static void Fallback(Form form, int radius)
    {
        var r = new Rectangle(0, 0, form.Width, form.Height);
        using var path = Theme.Rounded(r, radius);
        form.Region = new Region(path);
    }
}
