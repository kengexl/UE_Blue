using Godot;

// ============================================================================
// PortDefinition.cs — 端口定义 Resource
//
// 职责：
//   定义端口的数据结构。端口是组件与外部交互的唯一接口，
//   每个端口有方向（输入/输出）、类型（exec/int/bool 等）和连接数限制。
//
// 使用方式：
//   作为 Resource 子类，可直接在 Inspector 中编辑，
//   或在代码中通过 new PortDefinition() 创建后挂载到组件。
// ============================================================================

/// <summary>
/// 端口定义 — 描述一个端口的元数据。
/// 继承 Resource 使其可保存为 .tres 文件或内联到其他 Resource 中。
/// </summary>
[GlobalClass]
public partial class PortDefinition : Resource
{
    /// <summary>端口方向枚举。</summary>
    public enum DirectionType
    {
        /// <summary>输入端口 — 接收数据/执行信号。</summary>
        Input,
        /// <summary>输出端口 — 发送数据/执行信号。</summary>
        Output
    }

    // ────────────────────────────────────────────────────────────────
    // 导出属性：可在 Godot Inspector 中编辑
    // ────────────────────────────────────────────────────────────────

    /// <summary>端口唯一标识（在所属组件内唯一）。</summary>
    [Export] public int PortId { get; set; }

    /// <summary>端口显示名称，如 "In"、"Out"、"Execute"。</summary>
    [Export] public string PortName { get; set; } = "";

    /// <summary>端口方向：Input（输入）或 Output（输出）。</summary>
    [Export] public DirectionType Direction { get; set; }

    /// <summary>
    /// 端口数据类型标识字符串。
    /// 内置类型: "exec"（执行线）、"int"、"bool"、"string"、"float"。
    /// 可扩展自定义类型。
    /// </summary>
    [Export] public string PortType { get; set; } = "exec";

    /// <summary>Godot Variant.Type 映射值，用于运行时类型检查。</summary>
    [Export] public int DataType { get; set; }

    /// <summary>
    /// 连接数限制。-1 表示不限连接数。
    /// 通常输出端口不限，输入端口限制为 1。
    /// </summary>
    [Export] public int ConnectionLimit { get; set; } = -1;
}
