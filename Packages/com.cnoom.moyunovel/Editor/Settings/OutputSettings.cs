using System;
using System.IO;
using CNoom.MoYuNovel.Editor.Storage;
using CNoom.MoYuNovel.Runtime.Output;
using UnityEngine;

namespace CNoom.MoYuNovel.Editor.Settings
{
    /// <summary>settings.json 序列化载体（JsonUtility 契约：字段名即存储键）。</summary>
    [Serializable]
    internal sealed class OutputSettingsData
    {
        public float intervalSeconds = IntervalPolicy.DefaultSeconds;
        public string activeNovelId = string.Empty;
        public string disguisePresetId = "net";
        public string markerText = DisguiseFormatter.DefaultMarker;
    }

    /// <summary>
    /// 输出设置：模型 + settings.json 读写。
    /// 缺文件回落默认值；任何载入路径一律过 <see cref="IntervalPolicy.Clamp"/>（FR-003）。
    /// </summary>
    internal sealed class OutputSettings
    {
        private const string DefaultPresetId = "net";
        private OutputSettingsData data = new OutputSettingsData();

        /// <summary>当前间隔（秒，已钳制）。</summary>
        internal float IntervalSeconds => data.intervalSeconds;

        /// <summary>当前作品 id。</summary>
        internal string ActiveNovelId => data.activeNovelId;

        /// <summary>当前伪装预设 id。</summary>
        internal string DisguisePresetId => data.disguisePresetId;

        /// <summary>统一过滤标记。</summary>
        internal string MarkerText => data.markerText;

        /// <summary>设置间隔（自动钳制到 [0.2, 3600]）。</summary>
        internal void SetInterval(float seconds) => data.intervalSeconds = IntervalPolicy.Clamp(seconds);

        /// <summary>设置当前作品。</summary>
        internal void SetActiveNovel(string novelId) => data.activeNovelId = novelId ?? string.Empty;

        /// <summary>设置伪装预设（空值回落默认）。</summary>
        internal void SetDisguisePreset(string presetId) => data.disguisePresetId = string.IsNullOrEmpty(presetId) ? DefaultPresetId : presetId;

        /// <summary>设置统一标记：非空、≤8 字符，空值回落默认（data-model 约束）。</summary>
        internal void SetMarker(string marker)
        {
            marker = (marker ?? string.Empty).Trim();
            if (marker.Length == 0) marker = DisguiseFormatter.DefaultMarker;
            if (marker.Length > 8) marker = marker.Substring(0, 8);
            data.markerText = marker;
        }

        /// <summary>恢复全部配置默认值（进度由 ProgressStore 单独管理，不受影响）。</summary>
        internal void ResetToDefaults() => data = new OutputSettingsData();

        /// <summary>从 settings.json 载入；缺文件/损坏一律回落默认。</summary>
        internal static OutputSettings Load()
        {
            var settings = new OutputSettings();
            try
            {
                if (File.Exists(MoYuPaths.SettingsFile))
                {
                    settings.data = JsonUtility.FromJson<OutputSettingsData>(File.ReadAllText(MoYuPaths.SettingsFile))
                                    ?? new OutputSettingsData();
                }
            }
            catch
            {
                settings.data = new OutputSettingsData(); // 损坏文件视为默认
            }

            settings.data.intervalSeconds = IntervalPolicy.Clamp(settings.data.intervalSeconds);
            if (string.IsNullOrEmpty(settings.data.disguisePresetId)) settings.data.disguisePresetId = DefaultPresetId;
            if (string.IsNullOrWhiteSpace(settings.data.markerText)) settings.data.markerText = DisguiseFormatter.DefaultMarker;
            if (settings.data.activeNovelId == null) settings.data.activeNovelId = string.Empty;
            return settings;
        }

        /// <summary>写盘。</summary>
        internal void Save()
        {
            MoYuPaths.EnsureRoot();
            File.WriteAllText(MoYuPaths.SettingsFile, JsonUtility.ToJson(data, true));
        }
    }
}
