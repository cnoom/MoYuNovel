# Contract: Runtime 程序集公共 API

**Assembly**: `CNoom.MoYuNovel.Runtime`（纯 C#，`noEngineReferences: true`，
`autoReferenced: false`）| 全部公共 API 具中文 XML 文档注释（宪法质量标准）

设计约束：本程序集 MUST NOT 引用 UnityEngine/UnityEditor；输入输出全部为
BCL 类型（string/ IReadOnlyList 等），EditMode 测试直接调用。

## Novel 域

```csharp
/// 解析结果：作品元数据 + 章节目录 + 总行数。行数据按需流式读取，不整本驻留。
public sealed class Novel { string Id; string Title; NovelSource Source;
                            int TotalLineCount; IReadOnlyList<Chapter> Chapters; }

/// txt → Novel。章节标题行同时计入输出行流。
public static class NovelParser
{
    public static Novel Parse(string id, string title, string rawText);
    // 契约：无章节标记 → 单章"全文"；空/全空白文本 → 抛 EmptyNovelTextException
    //      （调用方负责拒绝入库并提示，边界用例「空文件」）
}

/// 超长段落切行：按 MaxLineChars（默认 120 全角）切分为多行，切点优先句读。
public static class LineSplitter
{
    public const int DefaultMaxLineChars = 120;
    public static IReadOnlyList<string> Split(string paragraph, int maxChars);
    // 契约：任何返回行长度 ≤ maxChars；空段落返回空集
}

/// 编码探测（纯探测，不解码）。
public static class EncodingDetector
{
    public static DetectedEncoding Detect(byte[] bytes);
    // 契约：BOM(UTF-8/16/32) 优先；其次 UTF-8 逐字节合法性；再 GBK 双字节
    //      结构启发式；均不满足 → Unknown（调用方提示转存 UTF-8）
}
```

## Progress 域

```csharp
public sealed class ReadingProgress { string NovelId; int GlobalLineIndex;
                                      DateTime UpdatedAt; }

public sealed class ProgressBook
{
    public static ProgressBook Empty();
    public ReadingProgress? Get(string novelId);
    public void Advance(string novelId, int globalLineIndex);   // 单调递增，回退忽略
    public void Clear();                                        // 全清（FR-007）
    // 契约：Advance 后 Get 返回的行号与输入一致（SC-003 偏差 0）
}
```

## Output 域

```csharp
/// 间隔策略：默认 1s、下限 0.2s（FR-003）。
public static class IntervalPolicy
{
    public const float DefaultSeconds = 1f;
    public const float MinSeconds = 0.2f;
    public const float MaxSeconds = 3600f;
    public static float Clamp(float seconds);   // NaN/负/过小 → Min；过大 → Max
}

/// 伪装格式化纯函数（FR-004/FR-013/SC-005）。
public static class DisguiseFormatter
{
    public static string Format(DisguisePreset preset, string marker, string text);
    // 契约：返回值 MUST 含 marker 子串（模板缺 {marker} 则强制尾置）——
    //      SC-005 零漏标由测试断言；text 原样保留，不转义不截断
}

public sealed class DisguisePreset { string Id; string Name; string PrefixTemplate; }
```

## 契约测试对应（Tests/EditMode）

| API | 断言要点 | 来源 |
|-----|----------|------|
| NovelParser.Parse | 章节正则命中/单章回退/空文本异常 | FR 澄清 1 |
| LineSplitter.Split | ≤maxChars、句读优先切点 | FR-012 |
| EncodingDetector.Detect | UTF-8/GBK 样本分类 | FR-009 |
| IntervalPolicy.Clamp | 默认 1/下限 0.2/NaN | FR-003 |
| ProgressBook.* | 续读偏差 0/清除后空 | SC-003/FR-007 |
| DisguiseFormatter.Format | 标记零漏 | SC-005 |
