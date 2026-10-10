using System.IO;

namespace CNoom.MoYuNovel.Editor.Storage
{
    /// <summary>
    /// 持久化存储布局（research R3）：工程 <c>UserSettings/MoYuNovel/</c>。
    /// 该目录已被仓库 .gitignore 覆盖——个人数据不入库、换工程互不串扰。
    /// </summary>
    internal static class MoYuPaths
    {
        /// <summary>测试根目录覆盖（生产恒为 null；仅供测试注入临时目录）。</summary>
        internal static string OverrideRoot;

        /// <summary>存储根目录。</summary>
        internal static string RootDir => OverrideRoot ?? Path.Combine("UserSettings", "MoYuNovel");

        /// <summary>输出设置文件路径。</summary>
        internal static string SettingsFile => Path.Combine(RootDir, "settings.json");

        /// <summary>阅读进度文件路径。</summary>
        internal static string ProgressFile => Path.Combine(RootDir, "progress.json");

        /// <summary>导入作品书库目录。</summary>
        internal static string LibraryDir => Path.Combine(RootDir, "library");

        /// <summary>确保根目录存在。</summary>
        internal static void EnsureRoot() => Directory.CreateDirectory(RootDir);

        /// <summary>确保书库目录存在。</summary>
        internal static void EnsureLibrary() => Directory.CreateDirectory(LibraryDir);
    }
}
