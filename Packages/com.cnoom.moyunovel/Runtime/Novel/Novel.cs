using System;
using System.Collections.Generic;

namespace CNoom.MoYuNovel.Runtime.Novel
{
    /// <summary>小说来源。</summary>
    public enum NovelSource
    {
        /// <summary>包内内置作品（Editor-only 资源，不进玩家构建）。</summary>
        Builtin,

        /// <summary>用户导入的作品（拷贝存于本机书库）。</summary>
        Imported,
    }

    /// <summary>章节：仅作显示分组，不参与进度计算；行号均为切分后输出行的全局行号。</summary>
    public sealed class Chapter
    {
        /// <summary>章节标题原文（该行同时计入输出行流）。</summary>
        public string Title { get; }

        /// <summary>起始全局行号（含，0 基）。</summary>
        public int StartLine { get; }

        /// <summary>结束全局行号（含，0 基）。</summary>
        public int EndLine { get; }

        /// <summary>创建章节区间。</summary>
        public Chapter(string title, int startLine, int endLine)
        {
            Title = title ?? string.Empty;
            StartLine = startLine;
            EndLine = endLine;
        }
    }

    /// <summary>
    /// 一部可读作品：元数据 + 切分后的输出行 + 章节目录。
    /// 任意时刻仅当前阅读的作品会被装载（书架其余作品不驻留内存）。
    /// </summary>
    public sealed class Novel
    {
        /// <summary>稳定唯一标识：内置为 <c>builtin:&lt;名&gt;</c>，导入为 <c>import:&lt;书库文件名&gt;</c>。</summary>
        public string Id { get; }

        /// <summary>展示标题。</summary>
        public string Title { get; }

        /// <summary>来源（内置 / 用户导入）。</summary>
        public NovelSource Source { get; }

        /// <summary>全部输出行（章节标题行计入；空行已跳过）。</summary>
        public IReadOnlyList<string> Lines { get; }

        /// <summary>总输出行数（进度上界）。</summary>
        public int TotalLineCount => Lines.Count;

        /// <summary>章节目录（无章节标记的作品为单章「全文」）。</summary>
        public IReadOnlyList<Chapter> Chapters { get; }

        /// <summary>创建作品。</summary>
        public Novel(string id, string title, NovelSource source, IReadOnlyList<string> lines, IReadOnlyList<Chapter> chapters)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? string.Empty;
            Source = source;
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            Chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
        }

        /// <summary>取第 <paramref name="index"/> 行（0 基）。</summary>
        public string GetLine(int index) => Lines[index];
    }
}
