using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CNoom.MoYuNovel.Editor.Storage;
using CNoom.MoYuNovel.Runtime.Novel;
using UnityEditor;
using UnityEngine;

namespace CNoom.MoYuNovel.Editor.Library
{
    /// <summary>书架条目：作品标识、标题、来源与正文文件路径（内置作品无文件路径）。</summary>
    internal sealed class ShelfItem
    {
        internal string Id;
        internal string Title;
        internal NovelSource Source;
        internal string FilePath;
    }

    /// <summary>导入结果：成功携带解析后的作品；失败携带用户可读原因（书架保证不变）。</summary>
    internal sealed class ImportOutcome
    {
        internal bool Ok;
        internal string Message;
        internal Novel Novel;

        internal static ImportOutcome Success(Novel novel) => new ImportOutcome { Ok = true, Message = "导入成功。", Novel = novel };
        internal static ImportOutcome Fail(string message) => new ImportOutcome { Ok = false, Message = message };
    }

    /// <summary>
    /// 书架（FR-008/FR-009）：
    /// 内置作品 = 包内 Editor-only TextAsset（绝不进玩家构建，宪法 I / SC-006 资源侧保障）；
    /// 导入作品 = 探测编码→解码→转存 UTF-8 拷贝入库，脱离原文件可用。
    /// </summary>
    internal sealed class NovelLibrary
    {
        private const string PackageRoot = "Packages/com.cnoom.moyunovel";
        private const string ImportIdPrefix = "import:";

        private static readonly (string Id, string Title, string AssetName)[] BuiltinCatalog =
        {
            ("builtin:journey-west", "西游记（节选）", "journey-west"),
        };

        /// <summary>枚举书架：内置目录 ∪ library/*.txt。</summary>
        internal List<ShelfItem> Enumerate()
        {
            var list = new List<ShelfItem>();
            foreach (var (id, title, _) in BuiltinCatalog)
            {
                list.Add(new ShelfItem { Id = id, Title = title, Source = NovelSource.Builtin });
            }

            MoYuPaths.EnsureLibrary();
            foreach (var path in Directory.GetFiles(MoYuPaths.LibraryDir, "*.txt"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(name)) continue;
                list.Add(new ShelfItem
                {
                    Id = ImportIdPrefix + name,
                    Title = name,
                    Source = NovelSource.Imported,
                    FilePath = path,
                });
            }
            return list;
        }

        /// <summary>按 id 装载作品（正文缺失/坏档时抛异常，由调用方提示）。</summary>
        internal Novel LoadById(string novelId)
        {
            foreach (var item in Enumerate())
            {
                if (item.Id == novelId) return Load(item);
            }
            throw new FileNotFoundException("书架中不存在作品: " + novelId);
        }

        /// <summary>装载并解析作品正文。</summary>
        internal Novel Load(ShelfItem item)
        {
            string text = item.Source == NovelSource.Builtin ? LoadBuiltinText(item.Id) : File.ReadAllText(item.FilePath);
            return NovelParser.Parse(item.Id, item.Title, item.Source, text);
        }

        /// <summary>
        /// 导入本地 txt（FR-009）：编码探测→解码→转存 UTF-8 入库。
        /// 契约：失败（空文件/无法解码）返回可读原因且书架不变；同内容重复导入幂等。
        /// </summary>
        internal ImportOutcome Import(string sourcePath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(sourcePath);
                if (bytes.Length == 0)
                {
                    return ImportOutcome.Fail("文件为空或不含有效正文，已拒绝导入（书架未变化）。");
                }
                string text;
                switch (EncodingDetector.Detect(bytes))
                {
                    case DetectedEncoding.Utf8:
                        text = new UTF8Encoding(false).GetString(bytes);
                        if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
                        break;
                    case DetectedEncoding.Utf16Le:
                        text = Encoding.Unicode.GetString(bytes);
                        break;
                    case DetectedEncoding.Utf16Be:
                        text = Encoding.BigEndianUnicode.GetString(bytes);
                        break;
                    case DetectedEncoding.Gbk:
                        try { text = Encoding.GetEncoding(936).GetString(bytes); }
                        catch (ArgumentException)
                        {
                            return ImportOutcome.Fail("检测到 GBK 文本，但当前环境缺少 GBK 解码支持，请转存 UTF-8 后重试。");
                        }
                        break;
                    default:
                        return ImportOutcome.Fail("未能识别文本编码，请转存 UTF-8 后重试。");
                }

                string title = Path.GetFileNameWithoutExtension(sourcePath);
                Novel novel;
                try
                {
                    novel = NovelParser.Parse(ImportIdPrefix + title, title, NovelSource.Imported, text);
                }
                catch (EmptyNovelTextException)
                {
                    return ImportOutcome.Fail("文件为空或不含有效正文，已拒绝导入（书架未变化）。");
                }

                MoYuPaths.EnsureLibrary();
                string targetPath = Path.Combine(MoYuPaths.LibraryDir, title + ".txt");
                if (File.Exists(targetPath) && File.ReadAllText(targetPath) != text)
                {
                    // 同名不同内容：追加内容短哈希区分，避免覆盖
                    targetPath = Path.Combine(MoYuPaths.LibraryDir, title + "-" + ShortHash(text) + ".txt");
                }
                File.WriteAllText(targetPath, text, new UTF8Encoding(false));

                string finalId = ImportIdPrefix + Path.GetFileNameWithoutExtension(targetPath);
                if (finalId != novel.Id)
                {
                    novel = NovelParser.Parse(finalId, title, NovelSource.Imported, text);
                }
                return ImportOutcome.Success(novel);
            }
            catch (IOException e)
            {
                return ImportOutcome.Fail("读取文件失败: " + e.Message);
            }
        }

        private static string ShortHash(string text)
        {
            using (var sha = SHA1.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        // 内置文本走 TextAsset：位于 Editor/ 目录的资产不参与玩家构建（research R4）。
        private static string LoadBuiltinText(string id)
        {
            string assetName = null;
            foreach (var (bid, _, name) in BuiltinCatalog)
            {
                if (bid == id) assetName = name;
            }
            if (assetName == null) throw new FileNotFoundException("未知内置作品: " + id);

            foreach (var guid in AssetDatabase.FindAssets(assetName + " t:TextAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith(PackageRoot, StringComparison.OrdinalIgnoreCase)) continue;
                if (!Path.GetFileNameWithoutExtension(path).Equals(assetName, StringComparison.OrdinalIgnoreCase)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset != null) return asset.text;
            }
            throw new FileNotFoundException("内置作品资源缺失: " + assetName);
        }
    }
}
