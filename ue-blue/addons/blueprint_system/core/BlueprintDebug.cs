using Godot;
using System;
using System.Text;

// ============================================================================
// BlueprintDebug.cs — 蓝图调试工具
//
// 职责：
//   提供统一的调试日志记录功能，用于跟踪节点创建、连接操作、
//   用户交互等关键操作，帮助开发人员定位问题。
//
// 使用方式：
//   BlueprintDebug.LogNodeCreation(...)
//   BlueprintDebug.LogConnection(...)
//   BlueprintDebug.LogInteraction(...)
//
// 输出格式（控制台）：
//   [调试] 2026-06-18 14:30:00.123 | 操作类型 | 详细信息...
// ============================================================================

/// <summary>
/// 蓝图调试工具 — 统一的调试日志记录器。
/// </summary>
public static class BlueprintDebug
{
    // ────────────────────────────────────────────────────────────────
    // 常量
    // ────────────────────────────────────────────────────────────────

    /// <summary>调试日志的前缀标记，便于在控制台搜索过滤。</summary>
    private const string TAG = "[蓝图调试]";

    /// <summary>日志输出开关，可全局关闭调试输出。</summary>
    public static bool Enabled { get; set; } = true;

    // ────────────────────────────────────────────────────────────────
    // 时间戳工具
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 获取当前时间戳字符串，精确到毫秒。
    /// 格式: "2026-06-18 14:30:00.123"
    /// </summary>
    private static string GetTimestamp()
    {
        var now = DateTime.Now;
        return now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }

    /// <summary>
    /// 获取高精度时间戳（用于性能测量）。
    /// 格式: "12345ms"
    /// </summary>
    private static long GetPreciseTimestamp()
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    // ────────────────────────────────────────────────────────────────
    // 核心日志方法
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 输出一条调试日志到 Godot 控制台。
    /// 格式: [蓝图调试] 时间戳 | 操作类型 | 详细信息
    /// </summary>
    private static void Log(string operationType, string details)
    {
        if (!Enabled) return;
        GD.Print($"{TAG} {GetTimestamp()} | {operationType} | {details}");
    }

    /// <summary>
    /// 输出一条警告级别的调试日志。
    /// </summary>
    private static void LogWarning(string operationType, string details)
    {
        if (!Enabled) return;
        GD.PushWarning($"{TAG} {GetTimestamp()} | {operationType} | {details}");
    }

    // ────────────────────────────────────────────────────────────────
    // 公开的日志 API
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 记录节点创建操作。
    /// </summary>
    /// <param name="componentId">组件 ID。</param>
    /// <param name="componentType">组件类型名称，如 "TestComponent"。</param>
    /// <param name="canvasPos">组件在画布上的放置位置。</param>
    /// <param name="screenPos">鼠标在屏幕上的位置（触发创建时的坐标）。</param>
    /// <param name="triggerMethod">触发方式，如 "右键菜单", "快捷键", "API调用"。</param>
    /// <param name="componentName">组件名称。</param>
    public static void LogNodeCreation(
        int componentId,
        string componentType,
        Vector2 canvasPos,
        Vector2 screenPos,
        string triggerMethod,
        string componentName = "")
    {
        var sb = new StringBuilder();
        sb.Append($"节点创建 [ID={componentId}]");
        sb.Append($" | 组件类型: {componentType}");
        sb.Append($" | 名称: \"{(string.IsNullOrEmpty(componentName) ? "未命名" : componentName)}\"");
        sb.Append($" | 画布坐标: ({canvasPos.X:F1}, {canvasPos.Y:F1})");
        sb.Append($" | 鼠标屏幕坐标: ({screenPos.X:F1}, {screenPos.Y:F1})");
        sb.Append($" | 触发方式: {triggerMethod}");

        Log("节点操作", sb.ToString());

        // 额外输出一条独立的坐标行，方便 grep 提取位置信息
        GD.Print($"{TAG} >> 位置数据 | ID={componentId} | Canvas=({canvasPos.X:F1},{canvasPos.Y:F1}) | Screen=({screenPos.X:F1},{screenPos.Y:F1})");
    }

    /// <summary>
    /// 记录连接创建操作。
    /// </summary>
    public static void LogConnectionCreated(
        int connectionId,
        int srcCompId, int srcPortId,
        int tgtCompId, int tgtPortId,
        string connectionType)
    {
        var sb = new StringBuilder();
        sb.Append($"连接创建 [ID={connectionId}]");
        sb.Append($" | 源: 组件{srcCompId}:端口{srcPortId}");
        sb.Append($" → 目标: 组件{tgtCompId}:端口{tgtPortId}");
        sb.Append($" | 类型: {connectionType}");

        Log("连接操作", sb.ToString());
    }

    /// <summary>
    /// 记录连接删除操作。
    /// </summary>
    public static void LogConnectionRemoved(int connectionId)
    {
        Log("连接操作", $"连接删除 [ID={connectionId}]");
    }

    /// <summary>
    /// 记录组件拖拽移动操作。
    /// </summary>
    public static void LogComponentMoved(
        int componentId,
        string componentName,
        Vector2 fromPosition,
        Vector2 toPosition)
    {
        var sb = new StringBuilder();
        sb.Append($"组件移动 [ID={componentId}]");
        sb.Append($" | 名称: \"{componentName}\"");
        sb.Append($" | 从: ({fromPosition.X:F1}, {fromPosition.Y:F1})");
        sb.Append($" | 到: ({toPosition.X:F1}, {toPosition.Y:F1})");
        sb.Append($" | 位移: ({toPosition.X - fromPosition.X:F1}, {toPosition.Y - fromPosition.Y:F1})");

        Log("组件操作", sb.ToString());
    }

    /// <summary>
    /// 记录组件选中操作。
    /// </summary>
    public static void LogComponentSelected(int componentId, string componentName)
    {
        Log("组件操作", $"组件选中 [ID={componentId}] | 名称: \"{componentName}\"");
    }

    /// <summary>
    /// 记录组件取消选中操作。
    /// </summary>
    public static void LogComponentDeselected(int componentId, string componentName)
    {
        Log("组件操作", $"组件取消选中 [ID={componentId}] | 名称: \"{componentName}\"");
    }

    /// <summary>
    /// 记录端口拖拽开始（用于连接线创建）。
    /// </summary>
    public static void LogDragStarted(int portId, int componentId, Vector2 portGlobalPos, Vector2 mousePos)
    {
        var sb = new StringBuilder();
        sb.Append($"端口拖拽开始");
        sb.Append($" | 端口ID: {portId}");
        sb.Append($" | 所属组件ID: {componentId}");
        sb.Append($" | 端口全局坐标: ({portGlobalPos.X:F1}, {portGlobalPos.Y:F1})");
        sb.Append($" | 鼠标位置: ({mousePos.X:F1}, {mousePos.Y:F1})");

        Log("交互操作", sb.ToString());
    }

    /// <summary>
    /// 记录连接操作失败。
    /// </summary>
    public static void LogConnectionFailed(
        int srcCompId, int srcPortId,
        int tgtCompId, int tgtPortId,
        string reason)
    {
        var sb = new StringBuilder();
        sb.Append($"连接失败");
        sb.Append($" | 源: 组件{srcCompId}:端口{srcPortId}");
        sb.Append($" → 目标: 组件{tgtCompId}:端口{tgtPortId}");
        sb.Append($" | 原因: {reason}");

        LogWarning("连接操作", sb.ToString());
    }

    /// <summary>
    /// 记录自定义调试事件。
    /// </summary>
    public static void LogEvent(string category, string message)
    {
        Log(category, message);
    }
}
