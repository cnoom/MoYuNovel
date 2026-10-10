using System;

namespace CNoom.MoYuNovel.Runtime.Output
{
    /// <summary>
    /// 伪装行格式化纯函数（FR-004）。
    /// 硬保证（SC-005 零漏标）：任何返回结果必然包含统一过滤标记。
    /// </summary>
    public static class DisguiseFormatter
    {
        /// <summary>默认统一过滤标记。</summary>
        public const string DefaultMarker = "nv";

        /// <summary>
        /// 按预设模板格式化一行。契约：结果必含 <paramref name="marker"/> 子串
        /// （模板缺 <c>{marker}</c> 占位时强制尾置）；正文原样保留，不转义不截断。
        /// </summary>
        public static string Format(DisguisePreset preset, string marker, string text)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));
            if (string.IsNullOrWhiteSpace(marker)) marker = DefaultMarker;
            if (text == null) text = string.Empty;

            string template = preset.PrefixTemplate;
            string line = template.Contains("{text}")
                ? template.Replace("{marker}", marker).Replace("{text}", text)
                : template.Replace("{marker}", marker) + " " + text;

            if (!template.Contains("{marker}")) line = line + " [" + marker + "]";
            return line;
        }
    }
}
