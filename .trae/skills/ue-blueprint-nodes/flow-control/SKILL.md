---
name: "ue-blueprint-flow-control"
description: "Reference for UE-style Flow Control nodes (Branch, Sequence, ForLoop, Delay, DoOnce, Gate, FlipFlop, Switch). Maps to DummyComponent with blue header in the Godot blueprint tool."
---

# UE 流程控制节点参考

流程控制节点改变蓝图执行流的走向。
在本系统中对应 **DummyComponent** / **TestComponent** (蓝色 Header, `#2196F3`)。

## 节点列表

### Branch (条件分支)
```
节点名: Branch
输入端口: [Exec (exec)] + [Condition (bool)]
输出端口: [True (exec)] + [False (exec)]
Header颜色: 🔵 蓝色
UE说明: 根据Condition布尔值选择执行True或False分支，是最常用的条件节点
对应API:
  factory.CreateNode("Branch",
    new[]{"Exec (exec)", "Condition (bool)"},
    new[]{"True", "False"})
```

### Sequence (序列)
```
节点名: Sequence
输入端口: [Exec (exec)]
输出端口: [Then 0 (exec)] + [Then 1 (exec)] + [...] (可扩展)
Header颜色: 🔵 蓝色
UE说明: 按顺序依次执行多个输出分支，用于并行触发多个操作
注意: 输出引脚数量可通过菜单动态添加
对应API:
  var seq = factory.CreateNode("Sequence",
    new[]{"Exec (exec)"},
    new[]{"Then 0", "Then 1", "Then 2", "Then 3"})
```

### For Loop / ForLoopWithBreak
```
节点名: ForLoop
输入端口: [Exec (exec)] + [FirstIndex (int)] + [LastIndex (int)]
输出端口: [LoopBody (exec)] + [Index (int)] + [Completed (exec)]
Header颜色: 🔵 蓝色
UE说明: 标准for循环，从FirstIndex循环LastIndex-1次，Completed在循环结束后触发
对应API:
  factory.CreateNode("ForLoop",
    new[]{"Exec (exec)", "FirstIndex (int)", "LastIndex (int)"},
    new[]{"LoopBody (exec)", "Index (int)", "Completed"})
```

### While Loop
```
节点名: WhileLoop
输入端口: [Exec (exec)] + [Condition (bool)]
输出端口: [LoopBody (exec)] + [Completed (exec)]
Header颜色: 🔵 蓝色
UE说明: 当Condition为true时持续循环，每次迭代完成后重新检查条件
对应API:
  factory.CreateNode("WhileLoop",
    new[]{"Exec (exec)", "Condition (bool)"},
    new[]{"LoopBody (exec)", "Completed"})
```

### Delay (延迟)
```
节点名: Delay
输入端口: [Exec (exec)] + [Duration (float)]
输出端口: [Completed (exec)]
Header颜色: 🔵/🟣 蓝色/紫色
UE说明: 延迟指定秒数后继续执行，Duration为秒数
对应API:
  factory.CreateNode("Delay",
    new[]{"Exec (exec)", "Duration (float)"},
    new[]{"Completed"})
```

### Retriggerable Delay
```
节点名: RetriggerableDelay
输入端口: [Exec (exec)] + [Duration (float)]
输出端口: [Completed (exec)]
Header颜色: 🔵/🟣 蓝色/紫色
UE说明: 可重新触发的延迟，每次触发重置计时器
对应API:
  factory.CreateNode("RetrigDelay",
    new[]{"Exec (exec)", "Duration (float)"},
    new[]{"Completed"})
```

### DoOnce (执行一次)
```
节点名: DoOnce
输入端口: [Exec (exec)] + [StartClosed (bool)]
输出端口: [Completed (exec)]
Header颜色: 🔵 蓝色
UE说明: 仅执行一次，之后需Reset才能再次触发
对应API:
  factory.CreateNode("DoOnce",
    new[]{"Exec (exec)"},
    new[]{"Completed"})
```

### DoN (执行N次)
```
节点名: DoN
输入端口: [Enter (exec)] + [N (int)] + [Reset (exec)]
输出端口: [Exit (exec)]
Header颜色: 🔵 蓝色
UE说明: 执行N次后停止，Reset后可以再次执行N次
对应API:
  factory.CreateNode("DoN",
    new[]{"Enter (exec)", "N (int)", "Reset (exec)"},
    new[]{"Exit"})
```

### Gate (门控)
```
节点名: Gate
输入端口: [Enter (exec)] + [Open (exec)] + [Close (exec)] + [Toggle (exec)]
输出端口: [Exit (exec)]
Header颜色: 🔵 蓝色
UE说明: Open时允许执行流通过，Close时阻止。Toggle切换状态
对应API:
  factory.CreateNode("Gate",
    new[]{"Enter (exec)", "Open (exec)", "Close (exec)", "Toggle (exec)"},
    new[]{"Exit"})
```

### FlipFlop (触发器)
```
节点名: FlipFlop
输入端口: [Exec (exec)]
输出端口: [A (exec)] + [B (exec)]
Header颜色: 🔵 蓝色
UE说明: 每次调用交替触发A或B。第一次触发A，第二次B，如此往复
对应API:
  factory.CreateNode("FlipFlop",
    new[]{"Exec (exec)"},
    new[]{"A", "B"})
```

### Switch on Int / String / Enum
```
节点名: SwitchOnInt / SwitchOnString
输入端口: [Exec (exec)] + [Selection (int/string)] + [Default (exec)]
输出端口: [匹配值1 (exec)] + [匹配值2 (exec)] + ...
Header颜色: 🔵 蓝色
UE说明: 根据输入值匹配输出分支，支持Int/String/Enum等类型
对应API:
  factory.CreateNode("SwitchOnInt",
    new[]{"Exec (exec)", "Selection (int)"},
    new[]{"Case 0", "Case 1", "Case 2", "Default"})
```

## 节点创建模板

```csharp
var factory = new BlueprintNodeFactory(ComponentSystem.Instance);

// Branch 节点
var branch = factory.CreateNode("Branch",
    new[] { "Exec", "Condition" },
    new[] { "True", "False" },
    position,
    new Color("#1976D2")  // 蓝色 Header = 流程控制
);
```
