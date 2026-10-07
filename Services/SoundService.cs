using System.Media;

namespace Memo.Services;

/// <summary>
/// 提醒音：运行时合成一段“叮-咚-叮”和弦风铃声（无需外部音频文件），循环播放两遍。
/// </summary>
public static class SoundService
{
    static readonly object _lock = new();
    static byte[]? _wav;

    public static void PlayAsync()
    {
        Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    _wav ??= BuildChime();
                    using var ms = new MemoryStream(_wav);
                    using var player = new SoundPlayer(ms);
                    player.PlaySync();
                }
                catch (Exception)
                {
                    // 声音设备不可用时静默忽略，不影响弹窗
                }
            }
        });
    }

    static byte[] BuildChime()
    {
        const int rate = 44100;
        (double freq, double secs)[] notes = { (784.0, 0.20), (988.0, 0.20), (1175.0, 0.36) }; // G5 B5 D6
        const double gap = 0.28; // 两遍之间的静音
        double phraseLen = notes.Sum(n => n.secs);
        int total = (int)((phraseLen * 2 + gap) * rate);
        var samples = new double[total];

        for (int pass = 0; pass < 2; pass++)
        {
            double t = pass == 0 ? 0 : (phraseLen + gap);
            foreach (var (freq, secs) in notes)
            {
                int start = (int)(t * rate);
                int len = (int)(secs * rate);
                for (int i = 0; i < len && start + i < total; i++)
                {
                    double p = (double)i / len;
                    double env = Math.Min(1.0, i / (0.012 * rate)) * (1.0 - Math.Pow(p, 2.2));
                    double s = 0.85 * Math.Sin(2 * Math.PI * freq * i / rate)
                             + 0.15 * Math.Sin(2 * Math.PI * freq * 2 * i / rate);
                    samples[start + i] += 0.32 * s * env;
                }
                t += secs;
            }
        }

        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            int dataLen = total * 2;
            w.Write("RIFF"u8);
            w.Write(36 + dataLen);
            w.Write("WAVE"u8);
            w.Write("fmt "u8);
            w.Write(16);
            w.Write((short)1);      // PCM
            w.Write((short)1);      // mono
            w.Write(rate);
            w.Write(rate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write("data"u8);
            w.Write(dataLen);
            foreach (var s in samples)
            {
                w.Write((short)Math.Round(Math.Clamp(s, -1.0, 1.0) * short.MaxValue));
            }
        }
        return ms.ToArray();
    }
}
