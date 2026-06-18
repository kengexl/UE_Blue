using Godot;

// ============================================================================
// EditorCanvas.cs — 编辑画布
//
// 职责：
//   蓝图编辑器的主画布区域。管理组件层和连接线层的定位/缩放。
//   提供屏幕坐标与画布坐标的转换方法。
//   托管上下文菜单的创建和响应。
//
// 注意：
//   输入事件不由本控件的 _GuiInput 处理，
//   而是通过 BlueprintEditor._Input() → InteractionSystem 统一路由。
// ============================================================================

/// <summary>编辑画布 — 蓝图编辑器的核心 UI 区域。</summary>
public partial class EditorCanvas : Control
{
    // ────────────────────────────────────────────────────────────────
    // 子节点引用
    // ────────────────────────────────────────────────────────────────

    private GridBackground _gridBackground;
    private Node2D _componentLayer;
    private Node2D _connectionLayer;

    // ────────────────────────────────────────────────────────────────
    // 编辑器状态
    // ────────────────────────────────────────────────────────────────

    private float _zoomLevel = 1.0f;
    private Vector2 _cameraOffset = Vector2.Zero;

    // ────────────────────────────────────────────────────────────────
    // 外部注入的系统引用
    // ────────────────────────────────────────────────────────────────

    public InteractionSystem InteractionSystem { get; set; }
    public ComponentSystem ComponentSystem { get; set; }
    public ConnectionManager ConnectionManager { get; set; }

    // ────────────────────────────────────────────────────────────────
    // 只读属性
    // ────────────────────────────────────────────────────────────────

    /// <summary>组件层（Node2D），所有组件实例的直接父节点。</summary>
    public Node2D ComponentLayer => _componentLayer;
    /// <summary>连接线层（Node2D），所有连接线的直接父节点。</summary>
    public Node2D ConnectionLayer => _connectionLayer;
    /// <summary>当前缩放级别。</summary>
    public float ZoomLevel => _zoomLevel;
    /// <summary>当前画布偏移量。</summary>
    public Vector2 CameraOffset => _cameraOffset;

    // ────────────────────────────────────────────────────────────────
    // 生命周期
    // ────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _gridBackground = GetNode<GridBackground>("GridBackground");
        _componentLayer = GetNode<Node2D>("ComponentLayer");
        _connectionLayer = GetNode<Node2D>("ConnectionLayer");

        // 修复：Control 作为 Node2D 子节点时锚点不生效，导致 Size=(0,0)
        UpdateSizeFromViewport();

        // 显式确保 EditorCanvas 位于 (0,0)
        Position = Vector2.Zero;
        OffsetLeft = 0;
        OffsetTop = 0;
        OffsetRight = 0;
        OffsetBottom = 0;

        // 输入由父节点 BlueprintEditor._Input() 处理
        MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    /// <summary>监听窗口大小变化，同步更新画布尺寸。</summary>
    public override void _Notification(int what)
    {
        if (what == Node.NotificationWMSizeChanged)
        {
            UpdateSizeFromViewport();
        }
    }

    /// <summary>
    /// 从视口获取大小并设置。Control 在 Node2D 父级下没有布局，
    /// 必须手动同步视口尺寸。
    /// </summary>
    private void UpdateSizeFromViewport()
    {
        var viewport = GetViewportRect();
        Size = viewport.Size;
    }

    // ────────────────────────────────────────────────────────────────
    // 画布控制
    // ────────────────────────────────────────────────────────────────

    /// <summary>设置缩放级别（0.25 ~ 3.0）。</summary>
    public void SetZoom(float level)
    {
        _zoomLevel = Mathf.Clamp(level, 0.25f, 3.0f);
        UpdateLayers();
    }

    /// <summary>平移到指定偏移。</summary>
    public void PanTo(Vector2 offset)
    {
        _cameraOffset = offset;
        UpdateLayers();
    }

    /// <summary>应用当前偏移和缩放到所有层。</summary>
    private void UpdateLayers()
    {
        if (_componentLayer != null)
        {
            _componentLayer.Position = _cameraOffset;
            _componentLayer.Scale = new Vector2(_zoomLevel, _zoomLevel);
        }
        if (_connectionLayer != null)
        {
            _connectionLayer.Position = _cameraOffset;
            _connectionLayer.Scale = new Vector2(_zoomLevel, _zoomLevel);
        }
        if (_gridBackground != null)
            _gridBackground.UpdateGrid(_cameraOffset, _zoomLevel);
    }

    // ────────────────────────────────────────────────────────────────
    // 坐标转换
    // ────────────────────────────────────────────────────────────────

    /// <summary>屏幕坐标 → 画布坐标。</summary>
    public Vector2 ScreenToCanvas(Vector2 screenPos)
    {
        return (screenPos - _cameraOffset) / _zoomLevel;
    }

    /// <summary>画布坐标 → 屏幕坐标。</summary>
    public Vector2 CanvasToScreen(Vector2 canvasPos)
    {
        return canvasPos * _zoomLevel + _cameraOffset;
    }

