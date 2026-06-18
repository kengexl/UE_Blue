# UE Blueprint Connection Tool — 脚本框架信息

> 生成日期: 2026-06-18
> 项目路径: `ue-blue/addons/blueprint_system/`

---

## 一、项目概览

一个基于 Godot 4.5 的蓝图可视化脚本编辑器系统。
使用 C# 实现，支持组件的拖拽、选中、端口连接、画布平移缩放。

### 核心特性

- 可视化节点编辑器，支持三种节点类型（输出专用/双向/输入专用）
- 端口到端口的连接系统，支持贝塞尔曲线绘制
- 交互状态机（空闲/拖拽/连接/平移）
- 事件总线架构，跨系统解耦通信
- 完整的单元测试和集成测试

---

## 二、目录结构

```
addons/blueprint_system/
├── autoloads/           # Autoload 单例（全局生命周期）
│   └── BlueprintEventBus.cs
├── core/                # 核心系统逻辑
│   ├── ComponentSystem.cs
│   ├── ConnectionManager.cs
│   └── InteractionSystem.cs
├── data/                # 数据模型（继承 Resource）
│   ├── ComponentData.cs
│   ├── ConnectionData.cs
│   ├── GraphData.cs
│   └── PortDefinition.cs
├── components/          # 组件实体（运行时）
│   ├── BlueprintComponent.cs
│   ├── TypeAComponent.cs
│   ├── TypeBComponent.cs
│   ├── TypeCComponent.cs
│   └── TestComponent.cs
├── ui/                  # UI 控件
│   ├── EditorCanvas.cs
│   ├── PortWidget.cs
│   ├── ConnectionLine.cs
│   └── GridBackground.cs
└── tests/               # 测试
	└── TestConnection.cs
```

---

## 三、脚本详细信息

### 3.1 autoloads/ — 自动加载单例

| 文件 | 继承 | 角色 | 关键 API |
|------|------|------|---------|
| `BlueprintEventBus.cs` | `Node` | 全局事件总线 | `Instance` 单例访问, 15 个信号定义 |

**信号列表**：
- 组件事件: `ComponentAdded`, `ComponentRemoved`, `ComponentMoved`, `ComponentSelected`, `ComponentDeselected`
- 连接事件: `ConnectionCreated`, `ConnectionRemoved`, `ConnectionSelected`
- 交互事件: `ContextMenuRequested`, `DragStarted`, `DragEnded`
- 图事件: `GraphChanged`, `GraphSaved`, `GraphLoaded`
- 编辑器状态: `BlueprintEditorStateChanged`

---

### 3.2 core/ — 核心系统

| 文件 | 继承 | 角色 | 关键 API |
|------|------|------|---------|
| `ComponentSystem.cs` | `Node` (Autoload) | 组件生命周期管理 | `RegisterComponent()`, `CreateComponent()`, `GetComponent()`, `GetAllComponents()` |
| `ConnectionManager.cs` | `Node` | 连接关系管理 | `ValidateAndConnect()`, `RemoveConnection()`, `GetDownstreamComponents()` |
| `InteractionSystem.cs` | `Node` | 交互状态机 | `HandleInput()`, 4 种交互模式 |

**ComponentSystem 职责**：
- 自增 ID 分配
- 工厂方法创建 3 种组件类型
- 组件注册/注销生命周期管理

**ConnectionManager 职责**：
- 自连接检查（同一组件输出→输入）
- 重复连接检查（相同端口对）
- 连接线（ConnectionLine）的创建和销毁
- BFS 下游组件遍历

**InteractionSystem 状态机**：
```
NONE → [左键点击组件标题] → DRAGGING_COMPONENT → [松开左键] → NONE
NONE → [左键点击输出端口] → CONNECTING_PORTS   → [点击输入端口] → NONE
NONE → [中键拖拽]         → PANNING_CANVAS     → [松开中键]     → NONE
```

---

### 3.3 data/ — 数据模型

| 文件 | 继承 | 角色 | 导出属性 |
|------|------|------|---------|
| `PortDefinition.cs` | `Resource` | 端口元数据 | `PortId`, `PortName`, `Direction`, `PortType`, `ConnectionLimit` |
| `ComponentData.cs` | `Resource` | 组件数据模型 | `ComponentId`, `ComponentName`, `Type`(枚举), `Position`, `Size`, `Ports` |
| `ConnectionData.cs` | `Resource` | 连接数据 | `ConnectionId`, `SourceComponentId`, `SourcePortId`, `TargetComponentId`, `TargetPortId` |
| `GraphData.cs` | `Resource` | 图顶层容器 | `GraphName`, `Components`(字典), `Connections`(数组), `Metadata` |

**组件类型枚举**：
- `OutputOnly` — 仅有输出端口（绿色 Header）
- `Dual` — 同时有输入和输出端口（蓝色 Header）
- `InputOnly` — 仅有输入端口（橙红 Header）

---

### 3.4 components/ — 运行时组件

| 文件 | 基类 | Header 颜色 | 端口 |
|------|------|-------------|------|
| `BlueprintComponent.cs` | `Node2D` | 蓝色(默认) | 基类，提供视觉树构建 |
| `TypeAComponent.cs` | `BlueprintComponent` | 绿色 #4CAF50 | 1 个输出 |
| `TypeBComponent.cs` | `BlueprintComponent` | 蓝色 #2196F3 | 1 输入 + 1 输出 |
| `TypeCComponent.cs` | `BlueprintComponent` | 橙红 #FF5722 | 1 个输入 |
| `TestComponent.cs` | `TypeBComponent` | 蓝色 | 1 输入 + 1 输出 + `TestLabel`/`TestValue` 导出属性 |

