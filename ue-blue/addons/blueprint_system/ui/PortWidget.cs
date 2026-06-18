using Godot;

// ============================================================================
// PortWidget.cs — 端口控件
//
// 职责：
//   端口的可视化和交互处理。
//   绘制输入端口（圆形，居左）和输出端口（菱形，居右）。
//   图标在控件的左右边缘，通过 Label 宽度控制布局不强制居中。
//
// 视觉规格：
//   ┌─────────────┬──────────┬─────────────┐
//   │             │ 输入端口 │  输出端口   │
//   ├─────────────┼──────────┼─────────────┤
//   │ 形状        │ 圆形     │ 菱形        │
//   │ 默认颜色    │ #90CAF9 │ #FFD54F     │
//   │ 悬停颜色    │ #64B5F6 │ #FFCA28     │
//   │ 已连接颜色  │ #42A5F5 │ #FFB300     │
//   │ Label 对齐  │ 图标右侧│ 图标左侧    │
//   └─────────────┴──────────┴─────────────┘
// ============================================================================

/// <summary>端口控件 — 端口的可视化和交互。</summary>
public partial class PortWidget : Control
{
    // ────────────────────────────────────────────────────────────────
    // 信号
    // ────────────────────────────────────────────────────────────────

    [Signal] public delegate void PressedEventHandler(PortWidget port);
    [Signal] public delegate void DragStartedEventHandler(PortWidget port);
    [Signal] public delegate void DragEndedEventHandler(PortWidget port);
    [Signal] public delegate void MouseEnteredWidgetEventHandler(PortWidget port);
    [Signal] public delegate void MouseExitedWidgetEventHandler(PortWidget port);

    // ────────────────────────────────────────────────────────────────
    // 导出属性
    // ────────────────────────────────────────────────────────────────

    /// <summary>端口定义（数据来源）。</summary>
    [Export] public PortDefinition PortDefinition { get; set; }

    /// <summary>所属组件。</summary>
    [Export] public BlueprintComponent ParentComponent { get; set; }

    // ────────────────────────────────────────────────────────────────
    // 运行时状态
    // ────────────────────────────────────────────────────────────────

    /// <summary>端口是否已经有连接（用于改变颜色）。</summary>
    public bool IsPortConnected { get; set; } = false;
    /// <summary>端口是否高亮（连接拖拽过程中）。</summary>
    public bool IsHighlighted { get; set; } = false;

    // ─── 视觉常量 ───
    private const float PortSize = 12.0f;
    private const float HighlightSize = 20.0f;

    // ─── 颜色常量 ───
    private static readonly Color InputColor = new Color("#90CAF9");
    private static readonly Color InputHoverColor = new Color("#64B5F6");
    private static readonly Color InputConnectedColor = new Color("#42A5F5");
    private static readonly Color OutputColor = new Color("#FFD54F");
    private static readonly Color OutputHoverColor = new Color("#FFCA28");
    private static readonly Color OutputConnectedColor = new Color("#FFB300");

    private bool _isHovered = false;
    private bool _isDragging = false;

