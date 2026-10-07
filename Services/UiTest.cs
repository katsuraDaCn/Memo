using Memo.Models;
using Memo.UI;

namespace Memo.Services;

/// <summary>
/// 界面诊断模式（Memo.exe --uitest）：把编辑对话框和主窗口渲染出来、展开分类下拉并截屏到工作目录，用于界面自检。
/// </summary>
public static class UiTest
{
    public static void Run()
    {
        string dir = AppContext.BaseDirectory;
        var store = new Store();

        // 1) 新建备忘对话框：分类下拉展开
        var dlg = new MemoEditForm(store, new MemoItem(), isNew: true)
        {
            TopMost = true,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(200, 120),
        };
        dlg.Show();
        Pump(0.6);
        var cmb = dlg.Controls.OfType<ComboBox>().FirstOrDefault();
        if (cmb != null) cmb.DroppedDown = true;
        Pump(1.2);
        Shot(Path.Combine(dir, "uitest_edit.png"));
        dlg.Close();
        dlg.Dispose();

        // 2) 主窗口：分类筛选下拉展开
        var main = new MainForm(store, new ReminderEngine(store))
        {
            TopMost = true,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(200, 100),
        };
        main.Show();
        Pump(0.8);
        var mcb = main.Controls.Cast<Control>()
            .SelectMany(p => p.Controls.OfType<ComboBox>())
            .FirstOrDefault();
        if (mcb != null) mcb.DroppedDown = true;
        Pump(1.2);
        Shot(Path.Combine(dir, "uitest_main.png"));
        main.Close();
        main.Dispose();
    }

    static void Pump(double seconds)
    {
        int steps = (int)(seconds * 20);
        for (int i = 0; i < steps; i++)
        {
            Application.DoEvents();
            Thread.Sleep(50);
        }
    }

    static void Shot(string path)
    {
        var b = Screen.PrimaryScreen!.Bounds;
        using var bmp = new Bitmap(b.Width, b.Height);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(b.Location, Point.Empty, b.Size);
        bmp.Save(path);
    }
}
