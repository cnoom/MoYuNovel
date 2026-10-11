# MoYuNovel

**Unity 编辑器摸鱼插件：让 Console 日志持续『连载』小说内容。**

老板路过时，看到的只是你在一丝不苟地盯着一串"业务日志"——而其实你在追《西游记》下一回。

以 UPM 包 `com.cnoom.moyunovel`（产品名 **Log Reader**）形式分发，全部逻辑限定在编辑器内，绝不进入构建产物。

---

## 特性

- **连载输出**：Console 按可调间隔（默认 1 秒、下限 0.2 秒）逐行输出小说正文，观感与常见业务日志一致；超长段落自动切行
- **伪装预设**：内置「网络模块 / 资源加载 / 数据访问」3 套前缀样式，输出行统一携带可过滤标记（默认 `nv`），一键过滤隔离、不干扰真实排错
- **断点续读**：进度持久化，重启编辑器后从上次停止行的下一行继续，偏差 0 行
- **内容来源**：内置公有领域示例（《西游记》第一回节选，开箱即读）+ 本地 .txt 导入（UTF-8 / UTF-16 / GBK 自动探测）
- **一键急停**：全局快捷键（默认 `Ctrl+Shift+F12`，可在设置窗口改键）+ Esc 尽力拦截 + 菜单入口；触发后 1 秒内停止一切待输出
- **急停清场**：急停时选择性移除 Console 中**本插件产生的条目**（小说行与系统日志），业务日志内容与相对顺序原样保留——消除痕迹的同时不破坏你的调试现场
- **默认静默**：编辑器重启、域重载、播放模式进出后绝不自动恢复输出；阅读进度、书架、设置均在工程 `UserSettings/`（已被 git 忽略），不进仓库

## 安全设计（宪法约束）

插件遵循项目宪法（`.specify/memory/constitution.md`）六原则，核心包括：

| 原则 | 承诺 |
|------|------|
| 编辑器边界 | 全部功能仅在 Editor 程序集，构建产物与真机日志中小说输出为 0 |
| 默认静默、一键急停 | 重启不自动恢复；急停 1 秒内落停，清场能力失效也不影响急停本身 |
| 伪装与干扰隔离 | 输出带统一过滤标记、间隔有下限，可 100% 与业务日志区分 |
| 零依赖 | 仅用 Unity 内置模块，单包拷贝即可在任意 Unity 2022.3+ 工程使用 |

## 安装

- **方式一（本仓库）**：克隆仓库后用 Unity 打开，包已嵌入 `Packages/com.cnoom.moyunovel/`
- **方式二（拷贝）**：把 `Packages/com.cnoom.moyunovel/` 整个目录拷入任意 Unity 2022.3+ 工程的 `Packages/` 下即可

## 快速上手

```text
Tools/Log Reader/
├── Start        # 开始连载（从上次进度续读）
├── Pause/Resume # 暂停 / 恢复
├── Stop         # 停止（进度保留）
├── Panic Stop   # 急停（= 快捷键 Ctrl+Shift+F12：停止 + 移除插件日志）
└── Settings...  # 书架 / 进度 / 节奏 / 伪装 / 急停键位与清场开关
```

1. 菜单 `Tools/Log Reader/Start`，Console 开始"连载"；
2. 过滤：Console 过滤框输入 `nv` 只看小说，输入 `-nv` 只看业务日志；
3. 老板来了：`Ctrl+Shift+F12`——输出立停，小说行从 Console 消失，业务日志原地不动；回来后 `Start` 从断点继续。

## 开发

- Unity **6000.3.26f1** 开发（包最低兼容 `unity: 2022.3`），零第三方依赖
- 全量测试（batchmode）：

  ```bash
  Unity.exe -batchmode -projectPath <本工程> -runTests -testPlatform EditMode \
    -testResults TestResults/editmode.xml -nographics
  ```

- 工程走 **spec-kit 流程**：feature 规格先行（`specs/NNN-<feature>/`），任务清单驱动实现；`.specify/scripts/` 提供 powershell 脚手架，细节见 `specs/README.md`
- 变更记录见包内 `Packages/com.cnoom.moyunovel/CHANGELOG.md`（Keep a Changelog / SemVer）

## 仓库结构

```text
├── Packages/com.cnoom.moyunovel/   # 插件包本体（Runtime 逻辑 / Editor 集成 / EditMode 测试）
├── specs/                          # spec-kit 规格与任务清单（001 连载 / 002 急停清场）
└── .specify/                       # 模板、脚本与项目宪法
```

## 免责声明

本项目仅供学习与技术娱乐。在非私人设备或严肃工作场合使用所产生的一切后果（被老板发现、绩效波动、社死等）由使用者自负。内置示例为公有领域文本；导入的本地小说版权由使用者自担。

## 许可

暂未附带开源许可证（保留所有权利）。如需引用或二次分发请先开 issue 沟通。
