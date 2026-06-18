using Godot;

// ============================================================================
// ConnectionData.cs — 连接数据 Resource
//
// 职责：
//   描述两个组件端口之间的连接关系。
//   记录了源组件/端口到目标组件/端口的单向连接。
//   包含连接有效性验证和哈希生成方法。
// ============================================================================

/// <summary>
/// 连接数据 — 两个组件端口之间的连接关系。
/// 继承 Resource 支持序列化保存和加载。
/// </summary>
[GlobalClass]
public partial class ConnectionData : Resource
{
    // ────────────────────────────────────────────────────────────────
    // 导出属性
    // ────────────────────────────────────────────────────────────────

    /// <summary>连接唯一标识（全局唯一）。</summary>
    [Export] public int ConnectionId { get; set; }

    /// <summary>源组件 ID（输出端口所在组件）。</summary>
    [Export] public int SourceComponentId { get; set; }

    /// <summary>源端口 ID（输出端口）。</summary>
    [Export] public int SourcePortId { get; set; }

    /// <summary>目标组件 ID（输入端口所在组件）。</summary>
    [Export] public int TargetComponentId { get; set; }

    /// <summary>目标端口 ID（输入端口）。</summary>
    [Export] public int TargetPortId { get; set; }

    /// <summary>
    /// 连接类型标识。默认 "exec" 表示执行流连接。
    /// 其他类型： "data"（数据流）、"event"（事件）等。
    /// </summary>
    [Export] public string ConnectionType { get; set; } = "exec";

    // ────────────────────────────────────────────────────────────────
    // 公共方法
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证连接合法性。
    /// 需满足：源端口为输出、目标端口为输入、非自连接。
    /// 完整验证由 ConnectionManager 执行。
    /// </summary>
    public bool IsValid()
    {
        return true; // 暂存，详细验证在 ConnectionManager 中
    }

    /// <summary>
    /// 获取连接的唯一哈希字符串。
    /// 格式："{SourceComponentId}_{SourcePortId}_{TargetComponentId}_{TargetPortId}"
    /// 用于检测重复连接。
    /// </summary>
    public string GetHash()
    {
        return $"{SourceComponentId}_{SourcePortId}_{TargetComponentId}_{TargetPortId}";
    }
}
