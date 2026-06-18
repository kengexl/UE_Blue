---
name: "ue-blueprint-connection-tool"
description: "Creates and manipulates UE-style blueprint visual node graphs in Godot 4 C#. Invoke when user asks to draw blueprint nodes, build connection graphs, create visual scripting, or operate Nodes/Ports/Links."
---

# 绘制UE蓝图节点连接关系

此技能用于在 Godot 4 C# 项目 `ue-blue` 中创建和操作 UE 风格的蓝图可视化节点连接图。

## 关联 Skill

创建特定类型的节点时，查阅以下节点参考 skills：

| Skill | 内容 | 调用时机 |
|-------|------|---------|
| `ue-blueprint-nodes` | 节点分类总览 | 了解节点体系和对应关系 |
| `ue-blueprint-nodes/events` | 事件节点 (Event BeginPlay, Tick, Input等) | 创建红色事件起点 |
| `ue-blueprint-nodes/flow-control` | 流程控制 (Branch, Sequence, ForLoop等) | 创建蓝色控制流 |
| `ue-blueprint-nodes/math-logic` | 数学逻辑 (Add, AND, Random等) | 创建绿色运算节点 |
| `ue-blueprint-nodes/variables` | 变量操作 (Get, Set, Array等) | 创建紫色变量节点 |
| `ue-blueprint-nodes/utilities` | 实用节点 (Print, Spawn, Timer等) | 创建橙色工具节点 |

## 导入/导出

蓝图图可以导出为 `.txt` 文件并重新导入：

| 操作 | 入口 | 说明 |
|------|------|------|
| 导出 | 右键画布 → "导出图..." | 当前编辑器图 → .txt |
| 导入 | 右键画布 → "导入图..." | .txt → 创建节点+连接 |

**文件格式规范**: `addons/blueprint_system/data/graph_format_spec.md`

**测试文件**: `blueprints_test/ue54_press_e_print.txt` — UE5.4 关卡蓝图: 按E打印信息

**AI 生成规则**: **根目录 `AI生成可导入蓝图的规则.txt`** — 含节点速查表 + AI 提示词模板，供 AI 直接生成可导入的 .txt 文件

**导入示例**:
```csharp
// 从文件导入
var importer = new GraphImporter();
var result = importer.ImportFromFile(path, editorCanvas, componentSystem, connectionManager);
if (result.Success)
{
    await ToSignal(GetTree(), "process_frame"); // 等节点 _Ready
    importer.CompleteConnections(path, editorCanvas, connectionManager);
}
```

---

## 一、前提知识

### 文件位置

```
ue-blue/addons/blueprint_system/
├── components/
│   ├── BlueprintComponent.cs    — 节点基类，提供 AddInputPort()/AddOutputPort()
│   ├── DummyComponent.cs        — 空模板节点（端口完全动态）
│   ├── TypeAComponent.cs        — 仅 OUT 端口（绿色Header，事件始点）
│   ├── TypeBComponent.cs        — 1 IN + 1 OUT（蓝色Header，中间节点）
│   ├── TypeCComponent.cs        — 仅 IN 端口（橙红Header，终点）
│   └── TestComponent.cs         — 测试节点（继承TypeB）
├── core/
│   ├── BlueprintNodeFactory.cs  — 节点工厂（便捷创建API）
│   ├── ComponentSystem.cs       — 组件注册/ID管理（Autoload）
│   ├── ConnectionManager.cs     — 连接创建/删除/查询
│   └── InteractionSystem.cs     — 交互状态机
├── data/
│   ├── PortDefinition.cs        — 端口定义（Resource）
│   ├── ConnectionData.cs        — 连接数据
│   └── GraphData.cs             — 图容器
└── ui/
    ├── EditorCanvas.cs          — 主画布
    ├── PortWidget.cs            — 端口可视化控件
    └── ConnectionLine.cs        — 贝塞尔连接线
```

---

## 二、核心 API — 创建节点

### 方式 1: 使用 BlueprintNodeFactory（推荐）

```csharp
var factory = new BlueprintNodeFactory(ComponentSystem.Instance);

// 创建节点: 名称, 输入端口列表, 输出端口列表
var branch = factory.CreateNode(
    "Branch",
    new[] { "Exec", "Condition" },
    new[] { "True", "False" },
    new Vector2(400, 200)
);

// 加入了 ComponentSystem 但未加入场景树（_Ready 尚未触发）
// 需要手动 AddChild 到 ComponentLayer
editorCanvas.ComponentLayer.AddChild(branch);
await ToSignal(GetTree(), "process_frame");  // 等待 _Ready
```

### 方式 2: 使用 BlueprintComponent API

```csharp
var comp = new DummyComponent();
comp.ComponentName = "MyNode";
comp.HeaderColor = new Color("#9C27B0");  // 紫色

// 设置端口
comp.SetInputPorts(("Exec","exec"), ("A","int"), ("B","int"));
comp.SetOutputPorts(("Result","int"), ("Then","exec"));

// 或逐个添加
comp.AddInputPort("ExtraIn", "string");
comp.AddOutputPort("ExtraOut", "bool");

// 重命名端口
comp.SetPortName(portId, "NewName");

// 注册 + 加入场景树
ComponentSystem.Instance.RegisterComponent(comp);
editorCanvas.ComponentLayer.AddChild(comp);
await ToSignal(GetTree(), "process_frame");
```

### 方式 3: 预定义组件

```csharp
// 使用预设组件（各有默认端口）
var eventStart = new TypeAComponent();  // 1 OUT: 绿色
var process    = new TypeBComponent();  // 1 IN + 1 OUT: 蓝色
var logOutput  = new TypeCComponent();  // 1 IN: 橙红

// 可继续动态添加端口
process.AddInputPort("Data", "int");
process.AddOutputPort("Result", "float");
```

