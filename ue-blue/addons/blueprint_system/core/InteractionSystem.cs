using Godot;
using System.Text;

// ============================================================================
// InteractionSystem.cs — 交互系统（状态机）
//
// 职责：
//   处理所有用户输入，管理交互状态机。
//   输入由 BlueprintEditor._Input() 统一接收后转发至此。
//
// 状态机：
//   NONE → [左键点击输出端口] → CONNECTING_PORTS → [点击输入端口] → NONE
//   NONE → [左键点击组件标题] → DRAGGING_COMPONENT → [松开左键] → NONE
//   NONE → [中键拖拽] → PANNING_CANVAS → [松开中键] → NONE
//
// 重要：
//   端口点击优先级高于组件点击（端口在组件之上）。
//   mousePos 来自 EditorCanvas.GetLocalMousePosition()，是 EditorCanvas 本地坐标。
//   所有空间查询都通过 EditorCanvas.ScreenToCanvas() 转成组件层坐标。
// ============================================================================

/// <summary>
/// 交互系统 — 处理用户输入的状态机。
/// </summary>
public partial class InteractionSystem : Node
{
    // ────────────────────────────────────────────────────────────────
    // 枚举
    // ────────────────────────────────────────────────────────────────

    /// <summary>交互模式枚举。</summary>
    public enum InteractionMode
    {
        /// <summary>空闲状态，无操作进行中。</summary>
        None,
        /// <summary>正在拖拽组件。</summary>
        DraggingComponent,
        /// <summary>正在拖拽端口连线（已点击输出端口，等待目标输入端口）。</summary>
        ConnectingPorts,
        /// <summary>中键拖拽平移画布。</summary>
        PanningCanvas
    }

    /// <summary>当前交互模式。</summary>
    public InteractionMode CurrentMode { get; private set; } = InteractionMode.None;

    // ── 拖拽组件 ──
    private BlueprintComponent _dragComponent;
    private Vector2 _dragMouseStart;    // EditorCanvas 本地坐标
    private Vector2 _dragComponentStart;

    /// <summary>拖拽起始鼠标位置（EditorCanvas 本地坐标），供调试面板读取。</summary>
    public Vector2 PublicDragMouseStart => _dragMouseStart;
    /// <summary>组件拖拽起始时的画布位置。</summary>
    public Vector2 PublicDragComponentStart => _dragComponentStart;
    /// <summary>当前正在拖拽的组件。</summary>
    public BlueprintComponent PublicDragComponent => _dragComponent;

    // ── 端口连接 ──
    private PortWidget _sourcePort;
    private BlueprintComponent _sourceComponent;

    /// <summary>连接操作的源端口，供调试面板读取。</summary>
    public PortWidget PublicSourcePort => _sourcePort;
    /// <summary>连接操作的源组件。</summary>
    public BlueprintComponent PublicSourceComponent => _sourceComponent;

    /// <summary>临时连线（跟随鼠标的 Line2D）。</summary>
    private Line2D _tempLine;

    // ── 端口高亮提示（CONNECTING_PORTS 模式下检测到的目标 IN 端口） ──
    /// <summary>当前鼠标悬停的目标输入端口。</summary>
    private PortWidget _hoveredTargetPort;
    /// <summary>悬停时显示"可连接"信息的标签。</summary>
    private Label _portHintLabel;

    // ── 画布平移 ──
    private Vector2 _panStart;
    private Vector2 _panOffsetStart;

    // ── 选中状态 ──
    private BlueprintComponent _selectedComponent;

    /// <summary>当前选中的组件，供调试面板读取。</summary>
    public BlueprintComponent PublicSelectedComponent => _selectedComponent;

    // ── 最新 EditorCanvas 本地鼠标坐标（在 HandleInput 中缓存） ──
    private Vector2 _lastLocalMousePos;

    // ── 右键位置缓存 ──
    private Vector2 _lastRightClickCanvasPos;
    private Vector2 _lastRightClickScreenPos;

    /// <summary>外部注入的系统引用。</summary>
    public ComponentSystem ComponentSystem { get; set; }
    public ConnectionManager ConnectionManager { get; set; }
    public EditorCanvas EditorCanvas { get; set; }

    // ────────────────────────────────────────────────────────────────
    // 主输入入口
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 主输入入口 — 由 BlueprintEditor._Input() 每帧调用。
    /// mousePos 来自 EditorCanvas.GetLocalMousePosition()，确保与组件层父级坐标一致。
    /// </summary>
    public void HandleInput(InputEvent @event, Vector2 mousePos)
    {
        _lastLocalMousePos = mousePos;

        if (@event is InputEventMouseButton mb)
            HandleMouseButton(mb, mousePos);
        else if (@event is InputEventMouseMotion mm)
            HandleMouseMotion(mm, mousePos);
    }

