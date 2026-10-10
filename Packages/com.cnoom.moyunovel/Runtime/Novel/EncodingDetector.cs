namespace CNoom.MoYuNovel.Runtime.Novel
{
    /// <summary>编码探测结果。</summary>
    public enum DetectedEncoding
    {
        /// <summary>UTF-8（含 BOM 或逐字节校验通过）。</summary>
        Utf8,

        /// <summary>UTF-16 小端（LE BOM）。</summary>
        Utf16Le,

        /// <summary>UTF-16 大端（BE BOM）。</summary>
        Utf16Be,

        /// <summary>GBK/GB2312 系（双字节结构启发式命中）。</summary>
        Gbk,

        /// <summary>无法识别（调用方应提示用户转存 UTF-8）。</summary>
        Unknown,
    }

    /// <summary>
    /// 文本编码探测器（纯字节探测，不做解码，FR-009）。
    /// 优先级：BOM → UTF-8 逐字节合法性 → GBK 双字节结构启发式 → Unknown。
    /// </summary>
    public static class EncodingDetector
    {
        /// <summary>探测字节流的文本编码。</summary>
        public static DetectedEncoding Detect(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return DetectedEncoding.Unknown;

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return DetectedEncoding.Utf8;
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return DetectedEncoding.Utf16Le;
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return DetectedEncoding.Utf16Be;

            if (LooksLikeValidUtf8(bytes)) return DetectedEncoding.Utf8;
            if (LooksLikeGbk(bytes)) return DetectedEncoding.Gbk;
            return DetectedEncoding.Unknown;
        }

        // 严格 UTF-8 校验：多字节序列必须完整、续字节 10xxxxxx、禁 C0/C1 过短头。
        private static bool LooksLikeValidUtf8(byte[] b)
        {
            int i = 0;
            while (i < b.Length)
            {
                int first = b[i];
                if (first <= 0x7F) { i++; continue; }

                int len;
                if ((first & 0xE0) == 0xC0) { len = 2; if (first < 0xC2) return false; }
                else if ((first & 0xF0) == 0xE0) len = 3;
                else if ((first & 0xF8) == 0xF0) len = 4;
                else return false;

                if (i + len > b.Length) return false;
                for (int j = 1; j < len; j++)
                {
                    if ((b[i + j] & 0xC0) != 0x80) return false;
                }
                i += len;
            }
            return true;
        }

        // GBK 结构启发式：所有高位字节必须构成合法双字节对（首 0x81-0xFE，
        // 尾 0x40-0xFE 且非 0x7F，兼容 GB18030 四字节区的 0x30-039 尾）。
        private static bool LooksLikeGbk(byte[] b)
        {
            int i = 0;
            int doubleByteCount = 0;
            while (i < b.Length)
            {
                if (b[i] <= 0x7F) { i++; continue; }
                if (b[i] < 0x81 || b[i] > 0xFE) return false;
                if (i + 1 >= b.Length) return false;

                byte trail = b[i + 1];
                bool trailDouble = trail >= 0x40 && trail <= 0xFE && trail != 0x7F;
                bool trail4Byte = trail >= 0x30 && trail <= 0x39;
                if (!trailDouble && !trail4Byte) return false;

                doubleByteCount++;
                i += 2;
            }
            return doubleByteCount > 0; // 纯 ASCII 已由 UTF-8 分支收编
        }
    }
}
