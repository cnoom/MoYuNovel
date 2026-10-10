---

description: "feature 001 任务清单（speckit-tasks 生成）"
---

# Tasks: Console 日志连载小说（摸鱼核心玩法）

**Input**: Design documents from `/specs/001-console-novel-log/`

**Prerequisites**: plan.md、spec.md、research.md、data-model.md、contracts/

**Tests**: 宪法质量标准强制要求核心逻辑 EditMode 测试覆盖，测试任务随各阶段包含。

**Organization**: 按用户故事分组；Runtime 纯 C# 核心为共享基础设施（Phase 2）。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 不同文件、无未完成依赖，可并行
- **[Story]**: 所属用户故事（US1–US5，对应 spec.md）

**根路径约定**：`PKG/` = `Packages/com.cnoom.moyunovel/`；`RT/` = `PKG/Runtime/`；
`ED/` = `PKG/Editor/`；`TS/` = `PKG/Tests/EditMode/`

---

## Phase 1: Setup（共享基础设施）

- [ ] T001 [P] 配置三段程序集 asmdef：`RT/CNoom.MoYuNovel.Runtime.asmdef` 设
  `noEngineReferences: true`、`autoReferenced: false`（宪法 I）；`ED/CNoom.MoYuNovel.Editor.asmdef`
  设 `includePlatforms: ["Editor"]` 并引用 Runtime；`TS/CNoom.MoYuNovel.Tests.EditMode.asmdef`
  引用 Runtime + Editor + nunit
- [ ] T002 [P] 创建存储路径工具 `ED/Storage/MoYuPaths.cs`：常量
  `UserSettings/MoYuNovel/`（settings.json / progress.json / library/），
  提供确保目录存在的方法（research R3）

**Checkpoint**: 程序集骨架与存储路径就绪

---

## Phase 2: Foundational（Runtime 纯 C# 核心，阻塞所有故事）

**⚠ CRITICAL**: 全部用户故事依赖本阶段；契约见 `contracts/runtime-api.md`

- [ ] T003 [P] 实现 `RT/Output/LineSplitter.cs`：按 maxChars（默认 120 全角）
  切分超长段落，切点优先句读（。！？；，、…"),无句读硬切；任何返回行 ≤
  maxChars（FR-012）；配套 `TS/LineSplitterTests.cs`
- [ ] T004 [P] 实现 `RT/Novel/EncodingDetector.cs`：BOM 优先 → UTF-8 逐字节
  校验 → GBK 双字节结构启发式 → Unknown（FR-009）；配套
  `TS/EncodingDetectorTests.cs`
- [ ] T005 [P] 实现 `RT/Output/IntervalPolicy.cs`：默认 1s、下限 0.2s、上限
  3600s，Clamp 处理 NaN/负/越界（FR-003 澄清值）；配套
  `TS/IntervalPolicyTests.cs`
- [ ] T006 [P] 实现 `RT/Progress/ReadingProgress.cs` + `RT/Progress/ProgressBook.cs`：
  Advance 单调递增（回退忽略）、Get、ClearOne、Clear（FR-007）；配套
  `TS/ProgressTests.cs`（SC-003 断点偏差 0）
- [ ] T007 [P] 实现 `RT/Output/DisguisePreset.cs` + `RT/Output/DisguiseFormatter.cs`：
  模板占位 {marker}/{text}，模板缺 {marker} 强制尾置标记——任何输出含标记
  （SC-005 零漏标）；配套 `TS/DisguiseFormatterTests.cs`
- [ ] T008 实现 `RT/Novel/Novel.cs` + `RT/Novel/NovelParser.cs`（依赖 T003）：
  章节正则（`第[数字中文]+[章回节卷集]`、`Chapter N`，标题行同时计入输出行），
  无标记→单章，空/全空白→EmptyNovelTextException，空行跳过（FR 澄清 1）；
  配套 `TS/NovelParserTests.cs`

**Checkpoint**: Runtime 核心完成且测试绿；所有故事可并行开工

---

## Phase 3: User Story 1 — 摸鱼连载输出（P1）→ MVP

**Goal**: 开启后 Console 按间隔逐行输出内置小说，观感同业务日志

**Independent Test**: 菜单 Start 后 Console 连续出现带伪装前缀的小说行，
间隔可调（spec US1 Independent Test）

