# Quickstart: 验证 Console 日志连载小说

**前置**：Unity 6000.3.26f1（或 2022.3+）打开本工程；包
`Packages/com.cnoom.moyunovel` 已随工程嵌入，无需安装。

## V1 开箱即读（US1 / FR-008 / SC-001）

1. 菜单 `Tools/Log Reader/Start`。
2. **预期**：Console 以默认间隔（1s）逐行输出《西游记》节选，每行形如
   `[Net][nv] 正文……`；输出期间编辑器交互无卡顿。
3. `Tools/Log Reader/Pause` → 停止输出；`Resume` → 从暂停行的下一行继续。

## V2 急停与默认静默（US2 / FR-002/005/006 / SC-002）

1. 输出运行中按 `Ctrl+Shift+F12`（或输出中按 Esc，尽力层）。
2. **预期**：最后一条输出 ≤1s 落停，状态回 Idle。
3. 完全重启 Unity，重新打开工程。
4. **预期**：无任何自动输出；`Start` 后从上次停止行的**下一行**继续（SC-003
   偏差 0 行，书架/进度显示在 `Tools/Log Reader/Settings...`）。
5. 进入再退出 Play Mode：同样不自动恢复输出。

## V3 导入自己的小说（US4 / FR-009 / SC-004）

1. `Settings...` → 书架区 Import → 选本地 .txt（UTF-8 或 GBK）。
2. **预期**：书架出现新书；切换后 Start 即从该书第一行输出；1MB 级文件导入
   到可读 <5s。GBK 样本正确显示中文；无法解码时弹明确提示且书架不变。

## V4 伪装与过滤隔离（US5 / FR-004/013 / SC-005）

1. `Settings...` → 伪装区切换「网络模块/资源加载」预设。
2. **预期**：后续输出行采用新前缀样式；每行均含 `nv` 标记。
3. Console 过滤框输入 `-nv`（排除）：**预期**摸鱼行全部隐藏、业务日志全留；
   输入 `nv`：只剩摸鱼行——两种过滤 0 漏标（SC-005）。

## V5 边界行为

| 操作 | 预期 |
|------|------|
| 间隔设为 0.01 | 生效值钳制为 0.2，UI 显示钳制后值 |
| 读完最后一行 | 输出完结提示后停止，不循环（FR-014） |
| 导入空 txt | 拒绝并提示，书架不变 |
| 超长单段落 | 自动切多行，单行 ≤120 全角字符（FR-012） |

## V6 构建隔离（FR-011 / SC-006，宪法 I）

1. 构建任意平台 Player（如 Windows）。
2. **预期**：构建产物中检索不到任何小说文本与输出行；包内 Editor 程序集与
   Editor/Resources 资源不进构建。

## 自动化验证

EditMode 测试（`Window/General/Test Runner` → EditMode → Run All）覆盖
核心逻辑契约，详见 [contracts/runtime-api.md](contracts/runtime-api.md)
的「契约测试对应」表；V2/V4/V6 中的编辑器交互场景由人工执行本指南。
