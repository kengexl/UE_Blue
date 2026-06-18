---
alwaysApply: false
description: 
---
# UE_Blue 项目规则

## 项目信息
- **引擎**: Godot 4.5 (Forward Plus)
- **语言**: C# (.NET) + GDScript
- **项目名称**: Ue_blue

## GodotPrompter 技能框架

本项目集成了 GodotPrompter-1.10.0 技能框架。所有 Godot 领域技能位于 `GodotPrompter-1.10.0/skills/` 目录下。

### 核心规则

**RULE: 在实现任何 Godot 系统之前，必须先查阅对应的 GodotPrompter 技能文件。**

每个技能文件位于 `GodotPrompter-1.10.0/skills/<skill-name>/SKILL.md`，包含 Godot 最佳实践、完整代码示例和实现检查清单。

### 技能速查表

| 实现内容 | 必须查阅的技能 |
|---------|---------------|
| 玩家移动/角色控制器 | `player-controller` |
| 状态机/FSM | `state-machine` |
| 信号/EventBus | `event-bus` |
| 场景树结构 | `scene-organization` |
| UI/HUD | `godot-ui`, `hud-system` |
| 背包系统 | `inventory-system` |
| 存档/读档 | `save-load` |
| 敌人AI/导航 | `ai-navigation` |
| 摄像机 | `camera-system` |
| 音频 | `audio-system` |
| 武器/战斗 | `component-system` |
| 资源/数据 | `resource-pattern` |
| 输入处理 | `input-handling` |
| 动画 | `animation-system`, `tween-animation` |
| 测试 | `godot-testing` |
| 项目搭建 | `godot-project-setup` |
| 着色器/VFX | `shader-basics`, `particles-vfx` |
| 物理 | `physics-system` |
| 多人游戏 | `multiplayer-basics` |
| 导出/部署 | `export-pipeline` |
| 代码审查 | `godot-code-review` |
| 调试 | `godot-debugging` |
| C#开发 | `csharp-godot`, `csharp-signals` |
| GDScript模式 | `gdscript-patterns` |
| 依赖注入 | `dependency-injection` |
| 对话系统 | `dialogue-system` |
| 2D基础 | `2d-essentials` |
| 3D基础 | `3d-essentials` |
| 优化 | `godot-optimization` |
| 本地化 | `localization` |
| 数学 | `math-essentials` |
| 程序化生成 | `procedural-generation` |
| 响应式UI | `responsive-ui` |
| XR开发 | `xr-development` |
| 移动开发 | `mobile-development` |
| 多线程 | `multithreading` |
| 插件开发 | `addon-development` |
| Beehave行为树 | `beehave` |
| LimboAI状态机 | `limboai` |
| GDExtension | `gdextension` |
| 专用服务器 | `dedicated-server` |
| 多人同步 | `multiplayer-sync` |
| 资产生成管线 | `assets-pipeline` |
| 能力系统 | `ability-system` |
| 构思/设计 | `godot-brainstorming` |

### 完整技能路径

基础路径: `GodotPrompter-1.10.0/skills/`

可用的 51 个技能:
- `2d-essentials`, `3d-essentials`, `ability-system`, `addon-development`
- `ai-navigation`, `animation-system`, `assets-pipeline`, `audio-system`
- `beehave`, `camera-system`, `component-system`, `csharp-godot`
- `csharp-signals`, `dedicated-server`, `dependency-injection`, `dialogue-system`
- `event-bus`, `export-pipeline`, `gdextension`, `gdscript-advanced`
- `gdscript-patterns`, `godot-brainstorming`, `godot-code-review`, `godot-debugging`
- `godot-optimization`, `godot-project-setup`, `godot-testing`, `godot-ui`
- `hud-system`, `input-handling`, `inventory-system`, `limboai`
- `localization`, `math-essentials`, `mobile-development`, `multiplayer-basics`
- `multiplayer-sync`, `multithreading`, `particles-vfx`, `physics-system`
- `player-controller`, `procedural-generation`, `resource-pattern`, `responsive-ui`
- `save-load`, `scene-organization`, `shader-basics`, `state-machine`
- `tween-animation`, `using-godot-prompter`, `xr-development`