**BlueprintComponent 视觉层次**：
```
Panel (背景+边框)
├─ ColorRect (Header 条, 24px)
├─ Label (标题)
└─ VBoxContainer (端口列表)
	├─ PortWidget (输入端口, 居左)
	└─ PortWidget (输出端口, 居右)
```

---

### 3.5 ui/ — UI 控件

| 文件 | 继承 | 角色 | 关键方法 |
|------|------|------|---------|
| `EditorCanvas.cs` | `Control` | 编辑画布 | `ScreenToCanvas()`, `CanvasToScreen()`, `ShowContextMenuAt()`, `SetZoom()`, `PanTo()` |
| `PortWidget.cs` | `Control` | 端口控件 | `GetGlobalPortPosition()`, `SetHighlight()`, 5 个信号 |
| `ConnectionLine.cs` | `Node2D` | 贝塞尔连接线 | `UpdatePosition()`, `IntersectsPoint()`, `SetSelected()`, `SetHovered()` |
| `GridBackground.cs` | `ColorRect` | 网格背景 | `UpdateGrid()`, 主网格和次网格双图层绘制 |

**EditorCanvas 坐标系统**：
```
屏幕坐标 → ScreenToCanvas() → 画布坐标
画布坐标 → CanvasToScreen() → 屏幕坐标
公式: canvasPos = (screenPos - cameraOffset) / zoomLevel
```

**PortWidget 视觉规格**：

| 属性 | 输入端口 | 输出端口 |
|------|---------|---------|
| 形状 | 圆形 | 菱形 |
| 默认色 | #90CAF9 (浅蓝) | #FFD54F (琥珀) |
| 悬停色 | #64B5F6 | #FFCA28 |
| 已连接色 | #42A5F5 | #FFB300 |
| 大小 | 12×12 | 12×12 |
| Label 位置 | 图标右侧 | 图标左侧 |

**ConnectionLine 贝塞尔曲线**：
- 控制距离 = max(|start.x - end.x| × 0.5, 50)
- 32 段线性近似
- 支持碰撞检测（点到线段距离 < 8px 视为选中）

**GridBackground 网格规格**：
- 背景色: #1E1E1E
- 次网格: 20px 间距, #2A2A2A
- 主网格: 100px 间距, #3A3A3A
- 使用 fmod 实现无限滚动效果

---

### 3.6 tests/ — 测试

| 文件 | 继承 | 测试数量 | 说明 |
|------|------|---------|------|
| `TestConnection.cs` | `Node` | 9 组 | 组件创建、连接验证、序列化、集成测试 |

**测试用例清单**：
1. `TestComponentCreation` — 4 种组件类型的创建和端口布局
2. `TestConnectionCreation` — 输出→输入连接成功
3. `TestSelfConnectRejected` — 自连接被拒绝
4. `TestDuplicateConnectRejected` — 重复连接被拒绝
5. `TestOutputToOutputRejected` — 输出→输出由交互层检查
6. `TestComponentMovement` — 组件位置更新
7. `TestGraphDataSerialization` — GraphData CRUD
8. `TestEventBusSignals` — 信号发射和接收
9. `TestInteractionFlow` — 完整的链式连接 + 拖拽模拟 + BFS 遍历

---

## 四、场景文件

| 场景 | 脚本 | 说明 |
|------|------|------|
| `scenes/blueprint_editor.tscn` | `BlueprintEditor.cs` | 主编辑器场景 |
| `scenes/test_blueprint_editor.tscn` | `TestConnection.cs` | 测试场景 |

---

## 五、配置 (project.godot)

```ini
[autoload]
BlueprintEventBus="*res://addons/blueprint_system/autoloads/BlueprintEventBus.cs"
ComponentSystem="*res://addons/blueprint_system/core/ComponentSystem.cs"
```

---

## 六、架构数据流

```
用户输入 (鼠标/键盘)
	│
	▼
BlueprintEditor._Input()        ← 统一输入入口
	│
	▼
InteractionSystem.HandleInput() ← 状态机分发
	├── 左键组件 → 选中 + 拖拽
	├── 左键端口 → 连接操作
	├── 中键     → 画布平移
	├── 右键     → 上下文菜单
	└── Ctrl+滚轮 → 缩放
	│
	├──▶ ComponentSystem          ← 组件 CRUD
	│       └── BlueprintComponent  ← 运行时实体
	│
	├──▶ ConnectionManager        ← 连接 CRUD
	│       ├── ConnectionData     ← 连接数据
	│       └── ConnectionLine     ← 连接线绘制
	│
	├──▶ EditorCanvas             ← 画布控制
	│       ├── GridBackground     ← 网格背景
	│       └── 坐标转换
	│
	└──▶ BlueprintEventBus        ← 事件广播
			└── 跨系统通信
```

---

## 七、C# Godot 迁移经验

详见 `.trae/rules/project_rules.md` 中的「C# GDScript 迁移模式」章节，包含以下要点：

1. **不要使用 namespace** — Godot Autoload 按全局类名查找
2. **手动实现单例** — `static Instance` + `_EnterTree()`
3. **命名冲突处理** — `IsConnected` → `IsPortConnected`, `EditorStateChanged` → `BlueprintEditorStateChanged`
4. **StyleBoxFlat API** — 用 `SetBorderWidthAll()` / `SetCornerRadiusAll()`
5. **PopupMenu API** — 用 `AddSubmenuItem()`
6. **long→int 转换** — Godot 内部 int 是 64-bit
7. **场景脚本引用** — `.tscn` 中必须显式 `script = ExtResource("N")`
8. **C# 节点生命周期** — 必须 `AddChild` 后 `_Ready()` 才执行
9. **交互系统架构** — 通过 `_Input()` 路由而非 `_GuiInput()`
