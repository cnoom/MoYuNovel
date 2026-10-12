# Changelog

本包遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 SemVer。

## [0.3.1] - 2026-10-12

### Fixed
- 修复通过 git 安装本包时编译失败（CS0234/CS0246：`CNoom.MoYuNovel.Editor.Library` 命名空间与 `NovelLibrary` / `ShelfItem` / `ProgressStore` 类型缺失）：根 .gitignore 中未锚定的 `[Ll]ibrary/` 规则把包内 `Editor/Library/` 源码排除在版本控制外，git 安装拿不到这两个文件。现已将 Unity 生成物目录全部锚定到仓库根目录，并把 `Editor/Library/`（NovelLibrary.cs、ProgressStore.cs）纳入版本控制。

## [0.3.0] - 2026-10-11

### Added
- 急停清场：急停（快捷键 / Esc / 菜单任一入口）在停止输出、进度落盘之后**选择性移除** Console 中本插件产生的条目（统一标记连载行/完结提示、`[LogReader]` 系统日志），业务日志内容与相对顺序原样保留（specs/002 FR-002；反射 `LogEntries` 快照→清表→回灌，公开 API 兜底，失败或过滤态静默降级且绝不误删业务日志）（FR-004）。
- 「急停时清空 Console」设置开关：设置窗口急停区块内，默认开启并持久化；关闭后急停仅停止输出、不清空（FR-006/007）。
- 设置窗口急停区块主急停键改键（ShortcutManager 读写、冲突拒绝、恢复默认），兑现 001 FR-002 的设置窗口自定义承诺。

## [0.2.0] - 2026-10-10

### Added
- 摸鱼连载输出：Console 按可配置间隔（默认 1s、下限 0.2s）逐行输出小说正文，观感同业务日志（US1/FR-001/003）。
- 菜单入口 `Tools/Log Reader/`：Start / Pause / Resume / Stop / Panic Stop / Settings（FR-001，中性命名）。
- 一键急停：ShortcutManager 主急停键（默认 Ctrl+Shift+F12，可自定义）+ Esc 尽力拦截（不破坏原生 Esc 行为）；触发后立即落停（FR-002/SC-002）。
- 默认静默：编辑器重启、域重载、播放模式进出后不自动恢复输出（FR-005/006，宪法 II）。
- 断点续读：进度持久化于 `UserSettings/MoYuNovel/progress.json`，重启续读偏差 0 行；支持从头再读与清除全部进度（FR-007/SC-003）。
- 内容来源：内置《西游记》第一回节选（公有领域，开箱即读）；支持导入本地 .txt（UTF-8/UTF-16/GBK 自动探测，失败明确提示）（FR-008/009）。
- 伪装预设：内置「网络模块 / 资源加载 / 数据访问」3 套，输出行统一携带可配置过滤标记（默认 `nv`），Console 一键过滤隔离（FR-004/013/SC-005）。
- 设置窗口 `Log Reader`：书架管理、进度展示与清除、间隔钳制生效值显示、预设与标记配置（FR-016）。
- Runtime 纯 C# 核心（`noEngineReferences` / `autoReferenced:false`）：解析（章节正则/单章回退）、超长段落切行（句读优先、上限 120 字）、编码探测、进度模型、间隔钳制、伪装格式化，全部附 EditMode 测试。

## [0.1.0] - 2026-10-10

### Added
- 项目初始化：包骨架（Runtime / Editor / Tests/EditMode）与 Unity 6000.3 开发工程接入 spec-kit 流程。
