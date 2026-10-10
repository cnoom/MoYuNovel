using CNoom.MoYuNovel.Runtime.Progress;
using NUnit.Framework;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>ProgressBook 契约测试（SC-003 断点偏差 0 / FR-007 清除语义）。</summary>
    public class ProgressTests
    {
        [Test]
        public void Get_MissingOrNull_ReturnsNull()
        {
            var book = ProgressBook.Empty();
            Assert.IsNull(book.Get("no-such-id"));
            Assert.IsNull(book.Get(null));
        }

        [Test]
        public void AdvanceThenGet_ReturnsExactIndex_Sc003()
        {
            var book = ProgressBook.Empty();
            book.Advance("novel-a", 42);
            var progress = book.Get("novel-a");
            Assert.IsNotNull(progress);
            Assert.AreEqual(42, progress.GlobalLineIndex, "续读位置 MUST 与写入一致（偏差 0 行）");
            Assert.AreEqual("novel-a", progress.NovelId);
        }

        [Test]
        public void Advance_Monotonic_RegressionIgnored()
        {
            var book = ProgressBook.Empty();
            book.Advance("n", 10);
            book.Advance("n", 3);
            Assert.AreEqual(10, book.Get("n").GlobalLineIndex, "回退请求 MUST 被忽略");
            book.Advance("n", 10); // 等值同样忽略（更新时间不回退）
            Assert.AreEqual(10, book.Get("n").GlobalLineIndex);
        }

        [Test]
        public void Advance_PerNovelIndependent()
        {
            var book = ProgressBook.Empty();
            book.Advance("a", 1);
            book.Advance("b", 99);
            Assert.AreEqual(1, book.Get("a").GlobalLineIndex);
            Assert.AreEqual(99, book.Get("b").GlobalLineIndex);
        }

        [Test]
        public void ClearOne_RemovesOnlyTarget()
        {
            var book = ProgressBook.Empty();
            book.Advance("a", 1);
            book.Advance("b", 2);
            book.ClearOne("a");
            Assert.IsNull(book.Get("a"));
            Assert.IsNotNull(book.Get("b"));
        }

        [Test]
        public void Clear_RemovesEverything_Fr007()
        {
            var book = ProgressBook.Empty();
            book.Advance("a", 1);
            book.Advance("b", 2);
            book.Clear();
            Assert.IsNull(book.Get("a"));
            Assert.IsNull(book.Get("b"));
            Assert.AreEqual(0, book.Snapshot().Count);
        }

        [Test]
        public void Restore_RebuildsEntries()
        {
            var book = ProgressBook.Empty();
            book.Restore(new[]
            {
                new ReadingProgress("x", 5, System.DateTime.UtcNow),
                null,
                new ReadingProgress(string.Empty, 7, System.DateTime.UtcNow), // 非法条目被跳过
            });
            Assert.AreEqual(5, book.Get("x").GlobalLineIndex);
            Assert.AreEqual(1, book.Snapshot().Count);
        }
    }
}