---

## 三、核心 API — 创建连接

```csharp
// 从节点A的OUT端口(portId=2)连接到节点B的IN端口(portId=1)
var result = connectionManager.ValidateAndConnect(
    nodeA.ComponentId, 2,   // 源: 节点ID, 端口ID
    nodeB.ComponentId, 1    // 目标: 节点ID, 端口ID
);

if (result.Success)
{
    var line = connectionManager.GetConnectionLine(result.Connection.ConnectionId);
    editorCanvas.ConnectionLayer.AddChild(line);
    line.UpdatePosition();
}
```

---

## 四、核心 API — 查询连接

```csharp
// 获取某节点的所有连接
var conns = connectionManager.GetConnectionsForComponent(comp.ComponentId);

// 遍历每个连接
foreach (var conn in conns)
{
    bool isSource = conn.SourceComponentId == comp.ComponentId;  // comp 是输出方
    bool isTarget = conn.TargetComponentId == comp.ComponentId;  // comp 是输入方

    var otherCompId = isSource ? conn.TargetComponentId : conn.SourceComponentId;
    var otherName = ComponentSystem.Instance.GetComponent(otherCompId)?.ComponentName;
}

// BFS遍历下游
var downstream = connectionManager.GetDownstreamComponents(startCompId);
```

---

## 五、端口编号规则

预设组件的端口ID:
| 组件 | IN端口 | OUT端口 |
|------|--------|---------|
| TypeAComponent | — | portId=1 "Out" |
| TypeBComponent | portId=1 "In" | portId=2 "Out" |
| TypeCComponent | portId=1 "In" | — |
| TestComponent | portId=1 "In" | portId=2 "Out" |

动态端口的 portId 从 100 开始自动递增（每节点独立计数）。

**查找端口ID**: `comp.GetPortWidgetById(portId)` 或遍历 `comp.GetPortWidgets()`。

---

## 六、交互支持

- **左键点击节点标题** → 选中 + 拖拽
- **左键点击 OUT 端口** → 拖拽临时连接线
- **松开鼠标在 IN 端口上** → 完成连接
- **右键空画布** → 新建节点菜单
- **右键组件** → 添加端口 / 删除节点
- **右键连接线** → 断开连接
- **中键拖拽** → 平移画布
- **Ctrl+滚轮** → 缩放

---

## 七、完整示例：搭建一个分支逻辑图

```csharp
// 在 BlueprintEditor 或任意脚本中
public async void CreateBranchGraph()
{
    var factory = new BlueprintNodeFactory(ComponentSystem.Instance);
    var canvasLayer = GetNode<EditorCanvas>("EditorCanvas");

    // 1. 创建三个节点
    var eventNode = factory.CreateNode("EventStart",
        new string[] { },
        new[] { "Out" },
        new Vector2(100, 200));
    eventNode.HeaderColor = new Color("#4CAF50");

    var branchNode = factory.CreateNode("Branch",
        new[] { "Exec", "Condition" },
        new[] { "True", "False" },
        new Vector2(350, 200));
    branchNode.HeaderColor = new Color("#2196F3");

    var logTrue = factory.CreateNode("LogTrue",
        new[] { "In" },
        new string[] { },
        new Vector2(600, 150));
    logTrue.HeaderColor = new Color("#FF5722");

    var logFalse = factory.CreateNode("LogFalse",
        new[] { "In" },
        new string[] { },
        new Vector2(600, 300));
    logFalse.HeaderColor = new Color("#FF5722");

    // 2. 加入场景树（触发 _Ready 构建视觉树）
    canvasLayer.ComponentLayer.AddChild(eventNode);
    canvasLayer.ComponentLayer.AddChild(branchNode);
    canvasLayer.ComponentLayer.AddChild(logTrue);
    canvasLayer.ComponentLayer.AddChild(logFalse);
    await ToSignal(GetTree(), "process_frame");

    // 3. 创建连接
    var cm = ConnectionManager.Instance;  // 或直接持有引用

    var r1 = cm.ValidateAndConnect(eventNode.ComponentId, 1,      // eventNode.Out
                                    branchNode.ComponentId, 1);    // branchNode.Exec
    if (r1.Success)
        canvasLayer.ConnectionLayer.AddChild(cm.GetConnectionLine(r1.Connection.ConnectionId));

    var r2 = cm.ValidateAndConnect(branchNode.ComponentId, 101,   // branchNode.True
                                    logTrue.ComponentId, 1);       // logTrue.In
    if (r2.Success)
        canvasLayer.ConnectionLayer.AddChild(cm.GetConnectionLine(r2.Connection.ConnectionId));

    var r3 = cm.ValidateAndConnect(branchNode.ComponentId, 102,   // branchNode.False
                                    logFalse.ComponentId, 1);      // logFalse.In
    if (r3.Success)
        canvasLayer.ConnectionLayer.AddChild(cm.GetConnectionLine(r3.Connection.ConnectionId));

    GD.Print("分支逻辑图创建完毕!");
}
```

---

## 八、注意事项

1. **插入场景树**：创建节点后必须 `componentLayer.AddChild(comp)` + `await process_frame`，否则 `_Ready()` 不执行，端口不渲染
2. **连接线添加**：连接创建后连接线不会自动加入场景树，需要手动 `connectionLayer.AddChild(line)`
3. **端口类型**：预设为 `"exec"`，可选 `"int"`, `"string"`, `"bool"`, `"float"`
4. **坐标变换**：所有坐标以画布空间为准，`EditorCanvas.ScreenToCanvas()` 进行转换
5. **节点命名**：Test/Dual 节点自动命名为 `test(1)`, `test(2)`...（持续递增）
