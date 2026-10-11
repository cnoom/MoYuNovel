using System.IO;
using System.Text;
using CNoom.MoYuNovel.Editor.Core;
using CNoom.MoYuNovel.Editor.Settings;
using CNoom.MoYuNovel.Editor.Stop;
using CNoom.MoYuNovel.Editor.Storage;
using CNoom.MoYuNovel.Runtime.Novel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>
    /// 急停清场接线测试（US1，research R4-2/R4-3）：急停按开关触发清空、
    /// 非急停路径永不触发、进度先于清空落盘、开关默认值与持久化。
    /// </summary>
    public class PanicClearWiringTests
    {
        private string tempRoot;
        private int clearCalls;

        [SetUp]
        public void SetUp()
        {
            tempRoot = TestEnv.UseTempRoot();
            MoYuEngine.ResetForTest(); // 模拟「刚重启的编辑器」，设置回落默认
            clearCalls = 0;
            ConsoleCleaner.RemoveImpl = _ => { clearCalls++; return true; };
        }

        [TearDown]
        public void TearDown()
        {
            ConsoleCleaner.ResetImplForTest();
            MoYuEngine.OutputSink = null;
            TestEnv.Teardown(tempRoot);
            MoYuEngine.ResetForTest(); // 归还生产根目录
        }

        private static Novel MakeNovel(int lineCount)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < lineCount; i++)
            {
                sb.Append("第").Append(i + 1).Append("行：摸鱼正文内容，观感同业务日志。");
                if (i < lineCount - 1) sb.AppendLine();
            }
            return NovelParser.Parse("test:novel", "测试作品", NovelSource.Builtin, sb.ToString());
        }

        private void StartRunning(int lineCount)
        {
            MoYuEngine.OutputSink = _ => { }; // 静音真实输出，测试只关心清空调用
            MoYuEngine.Start(MakeNovel(lineCount));
        }

        private long DueAfterLastOutput()
        {
            long intervalMs = (long)(MoYuEngine.Settings.IntervalSeconds * 1000f);
            return MoYuEngine.LastOutputElapsedMs + intervalMs;
        }

        [Test]
        public void PanicStop_TriggersClearByDefault()
        {
            MoYuEngine.PanicStop(); // 引擎空闲态触发也必须清场（FR-006）
            Assert.AreEqual(1, clearCalls, "默认开关开启时急停 MUST 触发一次清空");
            Assert.AreEqual(MoYuEngine.EngineState.Idle, MoYuEngine.State);
        }

        [Test]
        public void PanicStop_SkipsClearWhenToggleOff()
        {
            MoYuEngine.Settings.SetClearConsoleOnPanic(false);
            MoYuEngine.PanicStop();
            Assert.AreEqual(0, clearCalls, "开关关闭时急停仅停止输出、不清空（FR-007）");
        }

        [Test]
        public void Stop_Pause_Finish_PlayMode_NeverClear()
        {
            StartRunning(1);
            MoYuEngine.Stop();
            Assert.AreEqual(0, clearCalls, "普通停止 MUST NOT 清空（FR-003）");

            StartRunning(2);
            MoYuEngine.Pause();
            Assert.AreEqual(0, clearCalls, "暂停 MUST NOT 清空（FR-003）");

            MoYuEngine.Resume();
            MoYuEngine.TickAt(DueAfterLastOutput());        // 输出第 0 行
            MoYuEngine.TickAt(DueAfterLastOutput() + 500);  // 输出第 1 行 → 读完进入完结
            Assert.AreEqual(MoYuEngine.EngineState.Finished, MoYuEngine.State);
            Assert.AreEqual(0, clearCalls, "自然完结 MUST NOT 清空（FR-003）");

            StartRunning(2);
            MoYuEngine.SimulatePlayModeChangeForTest(PlayModeStateChange.EnteredEditMode);
            Assert.AreEqual(0, clearCalls, "播放模式静默停机 MUST NOT 清空（FR-003）");
        }

        [Test]
        public void PanicStop_SavesProgressBeforeClear()
        {
            StartRunning(5);
            MoYuEngine.TickAt(DueAfterLastOutput());          // 输出第 0 行
            MoYuEngine.TickAt(DueAfterLastOutput() + 500);    // 输出第 1 行

            MoYuEngine.PanicStop();

            var progress = MoYuEngine.Progress.Get("test:novel");
            Assert.IsNotNull(progress);
            Assert.AreEqual(1, progress.GlobalLineIndex, "急停后进度停在最后已输出行（SC-005 续读偏差 0）");
            Assert.AreEqual(1, clearCalls, "清空在进度落盘之后执行（research R2 顺序）");
        }

        [Test]
        public void ClearConsoleOnPanic_DefaultsTrue()
        {
            Assert.IsTrue(MoYuEngine.Settings.ClearConsoleOnPanic, "开关默认开启（FR-007）");
        }

        [Test]
        public void ClearConsoleOnPanic_RoundTripsThroughSaveLoad()
        {
            MoYuEngine.Settings.SetClearConsoleOnPanic(false);
            MoYuEngine.Settings.Save();

            var reloaded = OutputSettings.Load();
            Assert.IsFalse(reloaded.ClearConsoleOnPanic, "关闭状态 MUST 持久化（SC-006）");
        }

        [Test]
        public void ClearConsoleOnPanic_MissingFieldInOldJson_YieldsTrue()
        {
            // 001 时代的旧 settings.json（无 clearConsoleOnPanic 字段）：升级用户默认获得清场
            MoYuPaths.EnsureRoot();
            File.WriteAllText(MoYuPaths.SettingsFile,
                "{\"intervalSeconds\":1,\"activeNovelId\":\"\",\"disguisePresetId\":\"net\",\"markerText\":\"nv\"}");

            var loaded = OutputSettings.Load();
            Assert.IsTrue(loaded.ClearConsoleOnPanic, "缺字段（旧版本 JSON）MUST 视为 true（data-model 约束）");
        }
    }
}
