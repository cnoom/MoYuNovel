# MoYuNovel（com.cnoom.moyunovel · Log Reader）

摸鱼神器：让 Unity Console 以正常业务日志的口吻持续『连载』小说内容——老板路过，看到的只是你在盯着一串日志查报错。

> 产品对外命名 **Log Reader**（菜单、窗口均用中性名，不暴露摸鱼语义）。

## 功能

- **连载输出**：按可调间隔（默认 1s、下限 0.2s）逐行输出小说正文，超长段落自动切行，观感同业务日志
- **伪装预设**：内置「网络模块 / 资源加载 / 数据访问」3 套前缀样式，统一过滤标记（默认 `nv`）一键过滤隔离
- **断点续读**：进度持久化于工程 `UserSettings/MoYuNovel/`，重启编辑器续读偏差 0 行
- **内容来源**：内置《西游记》第一回节选（公有领域，开箱即读）；导入本地 .txt（UTF-8 / UTF-16 / GBK 自动探测）
- **一键急停**：ShortcutManager 主急停键（默认 `Ctrl+Shift+F12`，设置窗口内可改键）+ Esc 尽力拦截（不破坏编辑器原生 Esc 行为）+ 菜单入口
- **急停清场**：急停时仅移除本插件产生的 Console 条目（小说行与系统日志），业务日志内容与相对顺序原样保留；可在设置窗口关闭该开关
- **默认静默**：编辑器重启、域重载、播放模式进出后不自动恢复输出

## 环境要求

- Unity **2022.3 LTS 或更高**（6000.x 已验证）；仅编辑器环境，零第三方依赖

## 使用

```text
Tools/Log Reader/
├── Start / Pause / Resume / Stop   # 启停控制
├── Panic Stop                      # 急停（与快捷键等价）
└── Settings...                     # 书架、进度、节奏、伪装、急停键位与清场开关
```

- 过滤：Console 过滤框输入 `nv` 只看小说，输入 `-nv` 隐藏小说只看业务日志
- 急停键改键：`Settings... → 急停 → 改键…`（冲突组合键会被拒绝并提示占用方）

## 安装

把本目录整体拷入目标工程的 `Packages/` 下（嵌入包方式），无需其他配置。

## 开发与测试

本包随仓库根的 spec-kit 流程开发（规格见仓库根 `specs/`）；全量 EditMode 测试：

```bash
Unity.exe -batchmode -projectPath <工程> -runTests -testPlatform EditMode -nographics
```

## 许可

未附带开源许可证（保留所有权利）。
