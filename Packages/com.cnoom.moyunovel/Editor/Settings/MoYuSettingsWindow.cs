using System;
using System.Collections.Generic;
using CNoom.MoYuNovel.Editor.Core;
using CNoom.MoYuNovel.Editor.Disguise;
using CNoom.MoYuNovel.Editor.Library;
using CNoom.MoYuNovel.Runtime.Novel;
using CNoom.MoYuNovel.Runtime.Output;
using UnityEditor;
using UnityEngine;

namespace CNoom.MoYuNovel.Editor.Settings
{
    /// <summary>
    /// 设置窗口（FR-016）：书架 / 阅读进度 / 输出节奏 / 伪装样式 四区块集中管理。
    /// 窗口标题用中性命名「Log Reader」（contracts/editor-ui.md）。
    /// </summary>
    internal sealed class MoYuSettingsWindow : EditorWindow
    {
        private NovelLibrary library;
        private List<ShelfItem> shelf;
        private Novel displayNovel;      // 当前作品展示缓存（避免每帧重解析）
        private Vector2 shelfScroll;
        private int presetIndex;
        private string markerBuffer;
        private string intervalBuffer;

        /// <summary>打开设置窗口。</summary>
        internal static void Open() => GetWindow<MoYuSettingsWindow>(true, "Log Reader", true);

        private void OnEnable()
        {
            library = new NovelLibrary();
            RefreshShelf();
            markerBuffer = MoYuEngine.Settings.MarkerText;
            intervalBuffer = MoYuEngine.Settings.IntervalSeconds.ToString("0.##");
            presetIndex = IndexOfPreset(MoYuEngine.Settings.DisguisePresetId);
        }

        // 引擎逐行输出时刷新进度展示
        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            DrawShelf();
            EditorGUILayout.Space(8);
            DrawProgress();
            EditorGUILayout.Space(8);
            DrawInterval();
            EditorGUILayout.Space(8);
            DrawDisguise();
        }

        private void DrawShelf()
        {
            EditorGUILayout.LabelField("书架", EditorStyles.boldLabel);
            if (shelf == null || shelf.Count == 0)
            {
                EditorGUILayout.HelpBox("书架为空。", MessageType.Info);
                return;
            }

            var activeId = MoYuEngine.Settings.ActiveNovelId;
            shelfScroll = EditorGUILayout.BeginScrollView(shelfScroll, GUILayout.MaxHeight(150));
            foreach (var item in shelf)
            {
                var progress = MoYuEngine.Progress.Get(item.Id);
                string readInfo = progress == null ? "未读" : "已读 " + (progress.GlobalLineIndex + 1) + " 行";
                string row = item.Title + "   （" + (item.Source == NovelSource.Builtin ? "内置" : "导入") + " · " + readInfo + "）";
                bool selected = EditorGUILayout.ToggleLeft(row, item.Id == activeId,
                    item.Id == activeId ? EditorStyles.boldLabel : EditorStyles.label);
                if (selected && item.Id != activeId) SwitchActive(item);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("导入本地 .txt 小说…", GUILayout.Width(200)))
            {
                string path = EditorUtility.OpenFilePanel("选择小说文本文件", "", "txt");
                if (!string.IsNullOrEmpty(path)) ImportFile(path);
            }
        }

        private void DrawProgress()
        {
            EditorGUILayout.LabelField("阅读进度", EditorStyles.boldLabel);
            EnsureDisplayNovel();

            if (displayNovel == null)
            {
                EditorGUILayout.LabelField("尚未选择作品。");
            }
            else
            {
                var progress = MoYuEngine.Progress.Get(displayNovel.Id);
                int last = progress == null ? -1 : progress.GlobalLineIndex;
                string stateText = DescribeState(MoYuEngine.State);
                EditorGUILayout.LabelField(displayNovel.Title, stateText + " · " + MoYuEngine.DescribePosition(displayNovel, last));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从头再读")) RestartFromHead();
                if (GUILayout.Button("清除全部进度与配置")) ClearEverything();
            }
        }

