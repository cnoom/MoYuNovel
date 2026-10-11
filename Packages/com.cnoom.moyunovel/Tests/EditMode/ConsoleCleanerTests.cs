using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>
    /// Console 选择性清理测试（FR-002 修订，research R4-1/R5）：仅移除插件行、
    /// 业务日志内容与相对顺序保留、日志类型不串、降级不抛。
    /// Console 日志表为惰性刷入：断言前先轮询等条目落表，避免清空-落表竞态。
    /// </summary>
    public class ConsoleCleanerTests
    {
        [TearDown]
        public void TearDown() => Editor.Stop.ConsoleCleaner.ResetImplForTest();

        [Test]
        public void TryClear_RemovesOnlyPluginEntries_KeepsBusinessLogs()
        {
            Debug.Log("业务日志: 保留我 A");
            Debug.LogWarning("业务警告: 保留我 B");
            Debug.Log("[NetModule][nv] 小说第一行正文");
            Debug.Log("[LogReader] 书架为空，无法开始。");
            Assert.IsTrue(WaitForCount(count => count >= 4, 3000),
                "四条日志应在 3 秒内进入 Console 日志表（惰性刷入）");

            Assert.IsTrue(Editor.Stop.ConsoleCleaner.TryClear("nv"), "选择性清理在当前编辑器应可用");

            Assert.IsTrue(WaitForCount(count => count == 2, 3000),
                "急停后 MUST 仅剩两条业务日志（SC-001 本插件条目数为 0）");

            var remaining = ReadAllEntries();
            Assert.AreEqual(2, remaining.Count);
            Assert.IsTrue(remaining[0].message.Contains("保留我 A"), "业务日志内容保留且相对顺序不变");
            Assert.IsTrue(remaining[1].message.Contains("保留我 B"), "业务日志内容保留且相对顺序不变");
            Assert.AreNotEqual(remaining[0].mode, remaining[1].mode, "Log/Warning 类型 MUST 不串（mode 复制保真）");
        }

        [Test]
        public void IsPluginEntry_MatchesMarkerTokenAndSystemPrefix_Only()
        {
            Assert.IsTrue(Editor.Stop.ConsoleCleaner.IsPluginEntry("[NetModule][nv] 小说行", "nv"));
            Assert.IsTrue(Editor.Stop.ConsoleCleaner.IsPluginEntry("[LogReader] 书架为空", "nv"));
            Assert.IsFalse(Editor.Stop.ConsoleCleaner.IsPluginEntry("普通业务日志 env 变量", "nv"),
                "裸标记子串（如 nvme/env）不算插件行，仅方括号 token 匹配");
            Assert.IsFalse(Editor.Stop.ConsoleCleaner.IsPluginEntry(null, "nv"));
        }

        [Test]
        public void TryClear_SwallowsImplException_AndReturnsFalse()
        {
            Editor.Stop.ConsoleCleaner.RemoveImpl = _ => throw new InvalidOperationException("模拟清理能力失效");

            bool result = true;
            Assert.DoesNotThrow(() => result = Editor.Stop.ConsoleCleaner.TryClear("nv"),
                "清理失败 MUST 静默降级，不向外抛异常（FR-004）");
            Assert.IsFalse(result);
        }

        /// <summary>轮询 Console 条目数直至满足条件；反射不可用视为永不满足（测试降级失败并给出理由）。</summary>
        private static bool WaitForCount(Func<int, bool> predicate, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var entries = ReadAllEntries();
                if (entries.Count > 0 && predicate(entries.Count)) return true;
                if (entries.Count == 0 && predicate(0)) return true;
                Thread.Sleep(25);
            }
            return false;
        }

        /// <summary>反射读取全部条目的 (message, mode)；LogEntries/LogEntry 缺失时返回空表（测试降级）。</summary>
        private static List<(string message, int mode)> ReadAllEntries()
        {
            var result = new List<(string, int)>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type entriesType, entryType;
                try
                {
                    entriesType = assembly.GetType("UnityEditor.LogEntries", false);
                    if (entriesType == null) continue;
                    entryType = assembly.GetType("UnityEditor.LogEntry", false);
                }
                catch { continue; }
                if (entryType == null) continue;

                const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                const BindingFlags inst = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var getCount = entriesType.GetMethod("GetCount", all, null, Type.EmptyTypes, null);
                var getEntry = entriesType.GetMethod("GetEntryInternal", all, null, new[] { typeof(int), entryType }, null);
                var ctor = entryType.GetConstructor(Type.EmptyTypes);
                var messageField = entryType.GetField("message", inst);
                var modeField = entryType.GetField("mode", inst);
                if (getCount == null || getEntry == null || ctor == null || messageField == null || modeField == null)
                    return result;

                int count = (int)getCount.Invoke(null, null);
                for (int i = 0; i < count; i++)
                {
                    var entry = ctor.Invoke(null);
                    if (!(bool)getEntry.Invoke(null, new[] { (object)i, entry })) continue;
                    result.Add(((string)messageField.GetValue(entry), (int)modeField.GetValue(entry)));
                }
                return result;
            }
            return result;
        }
    }
}
