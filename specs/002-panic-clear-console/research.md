# Research: 急停清空 Console 日志

**Date**: 2026-10-11 | **Status**: 全部技术疑点已解决（无 NEEDS CLARIFICATION）

## R1: 程序化清空 Console 的 API 选型（FR-001/FR-004）

- **Decision**: 主路径反射 internal 的 `UnityEditor.LogEntries.Clear()`（6000.3
  CoreModule 元数据确认存在，含 `Clear`/`GetCount`/`StartGettingEntries`；与
  Console 窗口 Clear 按钮同源，同步清空日志表）；失败时以公开 API
  `UnityEngine.Debug.ClearDeveloperConsole()` 兜底；两级都失败则静默返回
  false——不弹窗、不写日志（FR-004，错误输出本身会暴露）。
  类型解析不硬编码程序集名（LogEntries 在不同版本曾分属 UnityEditor.dll 与
  UnityEditor.CoreModule.dll），遍历 AppDomain 已加载程序集按类型名查找，
  解析失败只探一次。
- **Rationale**: **T011 期间的实测修正**（初版为公开 API 优先）：batchmode
  EditMode 测试证实 `Debug.ClearDeveloperConsole()` 在无 Console 窗口的环境
  会**静默 no-op**（不抛异常，调用后 `LogEntries.GetCount()` 仍非零）——以
  异常触发兜底的设计在该环境下永远走不到兜底。LogEntries.Clear 同步操作
  日志表本体，与 GetCount 同源、可测、在无窗口环境同样生效，故升为主路径；
  公开 API 保留为反射失败时（未来版本类型迁移）的兜底。
- **Alternatives considered**:
  - 公开 API 优先——已在 batchmode 证实会静默 no-op 且无法感知，否决为主路径
    （保留为兜底）。
  - 反射 Console 窗口实例的 clear 按钮——依赖窗口存在与内部结构，更脆，否决。
  - 失败时弹窗提示——违背 FR-004（静默降级），否决。

## R2: 清空调用的挂接点与顺序（FR-001/FR-003/FR-005/FR-006）

- **Decision**: 挂接在 `MoYuEngine.PanicStop()` 内部（急停唯一漏斗：快捷键、
  Esc 拦截、菜单三个入口全部汇聚于此）；执行顺序 = 停止输出 → 进度落盘 →
  **最后**清空 Console；清空由开关 `settings.ClearConsoleOnPanic` 控制
  （默认 true）。非急停路径（`Stop`/`Pause`/`Resume`/`EnterFinished`/
  播放模式静默停机）不经过 PanicStop，天然满足 FR-003。
- **Rationale**: PanicStop 从 Idle 进入也照常执行（现有实现无 early-return），
  FR-006 的空闲清场免费获得；"先停后清"且清空放在进度保存之后——即使清空
  抛出意外异常（已被 TryClear 吞掉），进度与停止语义也已完成，SC-005 无损。
- **Alternatives considered**:
  - 挂在 PanicStopService（入口层）——菜单入口在 MoYuMenu，会漏掉一个入口
    或造成双处调用，否决。
  - 挂在 StateChanged 事件按"→Idle 转换"判定——Stop/完结也走 Idle 转换，
    需要额外区分来源，复杂且易漏，否决。
  - 异步/延迟清空（delayCall）——多一拍窗口期屏幕仍残留，违背"同拍"，否决。

## R3: 开关的存储与语义（FR-007）

- **Decision**: `OutputSettingsData` 新增 `public bool clearConsoleOnPanic
  = true;`（JsonUtility 字段名即存储键）；旧 settings.json 缺该字段时
  `FromJson` 保留字段初始化值 true——天然满足"默认开启"；「清除全部进度与
  配置」重置回 true（与其他配置项语义一致）。
- **Rationale**: 沿用 001 的 settings.json 单文件方案，零新存储面；bool
  字段向后兼容旧行为（升级用户默认获得清场能力，符合摸鱼安全底线）。
- **Alternatives considered**:
  - EditorPrefs——跨工程共享且不随「清除配置」重置，与 001 R3 决策冲突，否决。
  - 独立新 JSON 文件——为一个 bool 增加存储面，否决。

## R5: 选择性移除本插件 Console 条目的机制（FR-002 修订，用户决策）

- **Decision**: 快照 → 清表 → 回灌三步。① `StartGettingEntries()` 括号内经
  `GetEntryInternal(i, LogEntry)` 逐条读出条目（Cecil 反查 6000.3
  CoreModule 确认 API 面：`LogEntry` 含 message/mode/file/line/column/
  instanceID/identifier/callstackTextStartUTF8/16 等字段），按消息内容分流——
  含 `[<marker>]` token 或以 `[LogReader]` 开头者移除，其余保留并复制内容
  字段（callstack 偏移清零）；② `Clear()` 清表；③ 对保留条目按原顺序
  `AddMessageWithDoubleClickCallback(LogEntry)` 回灌。全程无插件条目时跳过
  清表（避免无谓丢失堆栈/时间戳）。
- **Rationale**: 日志表无按文本/谓词移除 API（`LogEntry.RemoveLogEntriesByMode`
  仅按 mode 掩码整类移除，无法区分同为 Log 型的业务日志），逐条重建是唯一
  可行路径；复制 mode 可保 Warning/Error 视觉类型不变。callstack 偏移指向
  原生缓冲，Clear 后失效，回灌前必须清零防悬垂访问。
- **降级语义（FR-004 修订）**: 任一 API 解析失败、或 Console 设有过滤文本
  （`GetFilteringText()` 非空——过滤态下 GetCount/GetEntryInternal 针对过滤
  视图，重建会丢被过滤隐藏的条目）→ 静默跳过清理，**绝不回退为全量 Clear**
  （"不动业务日志"优先级高于消痕）；急停停止能力不受影响。
- **Alternatives considered**:
  - `RemoveLogEntriesByMode`——无法区分同 mode 的业务日志，否决。
  - 设置过滤文本伪装"移除"——仅显示层，条目仍在表中，否决。
  - 重建失败回退全量清空——违背修订后 FR-002/FR-004，否决。

## R4: 测试策略（质量标准：核心逻辑 EditMode 覆盖）

- **Decision**: 三层验证。
  1. **ConsoleCleanerTests（真实清空）**：`Debug.Log` 若干条 →
     `ConsoleCleaner.TryClear()` 返回 true → 反射 `LogEntries.GetCount()`
     断言归零（GetCount 不可用时退化为只断言返回 true）；吞异常测试用注入
     替身抛异常断言 `TryClear` 不抛且返回 false。
  2. **MoYuEngineTests 扩展（接线与开关）**：`ConsoleCleaner.ClearImpl`
     换成计数替身——急停触发计数 +1（开关开）、开关关闭不计数、`Stop`/
     `Pause`/完结/播放模式静默停机永不计数。
  3. **OutputSettings round-trip**：开关默认 true、Set 后 Save/Load 保持、
     旧 JSON（无该字段）载入仍为 true。
- **Rationale**: `ConsoleCleaner` 暴露 `internal static Func<bool> ClearImpl`
  注入点（默认指向真实实现），使引擎接线逻辑不依赖真实 Console 即可测试；
  真实清空在 batchmode 下同样生效（LogEntries 是编辑器进程内的日志表），
  T029 的 batchmode CLI 即可全量回归。
- **Alternatives considered**:
  - 只做真实清空测试、不做接线替身——开关/路径区分逻辑无覆盖，否决。
  - 手动验证代替自动化——违背宪法质量标准（核心逻辑 MUST 有 EditMode 覆盖），否决。
