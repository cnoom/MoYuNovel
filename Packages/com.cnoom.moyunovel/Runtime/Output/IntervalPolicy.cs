namespace CNoom.MoYuNovel.Runtime.Output
{
    /// <summary>输出间隔策略：默认 1 秒、下限 0.2 秒、上限 1 小时（FR-003）。</summary>
    public static class IntervalPolicy
    {
        /// <summary>默认间隔（秒）。</summary>
        public const float DefaultSeconds = 1f;

        /// <summary>间隔下限（秒）——仅防 Console 渲染卡顿，阅读节奏由用户自调。</summary>
        public const float MinSeconds = 0.2f;

        /// <summary>间隔上限（秒）。</summary>
        public const float MaxSeconds = 3600f;

        /// <summary>
        /// 钳制到合法区间。契约：NaN/负值/过小 → 下限；过大 → 上限。
        /// </summary>
        public static float Clamp(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < MinSeconds) return MinSeconds;
            if (seconds > MaxSeconds) return MaxSeconds;
            return seconds;
        }
    }
}
