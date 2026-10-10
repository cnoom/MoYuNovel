using System;
using System.IO;
using CNoom.MoYuNovel.Editor.Storage;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>测试环境：为每次测试注入隔离的临时存储根目录。</summary>
    internal static class TestEnv
    {
        /// <summary>设置临时根目录并确保存在，返回该目录路径。</summary>
        internal static string UseTempRoot()
        {
            var dir = Path.Combine(Path.GetTempPath(), "MoYuNovelTests-" + Guid.NewGuid().ToString("N"));
            MoYuPaths.OverrideRoot = dir;
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>解除覆盖并删除临时目录。</summary>
        internal static void Teardown(string tempRoot)
        {
            MoYuPaths.OverrideRoot = null;
            if (tempRoot != null && Directory.Exists(tempRoot))
            {
                try { Directory.Delete(tempRoot, true); } catch { /* 临时目录清理失败不影响测试结论 */ }
            }
        }
    }
}
