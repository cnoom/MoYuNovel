using System.Collections.Generic;
using CNoom.MoYuNovel.Runtime.Output;

namespace CNoom.MoYuNovel.Editor.Disguise
{
    /// <summary>内置伪装预设库（FR-013：不少于 2 套；观感为常见业务模块日志）。</summary>
    internal static class DisguisePresetLibrary
    {
        private static readonly List<DisguisePreset> Presets = new List<DisguisePreset>
        {
            new DisguisePreset("net", "网络模块", "[NetModule][{marker}] {text}"),
            new DisguisePreset("res", "资源加载", "[ResLoader][{marker}] {text}"),
            new DisguisePreset("db", "数据访问", "[DbAccess][{marker}] {text}"),
        };

        /// <summary>全部内置预设。</summary>
        internal static IReadOnlyList<DisguisePreset> All => Presets;

        /// <summary>按 id 取预设；未命中回落首项（FR-004 持续可输出）。</summary>
        internal static DisguisePreset GetById(string id)
        {
            foreach (var preset in Presets)
            {
                if (preset.Id == id) return preset;
            }
            return Presets[0];
        }
    }
}