    // ────────────────────────────────────────────────────────────────
    // 鼠标按键处理
    // ────────────────────────────────────────────────────────────────

    private void HandleMouseButton(InputEventMouseButton mb, Vector2 mousePos)
    {
        if (mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                // ── 优先级 1: 端口点击（端口在组件之上） ──
                var clickedPort = GetPortAt(mousePos);
                if (clickedPort != null)
                {
                    HandlePortInput(clickedPort, mb);
                    GetViewport().SetInputAsHandled();
                    return;
                }

                // ── 优先级 2: 组件点击 ──
                var clickedComp = GetComponentAt(mousePos);
                if (clickedComp != null)
                {
                    // 组件被点击但没有命中任何端口 → 输出诊断信息
                    if (clickedComp.GetPortWidgets().Count > 0)
                    {
                        LogPortDiagnostics(clickedComp, mousePos);
                    }

                    if (IsClickingHeader(clickedComp, mousePos))
                    {
                        StartDragComponent(clickedComp, mousePos);
                        SelectComponent(clickedComp);
                    }
                    else
                        SelectComponent(clickedComp);
                    GetViewport().SetInputAsHandled();
                    return;
                }

                // ── 点击空白 → 取消选中 ──
                DeselectCurrent();
            }
            else // 左键释放
            {
                // 在连接端口模式下，松开时检测鼠标是否在 IN 端口上 → 自动完成连接
                if (CurrentMode == InteractionMode.ConnectingPorts)
                {
                    var dropPort = GetPortAt(mousePos);
                    if (dropPort != null && dropPort != _sourcePort
                        && dropPort.PortDefinition.Direction == PortDefinition.DirectionType.Input)
                    {
                        TryCompleteConnection(dropPort);
                        return;
                    }
                }
                EndCurrentOperation();
            }
        }
        else if (mb.ButtonIndex == MouseButton.Middle)
        {
            if (mb.Pressed)
            {
                CurrentMode = InteractionMode.PanningCanvas;
                _panStart = mousePos;
                _panOffsetStart = EditorCanvas?.CameraOffset ?? Vector2.Zero;
            }
            else if (CurrentMode == InteractionMode.PanningCanvas)
                CurrentMode = InteractionMode.None;
        }
        else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
        {
            // 优先级 1: 检查是否点击在连接线上（右键断开）
            var clickedLine = GetConnectionLineAt(mousePos);
            if (clickedLine != null)
            {
                ShowConnectionContextMenu(clickedLine, mb.GlobalPosition);
                return;
            }

            // 优先级 2: 右键点击组件 → 组件上下文菜单
            var clickedComp = GetComponentAt(mousePos);
            if (clickedComp != null)
            {
                ShowComponentContextMenu(clickedComp, mb.GlobalPosition);
                return;
            }

            // 优先级 3: 右键点击空白区域 → 画布上下文菜单
            if (GetComponentAt(mousePos) == null && GetPortAt(mousePos) == null)
            {
                _lastRightClickCanvasPos = EditorCanvas?.ScreenToCanvas(mousePos) ?? mousePos;
                _lastRightClickScreenPos = mb.GlobalPosition;
                EditorCanvas?.ShowContextMenuAt(_lastRightClickScreenPos, _lastRightClickCanvasPos);
            }
        }

