using System.Collections.Generic;
using System.Text;
using CNoom.MoYuNovel.Runtime.Novel;
using NUnit.Framework;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>LineSplitter 契约测试（FR-012：单行长度上限、句读优先切点）。</summary>
    public class LineSplitterTests
    {
        [Test]
        public void Split_ShortParagraph_ReturnsSameLine()
        {
            var result = LineSplitter.Split("短句一行。", LineSplitter.DefaultMaxLineChars);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("短句一行。", result[0]);
        }

        [Test]
        public void Split_EmptyOrWhitespace_ReturnsEmpty()
        {
            Assert.AreEqual(0, LineSplitter.Split(null, 120).Count);
            Assert.AreEqual(0, LineSplitter.Split(string.Empty, 120).Count);
        }

        [Test]
        public void Split_LongParagraph_NoChunkExceedsMax_AndRoundTrips()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 40; i++) sb.Append("这是第").Append(i + 1).Append("句测试内容，用于验证切行逻辑。");
            string paragraph = sb.ToString();

            var result = LineSplitter.Split(paragraph, LineSplitter.DefaultMaxLineChars);
            Assert.Greater(result.Count, 1);
            var rebuilt = new StringBuilder();
            foreach (var line in result)
            {
                Assert.LessOrEqual(line.Length, LineSplitter.DefaultMaxLineChars, "任何返回行 MUST ≤ maxChars");
                Assert.Greater(line.Length, 0);
                rebuilt.Append(line);
            }
            Assert.AreEqual(paragraph, rebuilt.ToString(), "切行不丢字、不改序");
        }

        [Test]
        public void Split_NoPunctuation_HardCutsAtExactMax()
        {
            var paragraph = new string('字', 300);
            var result = LineSplitter.Split(paragraph, 120);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(120, result[0].Length);
            Assert.AreEqual(120, result[1].Length);
            Assert.AreEqual(60, result[2].Length);
        }

        [Test]
        public void Split_PrefersSentenceEndPunctuation_OverComma()
        {
            // 位置 50 放逗号、位置 100 放句号：窗口 120 内句号更靠后，应优先句号处切分
            var sb = new StringBuilder();
            sb.Append(new string('甲', 50)).Append('，').Append(new string('乙', 49)).Append('。').Append(new string('丙', 100));
            var result = LineSplitter.Split(sb.ToString(), 120);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(101, result[0].Length, "首行应在句号之后切分");
            Assert.AreEqual('。', result[0][result[0].Length - 1]);
        }

        [Test]
        public void Split_InvalidMaxChars_FallsBackToDefault()
        {
            var paragraph = new string('字', 300);
            var result = LineSplitter.Split(paragraph, 0);
            foreach (var line in result)
            {
                Assert.LessOrEqual(line.Length, LineSplitter.DefaultMaxLineChars);
            }
        }

        [Test]
        public void Split_MaxCharsOne_SplitsPerCharacter()
        {
            var result = LineSplitter.Split("abc", 1);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("a", result[0]);
            Assert.AreEqual("b", result[1]);
            Assert.AreEqual("c", result[2]);
        }
    }
}
