# MoYuNovel Constitution

## Core Principles

### I. 编辑器边界（不可协商）
全部摸鱼功能（小说输出、进度管理、配置界面）MUST 仅存在于 Editor 程序集
（`CNoom.MoYuNovel.Editor`）；任何小说内容 MUST NOT 出现在玩家构建产物或
运行时设备的日志中。Runtime 程序集只允许承载可脱离编辑器测试的纯逻辑，
且 MUST NOT 被任何场景自动引用或自动激活。

### II. 默认静默、一键急停
插件 MUST 默认静默：编辑器重启后 MUST NOT 自动恢复小说输出。MUST 提供全局
一键停止入口（菜单项 + 快捷键），触发后 MUST 立即终止全部待输出内容，无需
等待当前章节/段落输出完毕。

### III. 伪装与干扰隔离
输出 MUST 支持伪装模板（日志前缀与格式可配置），并携带统一可过滤标记，
使使用者能一键与正常业务日志区分/过滤。输出间隔 MUST 可配置且设有下限，
MUST NOT 造成主线程卡顿、控制台刷屏以致影响正常排错。

### IV. 零依赖独立分发
包 MUST NOT 依赖任何私有或第三方 UPM 包（含 `com.cnoom.wframework` 等），
仅允许 Unity 内置模块；保证单包拷贝即可在任何 Unity 2022.3+ 工程中安装使用。

### V. 网络与内容安全
插件支持在线书源：可连接用户显式配置的小说网站获取目录与正文；网络访问
MUST 仅限于此目的，MUST NOT 采集、上报用户数据或附带任何遥测。本地内容
（内置示例与用户导入）MUST NOT 依赖网络可用。内置示例内容 MUST 无版权
风险（自写或公有领域文本）；在线书源的使用与版权合规由使用者自负。

### VI. 进度可恢复
阅读进度 MUST 持久化（断点续读：重启编辑器后可从上次位置继续），并 MUST
提供清除全部进度与配置的入口。

## 质量标准

- 核心逻辑（文本解析、进度推进、输出节流）MUST 有 EditMode 测试覆盖；
  bug 修复 MUST 附带回归测试。
- C# 遵循 .NET 标准命名规范；公共 API 具备中文 XML 文档注释。
- 程序集结构固定为 `Runtime` / `Editor` / `Tests/EditMode` 三段，新增程序集
  需在 spec 中说明理由。

## 开发流程

- 本仓所有工作走 spec-kit 流程：feature 目录 `specs/NNN-<feature>/` 自 001
  顺延，规格先行、代码后动；宪法与规格冲突时以宪法为准。
- 变更必须记入包内 `CHANGELOG.md`（Keep a Changelog 格式），版本号遵循 SemVer。

## Governance

- 宪法是本仓最高准则；修订必须说明动机、影响面与迁移步骤。
- 版本递增规则：MAJOR＝原则删除或重定义，MINOR＝新增原则/实质扩展，
  PATCH＝措辞澄清。
- 每个 feature 的任务收尾前 MUST 自查宪法合规性；引入的复杂度必须有对应
  spec 条目背书。
- 运行期开发指引以 `specs/` 内当前 feature 文档为准。

**Version**: 2.0.0 | **Ratified**: 2026-10-10 | **Last Amended**: 2026-10-10
