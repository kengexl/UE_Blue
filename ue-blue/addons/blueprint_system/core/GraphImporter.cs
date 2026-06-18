using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// ============================================================================
// GraphImporter.cs — 蓝图图导入器
//
// 职责：
//   从 .txt 文件（遵循 graph_format_spec.md 规范）解析节点和连接数据，
//   在编辑器中创建对应的蓝图组件和连接。
//
// 使用方式：
//   var importer = new GraphImporter();
//   importer.ImportFromFile("res://tests/test_ue54_level_blueprint.txt",
//       editorCanvas, componentSystem, connectionManager);
// ============================================================================

/// <summary>
/// 蓝图图导入器。解析 .txt 文件并创建到编辑器中。
/// </summary>
public class GraphImporter
{
    // ────────────────────────────────────────────────────────────────
    // 导入结果
    // ────────────────────────────────────────────────────────────────

    /// <summary>导入结果。</summary>
    public class ImportResult
    {
        public bool Success;
        public int NodesCreated;
        public int ConnectionsCreated;
        public string Error;
        public string GraphName = "Imported Graph";
    }

    // ────────────────────────────────────────────────────────────────
    // 内部状态
    // ────────────────────────────────────────────────────────────────

    /// <summary>文件中的 nodeId → 创建的 BlueprintComponent。</summary>
    private Dictionary<int, BlueprintComponent> _nodeMap = new();
    /// <summary>文件中的 nodeId → portName → portId。</summary>
    private Dictionary<int, Dictionary<string, int>> _portMap = new();

