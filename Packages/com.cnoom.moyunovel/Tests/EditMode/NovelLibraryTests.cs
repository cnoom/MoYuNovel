using System;
using System.IO;
using System.Text;
using CNoom.MoYuNovel.Editor.Library;
using CNoom.MoYuNovel.Runtime.Novel;
using NUnit.Framework;
using UnityEngine;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>NovelLibrary 契约测试（FR-008 内置 / FR-009 导入、编码、幂等、空文件拒绝）。</summary>
    public class NovelLibraryTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp() => tempRoot = TestEnv.UseTempRoot();

        [TearDown]
        public void TearDown() => TestEnv.Teardown(tempRoot);

        private string WriteSourceFile(string name, byte[] bytes)
        {
            var path = Path.Combine(tempRoot, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        [Test]
        public void Enumerate_ContainsBuiltinJourneyWest_Fr008()
        {
            var shelf = new NovelLibrary().Enumerate();
            int index = shelf.FindIndex(item => item.Id == "builtin:journey-west");
            Assert.GreaterOrEqual(index, 0, "开箱 MUST 含内置示例小说");
            Assert.AreEqual(NovelSource.Builtin, shelf[index].Source);
        }

        [Test]
        public void LoadBuiltin_StartsWithChapterTitle_AndHasLines()
        {
            var novel = new NovelLibrary().LoadById("builtin:journey-west");
            Assert.Greater(novel.TotalLineCount, 10);
            StringAssert.StartsWith("第一回", novel.GetLine(0));
            Assert.GreaterOrEqual(novel.Chapters.Count, 1);
        }

        [Test]
        public void Import_Utf8Text_ParsesAndShelved()
        {
            var content = new StringBuilder();
            content.AppendLine("第一章 开始");
            content.Append("这是导入的一段正文。");
            for (int i = 0; i < 30; i++) content.Append("续写内容第").Append(i).Append("段，保证触发超长切行逻辑。");

            var path = WriteSourceFile("我的小说.txt", Encoding.UTF8.GetBytes(content.ToString()));
            var outcome = new NovelLibrary().Import(path);

            Assert.IsTrue(outcome.Ok, outcome.Message);
            var shelf = new NovelLibrary().Enumerate();
            Assert.IsTrue(shelf.Exists(item => item.Id == outcome.Novel.Id), "导入后 MUST 出现在书架");

            var loaded = new NovelLibrary().LoadById(outcome.Novel.Id);
            Assert.AreEqual("我的小说", loaded.Title);
            Assert.Greater(loaded.TotalLineCount, 1);
        }

        [Test]
        public void Import_GbkText_DecodesCorrectly()
        {
            Encoding gbk;
            try { gbk = Encoding.GetEncoding(936); }
            catch (ArgumentException)
            {
                Assert.Ignore("当前环境缺少 GBK 解码支持（I18N 未加载），跳过。");
                return;
            }

            var path = WriteSourceFile("gbk书.txt", gbk.GetBytes("第二章 摸鱼时刻\n老板走过去了，继续输出正文。"));
            var outcome = new NovelLibrary().Import(path);

            Assert.IsTrue(outcome.Ok, outcome.Message);
            var loaded = new NovelLibrary().LoadById(outcome.Novel.Id);
            StringAssert.Contains("摸鱼时刻", loaded.GetLine(0), "GBK 中文 MUST 正确解码");
        }

        [Test]
        public void Import_EmptyFile_Rejected_ShelfUnchanged()
        {
            var shelfBefore = new NovelLibrary().Enumerate().Count;
            var path = WriteSourceFile("空.txt", new byte[0]);

            var outcome = new NovelLibrary().Import(path);

            Assert.IsFalse(outcome.Ok);
            StringAssert.Contains("为空", outcome.Message);
            Assert.AreEqual(shelfBefore, new NovelLibrary().Enumerate().Count, "拒绝导入 MUST NOT 影响书架");
        }

        [Test]
        public void Import_UnidentifiableEncoding_Rejected()
        {
            var shelfBefore = new NovelLibrary().Enumerate().Count;
            var path = WriteSourceFile("乱码.txt", new byte[] { 0x81, 0x00, 0x81, 0x00 });

            var outcome = new NovelLibrary().Import(path);

            Assert.IsFalse(outcome.Ok);
            Assert.AreEqual(shelfBefore, new NovelLibrary().Enumerate().Count);
        }

        [Test]
        public void Import_SameContentTwice_Idempotent()
        {
            var path = WriteSourceFile("重复.txt", Encoding.UTF8.GetBytes("唯一正文内容"));
            var library = new NovelLibrary();

            var first = library.Import(path);
            var second = library.Import(path);

            Assert.IsTrue(first.Ok && second.Ok);
            Assert.AreEqual(first.Novel.Id, second.Novel.Id, "同内容重复导入 MUST 幂等");
            Assert.AreEqual(1, library.Enumerate().FindAll(item => item.Id == first.Novel.Id).Count);
        }

        [Test]
        public void Import_SameTitleDifferentContent_DoesNotOverwrite()
        {
            var library = new NovelLibrary();
            // 同名内容需来自不同目录（源文件同名会互相覆盖，测不出「同名不同内容」）
            var dirA = Path.Combine(tempRoot, "a");
            var dirB = Path.Combine(tempRoot, "b");
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);
            var pathA = Path.Combine(dirA, "同名.txt");
            var pathB = Path.Combine(dirB, "同名.txt");
            File.WriteAllBytes(pathA, Encoding.UTF8.GetBytes("A 版本正文"));
            File.WriteAllBytes(pathB, Encoding.UTF8.GetBytes("B 版本完全不同的正文"));

            var first = library.Import(pathA);
            var second = library.Import(pathB);

            Assert.IsTrue(first.Ok && second.Ok);
            Assert.AreNotEqual(first.Novel.Id, second.Novel.Id, "同名不同内容 MUST NOT 相互覆盖");
            Assert.AreEqual(2, library.Enumerate().FindAll(item => item.Source == NovelSource.Imported).Count);
        }

        [Test]
        public void Load_MissingImportFile_Throws()
        {
            Assert.Throws<FileNotFoundException>(() => new NovelLibrary().LoadById("import:不存在"));
        }
    }
}
