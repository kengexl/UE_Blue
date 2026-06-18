using Godot;

// ============================================================================
// BlueprintEditor.cs — 蓝图编辑器根节点
//
// 职责：
//   蓝图编辑器场景的根节点，管理核心系统对象的初始化和连接。
//   所有输入事件由 _Input() 统一接收后转发到 InteractionSystem。
//
// 场景结构：
//   BlueprintEditor (Node2D) ← 本脚本
//   ├─ CanvasLayer (CanvasLayer) — UI 层
//   ├─ Camera2D — 视口控制
//   └─ EditorCanvas (Control) — 主编辑区域
//       ├─ GridBackground (ColorRect) — 网格背景
//       ├─ ComponentLayer (Node2D) — 组件容器
//       └─ ConnectionLayer (Node2D) — 连接线容器
// ============================================================================

/// <summary>
/// 蓝图编辑器根节点 — 初始化并协调所有子系统。
/// </summary>
public partial class BlueprintEditor : Node2D
{
    // ────────────────────────────────────────────────────────────────
    // 子节点引用
    // ────────────────────────────────────────────────────────────────

    private CanvasLayer _canvasLayer;
    private Camera2D _camera;
    private EditorCanvas _editorCanvas;

    // ────────────────────────────────────────────────────────────────
    // 子系统
    // ────────────────────────────────────────────────────────────────

    private ConnectionManager _connectionManager;
    private InteractionSystem _interactionSystem;

    /// <summary>可视化调试叠加层。</summary>
    private BlueprintDebugOverlay _debugOverlay;

    // ────────────────────────────────────────────────────────────────
    // 生命周期
    // ────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
        _camera = GetNode<Camera2D>("Camera2D");
        _editorCanvas = GetNode<EditorCanvas>("EditorCanvas");

        InitSystems();
        WireSystems();
        SetupCamera();
        InitDebugOverlay();
    }

    /// <summary>
    /// 初始化核心系统。
    /// ComponentSystem 已通过 Autoload 注册为单例。
    /// ConnectionManager 和 InteractionSystem 作为子节点创建。
    /// </summary>
    private void InitSystems()
    {
        // 连接管理器
        _connectionManager = new ConnectionManager();
        _connectionManager.Name = "ConnectionManager";
        _connectionManager.ComponentSystem = ComponentSystem.Instance;
        AddChild(_connectionManager);

        // 交互系统
        _interactionSystem = new InteractionSystem();
        _interactionSystem.Name = "InteractionSystem";
        _interactionSystem.ComponentSystem = ComponentSystem.Instance;
        _interactionSystem.ConnectionManager = _connectionManager;
        _interactionSystem.EditorCanvas = _editorCanvas;
        AddChild(_interactionSystem);
    }

    /// <summary>将系统引用注入到 EditorCanvas。</summary>
    private void WireSystems()
    {
        if (_editorCanvas != null)
        {
            _editorCanvas.InteractionSystem = _interactionSystem;
            _editorCanvas.ComponentSystem = ComponentSystem.Instance;
            _editorCanvas.ConnectionManager = _connectionManager;
        }
    }

    /// <summary>初始化 Camera2D。</summary>
    private void SetupCamera()
    {
        if (_camera != null)
        {
            _camera.Enabled = true;
            _camera.Zoom = Vector2.One;
            _camera.Position = Vector2.Zero;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 调试叠加层
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建并初始化可视化调试叠加层。
    /// 挂在 CanvasLayer 下，始终渲染在编辑器内容之上。
    /// </summary>
    private void InitDebugOverlay()
    {
        _debugOverlay = new BlueprintDebugOverlay();
        _debugOverlay.Name = "DebugOverlay";
        _canvasLayer?.AddChild(_debugOverlay);
        _debugOverlay.Initialize(_interactionSystem, _editorCanvas);
    }

    // ────────────────────────────────────────────────────────────────
    // 输入处理
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 所有输入事件的统一入口。
    /// 通过 Node2D 的 _Input 方法接收事件，确保可靠触发。
    /// 转发到 InteractionSystem 进行状态机处理。
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (_interactionSystem != null && _editorCanvas != null)
        {
            // 使用 EditorCanvas 的本地坐标空间，确保与组件层（_componentLayer）
            // 的父级坐标系统一致。GetLocalMousePosition() 返回相对于
            // EditorCanvas 左上角的坐标，与 _componentLayer 的子节点坐标匹配。
            var mousePos = _editorCanvas.GetLocalMousePosition();
            _interactionSystem.HandleInput(@event, mousePos);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 工具方法
    // ────────────────────────────────────────────────────────────────

    /// <summary>在指定画布位置创建测试组件。</summary>
    public BlueprintComponent CreateTestComponentAt(Vector2 pos)
    {
        if (_editorCanvas?.ComponentLayer == null) return null;

        var mousePos = GetViewport().GetMousePosition();
        var comp = ComponentSystem.Instance.CreateComponent(ComponentData.ComponentType.Dual, pos);
        if (comp != null)
        {
            _editorCanvas.ComponentLayer.AddChild(comp);

            // 调试日志：API 方式创建节点
            BlueprintDebug.LogNodeCreation(
                componentId: comp.ComponentId,
                componentType: comp.GetType().Name,
                canvasPos: pos,
                screenPos: mousePos,
                triggerMethod: "API调用(CreateTestComponentAt)",
                componentName: comp.ComponentName
            );

            BlueprintEventBus.Instance.EmitSignal(
                BlueprintEventBus.SignalName.ComponentAdded, comp.ComponentId);
        }
        return comp;
    }

    /// <summary>清空所有组件和连接线。</summary>
    public void ClearAll()
    {
        foreach (var comp in ComponentSystem.Instance.GetAllComponents())
            ComponentSystem.Instance.UnregisterComponent(comp.ComponentId);

        if (_editorCanvas?.ComponentLayer != null)
        {
            foreach (Node child in _editorCanvas.ComponentLayer.GetChildren())
                child.QueueFree();
        }

        if (_editorCanvas?.ConnectionLayer != null)
        {
            foreach (Node child in _editorCanvas.ConnectionLayer.GetChildren())
                child.QueueFree();
        }
    }
}
