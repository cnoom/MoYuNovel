using System.Linq;
using CNoom.MoYuNovel.Editor.Settings;
using NUnit.Framework;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>急停主键绑定读写测试（FR-002 设置窗口改键 / 冲突拒绝 / 恢复默认）。
    /// 激活 Profile 只读时（如 batchmode）自动切到临时可写 Profile，结束后删除还原。</summary>
    public class PanicKeyBindingTests
    {
        private const string TempProfileId = "MoYuNovelTests";

        private ShortcutBinding original;
        private string originalProfile;
        private string tempProfile;

        [SetUp]
        public void SetUp()
        {
            originalProfile = ShortcutManager.instance.activeProfileId;
            original = ShortcutManager.instance.GetShortcutBinding(PanicKeyBinding.ShortcutId);
            if (!ShortcutManager.instance.IsProfileReadOnly(originalProfile)) return;

            if (!ShortcutManager.instance.GetAvailableProfileIds().Contains(TempProfileId))
                ShortcutManager.instance.CreateProfile(TempProfileId);
            ShortcutManager.instance.activeProfileId = TempProfileId;
            tempProfile = TempProfileId;
            if (ShortcutManager.instance.IsProfileReadOnly(TempProfileId))
                Assert.Ignore("无可写的快捷键 Profile，跳过改键测试");
        }

        [TearDown]
        public void TearDown()
        {
            if (tempProfile == null)
            {
                // 可写 Profile 直改场景：还原用户原有绑定
                if (FirstCombination(original) == null)
                    ShortcutManager.instance.ClearShortcutOverride(PanicKeyBinding.ShortcutId);
                else
                    ShortcutManager.instance.RebindShortcut(PanicKeyBinding.ShortcutId, original);
                return;
            }
            // 临时 Profile 场景：切回原 Profile 后删除，改绑随 Profile 一并消失
            if (ShortcutManager.instance.activeProfileId != originalProfile)
                ShortcutManager.instance.activeProfileId = originalProfile;
            ShortcutManager.instance.DeleteProfile(TempProfileId);
            tempProfile = null;
        }

        [Test]
        public void ShortcutId_MatchesRegisteredAttributeDefault()
        {
            ShortcutManager.instance.ClearShortcutOverride(PanicKeyBinding.ShortcutId);
            var combo = FirstCombination(PanicKeyBinding.Current);
            Assert.IsNotNull(combo, "默认必须注册了主急停组合键");
            Assert.AreEqual(KeyCode.F12, combo.Value.keyCode);
            Assert.AreEqual(ShortcutModifiers.Control | ShortcutModifiers.Shift, combo.Value.modifiers);
        }

        [Test]
        public void TryRebind_ChangesEffectiveBinding()
        {
            Assert.IsTrue(PanicKeyBinding.TryRebind(KeyCode.K,
                ShortcutModifiers.Control | ShortcutModifiers.Alt, out var error), error);

            var combo = FirstCombination(
                ShortcutManager.instance.GetShortcutBinding(PanicKeyBinding.ShortcutId));
            Assert.AreEqual(KeyCode.K, combo.Value.keyCode);
            Assert.AreEqual(ShortcutModifiers.Control | ShortcutModifiers.Alt, combo.Value.modifiers);
        }

        [Test]
        public void TryRebind_RejectsComboTakenByOtherShortcut()
        {
            string conflictId = null;
            KeyCombination taken = default;
            foreach (var id in ShortcutManager.instance.GetAvailableShortcutIds())
            {
                if (id == PanicKeyBinding.ShortcutId) continue;
                var combo = FirstCombination(ShortcutManager.instance.GetShortcutBinding(id));
                if (combo != null)
                {
                    conflictId = id;
                    taken = combo.Value;
                    break;
                }
            }
            if (conflictId == null) Assert.Ignore("当前环境没有已绑定组合键的其他快捷键注册项");

            Assert.IsFalse(PanicKeyBinding.TryRebind(taken.keyCode, taken.modifiers, out var error));
            Assert.IsTrue(error != null && error.Contains("占用"), "错误信息应说明组合键被占用，实际：" + error);
            // 拒绝后原绑定保持不变
            var current = FirstCombination(
                ShortcutManager.instance.GetShortcutBinding(PanicKeyBinding.ShortcutId)).Value;
            var before = FirstCombination(original).Value;
            Assert.AreEqual(before.keyCode, current.keyCode);
            Assert.AreEqual(before.modifiers, current.modifiers);
        }

        [Test]
        public void ResetToDefault_RestoresAttributeBinding()
        {
            PanicKeyBinding.TryRebind(KeyCode.K, ShortcutModifiers.Control | ShortcutModifiers.Alt, out _);
            PanicKeyBinding.ResetToDefault();

            var combo = FirstCombination(PanicKeyBinding.Current);
            Assert.AreEqual(KeyCode.F12, combo.Value.keyCode);
        }

        /// <summary>取绑定的首个组合键；绑定为空时返回 null。</summary>
        private static KeyCombination? FirstCombination(ShortcutBinding binding)
        {
            foreach (var combo in binding.keyCombinationSequence) return combo;
            return null;
        }
    }
}
