using System;
using System.Collections.Generic;

namespace CNoom.MoYuNovel.Runtime.Progress
{
    /// <summary>全部作品的阅读进度集合（NovelId → 进度），每本进度独立保留。</summary>
    public sealed class ProgressBook
    {
        private readonly Dictionary<string, ReadingProgress> entries = new Dictionary<string, ReadingProgress>();

        /// <summary>创建空集合。</summary>
        public static ProgressBook Empty() => new ProgressBook();

        /// <summary>读取某作品进度；无记录返回 null。</summary>
        public ReadingProgress Get(string novelId)
        {
            return novelId != null && entries.TryGetValue(novelId, out var progress) ? progress : null;
        }

        /// <summary>推进进度。单调递增：回退请求被忽略，断点不因误操作回退。</summary>
        public void Advance(string novelId, int globalLineIndex)
        {
            if (string.IsNullOrEmpty(novelId)) throw new ArgumentException("novelId 不能为空", nameof(novelId));
            if (entries.TryGetValue(novelId, out var existing) && globalLineIndex <= existing.GlobalLineIndex) return;
            entries[novelId] = new ReadingProgress(novelId, globalLineIndex, DateTime.UtcNow);
        }

        /// <summary>清除单部作品进度（「从头再读」）。</summary>
        public void ClearOne(string novelId) => entries.Remove(novelId);

        /// <summary>清除全部进度（FR-007）。</summary>
        public void Clear() => entries.Clear();

        /// <summary>全部条目快照（供持久化枚举）。</summary>
        public IReadOnlyCollection<ReadingProgress> Snapshot() => entries.Values;

        /// <summary>装载持久化条目（仅供存储层恢复时调用）。</summary>
        public void Restore(IEnumerable<ReadingProgress> items)
        {
            if (items == null) return;
            foreach (var progress in items)
            {
                if (progress == null || string.IsNullOrEmpty(progress.NovelId)) continue;
                entries[progress.NovelId] = progress;
            }
        }
    }
}
