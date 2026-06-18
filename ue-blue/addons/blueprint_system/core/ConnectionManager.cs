using Godot;
using System.Collections.Generic;

// ============================================================================
// ConnectionManager.cs — 连接管理器
//
// 职责：
//   管理所有组件间的连接关系。
//   负责连接的验证规则（自连接检查、重复连接检查）、
//   连接创建/删除、连接线（ConnectionLine）的生命周期管理。
//
// 主要功能：
//   - ValidateAndConnect() — 验证并创建连接，返回结果
//   - RemoveConnection()   — 删除连接并清理连接线
//   - GetDownstreamComponents() — BFS 遍历下游组件
// ============================================================================

/// <summary>
/// 连接管理器 — 管理所有组件间的连接关系。
/// 包含验证逻辑和连接线的生命周期。
/// </summary>
public partial class ConnectionManager : Node
{
    // ────────────────────────────────────────────────────────────────
    // 信号
    // ────────────────────────────────────────────────────────────────

    /// <summary>连接创建完成时发射，携带连接数据和连接线。</summary>
    [Signal] public delegate void ConnectionCreatedEventHandler(ConnectionData connection, ConnectionLine line);
    /// <summary>连接移除时发射，携带连接 ID。</summary>
    [Signal] public delegate void ConnectionRemovedEventHandler(int connectionId);

    // ────────────────────────────────────────────────────────────────
    // 内部数据
    // ────────────────────────────────────────────────────────────────

    /// <summary>所有连接数据，以 connectionId 为键。</summary>
    private Dictionary<int, ConnectionData> _connections = new();
    /// <summary>所有连接线，以 connectionId 为键。</summary>
    private Dictionary<int, ConnectionLine> _connectionLines = new();
    /// <summary>组件关联的连接索引：componentId → [connectionId, ...]。</summary>
    private Dictionary<int, List<int>> _componentConnections = new();

    /// <summary>对 ComponentSystem 的引用，用于查找组件和端口。</summary>
    public ComponentSystem ComponentSystem { get; set; }

    // ────────────────────────────────────────────────────────────────
    // 公共 API
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证并创建连接。
    /// </summary>
    /// <returns>
    /// (Success, ErrorMessage, Connection)
    /// Success=false 时 ErrorMessage 包含失败原因。
    /// </returns>
    public (bool Success, string ErrorMessage, ConnectionData Connection) ValidateAndConnect(
        int srcCompId, int srcPortId, int tgtCompId, int tgtPortId)
    {
        // 规则 1: 不能自连接
        if (srcCompId == tgtCompId)
            return (false, "Cannot self-connect", null);

        // 规则 2: 不能重复连接（相同源端口到相同目标端口）
        var hash = $"{srcCompId}_{srcPortId}_{tgtCompId}_{tgtPortId}";
        foreach (var conn in _connections.Values)
        {
            if (conn.GetHash() == hash)
                return (false, "Duplicate connection", null);
        }

        // 创建连接数据
        var connection = new ConnectionData();
        connection.ConnectionId = _connections.Count + 1;
        connection.SourceComponentId = srcCompId;
        connection.SourcePortId = srcPortId;
        connection.TargetComponentId = tgtCompId;
        connection.TargetPortId = tgtPortId;
        _connections[connection.ConnectionId] = connection;

        // 创建连接线（暂不 UpdatePosition，等 AddChild 后由调用方处理）
        var line = new ConnectionLine();
        line.ConnectionData = connection;

        if (ComponentSystem != null)
        {
            var srcComp = ComponentSystem.GetComponent(srcCompId);
            var tgtComp = ComponentSystem.GetComponent(tgtCompId);
            if (srcComp != null && tgtComp != null)
            {
                line.SourcePort = srcComp.GetPortWidgetById(srcPortId);
                line.TargetPort = tgtComp.GetPortWidgetById(tgtPortId);
            }
        }

        _connectionLines[connection.ConnectionId] = line;

        // 更新组件连接索引
        if (!_componentConnections.ContainsKey(srcCompId))
            _componentConnections[srcCompId] = new List<int>();
        _componentConnections[srcCompId].Add(connection.ConnectionId);

        if (!_componentConnections.ContainsKey(tgtCompId))
            _componentConnections[tgtCompId] = new List<int>();
        _componentConnections[tgtCompId].Add(connection.ConnectionId);

        // 更新端口连接状态
        if (line.SourcePort != null)
        {
            line.SourcePort.IsPortConnected = true;
            line.SourcePort.QueueRedraw();
        }
        if (line.TargetPort != null)
        {
            line.TargetPort.IsPortConnected = true;
            line.TargetPort.QueueRedraw();
        }

        EmitSignal(SignalName.ConnectionCreated, connection, line);
        return (true, "", connection);
    }

