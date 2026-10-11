---
description: "feature 002 任务清单（speckit-tasks 生成）"
---

# Tasks: 急停清空 Console 日志（急停清场）

**Input**: Design documents from `/specs/002-panic-clear-console/`

**Prerequisites**: plan.md（技术方案）、spec.md（US1 + FR-001~007）、
research.md（R1 API 选型 / R2 挂接点 / R3 存储 / R4 测试策略）、
data-model.md（OutputSettings 扩展）、contracts/editor-ui.md（UI 增量）、
quickstart.md（V1–V5 验证）

**Tests**: 已请求——宪法质量标准要求核心逻辑（清空调用、开关判定）MUST 有
EditMode 覆盖，故每阶段含测试任务。

**Organization**: 单用户故事（US1 急停即清场，P1）；Foundational 为其阻塞
前置（设置字段 + 清空器），US1 阶段做接线、测试与 UI，Polish 收尾。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可并行（不同文件、无未完成依赖）
- **[US1]**: 用户故事标签（spec.md User Story 1）
- 路径约定：`ED/` = `Packages/com.cnoom.moyunovel/Editor`，`TS/EditMode/` =
  `Packages/com.cnoom.moyunovel/Tests/EditMode`，`PKG/` = 包根

---

## Phase 1: Setup

- [x] T001 确认回归基线：batchmode CLI 运行全量 EditMode 测试并记录当前
  通过数——实测基线 95/95 全绿（TestResults/baseline-002.xml），后续阶段以此判定无回归

**Checkpoint**: 基线全绿 → 进入 Foundational

---

## Phase 2: Foundational（US1 的阻塞前置）

- [x] T002 [P] 扩展 `ED/Settings/OutputSettings.cs`：`OutputSettingsData`
  新增字段 `public bool clearConsoleOnPanic = true`（data-model：JsonUtility
  缺字段保留初始化值 true，「清除全部进度与配置」重置为 true）+ 只读属性
  `ClearConsoleOnPanic` + `SetClearConsoleOnPanic(bool)`（research R3）
- [x] T003 [P] 新增 `ED/Stop/ConsoleCleaner.cs`：`internal static
  Func<bool> ClearImpl`（默认实现：先公开 API `Debug.ClearDeveloperConsole()`，
  异常时遍历 AppDomain 程序集反射 `UnityEditor.LogEntries.Clear()`，两级均
  失败返回 false）+ `TryClear()`（try/catch 吞一切异常返回 bool，绝不弹窗、
  绝不写日志，FR-004；research R1）

**Checkpoint**: 字段与清空器编译通过 → 进入 US1 接线

---

## Phase 3: User Story 1 - 急停即清场 (Priority: P1)

**Goal**: 急停（快捷键/Esc/菜单任一入口）触发后先停后清，开关控制清空，
非急停路径零副作用

**Independent Test**: 输出数行后急停 → Console 1 秒内清空；开关关闭后急停
只停不清；普通停止/暂停/完结/播放模式停机 Console 内容保持不变

### Implementation for User Story 1

- [x] T004 [US1] 扩展 `ED/Core/MoYuEngine.cs`：`PanicStop()` 在
  `ResetToIdle()` 与进度落盘**之后**、按 `settings.ClearConsoleOnPanic`
  调用 `ConsoleCleaner.TryClear()`（顺序固定：停止 → 落盘 → 清空，
  research R2）；`Stop`/`Pause`/`Resume`/`EnterFinished`/
  `OnPlayModeStateChanged` 一律不触碰清空（FR-001/003/005/006）
- [x] T005 [P] [US1] 新增 `TS/EditMode/ConsoleCleanerTests.cs`：①真实清空
  ——`Debug.Log` 数条 → `TryClear()` 返回 true → 反射 `LogEntries.GetCount()`
  断言归零（GetCount 不可用时退化为仅断言返回 true）；②降级安全——注入
  抛异常替身，`TryClear` 不抛且返回 false（research R4-1）
- [x] T006 [P] [US1] 新增 `TS/EditMode/PanicClearWiringTests.cs`：换入计数
  替身 `ClearImpl`——急停计数 +1（开关默认开）、开关关闭不计数、`Stop`/
  `Pause`/`TickAt` 完结/`SimulatePlayModeChangeForTest` 静默停机永不计数；
  清空调用发生在进度落盘之后（以 `Progress` 断言续读位置无损，SC-005）
  （research R4-2）
