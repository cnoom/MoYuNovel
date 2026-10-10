using System;

namespace CNoom.MoYuNovel.Runtime.Progress
{
    /// <summary>某部作品的阅读进度：已输出的最后一行的全局行号（0 基）。</summary>
    public sealed class ReadingProgress
    {
        /// <summary>所属作品标识。</summary>
        public string NovelId { get; }

        /// <summary>已输出的最后一行全局行号（0 基；续读从其下一行开始）。</summary>
        public int GlobalLineIndex { get; }

        /// <summary>最近更新时间（UTC）。</summary>
        public DateTime UpdatedAt { get; }

        /// <summary>创建进度记录。</summary>
        public ReadingProgress(string novelId, int globalLineIndex, DateTime updatedAt)
        {
            NovelId = novelId ?? throw new ArgumentNullException(nameof(novelId));
            GlobalLineIndex = globalLineIndex;
            UpdatedAt = updatedAt;
        }
    }
}
