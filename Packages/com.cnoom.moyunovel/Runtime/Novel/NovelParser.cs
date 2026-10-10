using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CNoom.MoYuNovel.Runtime.Novel
{
    /// <summary>正文为空或不含有效文本时的解析异常（调用方应拒绝入库并提示）。</summary>
    public sealed class EmptyNovelTextException : Exception
    {
        /// <summary>创建空正文异常。</summary>
        public EmptyNovelTextException() : base("小说正文为空或不含有效文本。") { }
    }

    /// <summary>
    /// txt 正文解析器：空行跳过、章节标题识别、超长段落切分。
    /// 章节标题行本身同时计入输出行（保持原著观感）。
    /// </summary>
    public static class NovelParser
    {
        /// <summary>无章节标记作品的单章标题。</summary>
        public const string SingleChapterTitle = "全文";

        /// <summary>首个章节标题之前存在正文时的卷首分组标题。</summary>
        public const string FrontMatterTitle = "卷首";

        // 「第…章/回/节/卷/集」：序号后必须紧跟分隔（空白/标点/行尾），
        // 避免「第一次回去」这类普通语句被误判为章节标题。
        private const string ChineseChapter =
            @"第[0-9零一二三四五六七八九十百千两]+[章回节卷集](?=$|\s|[：:，,。！？…—・.])";
        private const string EnglishChapter = @"[Cc]hapter\s+[0-9]+(?=$|\s|[：:，,。！？…—・.])";

        private static readonly Regex ChapterLinePattern = new Regex(
            @"^\s*(" + ChineseChapter + "|" + EnglishChapter + @").{0,40}\s*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>判断一行是否为章节标题行。</summary>
        public static bool IsChapterTitleLine(string line)
        {
            return ChapterLinePattern.IsMatch(line ?? string.Empty);
        }

        /// <summary>
        /// 解析正文为作品。
        /// 契约：无章节标记 → 单章「全文」；空/全空白 → 抛 <see cref="EmptyNovelTextException"/>。
        /// </summary>
        public static Novel Parse(string id, string title, NovelSource source, string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) throw new EmptyNovelTextException();

            var lines = new List<string>();
            var chapterStarts = new List<KeyValuePair<string, int>>();

            foreach (var rawLine in NormalizeNewlines(rawText).Split('\n'))
            {
                var paragraph = rawLine.Trim();
                if (paragraph.Length == 0) continue; // 空行跳过

                if (IsChapterTitleLine(paragraph)) chapterStarts.Add(new KeyValuePair<string, int>(paragraph, lines.Count));
                lines.AddRange(LineSplitter.Split(paragraph, LineSplitter.DefaultMaxLineChars));
            }

            if (lines.Count == 0) throw new EmptyNovelTextException();

            var chapters = BuildChapters(chapterStarts, lines.Count);
            return new Novel(id, title, source, lines, chapters);
        }

        private static string NormalizeNewlines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static IReadOnlyList<Chapter> BuildChapters(List<KeyValuePair<string, int>> starts, int totalLines)
        {
            if (starts.Count == 0)
            {
                return new[] { new Chapter(SingleChapterTitle, 0, totalLines - 1) };
            }

            var chapters = new List<Chapter>(starts.Count + 1);
            if (starts[0].Value > 0)
            {
                chapters.Add(new Chapter(FrontMatterTitle, 0, starts[0].Value - 1));
            }
            for (int i = 0; i < starts.Count; i++)
            {
                int end = i + 1 < starts.Count ? starts[i + 1].Value - 1 : totalLines - 1;
                chapters.Add(new Chapter(starts[i].Key, starts[i].Value, end));
            }
            return chapters;
        }
    }
}
