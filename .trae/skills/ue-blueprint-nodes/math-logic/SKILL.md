---
name: "ue-blueprint-math-logic"
description: "Reference for UE-style Math/Logic nodes (Add, Multiply, Random, AND, OR, NOT, Compare). Maps to DummyComponent in the Godot blueprint tool. Invoke when creating calculation or logic nodes."
---

# UE 数学与逻辑运算节点参考

数学和逻辑节点处理数据运算，通常无执行引脚（纯函数型），在本系统中对应 **DummyComponent**。

## 算术运算

### Add (加法)
```
节点名: Add (+)
输入端口: [A (int/float)] + [B (int/float)]
输出端口: [Result (int/float)]
Header颜色: 🟢 绿色 (#4CAF50)
UE说明: A + B，支持int和float类型自动转换
对应API:
  factory.CreateNode("Add",
    new[]{"A (int)", "B (int)"},
    new[]{"Result (int)"})
```

### Subtract (减法)
```
节点名: Subtract (−)
输入端口: [A (int/float)] + [B (int/float)]
输出端口: [Result (int/float)]
对应API:
  factory.CreateNode("Subtract",
    new[]{"A", "B"},
    new[]{"Result"})
```

### Multiply (乘法)
```
节点名: Multiply (×)
输入端口: [A (int/float)] + [B (int/float)]
输出端口: [Result (int/float)]
对应API:
  factory.CreateNode("Multiply",
    new[]{"A", "B"},
    new[]{"Result"})
```

### Divide (除法)
```
节点名: Divide (÷)
输入端口: [A (int/float)] + [B (int/float)]
输出端口: [Result (int/float)]
UE说明: A / B，注意被除数B不能为0
对应API:
  factory.CreateNode("Divide",
    new[]{"A", "B"},
    new[]{"Result"})
```

## 比较运算

### Equal (等于 ==)
```
节点名: Equal
输入端口: [A] + [B]
输出端口: [Result (bool)]
对应API:
  factory.CreateNode("Equal",
    new[]{"A", "B"},
    new[]{"Result (bool)"})
```

### NotEqual (不等于 !=)
```
节点名: NotEqual
输入端口: [A] + [B]
输出端口: [Result (bool)]
对应API:
  factory.CreateNode("NotEqual",
    new[]{"A", "B"},
    new[]{"Result (bool)"})
```

### Greater / Less / GreaterEqual / LessEqual
```
节点名: Greater
输入端口: [A (int/float)] + [B (int/float)]
输出端口: [Result (bool)]
对应API:
  factory.CreateNode("Greater",
    new[]{"A", "B"},
    new[]{"Result (bool)"})
```

## 逻辑运算

### AND (逻辑与)
```
节点名: AND
输入端口: [A (bool)] + [B (bool)]
输出端口: [Result (bool)]
UE说明: 两者均为true时Result=true
对应API:
  factory.CreateNode("AND",
    new[]{"A (bool)", "B (bool)"},
    new[]{"Result (bool)"})
```

### OR (逻辑或)
```
节点名: OR
输入端口: [A (bool)] + [B (bool)]
输出端口: [Result (bool)]
对应API:
  factory.CreateNode("OR",
    new[]{"A (bool)", "B (bool)"},
    new[]{"Result (bool)"})
```

### NOT (逻辑非)
```
节点名: NOT
输入端口: [A (bool)]
输出端口: [Result (bool)]
UE说明: 反转布尔值
对应API:
  factory.CreateNode("NOT",
    new[]{"A (bool)"},
    new[]{"Result (bool)"})
```

## 随机数

### RandomIntegerInRange
```
节点名: RandomIntInRange
输入端口: [Min (int)] + [Max (int)]
输出端口: [Result (int)]
UE说明: 生成Min到Max(含)之间的随机整数
对应API:
  factory.CreateNode("RandomInt",
    new[]{"Min (int)", "Max (int)"},
    new[]{"Result (int)"})
```

### RandomFloatInRange
```
节点名: RandomFloatInRange
输入端口: [Min (float)] + [Max (float)]
输出端口: [Result (float)]
UE说明: 生成Min到Max之间的随机浮点数
对应API:
  factory.CreateNode("RandomFloat",
    new[]{"Min (float)", "Max (float)"},
    new[]{"Result (float)"})
```

## Vector 运算

### Vector + Vector
```
节点名: VectorAdd
输入端口: [A (vector)] + [B (vector)]
输出端口: [Result (vector)]
对应API:
  factory.CreateNode("VectorAdd",
    new[]{"A (vector)", "B (vector)"},
    new[]{"Result (vector)"})
```

### Vector * Float (缩放)
```
节点名: VectorMultiplyFloat
输入端口: [V (vector)] + [F (float)]
输出端口: [Result (vector)]
对应API:
  factory.CreateNode("VectorScale",
    new[]{"V (vector)", "F (float)"},
    new[]{"Result (vector)"})
```

## Select (选择)
```
节点名: Select
输入端口: [Condition (bool)] + [TrueValue] + [FalseValue]
输出端口: [Result]
UE说明: 根据Condition选择TrueValue或FalseValue输出
对应API:
  factory.CreateNode("Select",
    new[]{"Condition (bool)", "TrueValue", "FalseValue"},
    new[]{"Result"})
```
