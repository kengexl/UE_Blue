---
name: "ue-blueprint-nodes"
description: "Comprehensive reference of UE-style blueprint node types (Events, Flow Control, Math, Variables). Maps UE blueprint concepts to the Godot ue-blue blueprint tool system. Invoke when creating blueprints with specific node types."
---

# UE 蓝图节点类型参考

此 skill 是 UE 蓝图节点类型的完整参考，映射到 `ue-blue` Godot 蓝图工具系统中的对应实现。

## 节点分类概览

UE 官方将蓝图节点分为以下几大类：

| 分类 | 子 Skill | 对应 Header 颜色 | 系统节点类型 |
|------|---------|-----------------|-------------|
| 事件节点 | `events/` | 🔴 红色 | TypeAComponent (OutputOnly) |
| 流程控制 | `flow-control/` | 🔵 蓝色 | TestComponent / DummyComponent |
| 数学/逻辑 | `math-logic/` | 🟢 绿色 | DummyComponent |
| 变量节点 | `variables/` | 🟣 紫色 | DummyComponent |
| 实用节点 | `utilities/` | 🟠 橙色 | TypeCComponent (InputOnly) / DummyComponent |

## 核心概念对应关系

| UE 概念 | 本系统对应 | 说明 |
|---------|-----------|------|
| 执行引脚 (Exec Pin) | `PortType="exec"` | 白色箭头，控制流 |
| 数据引脚 (Data Pin) | `PortType="int"/"float"/"string"/"bool"` | 类型化数据流 |
| 输入端口 | `DirectionType.Input` | 左侧圆形 |
| 输出端口 | `DirectionType.Output` | 右侧菱形 |
| 节点颜色 | `HeaderColor` | UE 风格颜色编码 |
| 连接线 | `ConnectionLine` (贝塞尔曲线) | 白色=执行, 彩色=数据 |
| Event Graph | `EditorCanvas` | 主编辑画布 |

## 端口类型对应

| UE 类型 | PortType | 端口形状颜色 |
|---------|----------|------------|
| exec | `"exec"` | 白色连接线 |
| bool | `"bool"` | 🔴 红色 |
| int | `"int"` | 🟢 绿色 |
| float | `"float"` | 🟢 绿色 |
| string | `"string"` | 🟣 紫色 |
| vector | `"vector"` | 🟡 金色 |
| object ref | `"object"` | 🔵 蓝色 |

## 使用示例

当用户说"创建一个 Branch 节点"时：
1. 调用此 skill 查找 Branch 节点的端口配置
2. 使用 `BlueprintNodeFactory` 创建：
```csharp
var branch = factory.CreateNode("Branch",
    new[] { "Exec (In)", "Condition (bool)" },
    new[] { "True", "False" },
    position,
    headerColor: new Color("#1976D2")  // 蓝色 = 流程控制
);
```