### 工作流

1. **设计阶段**: 查阅 `godot-brainstorming` 技能进行 Godot 特定的设计探索
2. **实现阶段**: 根据技能速查表查阅对应领域技能，获取最佳实践和代码示例
3. **审查阶段**: 使用 `godot-code-review` 技能审查代码质量

### 代码规范
- GDScript: snake_case 函数/变量, PascalCase 类名
- C#: PascalCase 方法, 匹配 Godot API 约定
- 目标版本: Godot 4.3+
- 不使用方法已弃用的 API

### C# GDScript 迁移模式（此项目经验总结）

#### 1. 命名空间规则
- **不要使用 `namespace`**：Godot 的 Autoload 系统和场景脚本解析按全局类名查找，带 namespace 的类会导致 `does not inherit from 'Node'` 错误
- 所有类必须在全局作用域声明（`public partial class Foo : Node`，无外层 namespace）

#### 2. Autoload 单例访问
```csharp
// ❌ 不可用 — SourceGenerator 可能不生成 .Singleton
BlueprintEventBus.Singleton

// ✅ 可靠 — 手动注册
public partial class MyAutoload : Node
{
    public static MyAutoload Instance { get; private set; }
    public override void _EnterTree() { Instance = this; }
    public override void _ExitTree() { if (Instance == this) Instance = null; }
}
// 使用: MyAutoload.Instance.EmitSignal(...)
```

#### 3. 属性/信号命名冲突
| 冲突名 | 继承来源 | 解决方案 |
|--------|---------|---------|
| `IsConnected` | `GodotObject.IsConnected(StringName, Callable)` | 重命名为 `IsPortConnected` |
| `EditorStateChanged` | `Node.EditorStateChanged` | 重命名为 `BlueprintEditorStateChanged` |

#### 4. StyleBoxFlat API 差异
```csharp
// ❌ GDScript 方式
bg_style.border_width = 1
bg_style.corner_radius = 4

// ❌ 错误 C# 方式
bgStyle.BorderWidth = 1;     // 不存在
bgStyle.CornerRadius = 4;    // 不存在

// ✅ 正确 C# 方式
bgStyle.SetBorderWidthAll(1);
bgStyle.SetCornerRadiusAll(4);
```

#### 5. PopupMenu 子菜单
```csharp
// ❌ 不存在
menu.SetSubmenuItem(index, "submenu_name");

// ✅ 正确
menu.AddSubmenuItem("菜单文本", "子菜单节点Name");
```

#### 6. long ↔ int 转换
Godot 内部 int 是 64-bit（C# `long`），但部分 API 参数是 `int`：
```csharp
// PopupMenu.IdPressed 的 id 是 long
menu.IdPressed += (long id) =>
{
    var idx = menu.GetItemIndex((int)id);  // 需要转型
    menu.SetItemChecked(idx, !menu.IsItemChecked(idx));
};
```

#### 7. 场景 (.tscn) 脚本引用
C# 脚本必须显式挂载到场景节点，否则 `GetNode<T>()` 转型失败：
```
[ext_resource type="Script" path="res://path/to/MyScript.cs" id="N"]

[node name="MyNode" type="Control" parent="."]
script = ExtResource("N")
```
对于 Autoload，直接在 `project.godot` 的 `[autoload]` 节注册 `.cs` 路径：
```
MyAutoload="*res://path/to/MyAutoload.cs"
```

#### 8. C# 节点生命周期与 _Ready()
C# 节点必须**加入场景树**后 `_Ready()` 才会执行：
```csharp
var comp = new MyComponent();
// comp._Ready() 尚未调用，子节点/端口等未构建
AddChild(comp);              // 加入场景树
// comp._Ready() 被延迟到下一帧执行
await ToSignal(GetTree(), "process_frame");
// 现在 comp 的子节点可用
```

