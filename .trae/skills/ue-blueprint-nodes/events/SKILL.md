---
name: "ue-blueprint-events"
description: "Reference for UE-style Event nodes (Event BeginPlay, Tick, Input, Collision). Maps to TypeAComponent (green header, OutputOnly) in the Godot blueprint tool. Invoke when creating event-triggering nodes."
---

# UE 事件节点参考

事件节点是蓝图逻辑的起点，仅有输出端口（无输入端口）。
在本系统中对应 **TypeAComponent** (绿色 Header, `#4CAF50`)。

## 节点列表

### Event BeginPlay
```
节点名: Event BeginPlay
输出端口: [Out (exec)]
Header颜色: 🔴 红色
UE说明: 游戏开始/对象生成时触发一次，所有蓝图逻辑的标准入口
对应API: factory.CreateNode("EventBeginPlay", new string[]{}, new[]{"Out"})
```

### Event Tick
```
节点名: Event Tick
输出端口: [Out (exec)] + [DeltaSeconds (float)]
Header颜色: 🔴 红色
UE说明: 每帧触发一次，DeltaSeconds为帧间隔秒数
注意: 避免在此放置重型逻辑，防止性能问题
对应API: factory.CreateNode("EventTick", new string[]{}, new[]{"Out (exec)", "DeltaSeconds (float)"})
```

### Event ActorBeginOverlap
```
节点名: Event BeginOverlap
输出端口: [Out (exec)] + [OtherActor (object)]
Header颜色: 🔴 红色
UE说明: 当其他对象与此对象发生重叠时触发
对应API: factory.CreateNode("BeginOverlap", new string[]{}, new[]{"Out (exec)", "OtherActor (object)"})
```

### Event ActorEndOverlap
```
节点名: Event EndOverlap
输出端口: [Out (exec)] + [OtherActor (object)]
Header颜色: 🔴 红色
UE说明: 当其他对象停止与此对象重叠时触发
对应API: factory.CreateNode("EndOverlap", new string[]{}, new[]{"Out (exec)", "OtherActor (object)"})
```

### Event Hit / OnComponentHit
```
节点名: OnComponentHit
输出端口: [Out (exec)] + [OtherActor (object)] + [NormalImpulse (vector)] + [HitResult]
Header颜色: 🔴 红色
UE说明: 物理碰撞时触发，携带碰撞信息
对应API: factory.CreateNode("OnHit", new string[]{}, new[]{"Out", "Other (object)", "Impulse (vector)"})
```

### Custom Event (自定义事件)
```
节点名: CustomEvent
输出端口: [Out (exec)] + 用户自定义数据引脚
Header颜色: 🔴 红色
UE说明: 用户定义的事件节点，可通过名称在代码中触发
对应API: factory.CreateNode("MyEvent", new string[]{}, new[]{"Out (exec)"})
```

### Keyboard Events
```
节点名: InputAction / KeyEvent
输出端口: [Pressed (exec)] + [Released (exec)]
Header颜色: 🔴 红色
UE说明: 按键按下/释放时触发，可绑定任意按键
对应API: factory.CreateNode("KeyEvent", new string[]{}, new[]{"Pressed (exec)", "Released (exec)"})
```

### Level Loaded / Destroyed
```
节点名: LevelLoaded / Destroyed
输出端口: [Out (exec)]
Header颜色: 🔴 红色
UE说明: 关卡加载完成/对象销毁时触发
对应API: factory.CreateNode("Destroyed", new string[]{}, new[]{"Out (exec)"})
```

## 节点创建模板

```csharp
// 在本系统中创建事件节点的标准方式
var factory = new BlueprintNodeFactory(ComponentSystem.Instance);
var eventNode = factory.CreateNode(
    "EventBeginPlay",          // 节点名
    new string[] { },          // 事件节点无输入端口
    new[] { "Out" },           // 输出端口
    new Vector2(200, 300),     // 画布位置
    new Color("#E53935")       // 红色 Header
);
editorCanvas.ComponentLayer.AddChild(eventNode);
await ToSignal(GetTree(), "process_frame");
```

## 关键规则

1. 事件节点**只有输出端口**，不能有输入端口
2. 一个关卡蓝图通常以多个事件节点开头
3. 事件可**并行触发**（不同于顺序函数调用）
4. EventBeginPlay 是单次执行，EventTick 是持续执行的典型代表
