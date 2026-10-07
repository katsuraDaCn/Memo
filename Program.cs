using Memo.Data;
using Memo.Services;
using Memo.UI;

namespace Memo;

internal static class Program
{
    /// <summary>第二个实例通知第一个实例“把主窗口亮出来”。</summary>
    public static volatile bool ShowRequested;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--selftest")) return SelfTest.Run();
        if (args.Contains("--uitest"))
        {
            ApplicationConfiguration.Initialize();
            UiTest.Run();
            return 0;
        }

        ApplicationConfiguration.Initialize();

        // 单实例：重复启动时把已在运行的那个窗口唤起
        bool createdNew;
        using var mutex = new Mutex(true, @"Local\Memo_SingleInstance", out createdNew);
        if (!createdNew)
        {
            try { EventWaitHandle.OpenExisting(@"Local\Memo_ShowWindow").Set(); }
            catch (Exception) { }
            return 0;
        }

        try
        {
            var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Memo_ShowWindow");
            var watcher = new Thread(() =>
            {
                while (showEvent.WaitOne()) ShowRequested = true;
            })
            { IsBackground = true };
            watcher.Start();
        }
        catch (Exception) { }

        var store = new Store();
        var engine = new ReminderEngine(store);
        Application.Run(new MainForm(store, engine));
        return 0;
    }
}