### Implementation for User Story 1

- [ ] T009 [P] [US1] 内置示例文本 `ED/Resources/journey-west.txt`：《西游记》
  第一回节选（公有领域，含「第一回…」标题行、诗曰与正文段落，UTF-8）
- [ ] T010 [P] [US1] 实现 `ED/Disguise/DisguisePresetLibrary.cs`：内置 ≥2 套
  预设（net「网络模块」/ res「资源加载」，模板含 {marker}{text}）+ GetById
  回落首项（FR-013）
- [ ] T011 [US1] 实现 `ED/Core/MoYuEngine.cs`：状态机 Idle/Running/Paused/
  Finished（仅内存不持久化）；`EditorApplication.update` + Stopwatch 到点
  输出下一行（Debug.Log + DisguiseFormatter）；暂停恢复不跳行不重复；
  读完输出完结提示后 Finished 不循环（FR-014）；PanicStop 立即清空（FR-002）
- [ ] T012 [US1] 实现 `ED/Core/MoYuMenu.cs`：菜单 `Tools/Log Reader/`
  Start/Pause/Resume/Stop/Panic Stop/Settings...，中性命名，状态勾选与
  Validate 同步（FR-001、contracts/editor-ui.md）
- [ ] T013 [US1] 实现 `TS/MoYuEngineTests.cs`：状态翻转合法、到点出下一行、
  暂停恢复位置一致、读完 Finished 不循环、急停后 Tick 不再输出（US1+US2
  引擎断言合集）

**Checkpoint**: MVP 可用——开箱即读（quickstart V1 可过）

---

## Phase 4: User Story 2 — 安全停止与默认静默（P1）

**Goal**: 一键急停 ≤1s 落停；重启/播放模式转换后保持静默

**Independent Test**: 急停后最后一条输出 1 秒内落停；重启编辑器无任何输出
（spec US2 Independent Test）

### Implementation for User Story 2

- [ ] T014 [US2] 实现 `ED/Stop/PanicStopService.cs`：主急停键 ShortcutManager
  `[Shortcut("Log Reader/Panic Stop", Ctrl+Shift+F12)]`；Esc 尽力拦截——
  Running 期间反射挂 `EditorApplication.globalEventHandler`（不 Use 事件，
  不破坏原生 Esc 行为），反射失败静默降级（research R1、FR-002）
- [ ] T015 [US2] 扩展 `ED/Core/MoYuEngine.cs`：注册
  `playModeStateChanged`——进入/退出播放模式即静默 Stop（FR-006）；
  `EditorApplication.quitting` 落盘进度（US3 存储接入前的空实现占位）
- [ ] T016 [US2] 扩展 `TS/MoYuEngineTests.cs`：默认静默断言（新实例恒 Idle）、
  急停后状态即时回 Idle 且后续 Tick 零输出、播放模式转换回调触发 Stop
  （FR-005/006）

**Checkpoint**: US1+US2 达标（quickstart V2 可过）

---

## Phase 5: User Story 3 — 断点续读（P2）

**Goal**: 进度持久化，重启后续读偏差 0 行；提供清除入口

**Independent Test**: 读到任意位置重启再开启，从上次停止行的下一行继续；
清除进度后从头开始（spec US3 Independent Test）

### Implementation for User Story 3

- [ ] T017 [US3] 实现 `ED/Settings/OutputSettings.cs`：模型（IntervalSeconds/
  ActiveNovelId/DisguisePresetId/MarkerText 默认 nv）+ settings.json 读写，
  缺文件回落默认值、载入后 IntervalPolicy.Clamp（data-model 约束）
- [ ] T018 [US3] 实现 `ED/Library/ProgressStore.cs`：progress.json 读写（数组
  序列化）、节流保存（间隔保存 + Pause/Stop/Finished/Quit 强制）、载入钳制
  GlobalLineIndex 到 [0, TotalLineCount-1]（data-model 校验）
- [ ] T019 [US3] 扩展 `ED/Core/MoYuEngine.cs`：接入 OutputSettings +
  ProgressStore——Start 从断点下一行续读（SC-003），逐行 Advance，按节流
  落盘
- [ ] T020 [US3] 实现 `ED/Settings/MoYuSettingsWindow.cs`：窗口骨架（标题
  Log Reader）+ 进度区块——当前位置（章节/行）、「从头再读」（ClearOne）、
  「清除全部进度与配置」（确认后清空）（FR-007/FR-016）
