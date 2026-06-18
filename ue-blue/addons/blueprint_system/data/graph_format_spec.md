# 蓝图图交换格式规范 v1.0

## 概述

此格式用于导入/导出蓝图节点连接图到 .txt 文件。
基于 UE5 蓝图概念，每行一个条目，支持 # 注释。

## 语法

### 注释
以 `#` 开头的行会被忽略。
行内 `#` 后的内容也被视为注释（取决于解析器实现）。

### 节点定义
```
[Node] id=N type=TYPE name="NAME" pos=(X,Y) color=HEXCOLOR
  PIN:NAME|PORTTYPE
```

| 字段 | 说明 | 示例 |
|------|------|------|
| `id=N` | 节点唯一 ID (int) | `id=1` |
| `type=TYPE` | 节点类型标识 | `EventBeginPlay`, `Branch`, `PrintString` |
| `name="NAME"` | 显示名称（引号包裹） | `"Event BeginPlay"` |
| `pos=(X,Y)` | 画布上的X,Y坐标 | `(200,100)` |
| `color=HEXCOLOR` | Header 颜色 | `#E53935` |
| `PIN:NAME|PORTTYPE` | 缩进一行，定义端口 | `Exec\|exec`, `InString\|string` |

### 端口定义语法
```
  PORTDIRECTION:PORTNAME|PORTTYPE
```
- `PORTDIRECTION`: `IN` 或 `OUT`
- `PORTNAME`: 端口显示名称（不含管道符）
- `PORTTYPE`: 数据类型 (exec, int, float, string, bool, vector, object)

### 连接定义
```
[Connection] SRC_NODE_ID:SRC_PORTNAME → TGT_NODE_ID:TGT_PORTNAME
```
箭号 `→` 统一使用 `→` (U+2192) 或 `->` 均可。

### 图元数据
```
[Meta] key="value"
```
可选。用于存储图名称、描述、版本等。

## 完整示例 —— UE5.4 关卡蓝图：按E打印

```
# ============================================================
# UE5.4 关卡蓝图 - 按E键打印测试信息
# 功能: 按下键盘 E 键后，屏幕输出 "Hello from UE5.4 Level BP!"
# ============================================================

[Meta] graph_name="UE5.4 Level Blueprint - Press E to Print"
[Meta] graph_version="1.0"
[Meta] engine="UE5.4"
[Meta] graph_type="LevelBlueprint"
[Meta] description="Test level blueprint: Press E → PrintString"

# --- 节点 1: 键盘事件 E ---
[Node] id=1 type=KeyEvent name="InputAction E" pos=(200.0,200.0) color=#E53935
  OUT:Pressed|exec
  OUT:Released|exec

# --- 节点 2: 输出文本 ---
[Node] id=2 type=PrintString name="Print Hello" pos=(500.0,200.0) color=#FF9800
  IN:Exec|exec
  IN:InString|string
  IN:Duration|float
  IN:TextColor|color
  OUT:Out|exec

# --- 连接: Key Pressed → Print Exec ---
[Connection] 1:Pressed → 2:Exec
```

## 已知节点类型与端口

| type | 名称 | IN端口 | OUT端口 |
|------|------|--------|---------|
| EventBeginPlay | 游戏开始 | — | Out(exec) |
| EventTick | 每帧 | — | Out(exec), DeltaSeconds(float) |
| KeyEvent | 按键事件 | — | Pressed(exec), Released(exec) |
| BeginOverlap | 开始重叠 | — | Out(exec), OtherActor(object) |
| PrintString | 输出文本 | Exec(exec), InString(string) | Out(exec) |
| Branch | 条件分支 | Exec(exec), Condition(bool) | True(exec), False(exec) |
| Sequence | 序列 | Exec(exec) | Then 0(exec), Then 1(exec), ... |
| Delay | 延迟 | Exec(exec), Duration(float) | Completed(exec) |
| ForLoop | 循环 | Exec(exec), FirstIndex(int), LastIndex(int) | LoopBody(exec), Index(int), Completed(exec) |
| SpawnActor | 生成 | Exec(exec), Class, Transform(vector) | Out(exec), ReturnValue(object) |
| CustomEvent | 自定义事件 | — | Out(exec) |
| TestComponent | 测试节点 | In(exec) | Out(exec) |

## portType 颜色参考

| portType | UE引脚色 | 用途 |
|----------|---------|------|
| exec     | 白色    | 执行流 |
| int      | 绿色    | 整数 |
| float    | 绿色    | 浮点 |
| bool     | 红色    | 布尔 |
| string   | 紫色    | 字符串 |
| vector   | 金色    | 坐标/向量 |
| object   | 蓝色    | 对象引用 |
| color    | 白色    | 颜色 |
