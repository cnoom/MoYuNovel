# Quickstart: 验证急停清空 Console 日志

**前置**：Unity 6000.3.26f1（或 2022.3+）打开本工程；包
`Packages/com.cnoom.moyunovel` 已随工程嵌入。EditMode 全量测试先行：
`Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode
-testResults TestResults/editmode.xml -nographics`，预期 0 失败
（含 ConsoleCleanerTests 真实清空用例）。

## V1 急停即清场（US1 场景 1 / FR-001 / SC-001、SC-002）

1. 手动 `Debug.Log("业务日志：急停后应保留")` 一条，再 `Start` 输出 3–5 行。
2. 按 `Ctrl+Shift+F12`（主急停键）。
3. **预期**：输出立即停止；小说行与 `[LogReader]` 系统日志全部消失，
   "业务日志：急停后应保留"**原样保留**且位置不变。

## V2 菜单急停与暂停残留（US1 场景 2、3）

1. `Start` 输出数行 → `Pause`（Console 保留已输出行）。
2. 菜单 `Tools/Log Reader/Panic Stop`。
3. **预期**：效果与快捷键一致——插件行全部移除，业务日志不动。

## V3 开关控制与持久化（FR-007 / SC-006、US1 场景 6）

1. `Settings...` → 急停区块，确认「急停时清空 Console」默认**勾选**。
2. 取消勾选 → `Start` 输出数行 → 急停。
   **预期**：输出停止，Console 内容**原样保留**（只停不清）。
3. 完全重启 Unity 重开工程 → 打开 `Settings...`。
   **预期**：开关仍为**取消**状态（持久化）；重新勾选后急停恢复清场。
4. 急停清场后 `Start`：**预期**从上次停止行的下一行继续（SC-005 偏差 0 行）。

## V4 非急停路径不清空（FR-003 / SC-003、US1 场景 4）

| 操作 | 预期 |
|------|------|
| 输出中 `Stop` | Console 内容保持不变 |
| 输出中 `Pause` / `Resume` | Console 内容保持不变 |
| 读到最后一行（完结提示出现） | Console 内容保持不变（含完结行） |
| 输出中进入/退出 Play Mode | 静默停机，Console 内容保持不变 |

## V5 清空后配置完好（FR-005）

急停清场后打开 `Settings...`：**预期**书架、间隔、伪装预设、统一标记、
急停键位全部保持；Console 过滤标记 `nv` 等设置未重置。

## V6 边界（FR-002 修订 / FR-004）

| 操作 | 预期 |
|------|------|
| Console 过滤框输入任意文本（过滤态）后急停 | 清理静默跳过，不弹窗不报错（显示层本已被过滤） |
| 清空过滤文本后再急停 | 插件行正常移除、业务日志保留 |
| 业务日志含字面 "[nv]" | 该条会被误判移除（已知边界，自定义标记可规避） |