    /// <summary>移除指定 ID 的连接，清理关联数据。</summary>
    public void RemoveConnection(int connectionId)
    {
        if (!_connections.TryGetValue(connectionId, out var conn))
            return;

        // 更新端口连接状态（仅在端口无其他连接时才设为 false）
        if (ComponentSystem != null)
        {
            var srcComp = ComponentSystem.GetComponent(conn.SourceComponentId);
            var tgtComp = ComponentSystem.GetComponent(conn.TargetComponentId);

            // 源端口（OUT）：检查是否还有其他连接
            if (srcComp != null)
            {
                var port = srcComp.GetPortWidgetById(conn.SourcePortId);
                if (port != null && GodotObject.IsInstanceValid(port)
                    && !PortHasOtherConnections(conn.SourceComponentId, conn.SourcePortId, connectionId))
                {
                    port.IsPortConnected = false;
                    port.QueueRedraw();
                }
            }
            // 目标端口（IN）：检查是否还有其他连接
            if (tgtComp != null)
            {
                var port = tgtComp.GetPortWidgetById(conn.TargetPortId);
                if (port != null && GodotObject.IsInstanceValid(port)
                    && !PortHasOtherConnections(conn.TargetComponentId, conn.TargetPortId, connectionId))
                {
                    port.IsPortConnected = false;
                    port.QueueRedraw();
                }
            }
        }

        _connections.Remove(connectionId);

        // 清理组件连接索引
        foreach (var compId in new[] { conn.SourceComponentId, conn.TargetComponentId })
        {
            if (_componentConnections.TryGetValue(compId, out var list))
                list.Remove(connectionId);
        }

        // 销毁连接线（曲线立即清除）
        if (_connectionLines.TryGetValue(connectionId, out var line))
        {
            line.QueueFree();
            _connectionLines.Remove(connectionId);
        }

        EmitSignal(SignalName.ConnectionRemoved, connectionId);
    }

    /// <summary>根据连接 ID 获取连接线。</summary>
    public ConnectionLine GetConnectionLine(int connectionId)
    {
        _connectionLines.TryGetValue(connectionId, out var line);
        return line;
    }

    /// <summary>获取指定组件的所有连接数据。</summary>
    public List<ConnectionData> GetConnectionsForComponent(int componentId)
    {
        var result = new List<ConnectionData>();
        if (_componentConnections.TryGetValue(componentId, out var connIds))
        {
            foreach (var cid in connIds)
            {
                if (_connections.TryGetValue(cid, out var conn))
                    result.Add(conn);
            }
        }
        return result;
    }

    /// <summary>获取总连接数（调试用）。</summary>
    public int GetTotalConnectionCount() => _connections.Count;

    /// <summary>
    /// 检查指定组件的指定端口是否还有其他连接（排除指定 connectionId）。
    /// 用于判断断开某条连接后端口是否应保持连接状态。
    /// </summary>
    public bool PortHasOtherConnections(int componentId, int portId, int excludeConnectionId)
    {
        if (_componentConnections.TryGetValue(componentId, out var connIds))
        {
            foreach (var cid in connIds)
            {
                if (cid == excludeConnectionId) continue;
                if (_connections.TryGetValue(cid, out var conn))
                {
                    if (conn.SourceComponentId == componentId && conn.SourcePortId == portId)
                        return true;
                    if (conn.TargetComponentId == componentId && conn.TargetPortId == portId)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// BFS 广度优先遍历下游组件。
    /// 从指定组件开始，沿输出端口方向遍历所有可达组件。
    /// </summary>
    public List<int> GetDownstreamComponents(int componentId)
    {
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        var result = new List<int>();

        queue.Enqueue(componentId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current)) continue;
            visited.Add(current);
            if (current != componentId) result.Add(current);

            foreach (var conn in _connections.Values)
            {
                if (conn.SourceComponentId == current && !visited.Contains(conn.TargetComponentId))
                    queue.Enqueue(conn.TargetComponentId);
            }
        }

        return result;
    }
}
