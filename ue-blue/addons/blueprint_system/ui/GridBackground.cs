using Godot;

// ============================================================================
// GridBackground.cs — 网格背景
//
// 职责：
//   绘制编辑器的网格背景，包含主网格和次网格两层。
//   支持跟随画布偏移和缩放。
//
// 视觉规格：
//   背景色:  #1E1E1E（深色）
//   次网格: 每 20px, #2A2A2A（几乎不可见）
//   主网格: 每 100px, #3A3A3A（略亮）
// ============================================================================

/// <summary>网格背景 — 编辑器画布的网格线背景。</summary>
[GlobalClass]
public partial class GridBackground : ColorRect
{
    // ────────────────────────────────────────────────────────────────
    // 导出属性
    // ────────────────────────────────────────────────────────────────

    /// <summary>主网格间距（像素）。</summary>
    [Export] public int MajorGridStep { get; set; } = 100;
    /// <summary>次网格间距（像素）。</summary>
    [Export] public int MinorGridStep { get; set; } = 20;
    /// <summary>主网格线颜色。</summary>
    [Export] public Color MajorGridColor { get; set; } = new Color("#3A3A3A");
    /// <summary>次网格线颜色。</summary>
    [Export] public Color MinorGridColor { get; set; } = new Color("#2A2A2A");
    /// <summary>背景填充色。</summary>
    [Export] public Color BackgroundColor { get; set; } = new Color("#1E1E1E");

    // ────────────────────────────────────────────────────────────────
    // 运行时状态
    // ────────────────────────────────────────────────────────────────

    private Vector2 _cameraOffset = Vector2.Zero;
    private float _zoomLevel = 1.0f;

    public override void _Ready()
    {
        Color = BackgroundColor;
        MouseFilter = Control.MouseFilterEnum.Ignore; // 穿透鼠标事件
    }

    /// <summary>
    /// 绘制网格线。
    /// 使用 fmod（取模）实现无限滚动的网格效果。
    /// </summary>
    public override void _Draw()
    {
        if (!Visible) return;

        var rect = GetRect();

        // ── 次网格（较暗，间距较小） ──
        float startX = Mathf.PosMod(_cameraOffset.X, MinorGridStep * _zoomLevel);
        float startY = Mathf.PosMod(_cameraOffset.Y, MinorGridStep * _zoomLevel);

        for (float x = startX; x < rect.Size.X; x += MinorGridStep * _zoomLevel)
            DrawLine(new Vector2(x, 0), new Vector2(x, rect.Size.Y), MinorGridColor, 1.0f);
        for (float y = startY; y < rect.Size.Y; y += MinorGridStep * _zoomLevel)
            DrawLine(new Vector2(0, y), new Vector2(rect.Size.X, y), MinorGridColor, 1.0f);

        // ── 主网格（略亮，间距较大） ──
        startX = Mathf.PosMod(_cameraOffset.X, MajorGridStep * _zoomLevel);
        startY = Mathf.PosMod(_cameraOffset.Y, MajorGridStep * _zoomLevel);

        for (float x = startX; x < rect.Size.X; x += MajorGridStep * _zoomLevel)
            DrawLine(new Vector2(x, 0), new Vector2(x, rect.Size.Y), MajorGridColor, 1.0f);
        for (float y = startY; y < rect.Size.Y; y += MajorGridStep * _zoomLevel)
            DrawLine(new Vector2(0, y), new Vector2(rect.Size.X, y), MajorGridColor, 1.0f);
    }

    /// <summary>
    /// 当画布偏移或缩放变化时调用，重绘网格。
    /// </summary>
    public void UpdateGrid(Vector2 offset, float zoom)
    {
        _cameraOffset = offset;
        _zoomLevel = zoom;
        QueueRedraw();
    }
}
