namespace Memo.Models;

public class AppSettings
{
    /// <summary>提醒声音开关（全局）。</summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>开机自动启动。</summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>免打扰：到点只弹窗不响铃。</summary>
    public bool DndEnabled { get; set; } = false;

    public string DndStart { get; set; } = "23:00";
    public string DndEnd { get; set; } = "08:00";

    /// <summary>首次最小化到托盘时给一次气泡提示。</summary>
    public bool FirstRun { get; set; } = true;

    /// <summary>主窗口位置 "x,y"。</summary>
    public string? MainPos { get; set; }
}
