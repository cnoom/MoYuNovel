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
    /// 断言全部基于语义（条目内容/顺序/类型），不对 Console 总条数做精确断言——
    /// batchmode 下测试框架自身日志会随时插入，总数天然不可预测。
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

            // 等我们的四条全部落表（避免清理后晚刷入造成误判），不关心框架杂条
            Assert.IsTrue(WaitUntil(entries => Contains(entries, "保留我 A")
                && Contains(entries, "保留我 B")
                && Contains(entries, "小说第一行正文")
                && Contains(entries, "书架为空，无法开始。"), 3000), "四条测试日志应落表");

            Assert.IsTrue(Editor.Stop.ConsoleCleaner.TryClear("nv"), "选择性清理在当前编辑器应可用");

            // 清理后：业务两条保留、插件条目为 0；框架日志可能继续插入，均不参与断言
            Assert.IsTrue(WaitUntil(entries => Contains(entries, "保留我 A")
                && Contains(entries, "保留我 B")
                && !HasPluginEntry(entries), 3000),
                "急停后业务日志 MUST 保留且本插件条目数为 0（SC-001/SC-002）");

            var remaining = ReadAllEntries();
            int indexA = remaining.FindIndex(e => e.message.Contains("保留我 A"));
            int indexB = remaining.FindIndex(e => e.message.Contains("保留我 B"));
            Assert.GreaterOrEqual(indexA, 0);
            Assert.GreaterOrEqual(indexB, 0);
            Assert.Less(indexA, indexB, "业务日志相对顺序 MUST 不变");
            Assert.AreNotEqual(remaining[indexA].mode, remaining[indexB].mode,
                "Log/Warning 类型 MUST 不串（mode 复制保真）");
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

        private static bool Contains(List<(string message, int mode)> entries, string text)
            => entries.FindIndex(e => e.message != null && e.message.Contains(text)) >= 0;

        private static bool HasPluginEntry(List<(string message, int mode)> entries)
            => entries.FindIndex(e => e.message != null
                && (e.message.Contains("[nv]") || e.message.StartsWith("[LogReader]", StringComparison.Ordinal))) >= 0;

        /// <summary>轮询条目表直至条件满足；超时返回 false（调用方转储诊断）。</summary>
        private static bool WaitUntil(Func<List<(string message, int mode)>, bool> predicate, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (predicate(ReadAllEntries())) return true;
                Thread.Sleep(50);
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
