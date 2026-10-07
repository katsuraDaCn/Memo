using Memo.Models;

namespace Memo.Services;

/// <summary>时间文案与免打扰判断。</summary>
public static class TimeText
{
    /// <summary>列表第二行显示：绝对时间 + 相对时间。</summary>
    public static string DueText(MemoItem m, DateTime now)
    {
        if (m.DueAt is not DateTime due) return "无提醒时间";
        return $"{due:MM-dd HH:mm} · {RelText(due, now)}";
    }

    public static string RelText(DateTime due, DateTime now)
    {
        var d = due - now;
        if (d.TotalSeconds >= 0)
        {
            if (d.TotalMinutes < 1) return "即将到期";
            if (d.TotalMinutes < 60) return $"还剩{(int)Math.Ceiling(d.TotalMinutes)}分钟";
            if (d.TotalHours < 24) return $"还剩{(int)Math.Ceiling(d.TotalHours)}小时";
            return $"还剩{(int)Math.Ceiling(d.TotalDays)}天";
        }
        d = now - due;
        if (d.TotalMinutes < 1) return "刚刚到期";
        if (d.TotalMinutes < 60) return $"超时{(int)d.TotalMinutes}分钟";
        if (d.TotalHours < 24) return $"超时{(int)d.TotalHours}小时";
        return $"超时{(int)d.TotalDays}天";
    }

    public static string RepeatText(MemoItem m) => m.Repeat switch
    {
        RepeatMode.Daily => "每天重复",
        RepeatMode.Weekly => "每周重复",
        RepeatMode.Monthly => "每月重复",
        _ => "",
    };

    static bool TryParseHm(string s, out TimeSpan t) => TimeSpan.TryParseExact(s, @"hh\:mm", null, out t);

    /// <summary>是否处于免打扰时段（支持跨午夜，如 23:00–08:00）。到点仍弹窗，只是不响铃。</summary>
    public static bool IsInDnd(AppSettings s, DateTime now)
    {
        if (!s.DndEnabled) return false;
        if (!TryParseHm(s.DndStart, out var start) || !TryParseHm(s.DndEnd, out var end)) return false;
        if (start == end) return false;
        var t = now.TimeOfDay;
        return start < end ? (t >= start && t < end) : (t >= start || t < end);
    }
}
