using CNoom.MoYuNovel.Editor.Disguise;
using CNoom.MoYuNovel.Runtime.Output;
using NUnit.Framework;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>内置伪装预设库测试（FR-013 ≥2 套 / SC-005 零漏标）。</summary>
    public class DisguisePresetLibraryTests
    {
        [Test]
        public void All_ContainsAtLeastTwoPresets_Fr013()
        {
            Assert.GreaterOrEqual(DisguisePresetLibrary.All.Count, 2, "内置预设 MUST 不少于 2 套");
        }

        [Test]
        public void EveryPreset_FormatsWithMarker_Sc005()
        {
            foreach (var preset in DisguisePresetLibrary.All)
            {
                var line = DisguiseFormatter.Format(preset, "nv", "任意正文");
                StringAssert.Contains("nv", line, "预设「" + preset.Id + "」输出漏标");
                StringAssert.Contains("任意正文", line);
            }
        }

        [Test]
        public void GetById_KnownId_ReturnsPreset()
        {
            var preset = DisguisePresetLibrary.GetById("res");
            Assert.AreEqual("res", preset.Id);
        }

        [Test]
        public void GetById_UnknownOrNull_FallsBackToFirst()
        {
            Assert.AreEqual(DisguisePresetLibrary.All[0].Id, DisguisePresetLibrary.GetById("no-such").Id);
            Assert.AreEqual(DisguisePresetLibrary.All[0].Id, DisguisePresetLibrary.GetById(null).Id);
        }
    }
}