    // ────────────────────────────────────────────────────────────────
    // 导入入口
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 从 .txt 文件导入蓝图图到编辑器。
    /// </summary>
    public ImportResult ImportFromFile(
        string filePath,
        EditorCanvas editorCanvas,
        ComponentSystem componentSystem,
        ConnectionManager connectionManager)
    {
        _nodeMap.Clear();
        _portMap.Clear();

        var result = new ImportResult();

        // ── 读取文件 ──
        string content;
        try
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                result.Error = $"Cannot open file: {filePath}";
                return result;
            }
            content = file.GetAsText();
            file.Close();
        }
        catch (System.Exception e)
        {
            result.Error = $"File read error: {e.Message}";
            return result;
        }

        // ── 解析 ──
        var lines = content.Split('\n');
        int i = 0;

        // Phase 1: 解析元数据和节点
        var pendingConnections = new List<(int srcNode, string srcPort, int tgtNode, string tgtPort)>();

        while (i < lines.Length)
        {
            var line = lines[i].Trim();

            // 跳过空行和注释
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            {
                i++; continue;
            }

            // ── 元数据 ──
            if (line.StartsWith("[Meta]"))
            {
                var metaMatch = Regex.Match(line, @"graph_name=""([^""]*)""");
                if (metaMatch.Success)
                    result.GraphName = metaMatch.Groups[1].Value;
                i++; continue;
            }

            // ── 节点 ──
            if (line.StartsWith("[Node]"))
            {
                var (ok, nodeId, comp, portDefs) = ParseNode(lines, ref i,
                    editorCanvas, componentSystem);
                if (ok)
                {
                    _nodeMap[nodeId] = comp;
                    _portMap[nodeId] = portDefs;

                    // 等待一帧让 _Ready() 构建视觉树（AddChild 后由调用方处理）
                    // 这里先注册节点数据，AddChild+await 由外部调用
                    editorCanvas.ComponentLayer.AddChild(comp);
                    result.NodesCreated++;
                }
                continue;
            }

            // ── 连接 ──
            if (line.StartsWith("[Connection]"))
            {
                var connMatch = Regex.Match(line,
                    @"\[Connection\]\s+(\d+):(.+?)\s*[→\-]+\s*(\d+):(.+)");
                if (connMatch.Success)
                {
                    int srcNode = int.Parse(connMatch.Groups[1].Value);
                    string srcPort = connMatch.Groups[2].Value.Trim();
                    int tgtNode = int.Parse(connMatch.Groups[3].Value);
                    string tgtPort = connMatch.Groups[4].Value.Trim();
                    pendingConnections.Add((srcNode, srcPort, tgtNode, tgtPort));
                }
                i++; continue;
            }

            // ── 端口（缩进行，已经被 ParseNode 处理） ──
            i++;
        }

        // Phase 2: 等待一帧让所有节点的 _Ready() 执行
        // （由外部调用方使用 await ToSignal(GetTree(), "process_frame")）

        // Phase 2.5: 延迟到调用方处理，这里标记需要处理
        result.Success = result.NodesCreated > 0;
        if (result.NodesCreated == 0)
        {
            result.Error = "No nodes parsed from file";
            return result;
        }

        // Phase 3: 连接将在外部调用 CompleteConnections 时处理
        // 将连接数据暂存，等待节点就绪
        pendingConnections.Clear(); // 这里只是模板，实际上要通过外部调用
        // 由于这里不能 await，连接需要延迟处理

        // 把待处理的连接存到结果中，供调用方使用
        result.ConnectionsCreated = 0; // 将在 CompleteImport 中填充

        GD.Print($"[GraphImporter] Parsed {result.NodesCreated} nodes from \"{result.GraphName}\"");
        result.Success = true;
        return result;
    }

    /// <summary>
    /// 在节点就绪后完成连接创建。应在 await ToSignal(GetTree(), "process_frame") 之后调用。
    /// </summary>
    public int CompleteConnections(
        string filePath,
        EditorCanvas editorCanvas,
        ConnectionManager connectionManager)
    {
        // 重新读取文件获取连接信息
        string content;
        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        if (file == null) return 0;
        content = file.GetAsText();
        file.Close();

        int createdCount = 0;
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith("[Connection]")) continue;

            var connMatch = Regex.Match(line,
                @"\[Connection\]\s+(\d+):(.+?)\s*[→\-]+\s*(\d+):(.+)");
            if (!connMatch.Success) continue;

            int srcNode = int.Parse(connMatch.Groups[1].Value);
            string srcPort = connMatch.Groups[2].Value.Trim();
            int tgtNode = int.Parse(connMatch.Groups[3].Value);
            string tgtPort = connMatch.Groups[4].Value.Trim();

            if (!_nodeMap.TryGetValue(srcNode, out var srcComp)) continue;
            if (!_nodeMap.TryGetValue(tgtNode, out var tgtComp)) continue;
            if (!_portMap.TryGetValue(srcNode, out var srcPorts)) continue;
            if (!_portMap.TryGetValue(tgtNode, out var tgtPorts)) continue;
            if (!srcPorts.TryGetValue(srcPort, out int srcPortId)) continue;
            if (!tgtPorts.TryGetValue(tgtPort, out int tgtPortId)) continue;

            var result = connectionManager.ValidateAndConnect(
                srcComp.ComponentId, srcPortId,
                tgtComp.ComponentId, tgtPortId);

            if (result.Success && result.Connection != null)
            {
                var lineNode = connectionManager.GetConnectionLine(result.Connection.ConnectionId);
                if (lineNode != null)
                {
                    editorCanvas.ConnectionLayer.AddChild(lineNode);
                    lineNode.UpdatePosition();
                }
                createdCount++;
            }
        }

        GD.Print($"[GraphImporter] Created {createdCount} connections from file");
        return createdCount;
    }

    // ────────────────────────────────────────────────────────────────
    // 解析方法
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 解析 [Node] 行及其后续缩进端口行。
    /// 返回 (成功, nodeId, component, portNameToId字典)。
    /// </summary>
    private (bool, int, BlueprintComponent, Dictionary<string, int>) ParseNode(
        string[] lines, ref int index,
        EditorCanvas editorCanvas, ComponentSystem componentSystem)
    {
        var line = lines[index];

        // 解析 Node 行
        var nodeMatch = Regex.Match(line,
            @"\[Node\]\s+id=(\d+)\s+type=(\w+)\s+name=""([^""]*)""\s*pos=\(([\d.\-]+),([\d.\-]+)\)(?:\s*color=([\da-fA-F#]+))?");

        if (!nodeMatch.Success)
        {
            index++;
            return (false, 0, null, new());
        }

        int nodeId = int.Parse(nodeMatch.Groups[1].Value);
        string typeName = nodeMatch.Groups[2].Value;
        string nodeName = nodeMatch.Groups[3].Value;
        float posX = float.Parse(nodeMatch.Groups[4].Value);
        float posY = float.Parse(nodeMatch.Groups[5].Value);
        string hexColor = nodeMatch.Groups[6].Success ? nodeMatch.Groups[6].Value : null;

        index++;

        // 收集后续的端口行
        var portNameToId = new Dictionary<string, int>();
        var inputPortConfigs  = new List<(string name, string portType)>();
        var outputPortConfigs = new List<(string name, string portType)>();
        int autoPortId = 100;

        // 把端口方向前缀去掉匹配: "  IN:PortName|type" or "  OUT:PortName|type"
        var pinRegex = new Regex(@"^\s+(IN|OUT):([^|]+)\|(\S+)");

        while (index < lines.Length)
        {
            var subLine = lines[index];
            if (!subLine.StartsWith("  ")) break; // 不再是缩进行

            var pinMatch = pinRegex.Match(subLine);
            if (!pinMatch.Success) { index++; continue; }

            string direction = pinMatch.Groups[1].Value;
            string portName = pinMatch.Groups[2].Value;
            string portType = pinMatch.Groups[3].Value;

            int portId = autoPortId++;
            portNameToId[portName] = portId;

            if (direction == "IN")
                inputPortConfigs.Add((portName, portType));
            else
                outputPortConfigs.Add((portName, portType));

            index++;
        }

        // 创建组件
        BlueprintComponent comp;
        switch (typeName)
        {
            case "PrintString":
            case "SpawnActor":
            case "GenericNode":
            case "ProcessNode":
            case "EndNode":
            case "EventNode":
            case "TestComponent":
                comp = new DummyComponent();
                break;
            default:
                comp = new DummyComponent();
                break;
        }
        comp.ComponentName = nodeName;
        comp.Position = new Vector2(posX, posY);

        if (!string.IsNullOrEmpty(hexColor))
        {
            var c = new Color(hexColor);
            comp.HeaderColor = c;
            comp.BorderColor = new Color(c.R * 0.7f, c.G * 0.7f, c.B * 0.7f);
        }

        // 设置端口
        if (inputPortConfigs.Count > 0)
            comp.SetInputPorts(inputPortConfigs.ToArray());
        if (outputPortConfigs.Count > 0)
            comp.SetOutputPorts(outputPortConfigs.ToArray());

        // 注册
        componentSystem.RegisterComponent(comp);

        return (true, nodeId, comp, portNameToId);
    }
}
