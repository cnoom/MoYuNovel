using System;
using System.Text;
using CNoom.MoYuNovel.Runtime.Novel;
using NUnit.Framework;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>EncodingDetector 契约测试（FR-009：编码探测）。</summary>
    public class EncodingDetectorTests
    {
        [Test]
        public void Detect_Utf8WithBom_ReturnsUtf8()
        {
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF, 0xE6, 0x91, 0xB8 }; // BOM + "摸"
            Assert.AreEqual(DetectedEncoding.Utf8, EncodingDetector.Detect(bytes));
        }

        [Test]
        public void Detect_Utf8NoBom_ReturnsUtf8()
        {
            var bytes = Encoding.UTF8.GetBytes("摸鱼测试小说正文");
            Assert.AreEqual(DetectedEncoding.Utf8, EncodingDetector.Detect(bytes));
        }

        [Test]
        public void Detect_PureAscii_ReturnsUtf8()
        {
            var bytes = Encoding.ASCII.GetBytes("plain ascii log line 123");
            Assert.AreEqual(DetectedEncoding.Utf8, EncodingDetector.Detect(bytes));
        }

        [Test]
        public void Detect_Utf16LeBom_ReturnsUtf16Le()
        {
            var bytes = Encoding.Unicode.GetPreamble();
            Assert.AreEqual(DetectedEncoding.Utf16Le, EncodingDetector.Detect(bytes));
        }

        [Test]
        public void Detect_Utf16BeBom_ReturnsUtf16Be()
        {
            var bytes = Encoding.BigEndianUnicode.GetPreamble();
            Assert.AreEqual(DetectedEncoding.Utf16Be, EncodingDetector.Detect(bytes));
        }

        [Test]
        public void Detect_GbkText_ReturnsGbk()
        {
            Encoding gbk;
            try { gbk = Encoding.GetEncoding(936); }
            catch (ArgumentException)
            {
                Assert.Ignore("当前环境缺少 GBK 解码支持（I18N 未加载），跳过。");
                return;
            }
            var bytes = gbk.GetBytes("摸鱼小说第二回：山中原有主，.Test");
            Assert.AreEqual(DetectedEncoding.Gbk, EncodingDetector.Detect(bytes));
        }

        [Test]
        public void Detect_Utf8TakesPriorityOverGbkHeuristic()
        {
            // 合法 UTF-8 中文即使双字节占比极高，也不应落入 GBK 分支
            var bytes = Encoding.UTF8.GetBytes("这是一段合法的中文文本，全部多字节序列都合法。");
            Assert.AreEqual(DetectedEncoding.Utf8, EncodingDetector.Detect(bytes));
        }

        [Test]
        public void Detect_InvalidBytes_ReturnsUnknown()
        {
            // 0x81 后接 0x00：既非合法 GBK 尾字节，也非合法 UTF-8
            var bytes = new byte[] { 0x81, 0x00, 0x81, 0x00 };
            Assert.AreEqual(DetectedEncoding.Unknown, EncodingDetector.Detect(bytes));
        }

        [Test]
        public void Detect_EmptyOrTruncated_ReturnsUnknownOrGraceful()
        {
            Assert.AreEqual(DetectedEncoding.Unknown, EncodingDetector.Detect(null));
            Assert.AreEqual(DetectedEncoding.Unknown, EncodingDetector.Detect(new byte[0]));
            // 0x80 单字节：既非合法 UTF-8 起始，也非合法 GBK 首字节（<0x81）
            Assert.AreEqual(DetectedEncoding.Unknown, EncodingDetector.Detect(new byte[] { 0x80 }));
        }

        [Test]
        public void Detect_TruncatedUtf8_ResolvesToGbkPerPriority()
        {
            // E6 91 是被截断的 UTF-8 三字节序列，但恰好构成合法 GBK 双字节——
            // 按探测优先级（UTF-8 校验失败 → GBK 启发式）应解析为 GBK 而非 Unknown
            Assert.AreEqual(DetectedEncoding.Gbk, EncodingDetector.Detect(new byte[] { 0xE6, 0x91 }));
        }
    }
}
