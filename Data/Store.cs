using System.Text.Json;
using System.Text.Json.Serialization;
using Memo.Models;

namespace Memo.Data;

/// <summary>
/// 本地数据存储：备忘列表 + 设置，保存在 %APPDATA%\Memo 下，写入采用“临时文件+替换”防止写坏。
/// </summary>
public class Store
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>备忘数据发生变化（增删改），UI 订阅后刷新。</summary>
    public event Action? Changed;

    public List<MemoItem> Memos { get; private set; } = new();
    public AppSettings Settings { get; private set; } = new();

    readonly string _dir;
    readonly string _memoPath;
    readonly string _settingsPath;

    public Store()
    {
        _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Memo");
        Directory.CreateDirectory(_dir);
        _memoPath = Path.Combine(_dir, "memos.json");
        _settingsPath = Path.Combine(_dir, "settings.json");
        Load();
    }

    public string DataDirectory => _dir;

    void Load()
    {
        Memos = LoadList(_memoPath) ?? new List<MemoItem>();
        Settings = LoadOne<AppSettings>(_settingsPath) ?? new AppSettings();
    }

    List<MemoItem>? LoadList(string path)
    {
        var text = ReadText(path);
        if (text == null) return null;
        try { return JsonSerializer.Deserialize<List<MemoItem>>(text, JsonOpts); }
        catch (JsonException) { BackupCorrupt(path); return null; }
    }

    T? LoadOne<T>(string path) where T : class
    {
        var text = ReadText(path);
        if (text == null) return null;
        try { return JsonSerializer.Deserialize<T>(text, JsonOpts); }
        catch (JsonException) { BackupCorrupt(path); return null; }
    }

    static string? ReadText(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : null; }
        catch (IOException) { return null; }
    }

    static void BackupCorrupt(string path)
    {
        try { File.Move(path, path + ".bak", overwrite: true); } catch (IOException) { }
    }

    void WriteFile<T>(string path, T obj)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(obj, JsonOpts));
        File.Move(tmp, path, overwrite: true);
    }

    public void SaveQuiet()
    {
        WriteFile(_memoPath, Memos);
        WriteFile(_settingsPath, Settings);
    }

    /// <summary>保存并广播变更（会触发界面刷新）。</summary>
    public void Save()
    {
        SaveQuiet();
        Changed?.Invoke();
    }

    /// <summary>只广播变更不写盘（提醒引擎改完状态已自己落盘）。</summary>
    public void NotifyChangedForUi() => Changed?.Invoke();

    public void Upsert(MemoItem item)
    {
        item.UpdatedAt = DateTime.Now;
        var i = Memos.FindIndex(m => m.Id == item.Id);
        if (i >= 0) Memos[i] = item;
        else Memos.Insert(0, item);
        Save();
    }

    /// <summary>不触发 Changed 的更新（如保存便签窗口位置），避免循环刷新。</summary>
    public void UpsertQuiet(MemoItem item)
    {
        var i = Memos.FindIndex(m => m.Id == item.Id);
        if (i >= 0) { Memos[i] = item; SaveQuiet(); }
    }

    public void Delete(MemoItem item)
    {
        Memos.RemoveAll(m => m.Id == item.Id);
        Save();
    }

    public IEnumerable<string> Categories() =>
        Memos.Select(m => m.Category?.Trim() ?? "").Where(c => c.Length > 0).Distinct().OrderBy(c => c, StringComparer.CurrentCulture);
}
