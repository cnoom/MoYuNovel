using System.Collections.Generic;
using System.Text;
using CNoom.MoYuNovel.Editor.Core;
using CNoom.MoYuNovel.Runtime.Novel;
using CNoom.MoYuNovel.Runtime.Output;
using NUnit.Framework;
using UnityEditor;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>
    /// MoYuEngine 状态机测试：默认静默（FR-005/006）、到点输出、暂停恢复、
    /// 急停即时（FR-002/SC-002）、读完即停不循环（FR-014）。
    /// </summary>
    public class MoYuEngineTests
    {
        private string tempRoot;
        private List<string> captured;

        [SetUp]
        public void SetUp()
        {
            tempRoot = TestEnv.UseTempRoot();
            captured = new List<string>();
            MoYuEngine.OutputSink = line => captured.Add(line);
            MoYuEngine.ResetForTest(); // 模拟「刚重启的编辑器」
        }

        [TearDown]
        public void TearDown()
        {
            MoYuEngine.PanicStop();
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

        private long DueAfterLastOutput()
        {
            long intervalMs = (long)(MoYuEngine.Settings.IntervalSeconds * 1000f);
            return MoYuEngine.LastOutputElapsedMs + intervalMs;
        }

        [Test]
        public void FreshEngine_IsIdle_AndTotallySilent_Fr005()
        {
            Assert.AreEqual(MoYuEngine.EngineState.Idle, MoYuEngine.State, "新引擎 MUST 默认静默");
            MoYuEngine.TickAt(long.MaxValue / 2);
            Assert.AreEqual(0, captured.Count, "未显式开启前 MUST 无任何输出");
        }

        [Test]
        public void Start_ThenIntervalDue_OutputsOneLine_AndAdvancesProgress()
        {
            var novel = MakeNovel(3);
            MoYuEngine.Start(novel);

            Assert.AreEqual(MoYuEngine.EngineState.Running, MoYuEngine.State);
            long before = DueAfterLastOutput();
            MoYuEngine.TickAt(before - 1);
            Assert.AreEqual(0, captured.Count, "未到间隔 MUST NOT 输出");

            MoYuEngine.TickAt(before);
            Assert.AreEqual(1, captured.Count);
            StringAssert.Contains("第1行", captured[0]);
            StringAssert.Contains("nv", captured[0], "输出行 MUST 携带统一标记（FR-004）");

            Assert.AreEqual(1, MoYuEngine.NextLineIndex, "续读指针指向下一行");
            Assert.AreEqual(0, MoYuEngine.Progress.Get(novel.Id).GlobalLineIndex, "进度=已输出最后一行");
        }

        [Test]
        public void Start_ActiveNovelDefaultsToBuiltin()
        {
            MoYuEngine.StartActive();
            Assert.AreEqual(MoYuEngine.EngineState.Running, MoYuEngine.State, "开箱即读：无配置时回落内置作品");
            Assert.AreEqual("builtin:journey-west", MoYuEngine.CurrentNovel.Id);
        }

        [Test]
        public void PauseResume_ContinuesAtNextLine_NoSkipNoRepeat()
        {
            var novel = MakeNovel(4);
            MoYuEngine.Start(novel);
            MoYuEngine.TickAt(DueAfterLastOutput()); // 输出第 1 行

            MoYuEngine.Pause();
            Assert.AreEqual(MoYuEngine.EngineState.Paused, MoYuEngine.State);
            MoYuEngine.TickAt(DueAfterLastOutput() + 1_000_000);
            Assert.AreEqual(1, captured.Count, "暂停后 MUST 停止输出");

            MoYuEngine.Resume();
            MoYuEngine.TickAt(DueAfterLastOutput());
            Assert.AreEqual(2, captured.Count);
            StringAssert.Contains("第2行", captured[1], "恢复 MUST 从暂停位置的下一行继续");
        }

        [Test]
        public void PanicStop_GoesIdleImmediately_AndKeepsProgress()
        {
            var novel = MakeNovel(5);
            MoYuEngine.Start(novel);
            MoYuEngine.TickAt(DueAfterLastOutput());
            MoYuEngine.TickAt(DueAfterLastOutput()); // 已输出 2 行

            int capturedBefore = captured.Count;
            MoYuEngine.PanicStop();

            Assert.AreEqual(MoYuEngine.EngineState.Idle, MoYuEngine.State, "急停 MUST 立即回 Idle（不等当前间隔）");
            MoYuEngine.TickAt(long.MaxValue / 2);
            Assert.AreEqual(capturedBefore, captured.Count, "急停后 MUST 零输出（SC-002）");
            Assert.AreEqual(1, MoYuEngine.Progress.Get(novel.Id).GlobalLineIndex, "已输出行的进度仍有效");
        }

        [Test]
        public void ReadingLastLine_EntersFinished_NoLoop_Fr014()
        {
            var novel = MakeNovel(1);
            MoYuEngine.Start(novel);
            MoYuEngine.TickAt(DueAfterLastOutput());

            Assert.AreEqual(MoYuEngine.EngineState.Finished, MoYuEngine.State);
            Assert.AreEqual(2, captured.Count, "最后一行 + 完结提示");
            MoYuEngine.TickAt(DueAfterLastOutput() + 1_000_000);
            Assert.AreEqual(2, captured.Count, "完结后 MUST NOT 自动循环");
        }

        [Test]
        public void RestartFinishedBook_ShowsHintOnly_NoReplay()
        {
            var novel = MakeNovel(1);
            MoYuEngine.Start(novel);
            MoYuEngine.TickAt(DueAfterLastOutput()); // 读完

            MoYuEngine.Start(novel);
            Assert.AreEqual(MoYuEngine.EngineState.Finished, MoYuEngine.State);
            Assert.AreEqual(3, captured.Count, "仅追加一条完结提示，不重播正文");
            MoYuEngine.TickAt(DueAfterLastOutput() + 1_000_000);
            Assert.AreEqual(3, captured.Count);
        }

        [Test]
        public void Start_ResumeProgressFromNextLine_Sc003()
        {
            var novel = MakeNovel(5);
            MoYuEngine.Start(novel);
            MoYuEngine.TickAt(DueAfterLastOutput());
            MoYuEngine.TickAt(DueAfterLastOutput()); // 输出到第 2 行
            MoYuEngine.Stop();

            // 模拟重启：重新装载后再次开始
            MoYuEngine.ResetForTest();
            MoYuEngine.Start(novel);

            MoYuEngine.TickAt(DueAfterLastOutput());
            StringAssert.Contains("第3行", captured[captured.Count - 1], "重启续读 MUST 从上次停止行的下一行开始（SC-003）");
        }

        [Test]
        public void Start_ProgressBeyondEnd_EntersFinishedWithoutReplay()
        {
            var novel = MakeNovel(2);
            MoYuEngine.Progress.Advance(novel.Id, 999); // 作品被替换变短的越界场景
            MoYuEngine.Start(novel);

            Assert.AreEqual(MoYuEngine.EngineState.Finished, MoYuEngine.State, "越界进度钳制到末尾 → 直接完结");
            Assert.AreEqual(1, captured.Count, "不重播正文");
        }

        [Test]
        public void PlayModeTransition_StopsSilently_NoAutoResume_Fr006()
        {
            var novel = MakeNovel(3);
            MoYuEngine.Start(novel);
            MoYuEngine.TickAt(DueAfterLastOutput());

            MoYuEngine.SimulatePlayModeChangeForTest(PlayModeStateChange.EnteredPlayMode);
            Assert.AreEqual(MoYuEngine.EngineState.Idle, MoYuEngine.State, "进入播放模式 MUST 静默停止");
            MoYuEngine.SimulatePlayModeChangeForTest(PlayModeStateChange.EnteredEditMode);
            Assert.AreEqual(MoYuEngine.EngineState.Idle, MoYuEngine.State, "回到编辑态 MUST 保持静默，不自动恢复");
            MoYuEngine.TickAt(long.MaxValue / 2);
            Assert.AreEqual(1, captured.Count);
        }

        [Test]
        public void IntervalChange_TakesEffectOnNextLine_Fr003()
        {
            var novel = MakeNovel(2);
            MoYuEngine.Start(novel);
            MoYuEngine.Settings.SetInterval(0.2f); // 热更新间隔

            long dueAt020 = MoYuEngine.LastOutputElapsedMs + 200;
            MoYuEngine.TickAt(dueAt020 - 1);
            Assert.AreEqual(0, captured.Count);
            MoYuEngine.TickAt(dueAt020);
            Assert.AreEqual(1, captured.Count, "新间隔 MUST 对后续行立即生效");
        }

        [Test]
        public void EveryOutputLine_CarriesMarker_Sc005()
        {
            var novel = MakeNovel(6);
            MoYuEngine.Start(novel);
            for (int i = 0; i < 6; i++) MoYuEngine.TickAt(DueAfterLastOutput());

            Assert.AreEqual(7, captured.Count); // 6 行 + 完结提示
            foreach (var line in captured)
            {
                StringAssert.Contains("nv", line, "任何输出行（含完结提示）MUST 可过滤（SC-005）");
            }
        }
    }
}
