using CNoom.MoYuNovel.Editor.Settings;
using UnityEditor;

namespace CNoom.MoYuNovel.Editor.Core
{
    /// <summary>
    /// 菜单入口（FR-001/FR-002/FR-016）：启停控制 + 设置窗口。
    /// 用户可见命名统一为中性「Log Reader」，不暴露摸鱼语义（contracts/editor-ui.md）。
    /// </summary>
    internal static class MoYuMenu
    {
        private const string Root = "Tools/Log Reader/";

        [MenuItem(Root + "Start", false, 1000)]
        internal static void StartMenu() => MoYuEngine.StartActive();

        [MenuItem(Root + "Start", true)]
        internal static bool StartValidate()
            => MoYuEngine.State == MoYuEngine.EngineState.Idle || MoYuEngine.State == MoYuEngine.EngineState.Finished;

        [MenuItem(Root + "Pause", false, 1001)]
        internal static void PauseMenu() => MoYuEngine.Pause();

        [MenuItem(Root + "Pause", true)]
        internal static bool PauseValidate() => MoYuEngine.State == MoYuEngine.EngineState.Running;

        [MenuItem(Root + "Resume", false, 1002)]
        internal static void ResumeMenu() => MoYuEngine.Resume();

        [MenuItem(Root + "Resume", true)]
        internal static bool ResumeValidate() => MoYuEngine.State == MoYuEngine.EngineState.Paused;

        [MenuItem(Root + "Stop", false, 1003)]
        internal static void StopMenu() => MoYuEngine.Stop();

        [MenuItem(Root + "Stop", true)]
        internal static bool StopValidate()
            => MoYuEngine.State == MoYuEngine.EngineState.Running || MoYuEngine.State == MoYuEngine.EngineState.Paused;

        // 急停不带菜单快捷键串——快捷键由 ShortcutManager 注册（见 PanicStopService），避免双重绑定
        [MenuItem(Root + "Panic Stop", false, 1004)]
        internal static void PanicStopMenu() => MoYuEngine.PanicStop();

        [MenuItem(Root + "Settings...", false, 1005)]
        internal static void SettingsMenu() => MoYuSettingsWindow.Open();
    }
}
