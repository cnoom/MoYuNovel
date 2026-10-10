using System;

namespace CNoom.MoYuNovel.Runtime.Output
{
    /// <summary>伪装预设：一套输出行外观规则（FR-004/FR-013）。</summary>
    public sealed class DisguisePreset
    {
        /// <summary>预设标识。</summary>
        public string Id { get; }

        /// <summary>展示名（如「网络模块」）。</summary>
        public string Name { get; }

        /// <summary>整行模板；占位符 <c>{marker}</c> 注入统一过滤标记，<c>{text}</c> 注入正文。</summary>
        public string PrefixTemplate { get; }

        /// <summary>创建预设。</summary>
        public DisguisePreset(string id, string name, string prefixTemplate)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = string.IsNullOrEmpty(name) ? id : name;
            PrefixTemplate = string.IsNullOrEmpty(prefixTemplate) ? "[{marker}] {text}" : prefixTemplate;
        }
    }
}