如果仅创建 `new MyComponent()` 不 `AddChild`，`_Ready()` 永远不会执行。

#### 9. 交互系统架构（蓝图编辑器模式）
 蓝图编辑器的交互（选中、拖拽、连接、菜单）统一在 **`BlueprintEditor._Input()`** 中处理，通过 `InteractionSystem` 状态机分发：
 ```
 BlueprintEditor._Input(event)
   └─ InteractionSystem.HandleInput(event, mousePos)
        ├─ HandleMouseButton() — 左键选中/拖拽/连接，右键菜单，滚轮缩放
        ├─ HandleMouseMotion() — 拖拽更新，画布平移
        └─ 状态机: NONE → DRAGGING/CONNECTING/PANNING → NONE
 ```
 
 关键规则：
 - **不要在 Control._GuiInput 中处理主交互**：Control 在 Node2D 父级下的 `_GuiInput` 不可靠
 - **端口点击优先于组件点击**：端口是组件的子控件，点击端口时必须先检测端口，再检测组件。`HandleMouseButton` 中 `GetPortAt()` 必须在 `GetComponentAt()` 之前
 - **Control 没有 ToLocal()**：`ToLocal()` 是 `Node2D` 的方法。Control 用 `GetGlobalTransformWithCanvas().AffineInverse() * globalPos` 转为本地坐标
 - **使用 GetLocalMousePosition() 统一坐标空间**：Node._Input 中 `mb.Position` 是 Viewport 坐标，Control 作为 Node2D 子节点时需通过 `control.GetLocalMousePosition()` 获取 Control 本地坐标
 - **组件坐标使用画布空间**：`EditorCanvas.ScreenToCanvas()` 将 EditorCanvas 本地坐标转换为组件层（画布）坐标
 - **连接线坐标使用 ConnectionLayer 本地空间**：通过 `Node2D.ToLocal(globalPos)` 转换

 #### 10. 端口拖拽连线流程
 完整连线流程如下：
 1. 用户左键点击一个 **输出端口**（菱形，琥珀色）
 2. `InteractionSystem` 进入 `CONNECTING_PORTS` 模式，源端口高亮
 3. 在 `ConnectionLayer` 上创建临时 `Line2D`（灰色线段）
 4. 鼠标移动时，`UpdateTempLine()` 将鼠标位置转为 ConnectionLayer 本地坐标更新线段终点
 5. 用户左键点击一个 **输入端口**（圆形，蓝色）
 6. `TryCompleteConnection()` 调用 `ConnectionManager.ValidateAndConnect()`
 7. 验证通过 → 创建正式 `ConnectionLine`（贝塞尔曲线），销毁临时线
 8. 验证失败 → 提示日志，销毁临时线
 9. 任何时刻右键/ESC/释放左键 → `CancelConnection()` 清理临时线

 注意：`PortWidget` 的 `_GuiInput` 在 Node2D 父链下**不会触发**，
 所有端口检测通过 `InteractionSystem.GetPortAt()` 实现。
 临时连线的坐标转换链：
 ```
 mousePos (EditorCanvas 本地坐标)
   → EditorCanvas.GetGlobalTransformWithCanvas() * mousePos → 全局坐标
   → connectionLayer.ToLocal(globalPos) → ConnectionLayer 本地坐标
 ```