        private void DrawInterval()
        {
            EditorGUILayout.LabelField("输出节奏（秒/行）", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            float sliderValue = EditorGUILayout.Slider("间隔", MoYuEngine.Settings.IntervalSeconds,
                IntervalPolicy.MinSeconds, 10f);
            if (EditorGUI.EndChangeCheck())
            {
                MoYuEngine.Settings.SetInterval(sliderValue);
                intervalBuffer = MoYuEngine.Settings.IntervalSeconds.ToString("0.##");
                MoYuEngine.Settings.Save();
            }

            EditorGUI.BeginChangeCheck();
            string typed = EditorGUILayout.TextField("精确值", intervalBuffer);
            if (EditorGUI.EndChangeCheck())
            {
                intervalBuffer = typed;
                if (float.TryParse(typed, out var value))
                {
                    MoYuEngine.Settings.SetInterval(value); // 非法值钳制，UI 同步显示生效值（FR-003）
                    MoYuEngine.Settings.Save();
                }
            }

            EditorGUILayout.LabelField("生效值", MoYuEngine.Settings.IntervalSeconds.ToString("0.##")
                + " 秒（下限 " + IntervalPolicy.MinSeconds.ToString("0.#") + " 秒）");
        }

        private void DrawDisguise()
        {
            EditorGUILayout.LabelField("伪装样式", EditorStyles.boldLabel);

            var presets = DisguisePresetLibrary.All;
            var names = new string[presets.Count];
            for (int i = 0; i < presets.Count; i++) names[i] = presets[i].Name;

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup("预设", Mathf.Clamp(presetIndex, 0, presets.Count - 1), names);
            if (EditorGUI.EndChangeCheck())
            {
                presetIndex = picked;
                MoYuEngine.Settings.SetDisguisePreset(presets[picked].Id);
                MoYuEngine.Settings.Save();
            }

            EditorGUI.BeginChangeCheck();
            string marker = EditorGUILayout.TextField("统一过滤标记", markerBuffer);
            if (EditorGUI.EndChangeCheck())
            {
                markerBuffer = marker;
                MoYuEngine.Settings.SetMarker(marker);
                MoYuEngine.Settings.Save();
            }

            EditorGUILayout.LabelField("过滤方法", "Console 过滤框输入 \"" + MoYuEngine.Settings.MarkerText
                + "\" 单独查看本插件输出；输入 -" + MoYuEngine.Settings.MarkerText + " 隐藏之。");
        }

        private static string DescribeState(MoYuEngine.EngineState state)
        {
            switch (state)
            {
                case MoYuEngine.EngineState.Running: return "连载中";
                case MoYuEngine.EngineState.Paused: return "已暂停";
                case MoYuEngine.EngineState.Finished: return "已完结";
                default: return "待机";
            }
        }

        private void RefreshShelf() => shelf = library.Enumerate();

        private void EnsureDisplayNovel()
        {
            var activeId = MoYuEngine.Settings.ActiveNovelId;
            if (displayNovel != null && displayNovel.Id == activeId) return;
            displayNovel = null;
            if (string.IsNullOrEmpty(activeId)) return;
            try
            {
                displayNovel = library.LoadById(activeId);
            }
            catch (Exception)
            {
                displayNovel = null; // 正文缺失等场景：窗口不阻塞，只显示「尚未选择」
            }
        }

        private void InvalidateDisplay()
        {
            displayNovel = null;
            EnsureDisplayNovel();
        }

        private void SwitchActive(ShelfItem item)
        {
            MoYuEngine.Stop(); // 状态机：切换作品即回 Idle
            MoYuEngine.Settings.SetActiveNovel(item.Id);
            MoYuEngine.Settings.Save();
            InvalidateDisplay();
        }

        private void ImportFile(string path)
        {
            var outcome = library.Import(path);
            EditorUtility.DisplayDialog("导入文本", outcome.Message, "确定");
            RefreshShelf();
            if (outcome.Ok)
            {
                MoYuEngine.Stop();
                MoYuEngine.Settings.SetActiveNovel(outcome.Novel.Id);
                MoYuEngine.Settings.Save();
                InvalidateDisplay();
            }
        }

        private void RestartFromHead()
        {
            var id = MoYuEngine.Settings.ActiveNovelId;
            if (string.IsNullOrEmpty(id)) return;
            MoYuEngine.Stop();
            MoYuEngine.Progress.ClearOne(id);
            MoYuEngine.StartActive();
        }

        private void ClearEverything()
        {
            if (!EditorUtility.DisplayDialog("清除确认",
                    "将清除全部阅读进度与输出配置（导入的小说保留）。确定？", "清除", "取消"))
            {
                return;
            }
            MoYuEngine.Stop();
            MoYuEngine.Progress.ClearAll();
            MoYuEngine.Settings.ResetToDefaults();
            MoYuEngine.Settings.Save();
            markerBuffer = MoYuEngine.Settings.MarkerText;
            intervalBuffer = MoYuEngine.Settings.IntervalSeconds.ToString("0.##");
            presetIndex = IndexOfPreset(MoYuEngine.Settings.DisguisePresetId);
            InvalidateDisplay();
        }

        private static int IndexOfPreset(string id)
        {
            var presets = DisguisePresetLibrary.All;
            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i].Id == id) return i;
            }
            return 0;
        }
    }
}
