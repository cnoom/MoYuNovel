using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CNoom.MoYuNovel.Editor.Storage;
using CNoom.MoYuNovel.Runtime.Progress;
using UnityEngine;

namespace CNoom.MoYuNovel.Editor.Library
{
    /// <summary>progress.json 单条序列化载体。</summary>
    [Serializable]
    internal sealed class ProgressEntryData
    {
        public string novelId;
        public int globalLineIndex;
        public string updatedAt;
    }

    /// <summary>progress.json 序列化载体。</summary>
    [Serializable]
    internal sealed class ProgressFileData
    {
        public List<ProgressEntryData> entries = new List<ProgressEntryData>();
    }

    /// <summary>
    /// 进度存储（FR-007）：progress.json 读写 + 节流保存。
    /// 节流策略（data-model）：逐行输出仅标记，≥10s 落盘一次；暂停/停止/完结/退出强制落盘。
    /// </summary>
    internal sealed class ProgressStore
    {
        private const double AutosaveIntervalSeconds = 10.0;
        private readonly ProgressBook book = ProgressBook.Empty();
        private DateTime lastSaveUtc = DateTime.UtcNow;

        /// <summary>底层进度集合（引擎经此读取/推进）。</summary>
        internal ProgressBook Book => book;

        /// <summary>读取某作品进度；无记录返回 null。</summary>
        internal ReadingProgress Get(string novelId) => book.Get(novelId);

        /// <summary>推进某作品进度（内存即时生效；节流落盘由 MarkDirty 驱动）。</summary>
        internal void Advance(string novelId, int globalLineIndex) => book.Advance(novelId, globalLineIndex);

        /// <summary>逐行输出后调用：节流落盘。</summary>
        internal void MarkDirty()
        {
            if ((DateTime.UtcNow - lastSaveUtc).TotalSeconds >= AutosaveIntervalSeconds) Save();
        }

        /// <summary>强制落盘。</summary>
        internal void Save()
        {
            var file = new ProgressFileData();
            foreach (var progress in book.Snapshot())
            {
                file.entries.Add(new ProgressEntryData
                {
                    novelId = progress.NovelId,
                    globalLineIndex = progress.GlobalLineIndex,
                    updatedAt = progress.UpdatedAt.ToString("o", CultureInfo.InvariantCulture),
                });
            }
            MoYuPaths.EnsureRoot();
            File.WriteAllText(MoYuPaths.ProgressFile, JsonUtility.ToJson(file, true));
            lastSaveUtc = DateTime.UtcNow;
        }

        /// <summary>从 progress.json 载入；缺文件/损坏一律视为无进度（不阻塞阅读）。</summary>
        internal void Load()
        {
            book.Clear();
            if (!File.Exists(MoYuPaths.ProgressFile)) return;
            try
            {
                var file = JsonUtility.FromJson<ProgressFileData>(File.ReadAllText(MoYuPaths.ProgressFile));
                if (file?.entries == null) return;

                var restored = new List<ReadingProgress>(file.entries.Count);
                foreach (var entry in file.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.novelId)) continue;
                    var time = DateTime.TryParse(entry.updatedAt, CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind, out var parsed)
                        ? parsed.ToUniversalTime()
                        : DateTime.UtcNow;
                    restored.Add(new ReadingProgress(entry.novelId, entry.globalLineIndex, time));
                }
                book.Restore(restored);
            }
            catch
            {
                book.Clear();
            }
        }

        /// <summary>清除单本进度（「从头再读」），立即落盘。</summary>
        internal void ClearOne(string novelId)
        {
            book.ClearOne(novelId);
            Save();
        }

        /// <summary>清除全部进度，立即落盘。</summary>
        internal void ClearAll()
        {
            book.Clear();
            Save();
        }
    }
}
