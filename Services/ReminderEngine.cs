using Memo.Models;

namespace Memo.Services;

public enum FireKind { Advance, Due }

public record Fire(MemoItem Item, FireKind Kind);

/// <summary>
/// 提醒判定核心（纯逻辑，便于自测）：
/// - 到期（含超时未提醒的补弹）
/// - 提前提醒
/// - 贪睡到期
/// - 重复备忘到期后自动滚动到下一次
/// </summary>
public static class ReminderEvaluator
{
    public static List<Fire> Evaluate(List<MemoItem> memos, DateTime now)
    {
        var fires = new List<Fire>();
        foreach (var m in memos)
        {
            if (m.Done || m.DueAt is not DateTime due) continue;

            // 1) 贪睡时间到，再响一次
            if (m.SnoozeUntil is DateTime sn && now >= sn)
            {
                fires.Add(new Fire(m, FireKind.Due));
                m.SnoozeUntil = null;
                continue;
            }

            // 2) 提前提醒（到期前 N 分钟，且还没提醒过、也还没到过期点）
            if (m.AdvanceRemindMinutes > 0 && !m.RemindedAdvance && !m.RemindedDue)
            {
                var advAt = due.AddMinutes(-m.AdvanceRemindMinutes);
                if (now >= advAt && now < due)
                {
                    fires.Add(new Fire(m, FireKind.Advance));
                    m.RemindedAdvance = true;
                    continue;
                }
            }

            // 3) 到期提醒（含开机补弹：程序没运行时过期的，一启动马上补上）
            if (!m.RemindedDue && now >= due)
            {
                fires.Add(new Fire(m, FireKind.Due));
                m.RemindedDue = true;
                RollRepeat(m, now);
            }
        }
        return fires;
    }

    /// <summary>重复备忘：触发后把到期时间滚动到下一次（按当前时间补齐错过的周期）。</summary>
    public static void RollRepeat(MemoItem m, DateTime now)
    {
        if (m.Repeat == RepeatMode.None || m.DueAt == null) return;
        var due = m.DueAt.Value;
        int guard = 0;
        while (due <= now && guard++ < 100000)
        {
            due = m.Repeat switch
            {
                RepeatMode.Daily => due.AddDays(1),
                RepeatMode.Weekly => due.AddDays(7),
                RepeatMode.Monthly => due.AddMonths(1),
                _ => due,
            };
        }
        m.DueAt = due;
        m.RemindedDue = false;
        m.RemindedAdvance = false;
        m.SnoozeUntil = null;
    }

    /// <summary>
    /// 贪睡：到点后 num 分钟再响一次。
    /// 若备忘已经到期（含超时未提醒），先按已提醒处理并滚动重复周期，避免贪睡期间被到期分支立刻再次触发。
    /// </summary>
    public static void ApplySnooze(MemoItem m, int minutes)
    {
        var now = DateTime.Now;
        if (m.DueAt is DateTime due && now >= due)
        {
            m.RemindedDue = true;
            RollRepeat(m, now);
        }
        m.SnoozeUntil = now.AddMinutes(minutes);
        m.UpdatedAt = now;
    }
}

/// <summary>
/// 提醒引擎：每 15 秒在 UI 线程检查一次；触发时保存状态、弹窗、按需响铃（免打扰时段静音）。
/// </summary>
public class ReminderEngine
{
    readonly Store _store;
    readonly System.Windows.Forms.Timer _timer;

    /// <summary>由主窗口设置：弹出提醒窗口（一批可能含多条）。</summary>
    public Action<List<Fire>>? OnFire;

    public ReminderEngine(Store store)
    {
        _store = store;
        _timer = new System.Windows.Forms.Timer { Interval = 15000 };
        _timer.Tick += (_, _) => RunNow();
    }

    public void Start()
    {
        RunNow();       // 启动立刻查一遍：开机补弹超时备忘
        _timer.Start();
    }

    public void RunNow()
    {
        var now = DateTime.Now;
        var fires = ReminderEvaluator.Evaluate(_store.Memos, now);
        if (fires.Count == 0) return;

        _store.SaveQuiet();
        _store.NotifyChangedForUi();
        OnFire?.Invoke(fires);

        bool silent = TimeText.IsInDnd(_store.Settings, now);
        if (_store.Settings.SoundEnabled && !silent)
            SoundService.PlayAsync();
    }
}
