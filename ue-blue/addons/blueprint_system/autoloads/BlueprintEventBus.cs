using Godot;

// ============================================================================
// BlueprintEventBus.cs — 蓝图事件总线（Autoload 单例）
//
// 职责：
//   作为全局信号的中枢路由器，所有跨系统的通信信号集中在此定义。
//   不相关节点通过 EventBus 间接通信，无需持有彼此引用。
//
// 使用方式：
//   发射信号： BlueprintEventBus.Instance.EmitSignal(SignalName.XXX, args...)
//   接收信号： BlueprintEventBus.Instance.XXX += Handler
//             或 BlueprintEventBus.Instance.Connect(SignalName.XXX, callable)
//
// 设计原则：
//   1. 所有信号使用类型化参数
//   2. C# 消费者必须在 _ExitTree() 中断开连接（防止委托悬空）
//   3. 不用于简单的父子直接通信 — 仅用于无关节点间的解耦
// ============================================================================

/// <summary>
/// 蓝图事件总线 — Autoload 单例。
/// 通过 project.godot 的 [autoload] 节注册，游戏启动时自动实例化。
/// </summary>
public partial class BlueprintEventBus : Node
{
    // ────────────────────────────────────────────────────────────────
    // 单例访问（手动实现，不依赖 Godot SourceGenerator 的 .Singleton）
    // ────────────────────────────────────────────────────────────────

    /// <summary>全局单例引用，在 _EnterTree 中自动赋值。</summary>
    public static BlueprintEventBus Instance { get; private set; }

    /// <summary>
    /// 节点进入场景树时注册单例。
    /// Autoload 在游戏启动时调用此方法。
    /// </summary>
    public override void _EnterTree()
    {
        Instance = this;
    }

    /// <summary>
    /// 节点退出场景树时清理单例引用。
    /// 防止场景重载后悬挂无效引用。
    /// </summary>
    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ────────────────────────────────────────────────────────────────
    // 信号声明区：所有跨系统通信信号在此定义
    // ────────────────────────────────────────────────────────────────

    // ── 组件生命周期事件 ──
    [Signal] public delegate void ComponentAddedEventHandler(int componentId);
    [Signal] public delegate void ComponentRemovedEventHandler(int componentId);
    [Signal] public delegate void ComponentMovedEventHandler(int componentId, Vector2 newPosition);
    [Signal] public delegate void ComponentSelectedEventHandler(int componentId);
    [Signal] public delegate void ComponentDeselectedEventHandler(int componentId);

    // ── 连接事件 ──
    [Signal] public delegate void ConnectionCreatedEventHandler(ConnectionData connectionData);
    [Signal] public delegate void ConnectionRemovedEventHandler(int connectionId);
    [Signal] public delegate void ConnectionSelectedEventHandler(int connectionId);

    // ── 交互事件 ──
    [Signal] public delegate void ContextMenuRequestedEventHandler(Vector2 screenPosition, Vector2 canvasPosition);
    [Signal] public delegate void DragStartedEventHandler(int portId, int componentId);
    [Signal] public delegate void DragEndedEventHandler(bool success);

    // ── 图（Graph）事件 ──
    [Signal] public delegate void GraphChangedEventHandler();
    [Signal] public delegate void GraphSavedEventHandler();
    [Signal] public delegate void GraphLoadedEventHandler();

    // ── 编辑器状态事件（重命名避免与 Node.EditorStateChanged 冲突） ──
    [Signal] public delegate void BlueprintEditorStateChangedEventHandler(Godot.Collections.Dictionary state);
}
