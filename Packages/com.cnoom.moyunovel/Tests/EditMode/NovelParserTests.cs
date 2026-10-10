using CNoom.MoYuNovel.Runtime.Novel;
using NUnit.Framework;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>NovelParser 契约测试（clarify 澄清 1：章节正则/单章回退/空文本异常）。</summary>
    public class NovelParserTests
    {
        [TestCase("第一回 灵根育孕源流出　心性修持大道生")]
        [TestCase("第十二章")]
        [TestCase("第三十八回：三藏赶行者")]
        [TestCase("第一百节 补记")]
        [TestCase("Chapter 12")]
        [TestCase("Chapter 12: The Stone Monkey")]
        [TestCase("chapter 3 beginning")]
        public void IsChapterTitleLine_KnownFormats_Detected(string line)
        {
            Assert.IsTrue(NovelParser.IsChapterTitleLine(line), "应识别为章节标题: " + line);
        }

        [TestCase("他第一次回到了家乡")]
        [TestCase("这是我第一次回京的路上")]
        [TestCase("时光倒回第五回合的比赛")]
        [TestCase("正文里提到第一章的内容")]
        [TestCase("普通正文一行")]
        public void IsChapterTitleLine_PlainSentences_NotDetected(string line)
        {
            Assert.IsFalse(NovelParser.IsChapterTitleLine(line), "普通语句不应误判为章节标题: " + line);
        }

        [Test]
        public void Parse_NoChapterMarkers_SingleChapterFallback()
        {
            var novel = NovelParser.Parse("t:1", "测试", NovelSource.Imported, "第一段内容\n\n第二段内容\n\n第三段内容");
            Assert.AreEqual(1, novel.Chapters.Count);
            Assert.AreEqual(NovelParser.SingleChapterTitle, novel.Chapters[0].Title);
            Assert.AreEqual(0, novel.Chapters[0].StartLine);
            Assert.AreEqual(2, novel.Chapters[0].EndLine);
            Assert.AreEqual(3, novel.TotalLineCount);
        }

        [Test]
        public void Parse_EmptyOrNull_ThrowsEmptyNovelText()
        {
            Assert.Throws<EmptyNovelTextException>(() => NovelParser.Parse("t", "t", NovelSource.Imported, null));
            Assert.Throws<EmptyNovelTextException>(() => NovelParser.Parse("t", "t", NovelSource.Imported, string.Empty));
        }

        [Test]
        public void Parse_WhitespaceOnly_ThrowsEmptyNovelText()
        {
            Assert.Throws<EmptyNovelTextException>(() =>
                NovelParser.Parse("t", "t", NovelSource.Imported, "  \n\n \t \n"));
        }

        [Test]
        public void Parse_BlankLinesSkipped_AndCRLFNormalized()
        {
            var novel = NovelParser.Parse("t", "t", NovelSource.Imported, "甲\r\n\r\n乙\r\n\n丙");
            Assert.AreEqual(3, novel.TotalLineCount);
            Assert.AreEqual("甲", novel.GetLine(0));
            Assert.AreEqual("乙", novel.GetLine(1));
            Assert.AreEqual("丙", novel.GetLine(2));
        }

        [Test]
        public void Parse_LongParagraph_AutoSplit_Fr012()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("开头。");
            for (int i = 0; i < 60; i++) sb.Append("连环句第").Append(i).Append("句，语义绵长便于触发切行。");
            var novel = NovelParser.Parse("t", "t", NovelSource.Imported, sb.ToString());

            Assert.Greater(novel.TotalLineCount, 1);
            foreach (var line in novel.Lines)
            {
                Assert.LessOrEqual(line.Length, LineSplitter.DefaultMaxLineChars, "单行输出 MUST 有长度上限");
            }
        }

        [Test]
        public void Parse_ChapterTitleLine_IsIncludedInOutputLines()
        {
            var novel = NovelParser.Parse("t", "t", NovelSource.Imported, "第一回 标题行\n正文一");
            StringAssert.StartsWith("第一回", novel.GetLine(0), "章节标题行本身 MUST 计入输出行（保持原著观感）");
        }

        [Test]
        public void Parse_MultipleChapters_RangesCorrect()
        {
            var novel = NovelParser.Parse("t", "t", NovelSource.Imported,
                "第一章 起\n正文一\n第二章 承\n正文二\n正文三");

            Assert.AreEqual(2, novel.Chapters.Count);
            Assert.AreEqual("第一章 起", novel.Chapters[0].Title);
            Assert.AreEqual(0, novel.Chapters[0].StartLine);
            Assert.AreEqual(1, novel.Chapters[0].EndLine);
            Assert.AreEqual("第二章 承", novel.Chapters[1].Title);
            Assert.AreEqual(2, novel.Chapters[1].StartLine);
            Assert.AreEqual(4, novel.Chapters[1].EndLine);
        }

        [Test]
        public void Parse_FrontMatterBeforeFirstChapter_GetsOwnGroup()
        {
            var novel = NovelParser.Parse("t", "t", NovelSource.Imported,
                "序言文字\n第一章 起\n正文一");

            Assert.AreEqual(2, novel.Chapters.Count);
            Assert.AreEqual(NovelParser.FrontMatterTitle, novel.Chapters[0].Title);
            Assert.AreEqual(0, novel.Chapters[0].EndLine);
            Assert.AreEqual("第一章 起", novel.Chapters[1].Title);
        }

        [Test]
        public void Parse_MetadataPreserved()
        {
            var novel = NovelParser.Parse("import:abc", "书名", NovelSource.Imported, "内容");
            Assert.AreEqual("import:abc", novel.Id);
            Assert.AreEqual("书名", novel.Title);
            Assert.AreEqual(NovelSource.Imported, novel.Source);
        }
    }
}