- [ ] T021 [P] [US3] 实现 `TS/ProgressStoreTests.cs`：临时目录读写回环、
  越界钳制、清除语义（T018 契约）

**Checkpoint**: US1–US3 达标（quickstart V2 第 4 步续读偏差 0 可过）

---

## Phase 6: User Story 4 — 内容来源（P2）

**Goal**: 开箱即读内置小说；导入本地 txt 建私人书架

**Independent Test**: 全新安装开启即读内置；导入 txt 后可切换并输出
（spec US4 Independent Test）

### Implementation for User Story 4

- [ ] T022 [US4] 实现 `ED/Library/NovelLibrary.cs`：内置作品枚举（在包内
  find TextAsset 加载，builtin: 前缀 id）；导入管线——读字节 →
  EncodingDetector → UTF-8 直接用 / GBK 走 `Encoding.GetEncoding(936)` /
  失败抛可提示异常 → 转存 UTF-8 至 `library/<id>.txt`；id =
  `import:<sha1 前 16 位>` 幂等；空/全空白拒绝（FR-008/009、research R2/R4）
- [ ] T023 [US4] 扩展 `ED/Settings/MoYuSettingsWindow.cs`：书架区块——作品
  列表（标题/来源/进度%）、Import 按钮（OpenFilePanel 选 .txt，失败弹明确
  提示且书架不变）、点击切换当前作品（FR-016/US4 验收 2/3）
- [ ] T024 [P] [US4] 实现 `TS/NovelLibraryTests.cs`：UTF-8/GBK 导入回环、
  空文件拒绝不改书架、重复导入幂等（临时 UserSettings 目录）

**Checkpoint**: US1–US4 达标（quickstart V3 可过）

---

## Phase 7: User Story 5 — 伪装定制与日志隔离（P3）

**Goal**: 预设切换、统一标记自定义，过滤一键隔离

**Independent Test**: 切换预设后新行样式变化；按标记过滤后 100% 区分
（spec US5 Independent Test）

### Implementation for User Story 5

- [ ] T025 [US5] 扩展 `ED/Settings/MoYuSettingsWindow.cs`：伪装区块——预设
  下拉（DisguisePresetLibrary 全量）、统一标记编辑（≤8 字符非空，空回落
  nv，写回 settings.json）（FR-004/013）
- [ ] T026 [US5] 扩展 `ED/Core/MoYuEngine.cs` + `MoYuSettingsWindow.cs`：
  节奏区块——间隔滑条+数值框（0.2–3600），实时显示钳制后生效值并热更新
  引擎间隔（FR-003 验收 2、边界「UI 同步显示生效值」）

**Checkpoint**: 五个故事全部达标（quickstart V4/V5 可过）

---

## Phase 8: Polish & Cross-Cutting

- [ ] T027 [P] 更新 `PKG/CHANGELOG.md`（Keep a Changelog，0.2.0 Added 条目）
  与 `PKG/package.json` 版本号
- [ ] T028 宪法合规自查：原则 I–VI 逐条对照（Editor-only 资源/零依赖/无
  网络代码/急停/静默/清除入口），结论记入 feature 收尾说明
- [ ] T029 全量 EditMode 测试通过（Unity Test Runner）+ quickstart.md V1–V5
  人工验证（V6 构建隔离视条件执行）

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup（T001–T002）→ Foundational（T003–T008，内部仅 T008 依赖 T003）→
  US1 → US2 → US3 → US4 → US5 → Polish
- US2 依赖 US1 的引擎；US3 依赖 US1 引擎与 US2 静默语义；US4 依赖 US3 的
  设置窗口骨架与存储；US5 依赖 US3/US4 的窗口与引擎接线

### Parallel Opportunities

- T003–T007 全部并行（不同文件）；T009/T010 与 T011 前置并行
- 各测试任务与对应实现同阶段并行（不同文件）

## Implementation Strategy

- **MVP First**: Phase 1–3 完成即达 US1 可交付（开箱即读），先验证再续
- **Incremental**: 每故事 Checkpoint 处独立验证（对应 quickstart 场景）
- **Constitution**: 收尾 T028 合规自查为硬门禁
