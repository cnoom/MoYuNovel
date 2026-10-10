# MoYuNovel

Unity 编辑器摸鱼插件：让 Console 日志持续『连载』小说内容。以 `com.cnoom.moyunovel` 嵌入包形式开发。

## 环境与结构

- Unity **6000.3.26f1**（包最低兼容 `unity: 2022.3`）
- 包本体：`Packages/com.cnoom.moyunovel/`（Runtime / Editor / Tests/EditMode）
- 规格与流程：`specs/`（spec-kit），宪法：`.specify/memory/constitution.md`

## 开发流程

本仓所有工作走 spec-kit 流程：`/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`，feature 目录 `specs/NNN-<feature>/` 自 001 顺延，细节见 `specs/README.md`。

## 当前状态

- [x] 工程 + 包骨架初始化
- [x] spec-kit 接入与项目宪法（v2.0.0）
- [x] feature 001 规格撰写（console 日志连载小说）
- [x] 实现与测试（`$speckit-clarify` / `$speckit-plan` / `$speckit-tasks` / `$speckit-implement`，v0.2.0，EditMode 90/90 通过）
