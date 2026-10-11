# Data Model: 急停清空 Console 日志

**Date**: 2026-10-11 | 来源: [spec.md](spec.md) FR-005/FR-007 + [research.md](research.md) R3

本 feature **不新增实体、不新增持久化文件**，仅扩展 001 的输出设置；引擎
状态机（Idle/Running/Paused/Finished）与急停语义不变。

## 扩展实体

### OutputSettings（输出设置，001 已有）—— 新增 1 字段

| 字段 | 类型 | 默认值 | 约束 |
|------|------|--------|------|
| clearConsoleOnPanic | bool | `true` | 持久化于 settings.json；缺字段（旧版本 JSON）视为 true；「清除全部进度与配置」重置为 true |

**语义**：`true` 时急停（`MoYuEngine.PanicStop`）在停止输出、进度落盘之后
清空 Console 全部日志；`false` 时急停仅停止输出。运行中修改即时生效（设置
窗口 Toggle → `Save()`），下一次急停即按新值执行。

## 不变量

- **清空不触碰数据面**：Console 清空只影响编辑器日志表；progress.json、
  settings.json、书架文本文件一律不变（FR-005 / SC-005 续读偏差 0）。
- **状态机不变**：急停仍为任意状态 → Idle；清空是该转换的附加副作用，
  不引入新状态、不改变 StateChanged 事件语义。
- **非急停路径零副作用**：`Stop` / `Pause` / `Resume` / `EnterFinished` /
  播放模式静默停机不触发清空（FR-003）——由 MoYuEngineTests 替身计数断言。