    // ────────────────────────────────────────────────────────────────
    // 生命周期
    // ────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        SetupVisual();
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        GuiInput += OnGuiInput;
    }

    /// <summary>
    /// 设置布局：
    /// - 输入端口：图标靠左边缘（x=0），Label 紧跟在图标右侧。
    /// - 输出端口：图标靠右边缘，Label 在图标左侧。
    /// 控件宽度由 Label 撑开，不用固定 120px。
    /// </summary>
    private void SetupVisual()
    {
        if (PortDefinition == null) return;

        var label = new Label();
        label.Text = PortDefinition.PortName;
        label.AddThemeColorOverride("font_color", new Color("#D0D0D0"));
        label.AddThemeFontSizeOverride("font_size", 11);
        label.MouseFilter = Control.MouseFilterEnum.Ignore;

        switch (PortDefinition.Direction)
        {
            case PortDefinition.DirectionType.Input:
                // Label 在图标右侧，中间 2px 间距
                label.Position = new Vector2(PortSize + 2 + 1, -2); // +1 额外间距
                label.HorizontalAlignment = HorizontalAlignment.Left;
                break;

            case PortDefinition.DirectionType.Output:
                // Label 在图标左侧
                label.HorizontalAlignment = HorizontalAlignment.Right;
                break;
        }

        AddChild(label);

        // 控件大小由 Label 撑开
        var labelSize = label.GetCombinedMinimumSize();
        switch (PortDefinition.Direction)
        {
            case PortDefinition.DirectionType.Input:
                CustomMinimumSize = new Vector2(PortSize + 2 + 1 + labelSize.X + 4, PortSize + 4);
                break;
            case PortDefinition.DirectionType.Output:
                CustomMinimumSize = new Vector2(PortSize + 2 + labelSize.X + 4, PortSize + 4);
                // 输出端口的 Label 需要重新定位到图标左侧
                label.Position = new Vector2(2, -2);
                break;
        }
        Size = CustomMinimumSize;

        QueueRedraw();
    }

    // ────────────────────────────────────────────────────────────────
    // 绘制
    // ────────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (PortDefinition == null) return;

        switch (PortDefinition.Direction)
        {
            case PortDefinition.DirectionType.Input: DrawInputPort(); break;
            case PortDefinition.DirectionType.Output: DrawOutputPort(); break;
        }
    }

    /// <summary>
    /// 绘制输入端口（圆形）。
    /// 圆形在控件左边缘 (PortSize/2, centerY)。
    /// 配合布局的负偏移让圆形探出到组件边框外侧。
    /// </summary>
    private void DrawInputPort()
    {
        var color = IsPortConnected ? InputConnectedColor :
            (_isHovered || IsHighlighted ? InputHoverColor : InputColor);
        var center = new Vector2(PortSize / 2, Size.Y / 2);

        if (IsHighlighted)
            DrawCircle(center, HighlightSize / 2, new Color(color, 0.3f));
        DrawCircle(center, PortSize / 2, color);
    }

    /// <summary>
    /// 绘制输出端口（菱形）。
    /// 菱形在控件右边缘 (Width - PortSize/2, centerY)。
    /// 配合布局的负偏移让菱形探出到组件边框外侧。
    /// </summary>
    private void DrawOutputPort()
    {
        var color = IsPortConnected ? OutputConnectedColor :
            (_isHovered || IsHighlighted ? OutputHoverColor : OutputColor);

        float rightX = Size.X - PortSize / 2;
        float centerY = Size.Y / 2;

        var diamond = new Vector2[]
        {
            new Vector2(rightX, centerY - PortSize / 2),
            new Vector2(rightX + PortSize / 2, centerY),
            new Vector2(rightX, centerY + PortSize / 2),
            new Vector2(rightX - PortSize / 2, centerY)
        };

        if (IsHighlighted)
            DrawCircle(new Vector2(rightX, centerY), HighlightSize / 2, new Color(color, 0.3f));
        DrawPolygon(diamond, new Color[] { color });
    }

    // ────────────────────────────────────────────────────────────────
    // 公共 API
    // ────────────────────────────────────────────────────────────────

    /// <summary>获取端口在全局画布上的位置（用于连接线起点/终点）。</summary>
    public Vector2 GetGlobalPortPosition()
    {
        return GlobalPosition + Size / 2;
    }

    /// <summary>设置端口高亮状态。</summary>
    public void SetHighlight(bool highlighted)
    {
        IsHighlighted = highlighted;
        QueueRedraw();
    }

    // ────────────────────────────────────────────────────────────────
    // 鼠标事件处理
    // ────────────────────────────────────────────────────────────────

    private void OnMouseEntered() { _isHovered = true; QueueRedraw(); }
    private void OnMouseExited() { _isHovered = false; QueueRedraw(); }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _isDragging = true;
                EmitSignal(SignalName.Pressed, this);
                EmitSignal(SignalName.DragStarted, this);
                AcceptEvent();
            }
            else if (_isDragging)
            {
                _isDragging = false;
                EmitSignal(SignalName.DragEnded, this);
                AcceptEvent();
            }
        }
    }
}
