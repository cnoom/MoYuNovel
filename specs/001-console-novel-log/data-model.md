# Data Model: Console 日志连载小说

**Date**: 2026-10-10 | 来源: [spec.md](spec.md) Key Entities + FR 约束

## 实体

### Novel（小说）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | string | 稳定唯一标识：内置作品为 `builtin:<文件名>`；导入作品为 `import:<sha1(内容)前16位>`（同文件重复导入幂等） |
| Title | string | 展示标题（内置取资源名，导入取文件名去扩展名） |
| Source | enum | `Builtin` / `Imported` |
| TotalLineCount | int | 切分后的总输出行数（进度上界） |
| Chapters | List\<Chapter\> | 章节目录（仅显示分组，不参与进度计算） |

**校验**：空文件/无有效正文 → 拒绝入库并提示（不污染现有书架，边界用例）。

### Chapter（章节）

| 字段 | 类型 | 说明 |
|------|------|------|
| Title | string | 识别出的章节标题原文 |
| StartLine | int | 起始全局行号（含） |
| EndLine | int | 结束全局行号（含） |

**规则**：由 `NovelParser` 正则识别标题行（`第[0-9一二三四五六七八九十百千]+[章回节卷]`、
`Chapter\s+\d+`，行内其余为空白视为标题行）；全文无匹配 → 单章（"全文"）。
章节行本身也是输出行（标题行照常输出，保持原著观感）。

### ReadingProgress（阅读进度）

| 字段 | 类型 | 说明 |
|------|------|------|
| NovelId | string | 所属作品 |
| GlobalLineIndex | int | 已输出的最后一行全局行号（0 基；续读从下一行开始） |
| UpdatedAt | ISO-8601 string | 更新时间 |

**校验**：`GlobalLineIndex` 载入时钳制到 `[0, TotalLineCount-1]`（防作品
文件被替换后越界）；续读偏差 MUST 为 0 行（SC-003）。

### ProgressBook（进度集合）

`NovelId → ReadingProgress` 的字典封装；每本作品进度独立保留，切换作品
不丢进度。清除入口一次性清空全部（FR-007）。

### OutputSettings（输出设置）

| 字段 | 类型 | 默认值 | 约束 |
|------|------|--------|------|
| IntervalSeconds | float | 1.0 | 载入/修改时钳制到 [0.2, 3600]，UI 同步显示生效值（FR-003） |
| ActiveNovelId | string? | 内置作品 id | 必须是书架内存在的作品 |
| DisguisePresetId | string | `net` | 必须是预设库内 id |
| MarkerText | string | `nv` | 非空、≤8 字符，空值回落默认 |
| PanicComboDefault | bool | true | 主急停键启用开关（Esc 尽力拦截独立于开关常开） |

### DisguisePreset（伪装预设）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | string | 预设标识 |
| Name | string | 展示名（如「网络模块」「资源加载」） |
| PrefixTemplate | string | 前缀模板，占位符 `{marker}` 注入统一标记 |

**内置 ≥2 套**（FR-013），示例：
- `net`：`[Net][{marker}] {text}` —— 观感为网络模块日志
- `res`：`[ResMgr][{marker}] {text}` —— 观感为资源加载日志

格式化由 `DisguiseFormatter` 纯函数完成：任何预设输出 MUST 含标记
（SC-005 零漏标——占位符缺失时强制追加到行尾）。

## 运行状态机（仅内存，不持久化——宪法 II「默认静默」的实现基础）

```text
Idle ──Start(novel)──▶ Running ──Pause──▶ Paused ──Resume──▶ Running
 │                      │  │
 │                      │  ├──PanicStop/Stop──▶ Idle（清空待输出，立即）
 │                      │  └──读到末行───────▶ Finished（提示已完结，不循环）
 │                      └── 切换作品 ──▶ Idle
 └── 编辑器重启/域重载 ──▶ 恒为 Idle（无任何自动恢复路径，FR-005/006）
```

- Running 期间 `EditorApplication.update` 驱动，按 IntervalSeconds 到点输出
  下一行；暂停/停止即停调度（急停不等当前间隔到期，下一拍即静默）。
- 进度在每次输出后更新内存，按节流策略落盘（间隔保存 + Pause/Stop/
  Finished/Quit 时强制保存）。

## 持久化布局

```text
UserSettings/MoYuNovel/
├── settings.json        # OutputSettings（结构即上表，含默认值合并）
├── progress.json        # ProgressBook 序列化
└── library/<Id>.txt     # 导入作品的正文拷贝（书架条目由此枚举）
```

内置作品不落盘（从包内 Editor 资源加载），书架 = 内置列表 ∪ library/ 枚举。
