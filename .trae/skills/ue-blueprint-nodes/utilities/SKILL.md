---
name: "ue-blueprint-utilities"
description: "Reference for UE-style Utility nodes (Print String, Spawn Actor, Set Actor Location, Timeline, Custom Event). Maps to DummyComponent/TypeCComponent in the Godot blueprint tool. Invoke when creating gameplay utility nodes."
---

# UE 实用节点参考

工具/系统节点，实现常见的游戏功能。
在本系统中对应 **DummyComponent** 或 **TypeCComponent** (橙色 Header)。

## 调试

### Print String (输出文本)
```
节点名: PrintString
输入端口: [Exec (exec)] + [InString (string)] + [Duration (float)] + [TextColor (color)]
输出端口: [Out (exec)]
Header颜色: 🟠 橙色
UE说明: 在屏幕上打印文本，用于调试输出
对应API:
  factory.CreateNode("PrintString",
    new[]{"Exec (exec)", "InString (string)"},
    new[]{"Out"})
```

## Actor 操作

### Spawn Actor (生成对象)
```
节点名: SpawnActor
输入端口: [Exec (exec)] + [Class] + [SpawnTransform]
输出端口: [Out (exec)] + [ReturnValue (object)]
Header颜色: 🟠 橙色
UE说明: 动态生成Actor对象
对应API:
  factory.CreateNode("SpawnActor",
    new[]{"Exec (exec)", "Class", "Transform (vector)"},
    new[]{"Out", "ReturnValue (object)"})
```

### Destroy Actor (销毁对象)
```
节点名: DestroyActor
输入端口: [Exec (exec)] + [Target (object)]
输出端口: [Out (exec)]
Header颜色: 🟠 橙色
UE说明: 销毁指定Actor
对应API:
  factory.CreateNode("DestroyActor",
    new[]{"Exec (exec)", "Target (object)"},
    new[]{"Out"})
```

### Set Actor Location
```
节点名: SetActorLocation
输入端口: [Exec (exec)] + [NewLocation (vector)] + [Sweep (bool)]
输出端口: [Out (exec)] + [SweepHitResult]
Header颜色: 🟠 橙色
UE说明: 设置Actor的世界坐标位置
对应API:
  factory.CreateNode("SetActorLocation",
    new[]{"Exec (exec)", "NewLocation (vector)"},
    new[]{"Out"})
```

### Get Actor Location
```
节点名: GetActorLocation
输入端口: [Target (object)]
输出端口: [ReturnValue (vector)]
Header颜色: 🟠 橙色
UE说明: 获取Actor的当前位置（纯函数）
对应API:
  factory.CreateNode("GetActorLocation",
    new[]{"Target (object)"},
    new[]{"ReturnValue (vector)"})
```

## 定时器

### Set Timer By Event
```
节点名: SetTimer
输入端口: [Exec (exec)] + [Time (float)] + [Looping (bool)]
输出端口: [Out (exec)]
Header颜色: 🟠 橙色
UE说明: 定时触发某事件，Looping=true时循环触发
对应API:
  factory.CreateNode("SetTimer",
    new[]{"Exec (exec)", "Time (float)", "Looping (bool)"},
    new[]{"Out"})
```

## 音频

### Play Sound
```
节点名: PlaySound
输入端口: [Exec (exec)] + [Sound] + [Location (vector)]
输出端口: [Out (exec)]
Header颜色: 🟠 橙色
UE说明: 在指定位置播放音效
对应API:
  factory.CreateNode("PlaySound",
    new[]{"Exec (exec)", "Location (vector)"},
    new[]{"Out"})
```

## Timeline (时间轴)
```
节点名: Timeline
输入端口: [Play (exec)] + [PlayFromStart (exec)] + [Stop (exec)] + [Reverse (exec)]
输出端口: [Update (exec)] + [Finished (exec)] + [Direction]
Header颜色: 🟠 橙色
UE说明: 时间轴动画控制器，Update输出当前帧的值
对应API:
  factory.CreateNode("Timeline",
    new[]{"Play (exec)", "Stop (exec)", "Reverse (exec)"},
    new[]{"Update (exec)", "Finished (exec)", "Value (float)"})
```

## 节点创建模板

```csharp
var factory = new BlueprintNodeFactory(ComponentSystem.Instance);

var printNode = factory.CreateNode("PrintString",
    new[] { "Exec", "InString" },
    new[] { "Out" },
    position,
    new Color("#FF9800"));
```
