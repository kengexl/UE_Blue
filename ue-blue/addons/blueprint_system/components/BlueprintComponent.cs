using Godot;
using System.Collections.Generic;

// ============================================================================
// BlueprintComponent.cs — 蓝图组件抽象基类
//
// 职责：
//   所有蓝图组件的基类，继承 Node2D 使其可在 2D 画布上定位。
//   提供统一的视觉树构建（背景、Header、标题、端口）、
//   选中/高亮机制、端口查找方法。
//
// 子类：
//   ├─ TypeAComponent  — 输出专用（绿色 Header）
//   ├─ TypeBComponent  — 双向（蓝色 Header）
//   ├─ TypeCComponent  — 输入专用（橙红 Header）
//   └─ TestComponent   — 测试组件（继承 TypeB）
// ============================================================================

/// <summary>
/// 蓝图组件基类 — 所有蓝图组件的运行时实体。
/// </summary>
public partial class BlueprintComponent : Node2D
{
    // ────────────────────────────────────────────────────────────────
    // 信号
    // ────────────────────────────────────────────────────────────────

    /// <summary>组件位置变化时发射。</summary>
    [Signal] public delegate void ComponentMovedEventHandler(Vector2 newPosition);
    /// <summary>端口被点击时发射。</summary>
    [Signal] public delegate void PortClickedEventHandler(PortWidget port, BlueprintComponent component);
    /// <summary>端口添加了连接时发射。</summary>
    [Signal] public delegate void ConnectionAddedEventHandler(int portId, ConnectionData connection);
    /// <summary>端口移除了连接时发射。</summary>
    [Signal] public delegate void ConnectionRemovedEventHandler(int portId, ConnectionData connection);

    // ────────────────────────────────────────────────────────────────
    // 属性
    // ────────────────────────────────────────────────────────────────

    /// <summary>组件唯一标识（由 ComponentSystem 分配）。</summary>
    public int ComponentId { get; set; } = -1;
    /// <summary>组件显示名称。</summary>
    public string ComponentName { get; set; } = "Component";
    /// <summary>组件类型（输出专用/双向/输入专用）。</summary>
    public ComponentData.ComponentType Type { get; set; }
    /// <summary>关联的数据模型对象。</summary>
    public ComponentData Data { get; set; }
    /// <summary>当前是否被选中。</summary>
    public bool IsSelected { get; set; } = false;

    // ────────────────────────────────────────────────────────────────
    // 子节点引用（在 BuildVisualTree 中创建）
    // ────────────────────────────────────────────────────────────────

    /// <summary>标题栏 ColorRect。</summary>
    public ColorRect HeaderBar { get; private set; }
    /// <summary>标题文本 Label。</summary>
    public Label TitleLabel { get; private set; }
    /// <summary>端口控件容器。</summary>
    public VBoxContainer PortContainer { get; private set; }
    /// <summary>背景面板。</summary>
    public Panel Background { get; private set; }

    // ────────────────────────────────────────────────────────────────
    // 端口数据（子类在构造函数中填充）
    // ────────────────────────────────────────────────────────────────

    /// <summary>输入端口定义列表。</summary>
    protected List<PortDefinition> _inputPorts = new();
    /// <summary>输出端口定义列表。</summary>
    protected List<PortDefinition> _outputPorts = new();
    /// <summary>所有端口控件列表。</summary>
    protected List<PortWidget> _portWidgets = new();
    /// <summary>输入端口控件列表。</summary>
    protected List<PortWidget> _inputPortWidgets = new();
    /// <summary>输出端口控件列表。</summary>
    protected List<PortWidget> _outputPortWidgets = new();

    /// <summary>下一个可用的自动端口 ID。</summary>
    private int _nextAutoPortId = 100;

    // ────────────────────────────────────────────────────────────────
    // 视觉配置（子类可覆盖）
    // ────────────────────────────────────────────────────────────────

