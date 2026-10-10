using System;
using System.Reflection;
using CNoom.MoYuNovel.Editor.Core;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace CNoom.MoYuNovel.Editor.Stop
{
    /// <summary>
    /// 急停服务（FR-002，research R1 双层设计）：
    /// ① 主急停键走 ShortcutManager（默认 Ctrl+Shift+F12，用户可在 Unity Shortcuts 窗口改键）；
    /// ② Esc 尽力拦截：输出运行期间反射挂接编辑器全局事件，失败静默降级（主键兜底）。
    /// 拦截不消费事件（不调用 Use），绝不破坏编辑器原生 Esc 行为（spec 边界用例）。
    /// </summary>
    [InitializeOnLoad]
    internal static class PanicStopService
    {
        private const string HandlerFieldName = "globalEventHandler";
        private static FieldInfo handlerField;
        private static Delegate hookedHandler;

        static PanicStopService()
        {
            MoYuEngine.StateChanged += OnStateChanged;
            var state = MoYuEngine.State;
            if (state == MoYuEngine.EngineState.Running || state == MoYuEngine.EngineState.Paused) HookEscape();
        }

        /// <summary>主急停键（ShortcutManager 注册项：Log Reader/Panic Stop）。</summary>
        [Shortcut("Log Reader/Panic Stop", KeyCode.F12, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        internal static void PanicByShortcut() => MoYuEngine.PanicStop();

        private static void OnStateChanged()
        {
            var state = MoYuEngine.State;
            if (state == MoYuEngine.EngineState.Running || state == MoYuEngine.EngineState.Paused) HookEscape();
            else UnhookEscape();
        }

        /// <summary>挂接 Esc 拦截（幂等；任何失败静默降级）。</summary>
        internal static void HookEscape()
        {
            if (hookedHandler != null) return;
            try
            {
                if (handlerField == null)
                {
                    handlerField = typeof(EditorApplication).GetField(HandlerFieldName, BindingFlags.Static | BindingFlags.NonPublic);
                }
                if (handlerField == null) return; // Unity 版本变动：静默降级，主急停键兜底

                var delegateType = handlerField.FieldType;
                var invoke = delegateType.GetMethod("Invoke");
                if (invoke == null) return;

                var target = invoke.GetParameters().Length == 0
                    ? typeof(PanicStopService).GetMethod(nameof(EscHandlerNoArg), BindingFlags.NonPublic | BindingFlags.Static)
                    : typeof(PanicStopService).GetMethod(nameof(EscHandlerWithEvent), BindingFlags.NonPublic | BindingFlags.Static);

                var created = Delegate.CreateDelegate(delegateType, target);
                var current = handlerField.GetValue(null) as Delegate;
                handlerField.SetValue(null, Delegate.Combine(current, created));
                hookedHandler = created;
            }
            catch
            {
                hookedHandler = null;
            }
        }

        /// <summary>卸载 Esc 拦截（幂等）。</summary>
        internal static void UnhookEscape()
        {
            if (hookedHandler == null) return;
            try
            {
                var current = handlerField?.GetValue(null) as Delegate;
                handlerField?.SetValue(null, Delegate.Remove(current, hookedHandler));
            }
            catch
            {
                // 反射清理失败不影响主流程
            }
            hookedHandler = null;
        }

        // globalEventHandler 存在两种历史签名：无参（经 Event.current 取事件）与带 Event 参数
        private static void EscHandlerNoArg() => HandleKeyEvent(Event.current);

        private static void EscHandlerWithEvent(Event evt) => HandleKeyEvent(evt);

        private static void HandleKeyEvent(Event evt)
        {
            if (evt == null || evt.type != EventType.KeyDown || evt.keyCode != KeyCode.Escape) return;
            var state = MoYuEngine.State;
            if (state != MoYuEngine.EngineState.Running && state != MoYuEngine.EngineState.Paused) return;
            MoYuEngine.PanicStop();
            // 有意不调用 evt.Use()：不消费事件，编辑器原生 Esc 行为（取消重命名、关弹窗等）不受影响
        }
    }
}
