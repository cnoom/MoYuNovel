using System;
using System.Collections.Generic;

namespace CNoom.MoYuNovel.Runtime.Novel
{
    /// <summary>
    /// 超长段落切行器（FR-012：单行输出长度有上限）。
    /// 切点优先落在句末标点（。！？等），其次句中停顿（，、等），均无则硬切。
    /// </summary>
    public static class LineSplitter
    {
        /// <summary>默认单行最大字符数（每个字符计 1，不区分全半角）。</summary>
        public const int DefaultMaxLineChars = 120;

        private const string PreferredBreakChars = "。！？!?；;…";
        private const string SecondaryBreakChars = "，,、：:—”』」）)\"";

        /// <summary>
        /// 将段落切分为不超过 <paramref name="maxChars"/> 的多行。
        /// 契约：任何返回行长度 ≤ maxChars；空段落返回空集；≤ maxChars 的段落原样返回。
        /// </summary>
        public static IReadOnlyList<string> Split(string paragraph, int maxChars)
        {
            if (string.IsNullOrEmpty(paragraph)) return Array.Empty<string>();
            if (maxChars <= 0) maxChars = DefaultMaxLineChars;
            if (paragraph.Length <= maxChars) return new[] { paragraph };

            var result = new List<string>();
            int start = 0;
            while (start < paragraph.Length)
            {
                int remaining = paragraph.Length - start;
                if (remaining <= maxChars)
                {
                    result.Add(paragraph.Substring(start));
                    break;
                }

                int cut = FindCut(paragraph, start, maxChars);
                result.Add(paragraph.Substring(start, cut - start));
                start = cut;
            }
            return result;
        }

        // 在 [start, start+maxChars) 窗口内自尾向首找切点；窗口中点之后才有资格，
        // 避免切出过短碎行；找不到任何标点则硬切满 maxChars。
        private static int FindCut(string text, int start, int maxChars)
        {
            int hardEnd = start + maxChars;
            int earliest = start + maxChars / 2;
            for (int i = hardEnd - 1; i > earliest; i--)
            {
                if (PreferredBreakChars.IndexOf(text[i]) >= 0) return i + 1;
            }
            for (int i = hardEnd - 1; i > earliest; i--)
            {
                if (SecondaryBreakChars.IndexOf(text[i]) >= 0) return i + 1;
            }
            return hardEnd;
        }
    }
}
