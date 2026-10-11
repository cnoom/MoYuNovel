using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CNoom.MoYuNovel.Editor.Stop
{
    /// <summary>
    /// Console 选择性清理器（FR-002 修订，research R5）：急停时仅移除本插件
    /// 产生的条目——携带 "[<marker>]" 标记 token 的连载行/完结提示与
    /// "<see cref="LogPrefix"/>" 前缀的系统日志；业务日志内容与相对顺序原样保留。
    /// 机制：反射快照（GetEntryInternal）→ Clear() 清表 → 回灌保留条目。
    /// 任一 API 解析失败或 Console 处于过滤态时静默跳过，绝不回退为全量清空
    /// 误删业务日志；全程不弹窗、不写任何日志（FR-004，错误输出本身会暴露）。
    /// RemoveImpl 为测试注入点。
    /// </summary>
    internal static class ConsoleCleaner
    {
        /// <summary>插件系统日志前缀（引擎内部提示行同用此前缀）。</summary>
        internal const string LogPrefix = "[LogReader]";

        // 反射解析失败只探一次（Unity 版本变动时避免每次急停重复全程序集扫描）
        private static bool reflectionUnavailable;

        private static MethodInfo getCountMethod, getEntryMethod, clearMethod, addMethod,
            startMethod, endMethod, getFilterMethod;
        private static ConstructorInfo entryCtor;
        private static FieldInfo messageField, modeField, fileField, lineField, columnField,
            instanceIdField, identifierField, globalLineIndexField, callstackUtf8Field, callstackUtf16Field;

        internal static Func<string, bool> RemoveImpl = RemoveOwnEntries;

        /// <summary>尽力移除本插件条目；<paramref name="marker"/> 为统一过滤标记原文（如 "nv"）。吞掉一切异常，失败返回 false（FR-004）。</summary>
        internal static bool TryClear(string marker)
        {
            try
            {
                return RemoveImpl(marker);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>还原默认实现（测试善后）。</summary>
        internal static void ResetImplForTest() => RemoveImpl = RemoveOwnEntries;

        private static bool RemoveOwnEntries(string marker)
        {
            if (reflectionUnavailable) return false;
            try
            {
                if (getCountMethod == null && !ResolveReflection())
                {
                    reflectionUnavailable = true;
                    return false;
                }

                // 过滤态下 GetCount/GetEntryInternal 针对过滤视图，重建会丢被隐藏条目：跳过（显示层本已过滤）
                var filter = (string)getFilterMethod.Invoke(null, null);
                if (!string.IsNullOrEmpty(filter)) return false;

                int count = (int)getCountMethod.Invoke(null, null);
                var kept = new List<object>(count);
                startMethod.Invoke(null, null);
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        var entry = entryCtor.Invoke(null);
                        if (!(bool)getEntryMethod.Invoke(null, new[] { (object)i, entry })) continue;
                        var message = (string)messageField.GetValue(entry);
                        if (IsPluginEntry(message, marker)) continue;

                        // callstack 偏移指向原生缓冲，Clear 后失效，回灌前必须清零（research R5）
                        callstackUtf8Field.SetValue(entry, 0);
                        callstackUtf16Field.SetValue(entry, 0);
                        kept.Add(entry);
                    }
                }
                finally
                {
                    endMethod.Invoke(null, null);
                }

                if (kept.Count == count) return true; // 无插件条目：不清表，保留条目的堆栈/时间戳不受扰动
                clearMethod.Invoke(null, null);
                foreach (var entry in kept) addMethod.Invoke(null, new[] { entry });
                return true;
            }
            catch
            {
                reflectionUnavailable = true;
                return false;
            }
        }

        /// <summary>插件条目判定：统一标记 token（如 "[nv]"）或系统日志前缀。</summary>
        internal static bool IsPluginEntry(string message, string marker)
        {
            if (string.IsNullOrEmpty(message)) return false;
            if (message.StartsWith(LogPrefix, StringComparison.Ordinal)) return true;
            return !string.IsNullOrEmpty(marker) && message.Contains("[" + marker + "]");
        }

        private static bool ResolveReflection()
        {
            var entriesType = FindLoadedType("UnityEditor.LogEntries");
            var entryType = FindLoadedType("UnityEditor.LogEntry");
            if (entriesType == null || entryType == null) return false;

            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            getCountMethod = entriesType.GetMethod("GetCount", all, null, Type.EmptyTypes, null);
            startMethod = entriesType.GetMethod("StartGettingEntries", all, null, Type.EmptyTypes, null);
            endMethod = entriesType.GetMethod("EndGettingEntries", all, null, Type.EmptyTypes, null);
            clearMethod = entriesType.GetMethod("Clear", all, null, Type.EmptyTypes, null);
            getFilterMethod = entriesType.GetMethod("GetFilteringText", all, null, Type.EmptyTypes, null);
            getEntryMethod = entriesType.GetMethod("GetEntryInternal", all, null, new[] { typeof(int), entryType }, null);
            addMethod = entriesType.GetMethod("AddMessageWithDoubleClickCallback", all, null, new[] { entryType }, null);
            entryCtor = entryType.GetConstructor(Type.EmptyTypes);

            const BindingFlags inst = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            messageField = entryType.GetField("message", inst);
            modeField = entryType.GetField("mode", inst);
            fileField = entryType.GetField("file", inst);
            lineField = entryType.GetField("line", inst);
            columnField = entryType.GetField("column", inst);
            instanceIdField = entryType.GetField("instanceID", inst);
            identifierField = entryType.GetField("identifier", inst);
            globalLineIndexField = entryType.GetField("globalLineIndex", inst);
            callstackUtf8Field = entryType.GetField("callstackTextStartUTF8", inst);
            callstackUtf16Field = entryType.GetField("callstackTextStartUTF16", inst);

            return getCountMethod != null && startMethod != null && endMethod != null && clearMethod != null
                && getFilterMethod != null && getEntryMethod != null && addMethod != null && entryCtor != null
                && messageField != null && modeField != null && fileField != null && lineField != null
                && columnField != null && instanceIdField != null && identifierField != null
                && globalLineIndexField != null && callstackUtf8Field != null && callstackUtf16Field != null;
        }

        private static Type FindLoadedType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName, false);
                    if (type != null) return type;
                }
                catch
                {
                    // 个别动态程序集查询可能抛异常：跳过继续找
                }
            }
            return null;
        }
    }
}
