using Memo.Models;

namespace Memo.Services;

/// <summary>引擎逻辑自测：Memo.exe --selftest，不启动界面，输出 PASS/FAIL。</summary>
public static class SelfTest
{
    public static int Run()
    {
        int fails = 0;
        void Check(bool cond, string name)
        {
            Console.WriteLine((cond ? "PASS " : "FAIL ") + name);
            if (!cond) fails++;
        }

        var now = new DateTime(2026, 10, 7, 12, 0, 0);

        // 1) 超时备忘触发一次，且不重复触发
        var m1 = new MemoItem { Title = "超时", DueAt = now.AddHours(-1) };
        var f1 = ReminderEvaluator.Evaluate(new List<MemoItem> { m1 }, now);
        Check(f1.Count == 1 && f1[0].Kind == FireKind.Due, "超时备忘触发提醒");
        var f1b = ReminderEvaluator.Evaluate(new List<MemoItem> { m1 }, now.AddMinutes(1));
        Check(f1b.Count == 0, "已提醒过不重复触发");

        // 2) 重复备忘：触发后滚动到未来，重置标记
        var m2 = new MemoItem { Title = "每天", DueAt = now.AddHours(-25), Repeat = RepeatMode.Daily };
        var f2 = ReminderEvaluator.Evaluate(new List<MemoItem> { m2 }, now);
        Check(f2.Count == 1 && m2.DueAt > now && !m2.RemindedDue, "每天重复滚动到下一次");
        var f2b = ReminderEvaluator.Evaluate(new List<MemoItem> { m2 }, now.AddMinutes(1));
        Check(f2b.Count == 0, "滚动后当天不再触发");

        // 漏掉多天也能补齐
        var m2c = new MemoItem { Title = "漏3天", DueAt = now.AddDays(-3), Repeat = RepeatMode.Daily };
        ReminderEvaluator.Evaluate(new List<MemoItem> { m2c }, now);
        Check(m2c.DueAt > now, "多天漏掉滚动补齐到未来");

        // 3) 提前提醒：先提前一次，再正常到期一次
        var m3 = new MemoItem { Title = "提前", DueAt = now.AddMinutes(10), AdvanceRemindMinutes = 5 };
        var f3a = ReminderEvaluator.Evaluate(new List<MemoItem> { m3 }, now);
        Check(f3a.Count == 0, "未到提前点不触发");
        var f3b = ReminderEvaluator.Evaluate(new List<MemoItem> { m3 }, now.AddMinutes(6));
        Check(f3b.Count == 1 && f3b[0].Kind == FireKind.Advance, "提前窗口触发提前提醒");
        var f3c = ReminderEvaluator.Evaluate(new List<MemoItem> { m3 }, now.AddMinutes(10).AddSeconds(1));
        Check(f3c.Count == 1 && f3c[0].Kind == FireKind.Due, "提前过后到期仍正常触发");

        // 4) 贪睡：到期后设贪睡，到点再响一次
        var m4 = new MemoItem { Title = "贪睡", DueAt = now.AddHours(-1) };
        ReminderEvaluator.Evaluate(new List<MemoItem> { m4 }, now);
        m4.SnoozeUntil = now.AddMinutes(10);
        var f4a = ReminderEvaluator.Evaluate(new List<MemoItem> { m4 }, now.AddMinutes(5));
        Check(f4a.Count == 0, "贪睡未到不触发");
        var f4b = ReminderEvaluator.Evaluate(new List<MemoItem> { m4 }, now.AddMinutes(11));
        Check(f4b.Count == 1 && m4.SnoozeUntil == null, "贪睡到点触发并清除贪睡");

        // 5) 完成的备忘不触发
        var m5 = new MemoItem { Title = "完成", DueAt = now.AddHours(-2), Done = true };
        var f5 = ReminderEvaluator.Evaluate(new List<MemoItem> { m5 }, now);
        Check(f5.Count == 0, "已完成备忘不触发");

        // 6) 免打扰时段（跨午夜 23:00–08:00）
        var st = new AppSettings { DndEnabled = true, DndStart = "23:00", DndEnd = "08:00" };
        Check(TimeText.IsInDnd(st, new DateTime(2026, 10, 7, 23, 0, 0)), "免打扰 23:00 开始生效");
        Check(TimeText.IsInDnd(st, new DateTime(2026, 10, 8, 3, 30, 0)), "免打扰跨午夜凌晨生效");
        Check(TimeText.IsInDnd(st, new DateTime(2026, 10, 8, 6, 59, 0)), "免打扰结束前生效");
        Check(!TimeText.IsInDnd(st, new DateTime(2026, 10, 8, 8, 0, 0)), "免打扰 08:00 结束");
        Check(!TimeText.IsInDnd(st, new DateTime(2026, 10, 7, 12, 0, 0)), "免打扰白天不生效");
        Check(!TimeText.IsInDnd(new AppSettings { DndEnabled = true }, now), "未配置时段不算免打扰");

        // 7) 每月重复滚动（9-07 09:00 在 10-07 12:00 触发后，滚到 11-07 09:00）
        var m7 = new MemoItem { Title = "每月", DueAt = new DateTime(2026, 9, 7, 9, 0, 0), Repeat = RepeatMode.Monthly };
        var f7 = ReminderEvaluator.Evaluate(new List<MemoItem> { m7 }, now);
        Check(f7.Count == 1 && m7.DueAt == new DateTime(2026, 11, 7, 9, 0, 0), "每月重复滚动加一个月");

        Console.WriteLine(fails == 0 ? "== ALL PASS ==" : $"== {fails} FAILED ==");
        return fails == 0 ? 0 : 1;
    }
}