- [x] T007 [US1] 扩展 `ED/Settings/MoYuSettingsWindow.cs`：急停区块新增
  Toggle「急停时清空 Console」——绑定 `ClearConsoleOnPanic`，变更即
  `Save()`；关闭时说明行追加「当前急停仅停止输出」；与改键按钮的只读禁用
  逻辑互不影响（FR-007，contracts/editor-ui.md）
- [x] T008 [US1] 扩展 `TS/EditMode/OutputSettingsRoundTrip` 断言（并入
  T006 或 `Tests/EditMode/` 既有设置测试文件）：开关默认 true、Set 后
  Save/Load 保持、构造"缺字段旧 JSON"载入仍为 true（research R4-3）

**Checkpoint**: 全量 EditMode 绿 → US1 交付（可独立演示：输出 → 急停 →
Console 清空）

---

## Phase 4: Polish & Cross-Cutting

- [x] T009 [P] 更新 `PKG/CHANGELOG.md`（0.3.0 Added：急停清场 + 开关）与
  `PKG/package.json` 版本号 0.2.0 → 0.3.0（SemVer MINOR）
- [x] T010 宪法合规自查：原则 I–VI 逐条对照（Editor-only/急停语义/伪装
  隔离/零依赖/无网络/进度无损），结论记入 feature 收尾说明
  —— **结论：六原则全部通过**（逐条依据见 plan.md Constitution Check 表；
  实现与设计无偏差：清空仅在 PanicStop 内、进度落盘之后执行，Runtime 程序集
  零改动，无新增依赖，无复杂度违规）
- [ ] T011 全量 EditMode 测试通过（batchmode CLI，相对 T001 基线无回归且
  新增用例全绿）+ quickstart.md V1–V5 人工验证
  —— 自动化部分已完成：**104/104 全绿**（TestResults/002-final.xml，基线
  95 + 新增 9，0 失败 0 跳过）；期间实测修正 research R1——batchmode 下
  `Debug.ClearDeveloperConsole()` 静默 no-op，主备调换为反射 LogEntries.Clear
  优先（详见 research.md/plan.md 修订）。剩 quickstart V1–V5 编辑器内人工
  验证（需交互式 Unity 会话，实现代理无法代按快捷键）
- [ ] T012 [Polish] FR-002 修订（仅移除本插件行）：`ConsoleCleaner` 重写为
  选择性移除——反射 `LogEntries` 快照（GetEntryInternal）→ Clear 清表 →
  AddMessageWithDoubleClickCallback 回灌保留条目，`TryClear(marker)` 签名
  变更、引擎传入 `settings.MarkerText`；识别规则 `[<marker>]` token 或
  `[LogReader]` 前缀；解析失败/过滤态静默跳过，绝不回退全量清空（research
  R5，Cecil 反查 6000.3 API 面）；`[LogReader]` 前缀提取为共享常量
- [ ] T013 [Polish] 测试同步：`ConsoleCleanerTests` 改为"插件行移除 + 业务
  日志内容/顺序/类型保留"断言 + `IsPluginEntry` 判定单测 + 降级吞异常；
  `PanicClearWiringTests` 替身适配 `Func<string,bool>` 签名
- [ ] T014 [Polish] 全量回归（batchmode）确认无回归且新用例全绿；
  spec/plan/research/contracts/quickstart/CHANGELOG 修订回写

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup（T001）→ Foundational（T002/T003 可并行）→ US1（T004 依赖 T002+
  T003；T005/T006 与 T004 后并行；T007 依赖 T002；T008 依赖 T002）→ Polish
- US1 为唯一故事，无跨故事依赖

### Parallel Opportunities

- T002 与 T003 并行（不同文件）
- T005、T006 与 T004 完成后并行（各自新文件）
- T009 与 T010 并行（不同文件）

## Implementation Strategy

- **MVP First**: T001–T008 完成即交付全部价值（本 feature 仅一个故事）
- **Constitution**: T010 合规自查为硬门禁；T011 全量回归 + quickstart
  人工验证收尾
