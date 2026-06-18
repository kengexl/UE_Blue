using Godot;
using System;
using System.Text;

// ============================================================================
// BlueprintDebugOverlay.cs — 可视化调试叠加层
// ============================================================================

public partial class BlueprintDebugOverlay : Control
{
    private InteractionSystem _interactionSystem;
    private EditorCanvas _editorCanvas;
    private ConnectionManager _connectionManager;

    // ─── 字体（延迟加载） ───
    private Font _debugFont;
    private int _fontSize = 12;

    // ─── 布局 ───
    private const float PanelX = 10;
    private const float PanelY = 10;
    private const float PanelWidth = 380;
    private const float LineHeight = 18;

    // ─── 颜色 ───
    private static readonly Color PanelBg = new Color(0.05f, 0.05f, 0.05f, 0.85f);
    private static readonly Color TextNormal = new Color(0.8f, 0.8f, 0.8f);
    private static readonly Color TextActive = new Color(0.3f, 1.0f, 0.3f);
    private static readonly Color TextWarning = new Color(1.0f, 0.8f, 0.2f);
    private static readonly Color TextDrag = new Color(0.4f, 0.8f, 1.0f);
    private static readonly Color TextCurveCreate = new Color(0.3f, 1.0f, 0.3f);
    private static readonly Color TextCurveIdle = new Color(0.6f, 0.6f, 0.6f);
    private static readonly Color TextCurveDone = new Color(0.3f, 0.6f, 1.0f);

    private string _cachedDebugText = "";
    private int _cachedHash;

