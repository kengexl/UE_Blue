using Godot;

// ============================================================================
// ComponentData.cs — 组件数据 Resource
//
// 职责：
//   描述一个蓝图组件的数据模型，包括组件类型、位置、尺寸、
//   关联端口列表和扩展属性字典。
//   与 BlueprintComponent（运行时实体）分离，支持序列化。
//
// 使用方式：
//   创建后通过 Initialize() 传递给 BlueprintComponent 构建运行时。
//   也可直接保存为 .tres 文件用于预设。
// ============================================================================

/// <summary>组件类型枚举。</summary>
public partial class ComponentData : Resource
{
    /// <summary>组件类型枚举。</summary>
    public enum ComponentType
    {
        /// <summary>输出专用 — 仅有输出端口，如事件起点。</summary>
        OutputOnly,
        /// <summary>双向 — 同时有输入和输出端口，如处理节点。</summary>
        Dual,
        /// <summary>输入专用 — 仅有输入端口，如日志输出。</summary>
        InputOnly
    }
    // ────────────────────────────────────────────────────────────────
    // 导出属性
    // ────────────────────────────────────────────────────────────────

    /// <summary>组件唯一标识（在整个图中唯一）。</summary>
    [Export] public int ComponentId { get; set; }

    /// <summary>组件显示名称，显示在标题栏上。</summary>
    [Export] public string ComponentName { get; set; } = "";

    /// <summary>组件类型，决定端口布局和 Header 颜色。</summary>
    [Export] public ComponentType Type { get; set; }

    /// <summary>组件在画布上的位置（画布坐标系）。</summary>
    [Export] public Vector2 Position { get; set; }

    /// <summary>组件在画布上的尺寸。默认 160×80。</summary>
    [Export] public Vector2 Size { get; set; } = new Vector2(160, 80);

    /// <summary>组件关联的端口定义列表。</summary>
    [Export] public Godot.Collections.Array<PortDefinition> Ports { get; set; } = new();

    /// <summary>
    /// 扩展属性字典，存储运行时需要的自定义数据。
    /// 例如：节点的执行参数、配置值等。
    /// </summary>
    [Export] public Godot.Collections.Dictionary Properties { get; set; } = new();
}
