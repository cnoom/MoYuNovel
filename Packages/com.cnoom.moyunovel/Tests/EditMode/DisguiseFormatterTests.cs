using System;
using System.Collections.Generic;
using CNoom.MoYuNovel.Runtime.Output;
using NUnit.Framework;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>DisguiseFormatter 契约测试（SC-005 统一标记零漏标 / FR-004 伪装格式化）。</summary>
    public class DisguiseFormatterTests
    {
        private static readonly DisguisePreset Standard = new DisguisePreset("net", "网络模块", "[NetModule][{marker}] {text}");
        private static readonly DisguisePreset NoMarker = new DisguisePreset("x1", "缺标记", "[Mod] {text}");
        private static readonly DisguisePreset NoText = new DisguisePreset("x2", "缺正文", "[Mod][{marker}]");

        [Test]
        public void Format_StandardTemplate_PrefixMarkerText()
        {
            var line = DisguiseFormatter.Format(Standard, "nv", "石猴出世");
            Assert.AreEqual("[NetModule][nv] 石猴出世", line);
        }

        [Test]
        public void Format_TemplateMissingMarker_MarkerStillPresent_Sc005()
        {
            var line = DisguiseFormatter.Format(NoMarker, "nv", "正文");
            StringAssert.Contains("nv", line, "模板缺 {marker} 时 MUST 强制补标记（SC-005）");
            StringAssert.Contains("正文", line);
        }

        [Test]
        public void Format_TemplateMissingText_TextStillPresent()
        {
            var line = DisguiseFormatter.Format(NoText, "nv", "正文内容");
            StringAssert.Contains("正文内容", line);
        }

        [Test]
        public void Format_NullOrEmptyMarker_FallsBackToDefault()
        {
            StringAssert.Contains(DisguiseFormatter.DefaultMarker, DisguiseFormatter.Format(Standard, null, "t"));
            StringAssert.Contains(DisguiseFormatter.DefaultMarker, DisguiseFormatter.Format(Standard, "", "t"));
            StringAssert.Contains(DisguiseFormatter.DefaultMarker, DisguiseFormatter.Format(Standard, "   ", "t"));
        }

        [Test]
        public void Format_NullText_EmptyString()
        {
            var line = DisguiseFormatter.Format(Standard, "nv", null);
            StringAssert.Contains("nv", line);
        }

        [Test]
        public void Format_ZeroMarkerLeakage_AcrossTexts_Sc005()
        {
            var presets = new List<DisguisePreset> { Standard, NoMarker, NoText };
            var texts = new List<string> { string.Empty, " ", "普通正文一行", "nv", "[nv] 标记样文本", "超长文本" + new string('长', 200) };
            foreach (var preset in presets)
            {
                foreach (var text in texts)
                {
                    var line = DisguiseFormatter.Format(preset, "nv", text);
                    StringAssert.Contains("nv", line,
                        "预设「" + preset.Id + "」× 文本「" + text + "」出现漏标（SC-005 不允许）");
                }
            }
        }

        [Test]
        public void Format_TextPreservedVerbatim()
        {
            string text = "原文 <>&\"' 特殊字符\n不入行";
            var line = DisguiseFormatter.Format(Standard, "nv", text);
            StringAssert.Contains(text, line, "正文 MUST 原样保留，不转义不截断");
        }

        [Test]
        public void Format_NullPreset_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DisguiseFormatter.Format(null, "nv", "t"));
        }
    }
}
