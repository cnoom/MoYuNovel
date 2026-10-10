# Research: Console 日志连载小说

**Date**: 2026-10-10 | **Status**: 全部 NEEDS CLARIFICATION 已解决

## R1: Esc 键全局拦截可行性（FR-002 急停默认键）

- **Decision**: 双层急停设计。
  (1) **主急停键**走 Unity ShortcutManager 官方 API 注册，默认绑定
  `Ctrl+Shift+F12`（可在 Unity 的 Shortcuts 窗口或我们的设置窗口改键）；
  (2) **Esc 尽力拦截**：输出运行期间通过反射挂接
  `EditorApplication.globalEventHandler`（internal），捕获 KeyDown/Escape
  即触发急停；反射失败或事件被编辑器先行消费时静默降级——Esc 不生效，
  主急停键始终可用。
- **Rationale**: ShortcutManager 的按键录制 UI 中按 Esc 会取消录制，**Esc
  无法作为 ShortcutManager 绑定**；而 spec 要求默认 Esc 且允许「技术上不可
  行时回退组合键并说明」（边界用例）。globalEventHandler 在 2022.3 LTS 与
  6000.x 均存在（internal 委托字段），是编辑器内唯一的全局 IMGUI 事件入口，
  输出运行中才挂接、停止即卸载，影响面最小。
- **Alternatives considered**:
  - 仅 ShortcutManager 绑 Esc——不可行（UI 无法录入该键）。
  - 隐藏 EditorWindow 捕获键盘——只有焦点在窗口内才收到事件，不满足
    「随时一键急停」。
  - 只用组合键放弃 Esc——违背 spec 默认键决策，且 Esc 确实是最快急停键。

## R2: GBK 解码的纯 C# 方案（FR-009 编码探测）

- **Decision**: 探测用自研逻辑（BOM 检查 + UTF-8 逐字节合法性校验 + GBK
  双字节结构启发式）；解码直接尝试 `System.Text.Encoding.GetEncoding(936)`
  （Unity Mono 运行时内置 I18N 程序集，编辑器环境下可用），抛异常则按
  spec「失败明确提示」引导用户转存 UTF-8。
- **Rationale**: spec 要求「尽力自动探测，失败时明确提示」——允许失败路径。
  Mono 自带 I18N 使 GBK 解码零成本；自带 GBK 码表（2 万+映射）体积不可接受，
  且引入 System.Text.Encoding.CodePages 等第三方 DLL 违反宪法原则 IV。
- **Alternatives considered**:
  - 自带完整 GBK 码表——体积大、维护蠢，否决。
  - 引 NuGet CodePages 包——第三方依赖，违反原则 IV，否决。
  - 只支持 UTF-8——低于「尽力」承诺，否决。

## R3: 进度与导入内容的存储位置

- **Decision**: 全部持久化数据放工程 `UserSettings/MoYuNovel/`
  （settings.json / progress.json / library/*.txt），JSON 人读友好格式。
- **Rationale**: (1) 根 .gitignore 已忽略 `UserSettings/`——摸鱼数据不会
  出现在仓库，隐蔽性与安全性最佳；(2) 工程级持久（区别于 Library/ 的可再
  生数据）， survives AssetDatabase 重建；(3) 导入小说拷贝入库后，用户删除
  原文件不影响阅读。写盘按「间隔保存 + 暂停/停止/完结/编辑器退出时保存」
  节流，避免每行刷盘。
- **Alternatives considered**:
  - EditorPrefs——机器级跨工程共享（进度会在不同工程间串），无法存文件
    内容，清除入口实现别扭，否决。
  - 包内目录（Packages/com.cnoom.moyunovel/...）——污染嵌入包且可能被
    提交，否决。
  - `%APPDATA%`——跨工程全局、不可见难排查，且换工程进度错乱，否决。

## R4: 内置小说资源的加载方式

- **Decision**: 文本放 `Editor/Resources/journey-west.txt`，作为 TextAsset
  导入；运行时 `AssetDatabase.FindAssets("journey-west t:TextAsset")` 定位
  加载（以实际文件名为准）。
- **Rationale**: 位于 Editor/ 目录的资源是 Editor-only 资产，**绝不进入
  玩家构建**（SC-006/原则 I 的资源侧保障）；TextAsset 由 Unity 导入管线
  管理，无需手写路径解析。
- **Alternatives considered**:
  - 包相对路径 File.ReadAllText——编辑器 DLL 位于 Library/ScriptAssemblies，
    解析包路径要绕 PackageManager API，脆弱，否决。
  - 字符串内嵌 C#——几万字正文进源码，可读性与 diff 噪音差，否决。

## R5: 输出调度机制

- **Decision**: `EditorApplication.update` 逐帧回调驱动；引擎内部用
  `Stopwatch` 计时决定本拍是否输出行。不使用协程、不使用线程。
- **Rationale**: 编辑器生命周期内 update 回调最轻（每帧一次委托调用，
  无输出时零分配）；计时判定让间隔精确到帧粒度内的用户配置值；线程/
  Task 会引入 Debug.Log 的主线程封送问题，纯属自找。
- **Alternatives considered**:
  - `EditorApplication.delayCall` 自循环——延迟语义模糊，否决。
  - System.Threading.Timer——日志 API 必须主线程调用，增加封送复杂度，
    否决。

## R6: 统一过滤标记的形态（FR-004/SC-005）

- **Decision**: 标记为可配置短字符串（默认 `nv`），由 DisguiseFormatter
  拼装在伪装前缀的固定尾部（如 `[Net][nv] 正文`）；不使用 LogType/context
  等不可见于过滤框的机制。
- **Rationale**: Console 过滤只认消息文本子串——标记必须落在消息文本内
  才能一键过滤/单独查看；两字符默认值在截图/瞥见场景下不可读不显眼，
  又可被用户改成任意串。
- **Alternatives considered**:
  - `[NOVEL]` 全大写长标记——过滤方便但一眼假，违背伪装目标，否决。
  - 依赖 stack trace/context 区分——Console 过滤不覆盖，否决。
