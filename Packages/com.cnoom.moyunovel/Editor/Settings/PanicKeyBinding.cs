using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace CNoom.MoYuNovel.Editor.Settings
{
    /// <summary>
    /// 主急停键绑定读写（FR-002「可在设置窗口中自定义」）：封装 ShortcutManager
    /// 对 "Log Reader/Panic Stop" 注册项的查询、改绑与恢复默认。
    /// Esc 无法经 ShortcutManager 注册（录制 UI 按 Esc 即取消），故仅主键可改，
    /// Esc 备用急停保持硬编码（research R1）。
    /// </summary>
    internal static class PanicKeyBinding
    {
        /// <summary>PanicStopService 上 [Shortcut] 特性的注册 ID，两处必须一致。</summary>
        internal const string ShortcutId = "Log Reader/Panic Stop";

        /// <summary>当前激活的快捷键 Profile 是否允许改键（batchmode / 只读 Profile 下为 false）。</summary>
        internal static bool ProfileWritable =>
            !ShortcutManager.instance.IsProfileReadOnly(ShortcutManager.instance.activeProfileId);

        internal static ShortcutBinding Current => ShortcutManager.instance.GetShortcutBinding(ShortcutId);

        /// <summary>当前主急停键描述（组合为空时给出降级提示）。</summary>
        internal static string Describe()
        {
            foreach (var combo in Current.keyCombinationSequence)
                return Format(combo);
            return "未设置（仅 Esc 备用急停）";
        }

        /// <summary>
        /// 改绑主急停键。组合键已被其他注册项占用时拒绝改绑（宁可让用户换键，
        /// 也不制造两个快捷键抢同一组合的歧义），回传 false 与占用方说明。
        /// </summary>
        internal static bool TryRebind(KeyCode key, ShortcutModifiers modifiers, out string error)
        {
            if (!ProfileWritable)
            {
                error = "当前快捷键 Profile 为只读，无法改键（可在 Unity Shortcuts 窗口切换 Profile 后重试）";
                return false;
            }
            string conflict = FindConflict(key, modifiers);
            if (conflict != null)
            {
                error = "组合键已被其他操作占用：" + conflict;
                return false;
            }
            ShortcutManager.instance.RebindShortcut(ShortcutId,
                new ShortcutBinding(new KeyCombination(key, modifiers)));
            error = null;
            return true;
        }

        /// <summary>清除覆盖，恢复 [Shortcut] 特性声明的默认键位；只读 Profile 下为无操作。</summary>
        internal static void ResetToDefault()
        {
            if (!ProfileWritable) return;
            ShortcutManager.instance.ClearShortcutOverride(ShortcutId);
        }

        /// <summary>返回占用该组合键的其他注册项 ID；无冲突返回 null。</summary>
        internal static string FindConflict(KeyCode key, ShortcutModifiers modifiers)
        {
            foreach (var id in ShortcutManager.instance.GetAvailableShortcutIds())
            {
                if (id == ShortcutId) continue;
                foreach (var combo in ShortcutManager.instance.GetShortcutBinding(id).keyCombinationSequence)
                {
                    if (combo.keyCode == key && combo.modifiers == modifiers) return id;
                }
            }
            return null;
        }

        /// <summary>组合键显示文本。6000.3 的 Action 在 Windows/Linux 上即 Ctrl、macOS 上即 Cmd，与字面 Control 同显为 Ctrl 去重。</summary>
        internal static string Format(KeyCombination combo)
        {
            var parts = new List<string>();
            var m = combo.modifiers;
            bool action = (m & ShortcutModifiers.Action) != 0;
            if (action || (m & ShortcutModifiers.Control) != 0) parts.Add("Ctrl");
            if ((m & ShortcutModifiers.Shift) != 0) parts.Add("Shift");
            if ((m & ShortcutModifiers.Alt) != 0) parts.Add("Alt");
            parts.Add(combo.keyCode.ToString());
            return string.Join("+", parts);
        }
    }
}
