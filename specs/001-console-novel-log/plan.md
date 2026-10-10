# Implementation Plan: Console 日志连载小说（摸鱼核心玩法）

**Branch**: `001-console-novel-log` | **Date**: 2026-10-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-console-novel-log/spec.md`

## Summary

让 Unity Console 以正常业务日志的观感逐行连载小说正文的编辑器插件。技术路线：
纯 C# 核心逻辑（解析/进度/切行/钳制/伪装格式化）下沉到 Runtime 程序集
（`noEngineReferences` + 不自动引用，满足宪法原则 I），编辑器集成（输出引擎、
菜单、设置窗口、快捷键、存储 IO）全部在 Editor 程序集；状态持久化为 JSON
文件存放于 `UserSettings/MoYuNovel/`（工程内、默认不入库）；核心逻辑全部
EditMode 测试覆盖；零第三方依赖（宪法原则 IV）。

## Technical Context

**Language/Version**: C# 9（Unity 2022.3 可编译）/ 开发环境 Unity 6000.3.26f1，
包最低兼容 `unity: 2022.3`

**Primary Dependencies**: 无第三方依赖（宪法 IV）——仅 UnityEngine / UnityEditor
内置模块与 .NET BCL

**Storage**: JSON + txt 文件，位于 `UserSettings/MoYuNovel/`（该目录已被根
.gitignore 覆盖：个人数据不入库，天然满足摸鱼隐蔽性）：
- `settings.json`（输出设置与当前作品）
- `progress.json`（全部作品的阅读进度）
- `library/`（导入小说的拷贝，脱离原文件可用）

**Testing**: Unity Test Framework（NUnit，EditMode），测试程序集引用 Runtime
与 Editor 两个程序集

**Target Platform**: Unity Editor（Windows 优先；不使用 Windows 专属 API，
macOS/Linux 编辑器同样可用）

**Project Type**: 嵌入式 UPM 包（`Packages/com.cnoom.moyunovel/`）内的编辑器插件

**Performance Goals**: 单行输出主线程开销毫秒级（无可感知卡顿，SC-001）；
1MB txt 解析+建目 <5s（SC-004）；急停即时生效（SC-002 上限 1s）

**Constraints**: 玩家构建产物与真机日志 0 小说行（SC-006/FR-011）；间隔下限
0.2s；输出引擎不逐帧产生 GC 分配

**Scale/Scope**: 单机单用户；书架 ≤500 本；单本 ≤10MB 文本；Runtime 核心不
持有整本字符串（流式按行读取）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 原则/标准 | 设计对策 | 结论 |
|-----------|----------|------|
| I. 编辑器边界 | 全部功能在 Editor 程序集；内置小说文本是 Editor 目录下资源（不进构建）；Runtime 程序集纯 C#（`noEngineReferences: true` + `autoReferenced: false`，无 MonoBehaviour、无自动激活） | PASS |
| II. 默认静默、一键急停 | 运行状态只存内存不持久化，重启/域重载后天然静默；急停=停调度+清队列，`EditorApplication.update` 下一拍生效（≪1s） | PASS |
| III. 伪装与干扰隔离 | DisguisePreset 纯格式化前缀+格式；每行固定位置携带可配置统一标记（默认 `nv`），SC-005 用测试断言零漏标 | PASS |
| IV. 零依赖独立分发 | package.json 无 dependencies；GBK 解码用 Mono 内置 I18N（`Encoding.GetEncoding(936)`），失败明确提示，不引码表库 | PASS |
| V. 网络与内容安全 | v1 无任何网络代码；内置文本为公有领域《西游记》节选，存放 Editor-only 资源目录 | PASS |
| VI. 进度可恢复 | progress.json 节流持久化（间隔保存+暂停/停止/完结时保存）；设置窗口提供清除全部进度与配置入口 | PASS |
| 质量标准：测试 | 解析/切行/钳制/进度/格式化/状态机全覆盖 EditMode 测试 | PASS |
| 质量标准：程序集三段 | Runtime / Editor / Tests/EditMode，与既有骨架一致，无新增程序集 | PASS |
| 质量标准：命名与文档 | .NET 标准命名；公共 API 中文 XML 文档注释 | PASS |

## Project Structure

### Documentation (this feature)

```text
specs/001-console-novel-log/
├── plan.md              # This file ($speckit-plan command output)
├── research.md          # Phase 0 output ($speckit-plan command)
├── data-model.md        # Phase 1 output ($speckit-plan command)
├── quickstart.md        # Phase 1 output ($speckit-plan command)
├── contracts/           # Phase 1 output ($speckit-plan command)
│   ├── runtime-api.md   # Runtime 程序集公共 API 契约
│   └── editor-ui.md     # 用户可见入口契约（菜单/快捷键/窗口）
└── tasks.md             # Phase 2 output ($speckit-tasks command - NOT created by $speckit-plan)
```

### Source Code (repository root)

```text
Packages/com.cnoom.moyunovel/
├── Runtime/                          # 纯 C# 核心（noEngineReferences, autoReferenced: false）
│   ├── CNoom.MoYuNovel.Runtime.asmdef
│   ├── Novel/
│   │   ├── Novel.cs                  # 作品元数据 + 章节目录 + 总行数
│   │   ├── NovelParser.cs            # txt → 行流 + 章节标题识别（第X章/回/节、Chapter N）
│   │   ├── LineSplitter.cs           # 超长段落切分（单行长度上限）
│   │   └── EncodingDetector.cs       # BOM/UTF-8 逐字节校验、GBK 结构探测
│   ├── Progress/
│   │   ├── ReadingProgress.cs        # 作品 id + 全局行号 + 时间戳
│   │   └── ProgressBook.cs           # 多本进度集合（纯字典逻辑）
│   └── Output/
│       ├── IntervalPolicy.cs         # 默认 1s、下限 0.2s、越界钳制
│       └── DisguiseFormatter.cs      # 伪装前缀 + 统一标记的纯格式化
├── Editor/                           # 编辑器集成（Editor-only）
│   ├── CNoom.MoYuNovel.Editor.asmdef
│   ├── Core/
│   │   ├── MoYuEngine.cs             # 输出引擎：update 调度、启停状态机、读完即停
│   │   └── MoYuMenu.cs               # 菜单入口（中性命名，不暴露摸鱼语义）
│   ├── Stop/
│   │   └── PanicStopService.cs       # Esc 尽力拦截（反射钩）+ ShortcutManager 主急停键
│   ├── Settings/
│   │   ├── MoYuSettingsWindow.cs     # 设置窗口：书架/进度/间隔/预设/清除
│   │   └── OutputSettings.cs         # 设置模型 + settings.json 读写
│   ├── Library/
│   │   ├── NovelLibrary.cs           # 书架：内置 + 导入拷贝入库 + 切换
│   │   └── ProgressStore.cs          # progress.json 读写（节流保存）
│   ├── Disguise/
│   │   └── DisguisePresetLibrary.cs  # 预设库（内置 ≥2 套：网络模块/资源加载 等）
│   └── Resources/
│       └── journey-west.txt          # 内置《西游记》节选（Editor-only 资源）
└── Tests/EditMode/
    ├── CNoom.MoYuNovel.Tests.EditMode.asmdef
    ├── NovelParserTests.cs           # 章节识别/无标记单章/空文件（回归：bug 修复必附测试）
    ├── LineSplitterTests.cs          # 超长段落/长度上限
    ├── EncodingDetectorTests.cs      # UTF-8/GBK 探测
    ├── IntervalPolicyTests.cs        # 默认值/下限钳制/非法值
    ├── ProgressTests.cs              # 断点续读偏差 0/清除
    ├── DisguiseFormatterTests.cs     # 统一标记零漏（SC-005）
    └── MoYuEngineTests.cs            # 状态机：默认静默/急停即时/读完即停不循环
```

**Structure Decision**: 沿用宪法固定的 Runtime / Editor / Tests/EditMode 三段
程序集。纯逻辑全部下沉 Runtime（`noEngineReferences: true` 使其成为无引擎依赖
的纯 C# 岛屿，EditMode 可测且绝不触碰播放器行为；`autoReferenced: false`
满足原则 I「不被自动引用」）；一切编辑器交互与 IO 在 Editor。存储不在包内，
而在工程 `UserSettings/MoYuNovel/`（个人数据与仓库隔离）。

## Complexity Tracking

> 无宪法违规项需要辩护——不适用。
