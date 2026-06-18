using Godot;
using System.Linq;

// ============================================================================
// GraphData.cs — 图数据顶层容器 Resource
//
// 职责：
//   作为整个蓝图图的顶层数据容器，管理所有组件和连接。
//   保存编辑器状态（视口偏移、缩放等）。
//   支持完整的 CRUD 操作（添加/移除组件和连接）。
// ============================================================================

/// <summary>
/// 图数据 — 蓝图图的顶层容器。
/// 保存所有组件数据和连接数据，以及编辑器的视口状态。
/// 是整个蓝图系统的数据根。
/// </summary>
[GlobalClass]
public partial class GraphData : Resource
{
    // ────────────────────────────────────────────────────────────────
    // 导出属性
    // ────────────────────────────────────────────────────────────────

    /// <summary>图的显示名称。</summary>
    [Export] public string GraphName { get; set; } = "New Graph";

    /// <summary>图唯一标识。</summary>
    [Export] public int GraphId { get; set; }

    /// <summary>
    /// 组件字典，以 componentId 为键。
    /// 使用 Dictionary 而不是 Array 以获得 O(1) 查找。
    /// </summary>
    [Export] public Godot.Collections.Dictionary<int, ComponentData> Components { get; set; } = new();

    /// <summary>连接列表。</summary>
    [Export] public Godot.Collections.Array<ConnectionData> Connections { get; set; } = new();

    /// <summary>扩展元数据字典。</summary>
    [Export] public Godot.Collections.Dictionary Metadata { get; set; } = new();

    /// <summary>
    /// 编辑器状态字典，保存视口偏移和缩放等状态，
    /// 用于在下次打开时恢复视图位置。
    /// </summary>
    [Export] public Godot.Collections.Dictionary EditorState { get; set; } = new();

    // ────────────────────────────────────────────────────────────────
    // 公共方法
    // ────────────────────────────────────────────────────────────────

    /// <summary>向图中添加一个组件。</summary>
    public void AddComponent(ComponentData data)
    {
        Components[data.ComponentId] = data;
    }

    /// <summary>
    /// 从图中移除一个组件以及与之关联的所有连接。
    /// </summary>
    public void RemoveComponent(int componentId)
    {
        Components.Remove(componentId);
        // 同时移除该组件相关的所有连接（作为源或目标）
        var toRemove = Connections
            .Where(c => c.SourceComponentId == componentId || c.TargetComponentId == componentId)
            .ToList();
        foreach (var conn in toRemove)
            Connections.Remove(conn);
    }

    /// <summary>向图中添加一条连接。如果无效则返回 false。</summary>
    public bool AddConnection(ConnectionData data)
    {
        if (!data.IsValid())
            return false;
        Connections.Add(data);
        return true;
    }

    /// <summary>从图中移除指定 ID 的连接。</summary>
    public void RemoveConnection(int connectionId)
    {
        var toRemove = Connections
            .Where(c => c.ConnectionId == connectionId)
            .ToList();
        foreach (var conn in toRemove)
            Connections.Remove(conn);
    }
}
