using Godot;

// ============================================================================
// ConnectionLine.cs — 连接线
//
// 职责：
//   在两个组件端口之间绘制三次贝塞尔曲线。
//
// 坐标系统（关键）：
//   ConnectionLine 是 ConnectionLayer (Node2D) 的子节点。
//   BPComponent 是 ComponentLayer (Node2D) 的子节点。
//   ConnectionLayer 和 ComponentLayer 是兄弟节点，具有相同的变换（Position/Scale）。
//
//   端口位置计算：
//   1. PortWidget.GetGlobalPortPosition() 返回 Control 全局坐标（含 Control 布局偏移）
//   2. BPComponent.ToLocal(portGlobal) 转成组件 Node2D 的本地坐标（去掉父级变换）
//   3. BPComponent.Position + localOffset = 画布空间坐标
//   4. 由于 ComponentLayer == ConnectionLayer 变换相同，画布空间 = ConnectionLayer 本地空间
//
//   这样完全避免了 Control.GlobalPosition 与 Node2D 全局坐标空间不一致的问题。
// ============================================================================

public partial class ConnectionLine : Node2D
{
    [Signal] public delegate void SelectedEventHandler(ConnectionLine connection);

    // ────────────────────────────────────────────────────────────────
    // 导出属性
    // ────────────────────────────────────────────────────────────────

    [Export] public ConnectionData ConnectionData { get; set; }
    [Export] public float LineWidth { get; set; } = 3.0f;
    [Export] public Color DefaultColor { get; set; } = new Color("#78909C");
    [Export] public Color SelectedColor { get; set; } = new Color("#42A5F5");
    [Export] public Color HoverColor { get; set; } = new Color("#B0BEC5");

    // ────────────────────────────────────────────────────────────────
    // 运行时状态
    // ────────────────────────────────────────────────────────────────

    public bool IsSelected { get; set; } = false;
    public bool IsHovered { get; set; } = false;
    public PortWidget SourcePort { get; set; }
    public PortWidget TargetPort { get; set; }

    /// <summary>缓存上一次构建的端点，避免重复重建。</summary>
    private Vector2 _lastStart;
    private Vector2 _lastEnd;
    private bool _curveDirty = true;

    /// <summary>检查 PortWidget 是否仍然有效（未释放）。</summary>
    private static bool IsPortValid(PortWidget pw)
        => pw != null && GodotObject.IsInstanceValid(pw);

    /// <summary>贝塞尔曲线分段点序列（32 段线性近似）。</summary>
    private Vector2[] _curvePoints = System.Array.Empty<Vector2>();

    // ────────────────────────────────────────────────────────────────
    // 位置更新（由外部调用）
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 更新连接线位置并请求重绘。
    /// 由外部代码（连接创建完成或组件拖拽完成时）调用。
    /// 计算逻辑委托给 ComputeCurvePoints()，避免与 _Draw() 中的逻辑重复。
    /// </summary>
    public void UpdatePosition()
    {
        if (!IsPortValid(SourcePort) || !IsPortValid(TargetPort)) return;

        var srcComp = SourcePort.ParentComponent;
        var tgtComp = TargetPort.ParentComponent;
        if (srcComp == null || tgtComp == null) return;

        var srcGlobal = SourcePort.GetGlobalPortPosition();
        var tgtGlobal = TargetPort.GetGlobalPortPosition();
        var srcLocal = srcComp.ToLocal(srcGlobal);
        var tgtLocal = tgtComp.ToLocal(tgtGlobal);
        var start = srcComp.Position + srcLocal;
        var end = tgtComp.Position + tgtLocal;

        if (!_curveDirty && start == _lastStart && end == _lastEnd)
            return;

        _lastStart = start;
        _lastEnd = end;
        _curveDirty = false;
        RebuildCurve(start, end);
        QueueRedraw();
    }

    public void MarkDirty() { _curveDirty = true; }

    /// <summary>重建贝塞尔曲线点序列（32 段线性近似）。</summary>
    private void RebuildCurve(Vector2 start, Vector2 end)
    {
        var controlDistance = Mathf.Max(Mathf.Abs(end.X - start.X) * 0.5f, 50.0f);
        int segments = 32;
        _curvePoints = new Vector2[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            var p0 = start;
            var p1 = new Vector2(start.X + controlDistance, start.Y);
            var p2 = new Vector2(end.X - controlDistance, end.Y);
            var p3 = end;

            var mt = 1.0f - t;
            _curvePoints[i] = mt * mt * mt * p0
                + 3.0f * mt * mt * t * p1
                + 3.0f * mt * t * t * p2
                + t * t * t * p3;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 绘制（自恢复：如果曲线为空则即时计算，不依赖外部先调用 UpdatePosition）
    // ────────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (!IsPortValid(SourcePort) || !IsPortValid(TargetPort))
            return;

        // 如果曲线点为空，就地计算（不调用 QueueRedraw，防止死循环）
        if (_curvePoints.Length < 2)
            ComputeCurvePoints();

        var color = IsSelected ? SelectedColor : (IsHovered ? HoverColor : DefaultColor);

        if (_curvePoints.Length >= 2)
        {
            for (int i = 0; i < _curvePoints.Length - 1; i++)
                DrawLine(_curvePoints[i], _curvePoints[i + 1], color, LineWidth, true);
        }
    }

    /// <summary>
    /// 即时计算曲线点（不触发 QueueRedraw）。
    /// 由 _Draw() 在曲线为空时自动调用，或由外部 UpdatePosition() 主动调用。
    /// 与 UpdatePosition 的区别：此方法不调用 QueueRedraw。
    /// </summary>
    private void ComputeCurvePoints()
    {
        if (!IsPortValid(SourcePort) || !IsPortValid(TargetPort)) return;

        var srcComp = SourcePort.ParentComponent;
        var tgtComp = TargetPort?.ParentComponent;
        if (srcComp == null || tgtComp == null) return;

        var srcGlobal = SourcePort.GetGlobalPortPosition();
        var tgtGlobal = TargetPort.GetGlobalPortPosition();
        var srcLocal = srcComp.ToLocal(srcGlobal);
        var tgtLocal = tgtComp.ToLocal(tgtGlobal);
        var start = srcComp.Position + srcLocal;
        var end = tgtComp.Position + tgtLocal;

        _lastStart = start;
        _lastEnd = end;
        RebuildCurve(start, end);
    }

    // ────────────────────────────────────────────────────────────────
    // 碰撞检测
    // ────────────────────────────────────────────────────────────────

    public bool IntersectsPoint(Vector2 point, float threshold = 8.0f)
    {
        if (_curvePoints.Length < 2) return false;
        for (int i = 0; i < _curvePoints.Length - 1; i++)
        {
            if (DistanceToSegment(point, _curvePoints[i], _curvePoints[i + 1]) <= threshold)
                return true;
        }
        return false;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var ap = p - a;
        var t = Mathf.Clamp(ap.Dot(ab) / ab.LengthSquared(), 0.0f, 1.0f);
        return p.DistanceTo(a + ab * t);
    }

    public void SetSelected(bool sel) { IsSelected = sel; QueueRedraw(); }
    public void SetHovered(bool hover) { IsHovered = hover; QueueRedraw(); }
}