    /// <summary>标题栏颜色。</summary>
    public Color HeaderColor { get; set; } = new Color("#2196F3");
    /// <summary>边框颜色。</summary>
    public Color BorderColor { get; set; } = new Color("#1976D2");
    /// <summary>背景颜色。</summary>
    public Color BackgroundColor { get; set; } = new Color("#2D2D2D");
    /// <summary>组件尺寸。</summary>
    public Vector2 ComponentSize { get; set; } = new Vector2(160, 80);

    // ────────────────────────────────────────────────────────────────
    // 生命周期
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 节点进入场景树时构建视觉树。
    /// 注意：必须确保组件已被 AddChild 加入场景树，_Ready 才会被调用。
    /// </summary>
    public override void _Ready()
    {
        BuildVisualTree();
    }

    // ────────────────────────────────────────────────────────────────
    // 视觉树构建
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 构建组件的视觉层次结构：
    ///   Panel（背景+边框）
    ///   ├─ ColorRect（Header 条）
    ///   ├─ Label（标题）
    ///   └─ VBoxContainer（端口列表）
    ///       ├─ PortWidget（输入端口）
    ///       └─ PortWidget（输出端口）
    /// </summary>
    protected void BuildVisualTree()
    {
        // 根据端口数量自动调整初始尺寸（在构建前计算）
        UpdateComponentHeight();

        // ── 背景面板（带边框和圆角） ──
        Background = new Panel();
        Background.Size = ComponentSize;
        Background.Position = Vector2.Zero;
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = BackgroundColor;
        bgStyle.BorderColor = BorderColor;
        bgStyle.SetBorderWidthAll(1);
        bgStyle.SetCornerRadiusAll(4);
        Background.AddThemeStyleboxOverride("panel", bgStyle);
        Background.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(Background);

        // ── 标题栏 ──
        HeaderBar = new ColorRect();
        HeaderBar.Color = HeaderColor;
        HeaderBar.Size = new Vector2(ComponentSize.X, 24);
        HeaderBar.Position = Vector2.Zero;
        HeaderBar.MouseFilter = Control.MouseFilterEnum.Pass;
        AddChild(HeaderBar);

        // ── 标题文本 ──
        TitleLabel = new Label();
        TitleLabel.Text = ComponentName;
        TitleLabel.Position = new Vector2(8, 2);
        TitleLabel.Size = new Vector2(ComponentSize.X - 16, 20);
        TitleLabel.AddThemeColorOverride("font_color", Colors.White);
        TitleLabel.AddThemeFontSizeOverride("font_size", 12);
        TitleLabel.MouseFilter = Control.MouseFilterEnum.Pass;
        AddChild(TitleLabel);

        // ── 端口容器（VBoxContainer，每行一个 HBoxContainer） ──
        PortContainer = new VBoxContainer();
        PortContainer.Position = new Vector2(0, 28);
        PortContainer.Size = new Vector2(ComponentSize.X, ComponentSize.Y - 28);
        PortContainer.ClipContents = false; // 允许端口图标探出组件边框
        PortContainer.MouseFilter = Control.MouseFilterEnum.Pass;
        AddChild(PortContainer);

        // ── 创建端口行：每行一个 HBoxContainer，水平排列 In + Spacer + Out ──
        // 输入端口居左，输出端口居右，同索引的 In/Out 在同一行水平对齐。
        // 端口图标绘制在控件边缘（圆形在 x=0，菱形在 x=width），
        // 因此可直观探出到组件边框位置。
        int maxPortCount = Mathf.Max(_inputPorts.Count, _outputPorts.Count);
        for (int i = 0; i < maxPortCount; i++)
        {
            var row = new HBoxContainer();
            row.MouseFilter = Control.MouseFilterEnum.Pass;
            row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            // 左侧：输入端口（如果有）
            if (i < _inputPorts.Count)
            {
                var portDef = _inputPorts[i];
                var pw = new PortWidget();
                pw.PortDefinition = portDef;
                pw.ParentComponent = this;
                pw.MouseFilter = Control.MouseFilterEnum.Pass;
                _inputPortWidgets.Add(pw);
                _portWidgets.Add(pw);
                row.AddChild(pw);
            }

            // 中间：弹性空白（让输入靠左、输出靠右）
            var spacer = new Control();
            spacer.MouseFilter = Control.MouseFilterEnum.Ignore;
            spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(spacer);

            // 右侧：输出端口（如果有）
            if (i < _outputPorts.Count)
            {
                var portDef = _outputPorts[i];
                var pw = new PortWidget();
                pw.PortDefinition = portDef;
                pw.ParentComponent = this;
                pw.MouseFilter = Control.MouseFilterEnum.Pass;
                _outputPortWidgets.Add(pw);
                _portWidgets.Add(pw);
                row.AddChild(pw);
            }

            // 垂直居中对齐
            row.Alignment = BoxContainer.AlignmentMode.Center;

            PortContainer.AddChild(row);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 公共 API
    // ────────────────────────────────────────────────────────────────

    /// <summary>从 ComponentData 初始化组件属性。</summary>
    public void Initialize(ComponentData data)
    {
        Data = data;
        ComponentId = data.ComponentId;
        ComponentName = data.ComponentName;
        Type = data.Type;
        Position = data.Position;
        ComponentSize = data.Size;
        if (TitleLabel != null) TitleLabel.Text = ComponentName;
        if (Background != null) Background.Size = ComponentSize;
        if (HeaderBar != null) HeaderBar.Size = new Vector2(ComponentSize.X, 24);
    }

    /// <summary>获取所有端口控件。</summary>
    public List<PortWidget> GetPortWidgets() => _portWidgets;

    /// <summary>根据端口 ID 查找端口控件。</summary>
    public PortWidget GetPortWidgetById(int portId)
    {
        foreach (var pw in _portWidgets)
            if (pw.PortDefinition.PortId == portId) return pw;
        return null;
    }

    /// <summary>获取所有输入端口控件。</summary>
    public List<PortWidget> GetInputPortWidgets() => _inputPortWidgets;

    /// <summary>获取所有输出端口控件。</summary>
    public List<PortWidget> GetOutputPortWidgets() => _outputPortWidgets;

    /// <summary>获取输入端口定义列表。</summary>
    public List<PortDefinition> GetInputPortDefs() => _inputPorts;
    /// <summary>获取输出端口定义列表。</summary>
    public List<PortDefinition> GetOutputPortDefs() => _outputPorts;

    // ────────────────────────────────────────────────────────────────
    // 端口管理 API（动态添加/重命名端口）
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 添加一个输入端口。如果视觉树已构建则即时添加。
    /// </summary>
    /// <param name="name">端口显示名称。</param>
    /// <param name="type">端口类型。默认 "exec"。</param>
    /// <param name="portId">端口 ID。-1 则自动分配。</param>
    public PortWidget AddInputPort(string name = "In", string type = "exec", int portId = -1)
    {
        var def = new PortDefinition();
        def.PortId = portId > 0 ? portId : _nextAutoPortId++;
        def.PortName = name;
        def.Direction = PortDefinition.DirectionType.Input;
        def.PortType = type;
        _inputPorts.Add(def);

        if (PortContainer != null)
            RebuildPortWidgets();
        return GetPortWidgetById(def.PortId);
    }

    /// <summary>
    /// 添加一个输出端口。如果视觉树已构建则即时添加。
    /// </summary>
    /// <param name="name">端口显示名称。</param>
    /// <param name="type">端口类型。默认 "exec"。</param>
    /// <param name="portId">端口 ID。-1 则自动分配。</param>
    public PortWidget AddOutputPort(string name = "Out", string type = "exec", int portId = -1)
    {
        var def = new PortDefinition();
        def.PortId = portId > 0 ? portId : _nextAutoPortId++;
        def.PortName = name;
        def.Direction = PortDefinition.DirectionType.Output;
        def.PortType = type;
        _outputPorts.Add(def);

        if (PortContainer != null)
            RebuildPortWidgets();
        return GetPortWidgetById(def.PortId);
    }

    /// <summary>重命名指定 ID 的端口。</summary>
    public void SetPortName(int portId, string newName)
    {
        foreach (var def in _inputPorts)
            if (def.PortId == portId) { def.PortName = newName; break; }
        foreach (var def in _outputPorts)
            if (def.PortId == portId) { def.PortName = newName; break; }

        foreach (var pw in _portWidgets)
        {
            if (pw.PortDefinition.PortId == portId)
            {
                foreach (Node child in pw.GetChildren())
                    if (child is Label label) { label.Text = newName; break; }
                break;
            }
        }
    }

    /// <summary>
    /// 批量设置输入端口（清空现有）。
    /// 用法: comp.SetInputPorts(("Exec","exec"), ("Data","int"))
    /// </summary>
    public void SetInputPorts(params (string name, string type)[] ports)
    {
        _inputPorts.Clear();
        foreach (var (name, type) in ports)
        {
            _inputPorts.Add(new PortDefinition
            {
                PortId = _nextAutoPortId++,
                PortName = name,
                Direction = PortDefinition.DirectionType.Input,
                PortType = type
            });
        }
        if (PortContainer != null) RebuildPortWidgets();
    }

    /// <summary>
    /// 批量设置输出端口（清空现有）。
    /// 用法: comp.SetOutputPorts(("Then","exec"), ("Result","int"))
    /// </summary>
    public void SetOutputPorts(params (string name, string type)[] ports)
    {
        _outputPorts.Clear();
        foreach (var (name, type) in ports)
        {
            _outputPorts.Add(new PortDefinition
            {
                PortId = _nextAutoPortId++,
                PortName = name,
                Direction = PortDefinition.DirectionType.Output,
                PortType = type
            });
        }
        if (PortContainer != null) RebuildPortWidgets();
    }

    /// <summary>重建端口控件（添加/删除端口后调用）。</summary>
    private void RebuildPortWidgets()
    {
        if (PortContainer == null) return;

        // 重要：先更新所有引用此组件端口的 ConnectionLine，
        // 防止 QueueFree 后 ConnectionLine 持有已释放的 PortWidget 引用。
        UpdateConnectionLineReferences();

        // 清除旧端口控件
        foreach (Node child in PortContainer.GetChildren())
            child.QueueFree();
        _portWidgets.Clear();
        _inputPortWidgets.Clear();
        _outputPortWidgets.Clear();

        // 重新构建端口行
        int maxPorts = Mathf.Max(_inputPorts.Count, _outputPorts.Count);
        for (int i = 0; i < maxPorts; i++)
        {
            var row = new HBoxContainer();
            row.MouseFilter = Control.MouseFilterEnum.Pass;
            row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            if (i < _inputPorts.Count)
            {
                var pw = MakeWidget(_inputPorts[i]);
                _inputPortWidgets.Add(pw);
                _portWidgets.Add(pw);
                row.AddChild(pw);
            }
            var spacer = new Control();
            spacer.MouseFilter = Control.MouseFilterEnum.Ignore;
            spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(spacer);

            if (i < _outputPorts.Count)
            {
                var pw = MakeWidget(_outputPorts[i]);
                _outputPortWidgets.Add(pw);
                _portWidgets.Add(pw);
                row.AddChild(pw);
            }
            row.Alignment = BoxContainer.AlignmentMode.Center;
            PortContainer.AddChild(row);
        }

        // 重建完成后更新所有 ConnectionLine 的端口引用为新的 PortWidget
        RefreshConnectionLineReferences();
        UpdateComponentHeight();
    }

    /// <summary>
    /// 在端口重建前，将旧端口的引用从 ConnectionLine 中清除。
    /// 防止 QueueFree 后 ConnectionLine 访问已释放的 PortWidget。
    /// </summary>
    private void UpdateConnectionLineReferences()
    {
        // 通过向上遍历到 EditorCanvas 查找 ConnectionManager
        // BlueprintComponent → ComponentLayer → EditorCanvas → InteractionSystem → ConnectionManager
        var layer = GetParent(); // ComponentLayer
        if (layer == null) return;
        var editorCanvas = layer.GetParent(); // EditorCanvas
        if (editorCanvas == null) return;

        // 查找 ConnectionManager — 通过场景中持有引用的节点
        // 简单方式：遍历 ConnectionLayer 的所有 ConnectionLine
        var connLayer = editorCanvas.GetNodeOrNull<Node2D>("ConnectionLayer");
        if (connLayer == null) return;

        foreach (Node child in connLayer.GetChildren())
        {
            if (child is ConnectionLine line)
            {
                // 清除指向本组件旧端口的引用
                if (line.SourcePort != null && line.SourcePort.ParentComponent == this)
                    line.SourcePort = null;
                if (line.TargetPort != null && line.TargetPort.ParentComponent == this)
                    line.TargetPort = null;
            }
        }
    }

    /// <summary>
    /// 端口重建后，将 ConnectionLine 的端口引用更新为新的 PortWidget。
    /// </summary>
    private void RefreshConnectionLineReferences()
    {
        var layer = GetParent();
        if (layer == null) return;
        var editorCanvas = layer.GetParent();
        if (editorCanvas == null) return;

        var connLayer = editorCanvas.GetNodeOrNull<Node2D>("ConnectionLayer");
        if (connLayer == null) return;

        foreach (Node child in connLayer.GetChildren())
        {
            if (child is ConnectionLine line)
            {
                if (line.ConnectionData != null)
                {
                    // 用新的 PortWidget 重新绑定
                    if (line.ConnectionData.SourceComponentId == ComponentId)
                        line.SourcePort = GetPortWidgetById(line.ConnectionData.SourcePortId);
                    if (line.ConnectionData.TargetComponentId == ComponentId)
                        line.TargetPort = GetPortWidgetById(line.ConnectionData.TargetPortId);
                }
                line.MarkDirty();
            }
        }
    }

    private PortWidget MakeWidget(PortDefinition def)
    {
        var pw = new PortWidget();
        pw.PortDefinition = def;
        pw.ParentComponent = this;
        pw.MouseFilter = Control.MouseFilterEnum.Pass;
        return pw;
    }

    private void UpdateComponentHeight()
    {
        int max = Mathf.Max(_inputPorts.Count, _outputPorts.Count);
        float h = 28f + max * 22f + 10f; // Header + 每端口行 22px + 底部内边距
        ComponentSize = new Vector2(ComponentSize.X, Mathf.Max(h, 60f));
        if (Background != null) Background.Size = ComponentSize;
        if (PortContainer != null) PortContainer.Size = new Vector2(ComponentSize.X, ComponentSize.Y - 28);
        if (HeaderBar != null) HeaderBar.Size = new Vector2(ComponentSize.X, 24);
    }

    /// <summary>高亮组件（白色边框）。</summary>
    public void Highlight()
    {
        if (!GodotObject.IsInstanceValid(Background)) return;
        var hlStyle = (StyleBoxFlat)Background.GetThemeStylebox("panel").Duplicate();
        hlStyle.BorderColor = Colors.White;
        hlStyle.SetBorderWidthAll(2);
        Background.AddThemeStyleboxOverride("panel", hlStyle);
    }

    /// <summary>取消高亮（恢复默认边框）。</summary>
    public void Unhighlight()
    {
        if (!GodotObject.IsInstanceValid(Background)) return;
        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = BackgroundColor;
        normalStyle.BorderColor = BorderColor;
        normalStyle.SetBorderWidthAll(1);
        normalStyle.SetCornerRadiusAll(4);
        Background.AddThemeStyleboxOverride("panel", normalStyle);
    }

    /// <summary>设置选中状态（选中=高亮，取消选中=取消高亮）。</summary>
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (selected) Highlight();
        else Unhighlight();
    }
}
