using System;
using System.Diagnostics;
using CNoom.MoYuNovel.Editor.Disguise;
using CNoom.MoYuNovel.Editor.Library;
using CNoom.MoYuNovel.Editor.Settings;
using CNoom.MoYuNovel.Editor.Storage;
using CNoom.MoYuNovel.Runtime.Novel;
using CNoom.MoYuNovel.Runtime.Output;
using CNoom.MoYuNovel.Runtime.Progress;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CNoom.MoYuNovel.Editor.Core
{
    /// <summary>
    /// 输出引擎：启停状态机 + <see cref="EditorApplication.update"/> 调度（FR-001/002/014）。
    /// 状态仅存内存、不持久化——编辑器重启/域重载后恒为 Idle，天然满足「默认静默」（FR-005/006，宪法 II）。
    /// </summary>
    [InitializeOnLoad]
    internal static class MoYuEngine
    {
        /// <summary>引擎状态（data-model 状态机）。</summary>
        internal enum EngineState
        {
            /// <summary>待机（初始/停止/急停后）。</summary>
            Idle,

            /// <summary>连载输出中。</summary>
            Running,

            /// <summary>已暂停。</summary>
            Paused,

            /// <summary>已读完（不自动循环）。</summary>
            Finished,
        }

        private const string FinishedHint = "【已完结】本书全部内容连载完毕，感谢阅读";

        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static EngineState state = EngineState.Idle;
        private static Novel currentNovel;
        private static int nextLineIndex;
        private static long lastOutputElapsedMs;
        private static OutputSettings settings;
        private static ProgressStore progressStore;
        private static bool initialized;

        /// <summary>输出行注入点（生产为 Debug.Log；测试替换以捕获断言）。</summary>
        internal static Action<string> OutputSink = line => Debug.Log(line);

        /// <summary>状态变化通知（急停服务据此挂/卸 Esc 拦截）。</summary>
        internal static event Action StateChanged;

        static MoYuEngine() => Initialize();

        private static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            MoYuPaths.EnsureRoot();
            settings = OutputSettings.Load();
            progressStore = new ProgressStore();
            progressStore.Load();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnQuitting;
        }

        /// <summary>当前状态。</summary>
        internal static EngineState State => state;

        /// <summary>当前连载中的作品（未开始时为 null）。</summary>
        internal static Novel CurrentNovel => currentNovel;

        /// <summary>下一待输出行号（0 基；仅供展示与测试）。</summary>
        internal static int NextLineIndex => nextLineIndex;

        /// <summary>上次输出时刻（Stopwatch 毫秒；测试用于推导到期时刻）。</summary>
        internal static long LastOutputElapsedMs => lastOutputElapsedMs;

        /// <summary>输出设置。</summary>
        internal static OutputSettings Settings => settings;

        /// <summary>进度存储。</summary>
        internal static ProgressStore Progress => progressStore;

        /// <summary>
        /// 按设置中的当前作品开始连载（US1/SC-001）；无记录回落书架首部，开箱即读。
        /// 续读：从上次已输出行的下一行开始（SC-003 偏差 0）。
        /// </summary>
        internal static void StartActive()
        {
            Initialize();
            var library = new NovelLibrary();
            var shelf = library.Enumerate();
            if (shelf.Count == 0)
            {
                Debug.LogWarning("[LogReader] 书架为空，无法开始。");
                return;
            }

            ShelfItem pick = null;
            var activeId = settings.ActiveNovelId;
            if (!string.IsNullOrEmpty(activeId))
            {
                foreach (var item in shelf)
                {
                    if (item.Id == activeId) { pick = item; break; }
                }
            }
            if (pick == null) pick = shelf[0];

            try
            {
                Start(library.Load(pick));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LogReader] 打开作品失败: " + e.Message);
            }
        }

        /// <summary>开始连载指定作品（状态机：任意状态 → Running；读完 → Finished）。</summary>
        internal static void Start(Novel novel)
        {
            Initialize();
            if (novel == null) throw new ArgumentNullException(nameof(novel));

            StopScheduling();
            currentNovel = novel;
            settings.SetActiveNovel(novel.Id);
            settings.Save();

            var last = progressStore.Get(novel.Id);
            if (last != null && last.GlobalLineIndex >= novel.TotalLineCount - 1)
            {
                // 已读到末行（或作品缩短后被钳到末尾）：不重播最后一行，直接进入完结态
                nextLineIndex = novel.TotalLineCount;
                state = EngineState.Finished;
                OutputLine(FinishedHint);
                RaiseStateChanged();
                return;
            }

            nextLineIndex = last == null ? 0 : last.GlobalLineIndex + 1;
            state = EngineState.Running;
            lastOutputElapsedMs = Clock.ElapsedMilliseconds - (long)(settings.IntervalSeconds * 1000f); // 开启即出首行
            EditorApplication.update += Tick;
            RaiseStateChanged();
        }

        /// <summary>暂停（进度落盘；状态机 Running → Paused）。</summary>
        internal static void Pause()
        {
            if (state != EngineState.Running) return;
            state = EngineState.Paused;
            StopScheduling();
            progressStore.Save();
            RaiseStateChanged();
        }

        /// <summary>恢复（从暂停位置的下一行继续，不跳行不重复）。</summary>
        internal static void Resume()
        {
            if (state != EngineState.Paused || currentNovel == null) return;
            state = EngineState.Running;
            lastOutputElapsedMs = Clock.ElapsedMilliseconds; // 恢复后先等一个间隔再出下一行
            EditorApplication.update += Tick;
            RaiseStateChanged();
        }

        /// <summary>停止连载（进度保留并落盘；状态 → Idle）。</summary>
        internal static void Stop()
        {
            if (state == EngineState.Running || state == EngineState.Paused) progressStore.Save();
            ResetToIdle();
        }

        /// <summary>急停（FR-002/SC-002）：立即终止一切待输出，不等待当前间隔到期。</summary>
        internal static void PanicStop()
        {
            var wasActive = state == EngineState.Running || state == EngineState.Paused;
            ResetToIdle();
            if (wasActive) progressStore.Save(); // 已输出行的进度仍有效，续读语义不变
        }

        private static void Tick() => TickAt(Clock.ElapsedMilliseconds);

        /// <summary>调度一步（测试入口：注入模拟时间轴，单位毫秒）。</summary>
        internal static void TickAt(long nowElapsedMs)
        {
            if (state != EngineState.Running || currentNovel == null) return;
            long due = lastOutputElapsedMs + (long)(settings.IntervalSeconds * 1000f);
            if (nowElapsedMs < due) return;
            lastOutputElapsedMs = nowElapsedMs;

            OutputLine(currentNovel.GetLine(nextLineIndex));
            progressStore.Advance(currentNovel.Id, nextLineIndex);
            progressStore.MarkDirty();
            nextLineIndex++;

            if (nextLineIndex >= currentNovel.TotalLineCount) EnterFinished();
        }

        private static void EnterFinished()
        {
            state = EngineState.Finished;
            StopScheduling();
            OutputLine(FinishedHint); // 完结提示同样带统一标记，可被过滤（FR-014）
            progressStore.Save();
            RaiseStateChanged();
        }

        private static void OutputLine(string text)
        {
            var preset = DisguisePresetLibrary.GetById(settings.DisguisePresetId);
            OutputSink(DisguiseFormatter.Format(preset, settings.MarkerText, text));
        }

        private static void ResetToIdle()
        {
            StopScheduling();
            currentNovel = null;
            nextLineIndex = 0;
            state = EngineState.Idle;
            RaiseStateChanged();
        }

        private static void StopScheduling() => EditorApplication.update -= Tick;

        private static void RaiseStateChanged() => StateChanged?.Invoke();

        // FR-006：进入/退出播放模式（含域重载回编辑态）一律静默停止，绝不自动恢复。
        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode || change == PlayModeStateChange.EnteredEditMode)
            {
                if (state == EngineState.Running || state == EngineState.Paused) Stop();
            }
        }

        private static void OnQuitting()
        {
            if (state == EngineState.Running || state == EngineState.Paused) progressStore.Save();
        }

        /// <summary>定位描述：章节 + 行位置（设置窗口展示用，纯函数）。</summary>
        internal static string DescribePosition(Novel novel, int lastLineIndex)
        {
            if (novel == null || novel.TotalLineCount == 0) return "（空）";
            int clamped = Math.Max(0, Math.Min(lastLineIndex, novel.TotalLineCount - 1));
            string chapter = NovelParser.SingleChapterTitle;
            foreach (var chapterRange in novel.Chapters)
            {
                if (clamped >= chapterRange.StartLine && clamped <= chapterRange.EndLine)
                {
                    chapter = chapterRange.Title;
                    break;
                }
            }
            if (chapter.Length > 24) chapter = chapter.Substring(0, 24) + "…";
            return chapter + " · 第 " + (clamped + 1) + "/" + novel.TotalLineCount + " 行";
        }

        /// <summary>注入播放模式变化（测试专用：模拟 FR-006 场景）。</summary>
        internal static void SimulatePlayModeChangeForTest(PlayModeStateChange change) => OnPlayModeStateChanged(change);

        /// <summary>重置为全新域状态（测试专用：模拟「刚重启的编辑器」）。</summary>
        internal static void ResetForTest()
        {
            StopScheduling();
            state = EngineState.Idle;
            currentNovel = null;
            nextLineIndex = 0;
            MoYuPaths.EnsureRoot();
            settings = OutputSettings.Load();
            progressStore = new ProgressStore();
            progressStore.Load();
            RaiseStateChanged();
        }
    }
}