        // ── Ctrl+滚轮缩放 ──
        if (mb.CtrlPressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
                EditorCanvas?.SetZoom(EditorCanvas.ZoomLevel + 0.1f);
            else if (mb.ButtonIndex == MouseButton.WheelDown)
                EditorCanvas?.SetZoom(EditorCanvas.ZoomLevel - 0.1f);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 鼠标移动处理
    // ────────────────────────────────────────────────────────────────

    private void HandleMouseMotion(InputEventMouseMotion mm, Vector2 mousePos)
    {
        switch (CurrentMode)
        {
            case InteractionMode.DraggingComponent: DragComponent(mm, mousePos); break;
            case InteractionMode.PanningCanvas: PanCanvas(mm, mousePos); break;
            case InteractionMode.ConnectingPorts:
                UpdateTempLine();
                UpdateTargetPortHighlight(mousePos);
                break;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 选中逻辑
    // ────────────────────────────────────────────────────────────────

    private void SelectComponent(BlueprintComponent comp)
    {
        DeselectCurrent();
        _selectedComponent = comp;
        comp.SetSelected(true);
        BlueprintDebug.LogComponentSelected(comp.ComponentId, comp.ComponentName);
        BlueprintEventBus.Instance.EmitSignal(BlueprintEventBus.SignalName.ComponentSelected, comp.ComponentId);
    }

    private void DeselectCurrent()
    {
        if (_selectedComponent != null)
        {
            BlueprintDebug.LogComponentDeselected(_selectedComponent.ComponentId, _selectedComponent.ComponentName);
            _selectedComponent.SetSelected(false);
            BlueprintEventBus.Instance.EmitSignal(BlueprintEventBus.SignalName.ComponentDeselected, _selectedComponent.ComponentId);
            _selectedComponent = null;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 组件拖拽
    // ────────────────────────────────────────────────────────────────

    private void StartDragComponent(BlueprintComponent component, Vector2 mousePos)
    {
        CurrentMode = InteractionMode.DraggingComponent;
        _dragComponent = component;
        _dragMouseStart = mousePos;  // EditorCanvas 本地坐标
        _dragComponentStart = component.Position;
    }

    private void DragComponent(InputEventMouseMotion mm, Vector2 mousePos)
    {
        if (_dragComponent == null || EditorCanvas == null) return;

        // mousePos 是 EditorCanvas 本地坐标，转成组件层坐标（画布坐标）
        var canvasNow = EditorCanvas.ScreenToCanvas(mousePos);
        var canvasStart = EditorCanvas.ScreenToCanvas(_dragMouseStart);
        var canvasDelta = canvasNow - canvasStart;
        _dragComponent.Position = _dragComponentStart + canvasDelta;

        // 拖拽中实时更新关联的连接线
        var conns = ConnectionManager?.GetConnectionsForComponent(_dragComponent.ComponentId);
        if (conns != null)
        {
            foreach (var conn in conns)
                ConnectionManager.GetConnectionLine(conn.ConnectionId)?.UpdatePosition();
        }
    }

    /// <summary>检测是否点击在组件标题区域（Y 0-24px）。</summary>
    private bool IsClickingHeader(BlueprintComponent comp, Vector2 localPos)
    {
        if (EditorCanvas == null) return false;
        var canvasPos = EditorCanvas.ScreenToCanvas(localPos) - comp.Position;
        return canvasPos.Y >= 0 && canvasPos.Y <= 24
            && canvasPos.X >= 0 && canvasPos.X <= comp.ComponentSize.X;
    }

    /// <summary>获取 EditorCanvas 本地坐标处的组件（倒序遍历）。</summary>
    private BlueprintComponent GetComponentAt(Vector2 localPos)
    {
        if (EditorCanvas?.ComponentLayer == null) return null;
        var canvasPos = EditorCanvas.ScreenToCanvas(localPos);
        var children = EditorCanvas.ComponentLayer.GetChildren();
        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is BlueprintComponent comp)
            {
                var rect = new Rect2(comp.Position, comp.ComponentSize);
                if (rect.HasPoint(canvasPos))
                    return comp;
            }
        }
        return null;
    }

    // ────────────────────────────────────────────────────────────────
    // 端口连接
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 获取画布空间坐标处的端口控件。
    /// 
    /// 坐标空间说明：
    ///   输入 mouseLocalPos → EditorCanvas.GetLocalMousePosition() 返回值（EditorCanvas 本地坐标）
    ///   GetPortAt 内部通过 EditorCanvas.ScreenToCanvas() 将 mouseLocalPos 转为画布空间。
    ///   端口的全局位置通过 ComponentLayer.ToLocal() 转为画布空间。
    ///   两者在画布空间中比较。
    /// 
    /// 关键修正：
    ///   避免使用 EditorCanvas.GetGlobalTransformWithCanvas()。
    ///   该 API 在 Control 作为 Node2D 子节点时会产生错误变换，
    ///   因为 Node2D 没有 Size 概念，Control 锚点不生效且 Size=0。
    ///   改用 ComponentLayer.ToLocal(portGlobal) 获取端口在画布空间的位置，
    ///   此空间与 ScreenToCanvas() 转换后的 mouse 坐标一致。
    /// </summary>
    private PortWidget GetPortAt(Vector2 mouseLocalPos)
    {
        if (EditorCanvas?.ComponentLayer == null) return null;

        // 鼠标位置从 EditorCanvas 本地 → 画布空间（组件层空间）
        var mouseCanvas = EditorCanvas.ScreenToCanvas(mouseLocalPos);

        foreach (Node child in EditorCanvas.ComponentLayer.GetChildren())
        {
            if (child is BlueprintComponent comp)
            {
                foreach (var pw in comp.GetPortWidgets())
                {
                    // 端口全局位置 → 画布空间（ComponentLayer 是 Node2D，ToLocal 可靠）
                    var portCanvas = EditorCanvas.ComponentLayer.ToLocal(pw.GetGlobalPortPosition());
                    float dist = mouseCanvas.DistanceTo(portCanvas);

                    if (dist < 16f)
                        return pw;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 获取画布空间坐标处的连接线。
    /// 遍历 ConnectionLayer 中的所有 ConnectionLine 进行碰撞检测。
    /// </summary>
    private ConnectionLine GetConnectionLineAt(Vector2 mouseLocalPos)
    {
        if (EditorCanvas?.ConnectionLayer == null) return null;

        var mouseCanvas = EditorCanvas.ScreenToCanvas(mouseLocalPos);

        foreach (Node child in EditorCanvas.ConnectionLayer.GetChildren())
        {
            if (child is ConnectionLine line)
            {
                if (line.IntersectsPoint(mouseCanvas, 10.0f))
                    return line;
            }
        }
        return null;
    }

    /// <summary>
    /// 右键点击连接线时弹出上下文菜单，提供"断开连接"选项。
    /// </summary>
    private void ShowConnectionContextMenu(ConnectionLine line, Vector2 screenPos)
    {
        var menu = new PopupMenu();
        menu.Position = (Vector2I)screenPos;

        var srcName = line.SourcePort?.ParentComponent?.ComponentName ?? "未知";
        var tgtName = line.TargetPort?.ParentComponent?.ComponentName ?? "未知";
        menu.AddItem($"断开 {srcName} → {tgtName}", 0);

        var connId = line.ConnectionData?.ConnectionId ?? -1;
        menu.IdPressed += (id) =>
        {
            if (id == 0 && connId > 0)
            {
                ConnectionManager?.RemoveConnection(connId);
            }
        };

        var canvasLayer = EditorCanvas?.GetParent()?.GetNode<CanvasLayer>("CanvasLayer");
        if (canvasLayer != null)
            canvasLayer.AddChild(menu);
        else
            EditorCanvas?.AddChild(menu);
        menu.Popup();
    }

    /// <summary>
    /// 右键点击组件时弹出上下文菜单：重命名、添加/删除端口等。
    /// </summary>
    private void ShowComponentContextMenu(BlueprintComponent comp, Vector2 screenPos)
    {
        var menu = new PopupMenu();
        menu.Position = (Vector2I)screenPos;

        // ── 基本信息 ──
        menu.AddItem($"节点: {comp.ComponentName} (ID={comp.ComponentId})", -1);
        menu.SetItemDisabled(-1, true);
        menu.AddSeparator();

        // ── 添加输入端口 ──
        var addInMenu = new PopupMenu();
        addInMenu.Name = "add_in_port";
        addInMenu.AddItem("exec", 0);
        addInMenu.AddItem("int", 1);
        addInMenu.AddItem("string", 2);
        addInMenu.AddItem("bool", 3);
        addInMenu.IdPressed += (id) =>
        {
            string type = id switch { 0 => "exec", 1 => "int", 2 => "string", 3 => "bool", _ => "exec" };
            comp.AddInputPort($"In_{comp.GetInputPortDefs().Count}", type);
        };
        menu.AddChild(addInMenu);
        menu.AddSubmenuItem("添加输入端口", "add_in_port");

        // ── 添加输出端口 ──
        var addOutMenu = new PopupMenu();
        addOutMenu.Name = "add_out_port";
        addOutMenu.AddItem("exec", 0);
        addOutMenu.AddItem("int", 1);
        addOutMenu.AddItem("string", 2);
        addOutMenu.AddItem("bool", 3);
        addOutMenu.IdPressed += (id) =>
        {
            string type = id switch { 0 => "exec", 1 => "int", 2 => "string", 3 => "bool", _ => "exec" };
            comp.AddOutputPort($"Out_{comp.GetOutputPortDefs().Count}", type);
        };
        menu.AddChild(addOutMenu);
        menu.AddSubmenuItem("添加输出端口", "add_out_port");

        menu.AddSeparator();

        // ── 删除选中组件 ──
        menu.AddItem("删除此节点", 200);

        menu.IdPressed += (id) =>
        {
            if (id == 200)
            {
                var conns = ConnectionManager?.GetConnectionsForComponent(comp.ComponentId);
                if (conns != null)
                    foreach (var conn in conns)
                        ConnectionManager?.RemoveConnection(conn.ConnectionId);
                ComponentSystem?.UnregisterComponent(comp.ComponentId);
            }
        };

        var canvasLayer = EditorCanvas?.GetParent()?.GetNode<CanvasLayer>("CanvasLayer");
        if (canvasLayer != null)
            canvasLayer.AddChild(menu);
        else
            EditorCanvas?.AddChild(menu);
        menu.Popup();
    }

    /// <summary>
    /// 端口坐标诊断：当点击组件但未命中端口时，输出所有端口的坐标对比。
    /// 所有坐标在画布空间中比较，避免 GetGlobalTransformWithCanvas() 的坐标系错误。
    /// </summary>
    private void LogPortDiagnostics(BlueprintComponent comp, Vector2 mouseLocalPos)
    {
        if (EditorCanvas == null) return;

        var mouseCanvas = EditorCanvas.ScreenToCanvas(mouseLocalPos);

        var sb = new StringBuilder(512);
        sb.AppendLine($"[端口诊断] ====================");
        sb.AppendLine($"鼠标(EditorCanvas本地)=({mouseLocalPos.X:F1},{mouseLocalPos.Y:F1})");
        sb.AppendLine($"鼠标(画布空间)=({mouseCanvas.X:F1},{mouseCanvas.Y:F1})");
        sb.AppendLine($"组件名称=\"{comp.ComponentName}\" ID={comp.ComponentId}");
        sb.AppendLine($"组件位置=({comp.Position.X:F1},{comp.Position.Y:F1})");
        sb.AppendLine($"组件尺寸=({comp.ComponentSize.X:F1},{comp.ComponentSize.Y:F1})");
        sb.AppendLine($"组件区域=[{comp.Position.X:F1},{comp.Position.Y:F1}]-[{comp.Position.X + comp.ComponentSize.X:F1},{comp.Position.Y + comp.ComponentSize.Y:F1}]");

        int idx = 0;
        foreach (var pw in comp.GetPortWidgets())
        {
            var portCanvas = EditorCanvas.ComponentLayer.ToLocal(pw.GetGlobalPortPosition());
            float dist = mouseCanvas.DistanceTo(portCanvas);
            sb.AppendLine($"  端口[{idx}] \"{pw.PortDefinition.PortName}\" "
                + $"| 方向={pw.PortDefinition.Direction} "
                + $"| 画布=({portCanvas.X:F1},{portCanvas.Y:F1}) "
                + $"| 距鼠标={dist:F1}px "
                + $"| {(dist < 16f ? "✅ 应命中" : "❌ 距离过远")}"
                + $"| 端口Size=({pw.Size.X:F1},{pw.Size.Y:F1})");
            idx++;
        }

        sb.AppendLine($"EditorCanvas.Size=({EditorCanvas.Size.X:F1},{EditorCanvas.Size.Y:F1})");
        sb.AppendLine($"EditorCanvas.CameraOffset=({EditorCanvas.CameraOffset.X:F1},{EditorCanvas.CameraOffset.Y:F1})");
        sb.AppendLine($"EditorCanvas.ZoomLevel={EditorCanvas.ZoomLevel:F2}");

        if (EditorCanvas.ComponentLayer != null)
        {
            sb.AppendLine($"ComponentLayer.Position=({EditorCanvas.ComponentLayer.Position.X:F1},{EditorCanvas.ComponentLayer.Position.Y:F1})");
            sb.AppendLine($"ComponentLayer.Scale=({EditorCanvas.ComponentLayer.Scale.X:F2},{EditorCanvas.ComponentLayer.Scale.Y:F2})");
        }

        sb.Append($"[端口诊断] ====================");
        GD.Print(sb.ToString());
    }

    /// <summary>
    /// 处理端口输入。
    /// - NONE 模式下点击输出端口 → 开始连接（CONNECTING_PORTS + 临时连线）
    /// - CONNECTING_PORTS 模式下点击输入端口 → 尝试完成连接
    /// - CONNECTING_PORTS 模式下再次点击输出端口 → 重新开始连接
    /// </summary>
    public void HandlePortInput(PortWidget port, InputEventMouseButton mb)
    {
        if (CurrentMode == InteractionMode.None)
        {
            // 只能从输出端口开始拖拽
            if (port.PortDefinition.Direction == PortDefinition.DirectionType.Output)
                StartPortConnection(port);
        }
        else if (CurrentMode == InteractionMode.ConnectingPorts)
        {
            // 如果再次点击的是输出端口，重新开始
            if (port.PortDefinition.Direction == PortDefinition.DirectionType.Output)
            {
                CancelConnection();
                StartPortConnection(port);
            }
            else
            {
                TryCompleteConnection(port);
            }
        }
    }

    /// <summary>从输出端口开始拖拽连线。</summary>
    private void StartPortConnection(PortWidget port)
    {
        GD.Print($"[连接操作] 开始连接 | 端口=\"{port.PortDefinition.PortName}\" | "
            + $"方向={port.PortDefinition.Direction} | "
            + $"端口全局=({port.GetGlobalPortPosition().X:F1},{port.GetGlobalPortPosition().Y:F1}) | "
            + $"组件=\"{port.ParentComponent.ComponentName}\"(ID={port.ParentComponent.ComponentId})");

        CurrentMode = InteractionMode.ConnectingPorts;
        _sourcePort = port;
        _sourceComponent = port.ParentComponent;
        port.SetHighlight(true);

        // 创建临时连线（Line2D），跟随鼠标移动
        _tempLine = new Line2D();
        _tempLine.DefaultColor = new Color("#78909C");
        _tempLine.Width = 3.0f;
        _tempLine.Antialiased = true;
        EditorCanvas?.ConnectionLayer?.AddChild(_tempLine);

        BlueprintDebug.LogDragStarted(
            port.PortDefinition.PortId, port.ParentComponent.ComponentId,
            port.GetGlobalPortPosition(), EditorCanvas?.GetViewport()?.GetMousePosition() ?? Vector2.Zero);
        BlueprintEventBus.Instance.EmitSignal(BlueprintEventBus.SignalName.DragStarted,
            port.PortDefinition.PortId, port.ParentComponent.ComponentId);
    }

    /// <summary>更新临时连线的终点为鼠标当前位置。</summary>
    private void UpdateTempLine()
    {
        if (_tempLine == null || _sourcePort == null) return;
        if (EditorCanvas == null) return;

        var connectionLayer = EditorCanvas.ConnectionLayer;
        if (connectionLayer == null) return;

        // 起点：端口位置 → 使用与 ConnectionLine 相同的方式
        // 即 BlueprintComponent (Node2D) 的 ToLocal + Position
        var srcComp = _sourcePort.ParentComponent;
        var startGlobal = _sourcePort.GetGlobalPortPosition();
        var startLocal = srcComp.ToLocal(startGlobal);
        var start = srcComp.Position + startLocal;

        // 终点：鼠标当前位置（EditorCanvas 本地）→ 画布空间
        // 由于 ConnectionLayer 与 ComponentLayer 变换相同，
        // 画布空间坐标 = 鼠标画布坐标
        var mouseCanvas = EditorCanvas.ScreenToCanvas(_lastLocalMousePos);

        _tempLine.Points = new Vector2[] { start, mouseCanvas };
        _tempLine.QueueRedraw();
    }

    /// <summary>尝试在目标输入端口上完成连接。</summary>
    private void TryCompleteConnection(PortWidget targetPort)
    {
        if (targetPort == _sourcePort) { CancelConnection(); return; }
        if (targetPort.PortDefinition.Direction == PortDefinition.DirectionType.Output) { CancelConnection(); return; }
        if (ConnectionManager == null) { CancelConnection(); return; }

        // 调试日志：连接完成前的坐标对比
        GD.Print($"[连接操作] 尝试完成连接");
        GD.Print($"  源端口 \"{_sourcePort.PortDefinition.PortName}\" → 目标端口 \"{targetPort.PortDefinition.PortName}\"");
        GD.Print($"  源端口全局=({_sourcePort.GetGlobalPortPosition().X:F1},{_sourcePort.GetGlobalPortPosition().Y:F1})");
        GD.Print($"  目标端口全局=({targetPort.GetGlobalPortPosition().X:F1},{targetPort.GetGlobalPortPosition().Y:F1})");

        var result = ConnectionManager.ValidateAndConnect(
            _sourceComponent.ComponentId, _sourcePort.PortDefinition.PortId,
            targetPort.ParentComponent.ComponentId, targetPort.PortDefinition.PortId);

        if (result.Success)
        {
            BlueprintEventBus.Instance.EmitSignal(BlueprintEventBus.SignalName.ConnectionCreated, result.Connection);

            BlueprintDebug.LogConnectionCreated(
                connectionId: result.Connection.ConnectionId,
                srcCompId: result.Connection.SourceComponentId,
                srcPortId: result.Connection.SourcePortId,
                tgtCompId: result.Connection.TargetComponentId,
                tgtPortId: result.Connection.TargetPortId,
                connectionType: result.Connection.ConnectionType);

            // 将正式连接线添加到 connectionLayer
            if (EditorCanvas?.ConnectionLayer != null && result.Connection != null)
            {
                var line = ConnectionManager.GetConnectionLine(result.Connection.ConnectionId);
                if (line != null)
                {
                    EditorCanvas.ConnectionLayer.AddChild(line);
                    line.UpdatePosition();
                }
            }

            // 连接成功视觉提示：在连线中点显示浮动标签
            ShowConnectionSuccessHint(_sourcePort, targetPort);
        }
        else
        {
            BlueprintDebug.LogConnectionFailed(
                _sourceComponent.ComponentId, _sourcePort.PortDefinition.PortId,
                targetPort.ParentComponent.ComponentId, targetPort.PortDefinition.PortId,
                result.ErrorMessage);
            GD.PushWarning($"Connection failed: {result.ErrorMessage}");
            ShowConnectionFailedHint(_sourcePort, targetPort, result.ErrorMessage);
        }

        CancelConnection();
    }

    /// <summary>
    /// 更新目标端口高亮：在 CONNECTING_PORTS 模式下，检测鼠标附近的输入端口，
    /// 高亮显示并弹出"可连接"信息标签。
    /// </summary>
    private void UpdateTargetPortHighlight(Vector2 mouseLocalPos)
    {
        if (_sourcePort == null) return;

        // 查找最近的输入端口
        PortWidget nearestInput = null;
        float nearestDist = 30f; // 激活距离阈值
        var mouseCanvas = EditorCanvas?.ScreenToCanvas(mouseLocalPos);

        if (mouseCanvas != null && EditorCanvas?.ComponentLayer != null)
        {
            foreach (Node child in EditorCanvas.ComponentLayer.GetChildren())
            {
                if (child is BlueprintComponent comp)
                {
                    foreach (var pw in comp.GetPortWidgets())
                    {
                        if (pw.PortDefinition.Direction == PortDefinition.DirectionType.Input
                            && pw != _sourcePort)
                        {
                            var portCanvas = EditorCanvas.ComponentLayer.ToLocal(pw.GetGlobalPortPosition());
                            float dist = mouseCanvas.Value.DistanceTo(portCanvas);
                            if (dist < nearestDist)
                            {
                                nearestDist = dist;
                                nearestInput = pw;
                            }
                        }
                    }
                }
            }
        }

        // 更新高亮和标签
        if (nearestInput != null && nearestInput != _hoveredTargetPort)
        {
            // 清除旧高亮
            ClearTargetPortHighlight();
            // 设置新高亮
            _hoveredTargetPort = nearestInput;
            _hoveredTargetPort.SetHighlight(true);
            ShowPortHintLabel(_hoveredTargetPort);
        }
        else if (nearestInput == null && _hoveredTargetPort != null)
        {
            ClearTargetPortHighlight();
        }
    }

    /// <summary>显示端口提示标签：显示"可连接"和源组件信息。</summary>
    private void ShowPortHintLabel(PortWidget targetPort)
    {
        if (_portHintLabel != null)
        {
            _portHintLabel.QueueFree();
            _portHintLabel = null;
        }

        var canvasLayer = EditorCanvas?.GetParent()?.GetNode<CanvasLayer>("CanvasLayer");
        if (canvasLayer == null) return;

        var sourceName = _sourceComponent?.ComponentName ?? "未知";
        var targetCompName = targetPort.ParentComponent?.ComponentName ?? "未知";

        // 端口全局位置（屏幕坐标）直接作为标签位置
        var portScreen = targetPort.GetGlobalPortPosition();

        _portHintLabel = new Label();
        _portHintLabel.Text = $"← 来自: {sourceName}\n可连接: {targetCompName}";
        _portHintLabel.AddThemeColorOverride("font_color", new Color("#4CAF50"));
        _portHintLabel.AddThemeFontSizeOverride("font_size", 11);
        _portHintLabel.Position = portScreen + new Vector2(20, -16);
        _portHintLabel.Size = new Vector2(160, 36);
        _portHintLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        canvasLayer.AddChild(_portHintLabel);
    }

    /// <summary>清除目标端口高亮和提示标签。</summary>
    private void ClearTargetPortHighlight()
    {
        if (_hoveredTargetPort != null)
        {
            _hoveredTargetPort.SetHighlight(false);
            _hoveredTargetPort = null;
        }
        if (_portHintLabel != null)
        {
            _portHintLabel.QueueFree();
            _portHintLabel = null;
        }
    }

    /// <summary>取消当前连接操作，清理临时连线。</summary>
    private void CancelConnection()
    {
        if (_sourcePort != null)
            _sourcePort.SetHighlight(false);
        if (_tempLine != null)
        {
            _tempLine.QueueFree();
            _tempLine = null;
        }

        ClearTargetPortHighlight();

        CurrentMode = InteractionMode.None;
        _sourcePort = null;
        _sourceComponent = null;

        BlueprintEventBus.Instance.EmitSignal(BlueprintEventBus.SignalName.DragEnded, false);
    }

    // ────────────────────────────────────────────────────────────────
    // 连接提示
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 连接成功时在连线中点显示浮动提示标签，约 1.5 秒后自动消失。
    /// 标签添加到 CanvasLayer（屏幕空间 UI），位置转换为屏幕坐标。
    /// 使用 Tween 实现上浮 + 淡出动画。
    /// </summary>
    private void ShowConnectionSuccessHint(PortWidget from, PortWidget to)
    {
        var connectionLayer = EditorCanvas?.ConnectionLayer;
        if (connectionLayer == null) return;

        // 计算连线中点（画布空间）
        var fromComp = from.ParentComponent;
        var toComp = to.ParentComponent;
        if (fromComp == null || toComp == null) return;

        var fromGlobal = from.GetGlobalPortPosition();
        var toGlobal = to.GetGlobalPortPosition();
        var fromLocal = fromComp.ToLocal(fromGlobal);
        var toLocal = toComp.ToLocal(toGlobal);
        var fromCanvas = fromComp.Position + fromLocal;
        var toCanvas = toComp.Position + toLocal;
        var midCanvas = (fromCanvas + toCanvas) / 2;

        // 将画布中点转为屏幕坐标
        var midScreen = EditorCanvas.CanvasToScreen(midCanvas);

        var hint = new Label();
        hint.Text = "✓ 连接成功";
        hint.AddThemeColorOverride("font_color", new Color("#4CAF50"));
        hint.AddThemeFontSizeOverride("font_size", 14);
        hint.Position = midScreen - new Vector2(50, 8);
        hint.Size = new Vector2(100, 20);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.MouseFilter = Control.MouseFilterEnum.Ignore;

        // 添加到 CanvasLayer（屏幕空间 UI），确保始终可见
        var canvasLayer = EditorCanvas.GetParent()?.GetNode<CanvasLayer>("CanvasLayer");
        if (canvasLayer != null)
            canvasLayer.AddChild(hint);
        else
            EditorCanvas.AddChild(hint);

        // Tween 动画：上浮 + 淡出
        var tween = CreateTween();
        tween.TweenProperty(hint, "position", hint.Position + new Vector2(0, -30), 0.8f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(hint, "modulate",
            new Color(1, 1, 1, 0), 1.2f)
             .SetDelay(0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(hint))
                hint.QueueFree();
        }));
    }

    /// <summary>
    /// 连接失败时在连线中点显示红色提示标签，约 2 秒后自动消失。
    /// </summary>
    private void ShowConnectionFailedHint(PortWidget from, PortWidget to, string reason)
    {
        var connectionLayer = EditorCanvas?.ConnectionLayer;
        if (connectionLayer == null) return;

        var fromComp = from.ParentComponent;
        var toComp = to.ParentComponent;
        if (fromComp == null || toComp == null) return;

        var fromGlobal = from.GetGlobalPortPosition();
        var toGlobal = to.GetGlobalPortPosition();
        var fromLocal = fromComp.ToLocal(fromGlobal);
        var toLocal = toComp.ToLocal(toGlobal);
        var fromCanvas = fromComp.Position + fromLocal;
        var toCanvas = toComp.Position + toLocal;
        var midCanvas = (fromCanvas + toCanvas) / 2;
        var midScreen = EditorCanvas.CanvasToScreen(midCanvas);

        var hint = new Label();
        hint.Text = $"✗ {reason}";
        hint.AddThemeColorOverride("font_color", new Color("#F44336"));
        hint.AddThemeFontSizeOverride("font_size", 12);
        hint.Position = midScreen - new Vector2(60, 8);
        hint.Size = new Vector2(120, 20);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.MouseFilter = Control.MouseFilterEnum.Ignore;

        var canvasLayer = EditorCanvas.GetParent()?.GetNode<CanvasLayer>("CanvasLayer");
        if (canvasLayer != null)
            canvasLayer.AddChild(hint);
        else
            EditorCanvas.AddChild(hint);

        var tween = CreateTween();
        tween.TweenProperty(hint, "position", hint.Position + new Vector2(0, -20), 1.0f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(hint, "modulate", new Color(1, 1, 1, 0), 1.5f)
             .SetDelay(0.5f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(hint))
                hint.QueueFree();
        }));
    }

    // ────────────────────────────────────────────────────────────────
    // 画布平移
    // ────────────────────────────────────────────────────────────────

    private void PanCanvas(InputEventMouseMotion mm, Vector2 mousePos)
    {
        if (EditorCanvas == null) return;
        EditorCanvas.PanTo(_panOffsetStart + mousePos - _panStart);
    }

    // ────────────────────────────────────────────────────────────────
    // 操作结束
    // ────────────────────────────────────────────────────────────────

    private void EndCurrentOperation()
    {
        switch (CurrentMode)
        {
            case InteractionMode.DraggingComponent:
                if (_dragComponent != null)
                {
                    BlueprintDebug.LogComponentMoved(
                        _dragComponent.ComponentId, _dragComponent.ComponentName,
                        _dragComponentStart, _dragComponent.Position);
                    BlueprintEventBus.Instance.EmitSignal(BlueprintEventBus.SignalName.ComponentMoved,
                        _dragComponent.ComponentId, _dragComponent.Position);
                }
                _dragComponent = null;
                CurrentMode = InteractionMode.None;
                break;
            case InteractionMode.ConnectingPorts:
                CancelConnection();
                break;
            default:
                CurrentMode = InteractionMode.None;
                break;
        }
    }
}