    // ────────────────────────────────────────────────────────────────
    // 上下文菜单
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 由 InteractionSystem 调用，在右键位置弹出上下文菜单。
    /// </summary>
    public void ShowContextMenuAt(Vector2 screenPos, Vector2 canvasPos)
    {
        var menu = new PopupMenu();
        menu.Position = (Vector2I)screenPos;

        // 缓存创建时的坐标，供闭包使用
        var creationScreenPos = screenPos;
        var creationCanvasPos = canvasPos;

        // ── 添加组件子菜单 ──
        var addMenu = new PopupMenu();
        addMenu.Name = "add_test_component";
        addMenu.AddItem("测试组件", 0);
        addMenu.IdPressed += (id) =>
        {
            if (id == 0)
                CreateComponentAt(ComponentData.ComponentType.Dual, creationCanvasPos, creationScreenPos, "右键菜单");
        };
        menu.AddChild(addMenu);
        menu.AddSubmenuItem("添加组件", "add_test_component");

        menu.AddSeparator();
        menu.AddItem("全选", 100);
        menu.AddItem("删除选中", 101);
        menu.AddSeparator();
        menu.AddItem("导出图...", 300);
        menu.AddItem("导入图...", 301);
        menu.AddSeparator();
        menu.AddItem("显示网格", 200);

        menu.IdPressed += (id) =>
        {
            switch (id)
            {
                case 100: SelectAllComponents(); break;
                case 101: DeleteSelected(); break;
                case 200:
                    var idx = menu.GetItemIndex((int)id);
                    menu.SetItemChecked(idx, !menu.IsItemChecked(idx));
                    ToggleGrid(menu.IsItemChecked(idx));
                    break;
                case 300: ExportGraph(); break;
                case 301: ImportGraph(); break;
            }
        };

        AddChild(menu);
        menu.Popup();
    }

    /// <summary>在画布指定位置创建组件，并输出调试日志。</summary>
    private void CreateComponentAt(ComponentData.ComponentType compType, Vector2 canvasPos, Vector2 screenPos, string triggerMethod)
    {
        if (ComponentSystem == null || _componentLayer == null) return;
        var comp = ComponentSystem.CreateComponent(compType, canvasPos);
        if (comp != null)
        {
            _componentLayer.AddChild(comp);

            // 调试日志：记录节点创建的关键信息
            BlueprintDebug.LogNodeCreation(
                componentId: comp.ComponentId,
                componentType: comp.GetType().Name,
                canvasPos: canvasPos,
                screenPos: screenPos,
                triggerMethod: triggerMethod,
                componentName: comp.ComponentName
            );

            BlueprintEventBus.Instance.EmitSignal(
                BlueprintEventBus.SignalName.ComponentAdded, comp.ComponentId);
        }
    }

    /// <summary>选中所有组件。</summary>
    private void SelectAllComponents()
    {
        if (_componentLayer == null) return;
        foreach (Node child in _componentLayer.GetChildren())
        {
            if (child is BlueprintComponent comp)
            {
                comp.SetSelected(true);
                BlueprintEventBus.Instance.EmitSignal(
                    BlueprintEventBus.SignalName.ComponentSelected, comp.ComponentId);
            }
        }
    }

    /// <summary>删除当前选中的组件及其关联连接。</summary>
    private void DeleteSelected()
    {
        if (_componentLayer == null) return;
        var toDelete = new System.Collections.Generic.List<BlueprintComponent>();
        foreach (Node child in _componentLayer.GetChildren())
        {
            if (child is BlueprintComponent comp && comp.IsSelected)
                toDelete.Add(comp);
        }
        foreach (var comp in toDelete)
        {
            // 先移除关联连接
            var conns = ConnectionManager?.GetConnectionsForComponent(comp.ComponentId);
            if (conns != null)
            {
                foreach (var conn in conns)
                    ConnectionManager?.RemoveConnection(conn.ConnectionId);
            }
            ComponentSystem?.UnregisterComponent(comp.ComponentId);
        }
    }

    /// <summary>切换网格可见性。</summary>
    private void ToggleGrid(bool visible)
    {
        if (_gridBackground != null)
            _gridBackground.Visible = visible;
    }

    // ────────────────────────────────────────────────────────────────
    // 导入/导出
    // ────────────────────────────────────────────────────────────────

    /// <summary>导出当前图为 .txt 文件。</summary>
    private void ExportGraph()
    {
        var fileDialog = new FileDialog();
        fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        fileDialog.Access = Godot.FileDialog.AccessEnum.Filesystem;
        fileDialog.AddFilter("*.txt", "Blueprint Graph Text");
        fileDialog.CurrentFile = "blueprint_graph.txt";
        fileDialog.CurrentDir = "res://";
        fileDialog.Title = "导出蓝图图";
        fileDialog.FileSelected += (path) =>
        {
            GraphExporter.ExportToFile(path, this, ConnectionManager, "Exported Graph");
        };
        GetTree().Root.AddChild(fileDialog);
        fileDialog.PopupCentered();
    }

    /// <summary>从 .txt 文件导入蓝图图。</summary>
    private async void ImportGraph()
    {
        var fileDialog = new FileDialog();
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.Access = Godot.FileDialog.AccessEnum.Filesystem;
        fileDialog.AddFilter("*.txt", "Blueprint Graph Text");
        fileDialog.CurrentDir = "res://";
        fileDialog.Title = "导入蓝图图";
        fileDialog.FileSelected += async (path) =>
        {
            var importer = new GraphImporter();
            var result = importer.ImportFromFile(path, this, ComponentSystem, ConnectionManager);
            if (result.Success)
            {
                await ToSignal(GetTree(), "process_frame");
                int conns = importer.CompleteConnections(path, this, ConnectionManager);
                GD.Print($"[EditorCanvas] Imported: {result.NodesCreated} nodes, {conns} connections");
            }
            else
            {
                GD.PushError($"[EditorCanvas] Import failed: {result.Error}");
            }
        };
        GetTree().Root.AddChild(fileDialog);
        fileDialog.PopupCentered();
    }
}
