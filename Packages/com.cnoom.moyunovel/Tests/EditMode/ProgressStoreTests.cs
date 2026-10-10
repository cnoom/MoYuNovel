using CNoom.MoYuNovel.Editor.Storage;
using System.IO;
using System.Text;
using CNoom.MoYuNovel.Editor.Library;
using NUnit.Framework;
using UnityEngine;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>ProgressStore 契约测试（FR-007 持久化 / 节流落盘 / 坏档容错）。</summary>
    public class ProgressStoreTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp() => tempRoot = TestEnv.UseTempRoot();

        [TearDown]
        public void TearDown() => TestEnv.Teardown(tempRoot);

        [Test]
        public void SaveThenLoad_RoundTripsExactIndices()
        {
            var store = new ProgressStore();
            store.Advance("novel-a", 7);
            store.Advance("novel-b", 1234);
            store.Save();

            var reloaded = new ProgressStore();
            reloaded.Load();
            Assert.AreEqual(7, reloaded.Get("novel-a").GlobalLineIndex);
            Assert.AreEqual(1234, reloaded.Get("novel-b").GlobalLineIndex);
        }

        [Test]
        public void Load_NoFile_YieldsEmpty()
        {
            var store = new ProgressStore();
            store.Load();
            Assert.IsNull(store.Get("anything"));
        }

        [Test]
        public void Load_CorruptFile_YieldsEmptyWithoutThrow()
        {
            MoYuPaths.EnsureRoot();
            File.WriteAllText(MoYuPaths.ProgressFile, "{ not valid json !!!");

            var store = new ProgressStore();
            Assert.DoesNotThrow(store.Load);
            Assert.IsNull(store.Get("anything"));
        }

        [Test]
        public void ClearAll_PersistsRemoval()
        {
            var store = new ProgressStore();
            store.Advance("n", 5);
            store.ClearAll();

            var reloaded = new ProgressStore();
            reloaded.Load();
            Assert.IsNull(reloaded.Get("n"));
        }

        [Test]
        public void ClearOne_RemovesOnlyTarget()
        {
            var store = new ProgressStore();
            store.Advance("a", 1);
            store.Advance("b", 2);
            store.ClearOne("a");
            Assert.IsNull(store.Get("a"));
            Assert.IsNotNull(store.Get("b"));
        }

        [Test]
        public void MarkDirty_WithinThrottleWindow_DoesNotRewriteFile()
        {
            var store = new ProgressStore();
            store.Save();
            var before = File.ReadAllBytes(MoYuPaths.ProgressFile);

            store.Advance("n", 1);
            store.MarkDirty(); // 距上次保存不足 10s，应被节流

            CollectionAssert.AreEqual(before, File.ReadAllBytes(MoYuPaths.ProgressFile), "节流窗口内 MUST NOT 落盘");
            Assert.IsNotNull(store.Get("n"), "内存进度即时可见");
        }

        [Test]
        public void ProgressFile_IsHumanReadableJson()
        {
            var store = new ProgressStore();
            store.Advance("n", 3);
            store.Save();
            var json = File.ReadAllText(MoYuPaths.ProgressFile, Encoding.UTF8);
            StringAssert.Contains("novelId", json);
            StringAssert.Contains("globalLineIndex", json);
        }
    }
}