#### 11. 可视化调试叠加层 (BlueprintDebugOverlay)

 用于调试交互状态的可视化叠加面板，通过 `CanvasLayer` 渲染在编辑器顶层：

 ```
 BlueprintEditor
  ├─ CanvasLayer (CanvasLayer)
  │    └─ BlueprintDebugOverlay (Control) ← 本面板
  ...
 ```

 **功能**：
 | 行 | 内容 | 颜色规则 |
 |----|------|---------|
 | 模式 | 当前 InteractionMode | 空闲=灰色, 拖拽=黄色, 连接=绿色 |
 | 端口 | 被高亮/已连接的端口列表 | 已交互=绿色, 无交互=灰色 |
 | 拖拽 | 起始位置 + 当前鼠标 + 偏移 | 蓝色 |
 | 曲线 | 连接线状态描述 | 连接中=绿色, 已完成=蓝色, 空闲=灰色 |

 **关键规则**：
 - `InteractionSystem` 中暴露 `PublicDragMouseStart`, `PublicDragComponentStart`, `PublicDragComponent`, `PublicSourcePort`, `PublicSourceComponent` 供调试面板只读访问
 - 调试面板每帧通过 `_Process()` 轮询更新，仅当文本内容变化时触发 `QueueRedraw()`（通过哈希比较）
 - 使用 `MouseFilter = Ignore` 不拦截鼠标事件

 #### 12. 调试排错经验（此项目实践总结）

 以下是本项目中遇到并解决的典型 Godot C# 问题，供后续开发参考。

 #### 12.1 Control 作为 Node2D 子节点时 Size=(0,0)

 **现象**：`EditorCanvas`（继承 Control）作为 `BlueprintEditor`（继承 Node2D）的子节点时，
 `EditorCanvas.Size=(0,0)`，导致 `GetLocalMousePosition()` 返回值正常，但坐标变换完全错误。

 **根因**：Control 的锚点布局系统在 Node2D 父级下**不生效**。`SetAnchorsAndOffsetsPreset(FullRect)` 不会自动设置 Size。

 **修复**：在 `_Ready()` 中手动同步视口尺寸：
 ```csharp
 public override void _Ready()
 {
     Size = GetViewportRect().Size;          // 手动设置大小
     Position = Vector2.Zero;                // 归零位置
     MouseFilter = Control.MouseFilterEnum.Ignore;
 }

 // 监听窗口大小变化
 public override void _Notification(int what)
 {
     if (what == Node.NotificationWMSizeChanged)
         Size = GetViewportRect().Size;
 }
 ```

 #### 12.2 GetGlobalTransformWithCanvas() 在 Node2D 子 Control 上失效

 **现象**：`EditorCanvas.GetGlobalTransformWithCanvas().AffineInverse() * globalPos` 返回完全错误的坐标。
 日志显示端口全局坐标 (-116,-219) 被变换到 (-1076,-759)，偏离近 1000px。

 **根因**：`GetGlobalTransformWithCanvas()` 在 Control 作为 Node2D 子节点时，由于 Node2D 没有 Size 概念，
 且 Control 锚点未生效，其变换矩阵不正确。

 **修复**：避免使用此 API。改用 `ComponentLayer.ToLocal(globalPos)`（Node2D 的 ToLocal 可靠）：
 ```csharp
 // ❌ 错误 — Control 在 Node2D 下变换不正确
 var portLocal = EditorCanvas.GetGlobalTransformWithCanvas().AffineInverse() * portGlobal;

 // ✅ 正确 — 统一到画布空间（ComponentLayer 本地坐标）比较
 var mouseCanvas = EditorCanvas.ScreenToCanvas(mouseLocalPos);  // 鼠标 → 画布空间
 var portCanvas  = EditorCanvas.ComponentLayer.ToLocal(portGlobal); // 端口 → 画布空间
 float dist = mouseCanvas.DistanceTo(portCanvas);
 ```

 #### 12.3 _Draw() 中不能调用 QueueRedraw()

 **现象**：`ConnectionLine` 的 `_Draw()` 中调用了 `UpdatePosition()`（末尾含 `QueueRedraw()`），
 导致曲线永远不会被绘制。

 **根因**：Godot 在 `_Draw()` 上下文中调用 `QueueRedraw()` 会被**静默忽略**（防止无限循环重绘）。

 **修复**：`_Draw()` 和 `UpdatePosition()` 彻底分离：
 - `UpdatePosition()`：由外部代码调用，重建曲线点 + `QueueRedraw()`
 - `_Draw()`：仅使用已有的 `_curvePoints` 数组绘制，绝不触发更新

 #### 12.4 端口点击检测优先级

 **现象**：点击端口时触发组件选中而非端口连接。

 **根因**：`HandleMouseButton()` 中组件检测（`GetComponentAt`）在端口检测（`GetPortAt`）之前，
 且端口在组件的矩形区域内，组件先"截获"了点击。

 **修复**：交换检测顺序：先 `GetPortAt()`，后 `GetComponentAt()`。

 #### 12.5 端口坐标比较必须在同一空间

 **现象**：诊断日志显示端口距离鼠标 > 1000px，实际视觉上鼠标就在端口上。

 **根因**：鼠标坐标来自 `EditorCanvas.GetLocalMousePosition()`（EditorCanvas 本地空间），
 端口坐标经过错误变换后在不同空间。

 **修复**：统一到**画布空间（canvas space）** 比较：
 - 鼠标: `EditorCanvas.ScreenToCanvas(localPos)`
 - 端口: `ComponentLayer.ToLocal(portGlobal)`
 - 两者都是画布空间中的坐标

 #### 12.6 Godot 4 C# 窗口大小变化通知

 ```csharp
 // ❌ 不存在
 NotificationWMWindowResized

 // ✅ 正确
 Node.NotificationWMSizeChanged    // 值 = 1008
 ```

 #### 12.7 坐标诊断日志输出格式

 调试端口点击问题时，`LogPortDiagnostics()` 输出格式示例：
 ```
 [端口诊断] ====================
 鼠标(EditorCanvas本地)=(-122.0,-220.0)
 鼠标(画布空间)=(-122.0,-220.0)
 组件名称="Test Node" ID=1
 组件位置=(-131.0,-255.0)
 组件尺寸=(160.0,80.0)
 组件区域=[-131.0,-255.0]-[29.0,-175.0]
   端口[0] "In" | 方向=Input | 画布=(-116.0,-219.0) | 距鼠标=6.1px | ✅ 应命中
   端口[1] "Out" | 方向=Output | 画布=(10.0,-219.0) | 距鼠标=132.0px | ❌ 距离过远
 EditorCanvas.Size=(1920.0,1080.0)
 EditorCanvas.CameraOffset=(0.0,0.0)
 EditorCanvas.ZoomLevel=1.00
 ComponentLayer.Position=(0.0,0.0)
 [端口诊断] ====================
  ```

  #### 12.8 Control 坐标 vs Node2D 坐标空间冲突（连接线偏右下）

  **现象**：端口连接功能正常，端口可被点击检测到，但生成的连接线（贝塞尔曲线）偏向右下。
  临时连线（Line2D）和正式连接线（ConnectionLine）均有此偏移。

  **根因**：`PortWidget.GetGlobalPortPosition()` 返回的是 **Control 全局坐标**（`Control.GlobalPosition + Size/2`），
  而 `ConnectionLayer.ToLocal(globalPos)` 期望的是 **Node2D 全局坐标空间**。

  Godot 4 中，当 Control 是 Node2D 的子节点时，两者的全局坐标空间**并不完全一致**：
  - `Control.GlobalPosition`：包含父 Node2D 的变换，但也包含 Control 的锚点/边距/布局系统偏移
  - `Node2D.GlobalPosition`：只包含父 Node2D 的变换链

  直接调用 `connectionLayer.ToLocal(portGlobal)` 将 Control 全局坐标传给 Node2D 的空间变换方法，
  会导致计算结果偏移。

  **正确解法（两步转换法）**：
  ```csharp
  // ❌ 错误：混用 Control 全局坐标和 Node2D.ToLocal
  var start = connectionLayer.ToLocal(portWidget.GetGlobalPortPosition());

  // ✅ 正确：通过 BlueprintComponent (Node2D) 中转
  // 第1步：用端口所在 BlueprintComponent (Node2D) 的 ToLocal 转为组件本地坐标
  var srcComp = SourcePort.ParentComponent;
  var portLocal = srcComp.ToLocal(SourcePort.GetGlobalPortPosition());
  // 第2步：组件本地坐标 + 组件的画布位置 = 画布空间坐标
  //        由于 ConnectionLayer == ComponentLayer 变换相同，画布空间 = ConnectionLayer 本地
  var start = srcComp.Position + portLocal;
  ```

  **关键原理**：
  ```
  PortWidget (Control) 的全局坐标
    → BlueprintComponent (Node2D).ToLocal()
      → 组件 Node2D 本地坐标（去掉 BlueprintComponent 的父级变换）
    → + BlueprintComponent.Position（组件在画布中的位置）
      → 画布空间坐标 = ComponentLayer 本地坐标
    → ConnectionLayer 与 ComponentLayer 变换相同
      → ConnectionLayer 本地坐标 ← 最终结果
  ```

  **适用场景**：任何需要在 `Control`（端口）和 `Node2D`（连接层）之间转换坐标的情况。
  核心原则：**不要混用 Control.GlobalPosition 和 Node2D.ToLocal**，
  永远通过一个共享的 Node2D 祖先中转。

  #### 12.9 蓝图图文件格式规范 (graph_format_spec.md)

  **规范文件位置**: `addons/blueprint_system/data/graph_format_spec.md`

  蓝图图以 `.txt` 文件存储，格式如下：
  ```
  # 注释行
  [Meta] key="value"                        # 元数据
  [Node] id=N type=TYPE name="NAME" pos=(X,Y) color=HEX
    IN:PortName|portType                    # 输入端口
    OUT:PortName|portType                   # 输出端口
  [Connection] SRC_NODE:SRC_PORT → TGT_NODE:TGT_PORT
  ```

  关键规则:
  - 每个节点以 `[Node]` 行定义，后续缩进行定义端口
  - 连接格式: `SRC_NODE_ID:PORT_NAME → TGT_NODE_ID:PORT_NAME`
  - 端口类型(portType): exec, int, float, string, bool, vector, object, color
  - 箭号支持 `→` (U+2192) 或 `->`

  #### 12.10 导入/导出系统 (GraphExporter/GraphImporter)

  | 文件 | 功能 |
  |------|------|
  | `core/GraphExporter.cs` | 导出: 编辑器图 → .txt 文件 |
  | `core/GraphImporter.cs` | 导入: .txt 文件 → 编辑器图 |
  | `data/graph_format_spec.md` | 格式规范文档 |
   | `blueprints_test/` | 测试蓝图文件目录 |
   | **`根目录/AI生成可导入蓝图的规则.txt`** | **AI 生成导入文件的规则（含节点速查表+提示词）** |

  使用流程:
  ```
  【导出】右键画布 → "导出图..." → FileDialog → 生成 .txt
  【导入】右键画布 → "导入图..." → FileDialog → 选择 .txt → 创建节点+连接
  ```

  导入注意事项:
  1. 节点 AddChild 后需 `await ToSignal(GetTree(), "process_frame")` 等待 _Ready
  2. 连接创建在节点就绪后才能执行 (`CompleteConnections()`)
  3. 端口名称匹配: 文件中的 portName 必须与创建的 PortWidget.PortName 一致

  #### 13. 信号声明与发射
```csharp
// 声明
[Signal]
public delegate void MySignalEventHandler(int arg1, string arg2);

// 发射
EmitSignal(SignalName.MySignal, 42, "hello");

// 连接（C# 事件风格）
instance.MySignal += (arg1, arg2) => { ... };

// 或 Callable 方式
var callable = Callable.From((int id) => { ... });
instance.Connect(SignalName.MySignal, callable);
```