    public override void _Ready()
    {
        MouseFilter = Control.MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public void Initialize(InteractionSystem interactionSystem, EditorCanvas editorCanvas)
    {
        _interactionSystem = interactionSystem;
        _editorCanvas = editorCanvas;
        _connectionManager = interactionSystem?.ConnectionManager;
    }

    /// <summary>安全获取绘制字体。</summary>
    private Font SafeGetFont()
    {
        if (_debugFont != null) return _debugFont;
        _debugFont = ThemeDB.FallbackFont;
        return _debugFont;
    }

    // ────────────────────────────────────────────────────────────────
    // 每帧更新
    // ────────────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_interactionSystem == null) return;

        var sb = new StringBuilder(512);
        var mode = _interactionSystem.CurrentMode;
        string modeStr = mode switch
        {
            InteractionSystem.InteractionMode.None => "空闲",
            InteractionSystem.InteractionMode.DraggingComponent => "拖拽组件",
            InteractionSystem.InteractionMode.ConnectingPorts => "连接端口",
            InteractionSystem.InteractionMode.PanningCanvas => "平移画布",
            _ => "未知"
        };
        sb.AppendLine($"模式: {modeStr}");
        sb.AppendLine($"端口: {GetPortStateText()}");
        sb.AppendLine($"拖拽: {GetDragInfoText()}");
        sb.AppendLine($"曲线: {GetCurveStateText()}");
        sb.AppendLine($"选中: {GetSelectedInfoText()}");
        sb.AppendLine($"连接: 管理器已就绪");

        var newText = sb.ToString().TrimEnd('\n');
        var newHash = newText.GetHashCode();
        if (newHash != _cachedHash)
        {
            _cachedDebugText = newText;
            _cachedHash = newHash;
            QueueRedraw();
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 绘制
    // ────────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (string.IsNullOrEmpty(_cachedDebugText)) return;

        var font = SafeGetFont();
        if (font == null) return;

        var lines = _cachedDebugText.Split('\n');
        float panelHeight = lines.Length * LineHeight + 14;
        DrawRect(new Rect2(PanelX, PanelY, PanelWidth, panelHeight), PanelBg);

        float y = PanelY + 6;
        foreach (var line in lines)
        {
            int colonIdx = line.IndexOf(':');
            string prefix = colonIdx > 0 ? line.Substring(0, colonIdx) : "";
            Color color = prefix switch
            {
                "模式" when line.Contains("空闲") => TextNormal,
                "模式" when line.Contains("拖拽") => TextWarning,
                "模式" when line.Contains("连接") => TextCurveCreate,
                "端口" when line.Contains("高亮") => TextActive,
                "拖拽" => TextDrag,
                "曲线" when line.Contains("连接中") => TextCurveCreate,
                "曲线" when line.Contains("已完成") => TextCurveDone,
                "曲线" => TextCurveIdle,
                "选中" when line.Contains("无") => TextNormal,
                "选中" => TextActive,
                _ => TextNormal
            };

            DrawString(font, new Vector2(PanelX + 6, y + _fontSize), line,
                       HorizontalAlignment.Left, PanelWidth - 12, _fontSize, color);
            y += LineHeight;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 状态采集
    // ────────────────────────────────────────────────────────────────

    private string GetPortStateText()
    {
        if (_editorCanvas?.ComponentLayer == null) return "N/A";
        string result = "";
        foreach (Node child in _editorCanvas.ComponentLayer.GetChildren())
        {
            if (child is BlueprintComponent comp)
            {
                foreach (var pw in comp.GetPortWidgets())
                {
                    string dir = pw.PortDefinition.Direction == PortDefinition.DirectionType.Input ? "In" : "Out";
                    string conn = pw.IsPortConnected ? "●" : "○";
                    string highlight = pw.IsHighlighted ? "[高亮]" : "";
                    if (pw.IsHighlighted || pw.IsPortConnected)
                        result += $"{dir}:{pw.PortDefinition.PortName}({conn}{highlight}) ";
                }
            }
        }
        return string.IsNullOrEmpty(result) ? "无交互" : result.Trim();
    }

    private string GetDragInfoText()
    {
        if (_interactionSystem == null) return "N/A";
        var mousePos = _editorCanvas?.GetLocalMousePosition() ?? Vector2.Zero;

        if (_interactionSystem.CurrentMode == InteractionSystem.InteractionMode.DraggingComponent)
        {
            var startPos = _interactionSystem.PublicDragMouseStart;
            return $"起点=({startPos.X:F1},{startPos.Y:F1}) 鼠标=({mousePos.X:F1},{mousePos.Y:F1}) 偏移=({mousePos.X - startPos.X:F1},{mousePos.Y - startPos.Y:F1})";
        }
        else if (_interactionSystem.CurrentMode == InteractionSystem.InteractionMode.ConnectingPorts)
        {
            var srcPort = _interactionSystem.PublicSourcePort;
            string portInfo = srcPort != null ? $"源端口=({srcPort.PortDefinition.PortName})" : "源端口=无";
            return $"{portInfo} 鼠标=({mousePos.X:F1},{mousePos.Y:F1})";
        }
        return $"鼠标=({mousePos.X:F1},{mousePos.Y:F1})";
    }

    private string GetCurveStateText()
    {
        if (_interactionSystem == null) return "N/A";
        return _interactionSystem.CurrentMode switch
        {
            InteractionSystem.InteractionMode.None => "空闲 — 无曲线操作",
            InteractionSystem.InteractionMode.ConnectingPorts => "连接中... 点击输入端口完成连接",
            InteractionSystem.InteractionMode.DraggingComponent => "组件拖拽中 (曲线未受影响)",
            InteractionSystem.InteractionMode.PanningCanvas => "画布平移中 (曲线未受影响)",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取选中组件的连接状态。
    /// 显示该组件主动连接（输出）到哪些组件，以及被哪些组件连接（输入）。
    /// </summary>
    private string GetSelectedInfoText()
    {
        if (_interactionSystem == null) return "N/A";
        var comp = _interactionSystem.PublicSelectedComponent;
        if (comp == null) return "无选中";

        var result = $"\"{comp.ComponentName}\" (ID={comp.ComponentId})";

        if (_connectionManager == null)
            return result + " | 连接管理器不可用";

        var conns = _connectionManager.GetConnectionsForComponent(comp.ComponentId);
        if (conns.Count == 0)
            return result + " | 无连接关系";

        int asSource = 0, asTarget = 0;
        var sources = new System.Collections.Generic.List<string>();
        var targets = new System.Collections.Generic.List<string>();

        foreach (var conn in conns)
        {
            if (conn.SourceComponentId == comp.ComponentId)
            {
                asSource++;
                var targetName = _connectionManager.ComponentSystem?.GetComponent(conn.TargetComponentId)?.ComponentName ?? $"ID={conn.TargetComponentId}";
                targets.Add(targetName);
            }
            if (conn.TargetComponentId == comp.ComponentId)
            {
                asTarget++;
                var sourceName = _connectionManager.ComponentSystem?.GetComponent(conn.SourceComponentId)?.ComponentName ?? $"ID={conn.SourceComponentId}";
                sources.Add(sourceName);
            }
        }

        if (asSource > 0)
            result += $" | →连接({string.Join(",", targets)})";
        if (asTarget > 0)
            result += $" | ←被接({string.Join(",", sources)})";

        return result;
    }
}
