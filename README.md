# 备忘录（Memo）

电脑端备忘录软件（Windows 桌面原生程序，C# WinForms / .NET 10）。

## 直接使用

双击 **`publish\Memo.exe`** 即可（单文件，无需安装、无需 .NET 环境，拷到别的电脑也能跑）。

- 关闭主窗口只是**最小化到托盘**，提醒照常工作；托盘图标右键 →「退出」才是真正退出。
- 数据保存在本机 `%APPDATA%\Memo`（memos.json / settings.json）。

## 功能

| 功能 | 说明 |
| --- | --- |
| 到时提醒 | 弹窗（屏幕右下角置顶）+ 铃声；底部「🔔 提醒声音」或设置里可关声音 |
| 开机启动 | 设置 →「开机自动启动」（写入当前用户注册表 Run 键，任务管理器可关） |
| 超时补弹 | 程序没运行时过期的备忘，一启动**马上**弹窗（多条合并成一个窗口） |
| 稍后提醒 | 弹窗或右键菜单里选 10分钟 / 30分钟 / 1小时 / 明天 |
| 提前提醒 | 每条备忘可单独设「到期前 5/10/30 分钟 / 1小时」先提醒一次 |
| 重复备忘 | 每天 / 每周 / 每月，到期触发后自动滚到下一轮 |
| 免打扰 | 设置时段（如 23:00–08:00，支持跨午夜）：到点只弹窗不响铃 |
| 分类 + 搜索 | 固定分类「工作 / 生活」（彩色标签，不可自由输入），顶部搜索框按标题/内容过滤 |
| 完成打勾 | 列表点击方框或右键标记完成，按「全部/未完成/已完成」筛选 |
| 桌面便签 | 右键「钉为桌面便签」，重要备忘常驻屏幕（可拖动、记住位置） |
| 托盘常驻 | 最小化到托盘、双击唤起；单实例运行，重复启动会唤起已有窗口 |

## 从源码构建

```bash
dotnet build -c Release                              # 编译（bin/Release/net10.0-windows/Memo.exe）
./bin/Release/net10.0-windows/Memo.exe --selftest    # 提醒引擎逻辑自测（18 项断言）
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -o publish   # 发布单文件 exe
```

## 代码结构

```
Program.cs                 入口：单实例、托盘唤起、--selftest
Models/MemoItem.cs         备忘数据（到期/提前/重复/贪睡/置顶等）
Models/AppSettings.cs      设置（声音/开机启动/免打扰）
Data/Store.cs              %APPDATA%\Memo 下 JSON 存储（原子写入、损坏自动备份）
Services/ReminderEngine.cs 提醒判定核心 + 15 秒轮询引擎（纯逻辑可自测）
Services/SoundService.cs   运行时合成“叮-咚-叮”铃声（无外部音频文件）
Services/StartupService.cs 开机启动注册表读写
UI/MainForm.cs             主界面（自绘列表/搜索/筛选/托盘）
UI/MemoEditForm.cs         新建/编辑（时间、提前、重复、钉便签）
UI/ReminderPopupForm.cs    提醒弹窗（稍后/完成，多条件堆叠）
UI/StickyForm.cs           桌面便签（无边框置顶、可拖动、记住位置）
UI/SettingsForm.cs         设置窗口
UI/Theme.cs                配色、圆角、DWM、运行时生成图标
```
