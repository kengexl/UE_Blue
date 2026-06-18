---
name: "ue-blueprint-variables"
description: "Reference for UE-style Variable nodes (Get, Set, Array, Promotion). Maps to DummyComponent in the Godot blueprint tool. Invoke when creating variable-based blueprint nodes."
---

# UE 变量节点参考

变量节点用于存储、读取和操作数据。
在本系统中对应 **DummyComponent** (紫色 Header, `#9C27B0`)。

## 基本变量操作

### Get Variable (获取变量)
```
节点名: Get
输入端口: 无（纯函数）
输出端口: [Value]
Header颜色: 🟣 紫色 (#9C27B0)
UE说明: 读取变量的当前值，纯函数无执行引脚
对应API:
  factory.CreateNode("Get (变量名)",
    new string[]{},
    new[]{"Value"})
```

### Set Variable (设置变量)
```
节点名: Set
输入端口: [Exec (exec)] + [Value]
输出端口: [Out (exec)]
Header颜色: 🟣 紫色
UE说明: 写入变量新值，有执行引脚触发写入操作
对应API:
  factory.CreateNode("Set (变量名)",
    new[]{"Exec (exec)", "Value"},
    new[]{"Out"})
```

### IncrementInt / DecrementInt
```
节点名: IncrementInt
输入端口: [Exec (exec)]
输出端口: [Out (exec)] + [Result (int)]
Header颜色: 🟣 紫色
UE说明: 对整数变量执行+1或-1操作
对应API:
  factory.CreateNode("IncrementInt",
    new[]{"Exec (exec)"},
    new[]{"Out", "Result (int)"})
```

## 数组操作

### Array Add
```
节点名: ArrayAdd
输入端口: [Exec (exec)] + [TargetArray] + [NewItem]
输出端口: [Out (exec)] + [Index (int)]
对应API:
  factory.CreateNode("ArrayAdd",
    new[]{"Exec (exec)", "Item"},
    new[]{"Out", "Index (int)"})
```

### Array Get
```
节点名: ArrayGet
输入端口: [TargetArray] + [Index (int)]
输出端口: [Item]
UE说明: 按索引读取数组元素（纯函数）
对应API:
  factory.CreateNode("ArrayGet",
    new[]{"Index (int)"},
    new[]{"Item"})
```

### Array Remove
```
节点名: ArrayRemove
输入端口: [Exec (exec)] + [TargetArray] + [Index (int)]
输出端口: [Out (exec)]
对应API:
  factory.CreateNode("ArrayRemove",
    new[]{"Exec (exec)", "Index (int)"},
    new[]{"Out"})
```

### Array Length
```
节点名: ArrayLength
输入端口: [TargetArray]
输出端口: [Length (int)]
UE说明: 获取数组元素个数（纯函数）
对应API:
  factory.CreateNode("ArrayLength",
    new[]{"TargetArray"},
    new[]{"Length (int)"})
```

## 类型转换

### Cast To (类型转换)
```
节点名: CastTo
输入端口: [Exec (exec)] + [Object]
输出端口: [As Type (exec)] + [CastFailed (exec)]
Header颜色: 🔵 蓝色
UE说明: 尝试将Object转换为指定类型，成功走As分支，失败走CastFailed
对应API:
  factory.CreateNode("CastTo",
    new[]{"Exec (exec)", "Object"},
    new[]{"AsType (exec)", "CastFailed"})
```

## 结构体 (Struct)

### Make Struct / Break Struct
```
节点名: Make / Break
输入端口: 各字段值 (Make) / Struct引用 (Break)
输出端口: Struct (Make) / 各字段值 (Break)
Header颜色: 🟡 金色 (#FFC107)
对应API:
  factory.CreateNode("MakeVector",
    new[]{"X (float)", "Y (float)", "Z (float)"},
    new[]{"Vector"})
```

## 变量类型参考表

| UE类型 | Godot类型 | PortType 字符串 |
|--------|----------|----------------|
| Boolean | bool | `"bool"` |
| Integer | int | `"int"` |
| Float | float | `"float"` |
| String | string | `"string"` |
| Vector | Vector2/Vector3 | `"vector"` |
| Rotator | rotation | `"rotation"` |
| Actor/Object | Node2D/Node | `"object"` |
| Enum | enum | `"enum"` |

## 示例：创建带变量的节点

```csharp
// 创建从变量读取到显示的节点链
var getVar = factory.CreateNode("Get Health",
    new string[] { },
    new[] { "Value (int)" },
    position,
    new Color("#9C27B0"));
```
