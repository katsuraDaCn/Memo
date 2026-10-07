namespace Memo.Models;

public enum RepeatMode
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
}

public class MemoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>到期时间；null 表示纯备忘，不提醒。</summary>
    public DateTime? DueAt { get; set; }

    /// <summary>提前提醒分钟数：0 = 不提前，只在到期时提醒。</summary>
    public int AdvanceRemindMinutes { get; set; } = 0;

    public RepeatMode Repeat { get; set; } = RepeatMode.None;

    public bool Done { get; set; } = false;

    // —— 以下为提醒状态（随数据持久化，保证重启后不重复、漏掉的能补弹）——
    public bool RemindedAdvance { get; set; } = false;
    public bool RemindedDue { get; set; } = false;

    /// <summary>贪睡：到这个时间再响一次。</summary>
    public DateTime? SnoozeUntil { get; set; }

    /// <summary>是否钉为桌面便签。</summary>
    public bool Pinned { get; set; } = false;

    /// <summary>便签窗口位置 "x,y"。</summary>
    public string? StickyPos { get; set; }
}
