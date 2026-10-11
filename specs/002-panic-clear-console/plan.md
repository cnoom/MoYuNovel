# Implementation Plan: 急停清空 Console 日志（急停清场）

**Branch**: `002-panic-clear-console` | **Date**: 2026-10-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-panic-clear-console/spec.md`

## Summary

急停（快捷键/菜单）触发后，在停止全部输出的同一拍清空 Console 全部日志，
消除屏幕上的摸鱼痕迹；行为受设置窗口新增的「急停时清空 Console」开关控制
（默认开启、持久化）。清空以反射 `UnityEditor.LogEntries.Clear()` 为主路径
（同步清日志表，batchmode 实测公开 API `Debug.ClearDeveloperConsole()` 会
静默 no-op，故降为兜底），失败静默降级——急停的停止能力不受影响。非急停
路径（普通停止/暂停/完结/播放模式静默停机）绝不触发清空。

## Technical Context

**Language/Version**: C# 9 / .NET Standard 2.1（Unity 6000.3.26f1，兼容 2022.3+）

**Primary Dependencies**: 仅 Unity 内置模块（UnityEditor.CoreModule /
UnityEngine.CoreModule）；无第三方依赖（宪法 IV）

**Storage**: `UserSettings/MoYuNovel/settings.json`（沿用 001 的
OutputSettings JSON，新增 `clearConsoleOnPanic` 字段；.gitignore 已忽略该目录）

**Testing**: NUnit EditMode（Unity Test Runner，batchmode CLI 运行，
`-runTests -testPlatform EditMode`）

**Target Platform**: Windows 桌面编辑器为主（2022.3 LTS 与 6000.x）

**Project Type**: Unity Editor 包（Editor 程序集增强，无 Runtime 改动）

**Performance Goals**: 急停触发到 Console 清空完成 < 1 秒（SC-001；单次
编辑器内调用，实际为毫秒级）

**Constraints**: 清空失败不弹窗、不写任何日志（FR-004，错误本身会暴露）；
阅读进度/书架/配置不受清空影响（FR-005）

**Scale/Scope**: 单用户单编辑器；改动面 4 个既有文件 + 1 个新文件 + 2 个测试文件

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 原则 | 结论 | 依据 |
|------|------|------|
| I. 编辑器边界 | ✅ 通过 | 清空目标仅编辑器 Console；新代码全部落在 Editor 程序集，Runtime 零改动 |
| II. 默认静默、一键急停 | ✅ 通过 | 先停后清、同拍完成；清空失败时急停停止能力不受影响（research R2 顺序保证） |
| III. 伪装与干扰隔离 | ✅ 通过 | 清空仅在用户显式急停时发生；"连业务日志一并清除"的权衡已记入 spec Assumptions |
| IV. 零依赖独立分发 | ✅ 通过 | 仅用 Unity 公开 API + internal 反射兜底，无新包 |
| V. 网络与内容安全 | ✅ 通过 | 不涉及网络与内容获取 |
| VI. 进度可恢复 | ✅ 通过 | 清空在进度落盘之后执行（research R2），断点续读语义不变（SC-005） |

**质量标准自查**：清空调用与开关判定核心逻辑 MUST 有 EditMode 覆盖
（ConsoleCleanerTests + MoYuEngineTests 扩展，见 research R4）；变更记入
CHANGELOG（0.2.0 → 0.3.0，MINOR=新增功能，tasks 阶段执行）。

## Project Structure

### Documentation (this feature)

```text
specs/002-panic-clear-console/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── editor-ui.md     # Phase 1 output（设置窗口急停区块契约更新）
└── tasks.md             # Phase 2 output（$speckit-tasks 生成）
```

### Source Code (repository root)

```text
Packages/com.cnoom.moyunovel/
├── Editor/
│   ├── Core/
│   │   └── MoYuEngine.cs          # 修改：PanicStop 内按开关触发清空（急停唯一漏斗）
│   ├── Settings/
│   │   ├── OutputSettings.cs      # 修改：OutputSettingsData 新增 clearConsoleOnPanic（默认 true）
│   │   └── MoYuSettingsWindow.cs  # 修改：急停区块新增开关 Toggle
│   └── Stop/
│       ├── PanicStopService.cs    # 不动（急停入口层）
│       └── ConsoleCleaner.cs      # 新增：清空 Console（公开 API + 反射兜底，静默降级，可注入测试替身）
└── Tests/EditMode/
    ├── ConsoleCleanerTests.cs     # 新增：真实清空生效 + 降级不抛
    └── MoYuEngineTests.cs         # 扩展：急停清空调用 / Stop、Pause 不调用 / 开关关闭不调用
```

**Structure Decision**: 沿用 001 固定的三段程序集结构（宪法质量标准），全部
改动在 `Editor` 与 `Tests/EditMode` 内；清空器放在 `Editor/Stop/`（与急停
服务同域，语义内聚）。

## Complexity Tracking

> 无宪法违规，无需记录。
